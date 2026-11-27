"use client";

import { useEffect, useState, useSyncExternalStore, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { useAuth } from "@game-guild/client/react";
import { recoverFromStaleServerAction } from "@/lib/auth/server-action-errors";
import {
  MAX_VERIFICATION_TOKEN_LENGTH,
  VERIFICATION_RESEND_COOLDOWN_MS,
} from "@/lib/auth/email-verification";
import { markSessionEmailVerified } from "@/lib/auth/email-verification-session";
import {
  resendVerificationEmailAction,
  verifyEmailAction,
} from "@/lib/auth/password-actions";
import { Button } from "@game-guild/ui/components/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@game-guild/ui/components/field";
import { Input } from "@game-guild/ui/components/input";

const subscribeToHydration = () => () => undefined;

export type VerifyPageState =
  /** Waiting for the visitor to open the emailed link, or paste its token. */
  | { kind: "pending"; email: string; fresh: boolean }
  /** The token was consumed and the address is verified. */
  | { kind: "verified"; email: string }
  /** The token was invalid, expired or already used. */
  | { kind: "failed"; email: string };

type Notice = { tone: "error" | "success"; text: string } | null;

const CARD_CLASS = "border-white/10 bg-slate-900/85 text-white shadow-2xl shadow-sky-950/30 backdrop-blur";

/**
 * Email-verification page (#1298 J1).
 *
 * The API's artifact is a 32-character token inside a deep link; the page is
 * reached three ways. With `?token=` the server already consumed it before
 * rendering, so this component starts on the outcome. With `?email=` it is the
 * "check your inbox" interstitial: a paste-token fallback for clients that
 * strip links, and a resend button that mirrors the API's two-minute resend
 * window. With neither, the server bounces to /sign-in.
 */
export function VerifyPageContent({
  state,
  redirectTo,
  signedIn,
}: {
  state: VerifyPageState;
  /** Where "Continue" and "Skip for now" go; already allow-listed by the caller. */
  redirectTo: string;
  signedIn: boolean;
}) {
  const t = useTranslations("emailVerification");
  const { signOut } = useAuth();
  const isHydrated = useSyncExternalStore(
    subscribeToHydration,
    () => true,
    () => false,
  );
  const [view, setView] = useState<VerifyPageState>(state);
  const [notice, setNotice] = useState<Notice>(null);
  const [busy, setBusy] = useState<"verify" | "resend" | null>(null);
  // Sign-up dispatches the email itself, so a fresh account starts inside the
  // resend window. The number on screen is only rendered once hydrated, so
  // the server and the client never disagree about it.
  const [cooldownUntil, setCooldownUntil] = useState<number | null>(() =>
    state.kind === "pending" && state.fresh ? Date.now() + VERIFICATION_RESEND_COOLDOWN_MS : null,
  );
  const [now, setNow] = useState(() => Date.now());
  const counting = cooldownUntil !== null && cooldownUntil > now;
  const secondsLeft =
    cooldownUntil === null ? 0 : Math.max(0, Math.ceil((cooldownUntil - now) / 1000));

  useEffect(() => {
    if (!counting) return;
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [counting]);

  useEffect(() => {
    if (view.kind !== "verified") return;
    // Tell the signed-in session about the new verdict so the reminder banner
    // goes away without a new sign-in. A pasted token never reaches the page
    // URL, and the server lands a verified link on a token-free address, so
    // there is nothing to strip from the address bar here.
    void markSessionEmailVerified().catch(() => undefined);
  }, [view]);

  async function handleVerify(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy) return;
    const token = (new FormData(event.currentTarget).get("token") as string).trim();
    if (!token) return;

    setBusy("verify");
    setNotice(null);
    try {
      const result = await verifyEmailAction(token);
      if (!result.success) {
        setNotice({ tone: "error", text: t("tokenRejected") });
        return;
      }
      setView({ kind: "verified", email: view.email });
    } catch (error) {
      if (recoverFromStaleServerAction(error)) return;
      setNotice({ tone: "error", text: t("tokenRejected") });
    } finally {
      setBusy(null);
    }
  }

  async function handleResend(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy || counting) return;
    const email =
      view.kind === "failed"
        ? (new FormData(event.currentTarget).get("email") as string).trim()
        : view.email;
    if (!email) return;

    setBusy("resend");
    setNotice(null);
    try {
      const result = await resendVerificationEmailAction(email);
      if (!result.success) {
        setNotice({ tone: "error", text: t("resendFailed") });
        return;
      }
      setCooldownUntil(Date.now() + VERIFICATION_RESEND_COOLDOWN_MS);
      setNotice({ tone: "success", text: t("resendSent") });
      if (view.kind === "failed") setView({ kind: "pending", email, fresh: false });
    } catch (error) {
      if (recoverFromStaleServerAction(error)) return;
      setNotice({ tone: "error", text: t("resendFailed") });
    } finally {
      setBusy(null);
    }
  }

  function continueTo() {
    window.location.href = redirectTo;
  }

  const verifying = busy === "verify";
  const resending = busy === "resend";

  if (view.kind === "verified") {
    return (
      <Card role="status" data-testid="email-verification-verified" className={CARD_CLASS}>
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            <h1>{t("verifiedTitle")}</h1>
          </CardTitle>
          <CardDescription className="text-slate-300">{t("verifiedDescription")}</CardDescription>
        </CardHeader>
        <CardContent>
          <Button type="button" onClick={continueTo} data-testid="email-verification-continue">
            {t("continue")}
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={CARD_CLASS}>
      <CardHeader className="text-center">
        <CardTitle className="text-2xl">
          <h1>{view.kind === "failed" ? t("failedTitle") : t("title")}</h1>
        </CardTitle>
        <CardDescription className="text-slate-300">
          {view.kind === "failed"
            ? t("failedDescription")
            : t(view.fresh ? "descriptionNew" : "description", { email: view.email })}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <FieldGroup>
          {notice && (
            <p
              role={notice.tone === "error" ? "alert" : "status"}
              data-testid={`email-verification-${notice.tone}`}
              className="text-sm text-slate-200"
            >
              {notice.text}
            </p>
          )}

          {view.kind === "failed" ? (
            <form onSubmit={(event) => void handleResend(event)}>
              <FieldGroup>
                <Field>
                  <FieldLabel htmlFor="email-verification-email">{t("emailLabel")}</FieldLabel>
                  <Input
                    id="email-verification-email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    defaultValue={view.email}
                    required
                    disabled={resending}
                    className="border-white/10 bg-white/5 text-white"
                  />
                </Field>
                <Button type="submit" disabled={resending} data-testid="email-verification-request">
                  {resending ? t("sending") : t("requestNew")}
                </Button>
              </FieldGroup>
            </form>
          ) : (
            <>
              <form onSubmit={(event) => void handleVerify(event)}>
                <FieldGroup>
                  <Field>
                    <FieldLabel htmlFor="email-verification-token">{t("tokenLabel")}</FieldLabel>
                    <Input
                      id="email-verification-token"
                      name="token"
                      type="text"
                      autoComplete="off"
                      maxLength={MAX_VERIFICATION_TOKEN_LENGTH}
                      required
                      disabled={verifying}
                      aria-invalid={notice?.tone === "error" || undefined}
                      className="border-white/10 bg-white/5 font-mono text-white"
                    />
                    <FieldDescription className="text-slate-400">{t("tokenDescription")}</FieldDescription>
                  </Field>
                  <Button type="submit" disabled={verifying} data-testid="email-verification-submit">
                    {verifying ? t("verifying") : t("verify")}
                  </Button>
                </FieldGroup>
              </form>

              <form onSubmit={(event) => void handleResend(event)}>
                <Button
                  type="submit"
                  variant="outline"
                  disabled={resending || verifying || counting}
                  className="w-full border-white/10 text-slate-200 hover:bg-white/10 hover:text-white"
                  data-testid="email-verification-resend"
                >
                  {resending
                    ? t("sending")
                    : isHydrated && counting
                      ? t("resendCountdown", { seconds: secondsLeft })
                      : t("resend")}
                </Button>
              </form>

              <p className="text-xs text-slate-400">{t("passwordRecoveryNote")}</p>

              <div className="flex flex-col gap-2 text-center text-sm">
                {view.fresh && (
                  <Button
                    type="button"
                    variant="ghost"
                    className="text-sky-200 hover:bg-white/10 hover:text-sky-100"
                    onClick={continueTo}
                    data-testid="email-verification-skip"
                  >
                    {t("skip")}
                  </Button>
                )}
                {signedIn && (
                  <Button
                    type="button"
                    variant="link"
                    className="text-slate-400 underline-offset-4 hover:text-white"
                    onClick={() => {
                      void signOut({ redirectTo: "/sign-up" });
                    }}
                    data-testid="email-verification-sign-out"
                  >
                    {t("wrongEmail")}
                  </Button>
                )}
              </div>
            </>
          )}
        </FieldGroup>
      </CardContent>
    </Card>
  );
}
