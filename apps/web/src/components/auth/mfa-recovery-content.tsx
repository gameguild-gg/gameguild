"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { recoverFromStaleServerAction } from "@/lib/auth/server-action-errors";
import {
  cancelMfaRecoveryAction,
  continueMfaRecoveryAction,
  requestMfaRecoveryAction,
} from "@/lib/auth/mfa-recovery-actions";
import { Button } from "@game-guild/ui/components/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import { Field, FieldGroup, FieldLabel } from "@game-guild/ui/components/field";
import { Input } from "@game-guild/ui/components/input";

/**
 * Where a visitor is in the lost-second-factor journey (issue #1298 J2, part C). The
 * server decides the first stage from the emailed link; this component only moves forward.
 */
export type MfaRecoveryStage =
  | { kind: "request" }
  | { kind: "requested" }
  /** The request token is valid but its cooling-off window is still open. */
  | { kind: "cooling-off"; requestToken: string; retryAfterSeconds: number }
  /** The API could not be reached, or its answer could not be read: try again, the token is still good. */
  | { kind: "continue-failed"; requestToken: string }
  | { kind: "confirmation-sent" }
  | { kind: "confirmed" }
  | { kind: "confirm-failed"; confirmationToken: string }
  /** One generic denial: unknown, expired, consumed and cancelled all read the same. */
  | { kind: "denied" };

type Notice = { tone: "error" | "success"; text: string } | null;

/** The cooling-off countdown as `hh:mm:ss`, with the days in front only when they matter. */
export function formatRemaining(seconds: number): string {
  const total = Math.max(0, Math.floor(seconds));
  const days = Math.floor(total / 86_400);
  const clock = [Math.floor((total % 86_400) / 3_600), Math.floor((total % 3_600) / 60), total % 60]
    .map((part) => String(part).padStart(2, "0"))
    .join(":");
  return days > 0 ? `${days}d ${clock}` : clock;
}

const CARD_CLASS = "border-white/10 bg-slate-900/85 text-white shadow-2xl shadow-sky-950/30 backdrop-blur";
const LINK_CLASS = "text-sky-200 underline-offset-4 hover:underline";

export function MfaRecoveryContent({
  stage,
  signedIn,
}: {
  stage: MfaRecoveryStage;
  /** The API's cancel is authenticated: only a signed-in owner is offered it. */
  signedIn: boolean;
}) {
  const t = useTranslations("mfaRecovery");
  const router = useRouter();
  const headingRef = useRef<HTMLHeadingElement>(null);
  const [view, setView] = useState<MfaRecoveryStage>(stage);
  const [email, setEmail] = useState("");
  const [notice, setNotice] = useState<Notice>(null);
  const [busy, setBusy] = useState<"request" | "continue" | "cancel" | null>(null);

  // A transient failure never borrows the denial's words: the token is still good.
  const copy: Record<MfaRecoveryStage["kind"], { title: Parameters<typeof t>[0]; description: Parameters<typeof t>[0] }> = {
    request: { title: "title", description: "requestDescription" },
    requested: { title: "requestedTitle", description: "requestedDescription" },
    "cooling-off": { title: "coolingOffTitle", description: "coolingOffDescription" },
    "continue-failed": { title: "coolingOffTitle", description: "continueFailed" },
    "confirmation-sent": { title: "confirmationSentTitle", description: "confirmationSentDescription" },
    confirmed: { title: "confirmedTitle", description: "confirmedDescription" },
    "confirm-failed": { title: "title", description: "confirmFailed" },
    denied: { title: "deniedTitle", description: "deniedDescription" },
  };

  const coolingOff = view.kind === "cooling-off" ? view : null;
  const [remaining, setRemaining] = useState(0);

  // Seeded from the API's own number of seconds and measured against a deadline, so a
  // throttled tab never shows a shorter wait than the API will enforce.
  useEffect(() => {
    if (!coolingOff) {
      return;
    }
    const deadline = Date.now() + coolingOff.retryAfterSeconds * 1000;
    const tick = () => setRemaining(Math.max(0, Math.ceil((deadline - Date.now()) / 1000)));
    tick();
    const timer = window.setInterval(tick, 1000);
    return () => window.clearInterval(timer);
  }, [coolingOff]);

  // A stage change is a new page for a screen reader: move focus to its heading.
  useEffect(() => {
    headingRef.current?.focus();
  }, [view.kind]);

  async function handleRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const address = email.trim();
    if (busy || !address) {
      return;
    }
    setBusy("request");
    setNotice(null);
    try {
      const result = await requestMfaRecoveryAction(address);
      if (result.status === "sent") {
        // The API answers every address the same way, and so does this page.
        setView({ kind: "requested" });
      } else {
        setNotice({ tone: "error", text: t("requestFailed") });
      }
    } catch (error) {
      if (recoverFromStaleServerAction(error)) return;
      setNotice({ tone: "error", text: t("requestFailed") });
    } finally {
      setBusy(null);
    }
  }

  async function handleContinue() {
    const requestToken =
      view.kind === "cooling-off" || view.kind === "continue-failed" ? view.requestToken : "";
    if (busy || !requestToken) {
      return;
    }
    setBusy("continue");
    setNotice(null);
    try {
      const result = await continueMfaRecoveryAction(requestToken);
      if (result.status === "sent") {
        setView({ kind: "confirmation-sent" });
        // The request token did its job: land on an address that no longer carries it.
        router.replace("/auth/mfa-recovery?sent=1");
      } else if (result.status === "cooling-off") {
        setView({ kind: "cooling-off", requestToken, retryAfterSeconds: result.retryAfterSeconds });
      } else if (result.status === "denied") {
        setView({ kind: "denied" });
      } else {
        setView({ kind: "continue-failed", requestToken });
      }
    } catch (error) {
      if (recoverFromStaleServerAction(error)) return;
      setView({ kind: "continue-failed", requestToken });
    } finally {
      setBusy(null);
    }
  }

  async function handleCancel() {
    if (busy) {
      return;
    }
    setBusy("cancel");
    setNotice(null);
    try {
      const result = await cancelMfaRecoveryAction();
      if (result.status === "cancelled") {
        setNotice({ tone: "success", text: t("cancelled") });
      } else if (result.status === "denied") {
        setNotice({ tone: "error", text: t("nothingToCancel") });
      } else {
        setNotice({ tone: "error", text: t("cancelFailed") });
      }
    } catch (error) {
      if (recoverFromStaleServerAction(error)) return;
      setNotice({ tone: "error", text: t("cancelFailed") });
    } finally {
      setBusy(null);
    }
  }

  const waiting = view.kind === "cooling-off" && remaining > 0;

  return (
    <Card className={CARD_CLASS} data-testid="mfa-recovery" data-stage={view.kind}>
      <CardHeader className="text-center">
        <CardTitle className="text-2xl">
          <h1 ref={headingRef} tabIndex={-1}>
            {t(copy[view.kind].title)}
          </h1>
        </CardTitle>
        <CardDescription className="text-slate-300">{t(copy[view.kind].description)}</CardDescription>
      </CardHeader>
      <CardContent>
        {notice && (
          <p
            role={notice.tone === "error" ? "alert" : "status"}
            data-testid={`mfa-recovery-${notice.tone}`}
            className="mb-4 text-sm text-slate-200"
          >
            {notice.text}
          </p>
        )}

        {view.kind === "request" && (
          <form onSubmit={(event) => void handleRequest(event)}>
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="mfa-recovery-email">{t("emailLabel")}</FieldLabel>
                <Input
                  id="mfa-recovery-email"
                  name="email"
                  type="email"
                  autoComplete="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  required
                  disabled={busy !== null}
                  className="border-white/10 bg-white/5 text-white"
                />
              </Field>
              <Button type="submit" disabled={busy !== null || email.trim().length === 0} data-testid="mfa-recovery-submit">
                {busy === "request" ? t("sending") : t("submit")}
              </Button>
            </FieldGroup>
          </form>
        )}

        {(view.kind === "cooling-off" || view.kind === "continue-failed") && (
          <div className="space-y-3">
            {view.kind === "cooling-off" && (
              <p role="status" className="text-center font-mono text-lg text-slate-100" data-testid="mfa-recovery-countdown">
                {formatRemaining(remaining)}
              </p>
            )}
            <Button
              type="button"
              onClick={() => void handleContinue()}
              disabled={busy !== null || waiting}
              data-testid="mfa-recovery-continue"
            >
              {busy === "continue" ? t("continuing") : t("continueLabel")}
            </Button>
          </div>
        )}

        {view.kind === "confirm-failed" && (
          // The confirmation token is still in the address: reloading runs it again.
          <Button type="button" onClick={() => window.location.reload()} data-testid="mfa-recovery-retry">
            {t("retry")}
          </Button>
        )}

        {view.kind === "confirmed" && (
          <Button type="button" onClick={() => router.push("/sign-in")} data-testid="mfa-recovery-sign-in">
            {t("signIn")}
          </Button>
        )}

        <div className="mt-4 flex flex-col gap-2 text-center text-sm">
          {view.kind === "denied" && (
            <Link href="/auth/mfa-recovery" className={LINK_CLASS} data-testid="mfa-recovery-restart">
              {t("restart")}
            </Link>
          )}
          {view.kind !== "confirmed" && (
            <Link href="/sign-in" className={`text-slate-400 ${LINK_CLASS}`} data-testid="mfa-recovery-back">
              {t("backToSignIn")}
            </Link>
          )}
        </div>

        {signedIn && (
          <div className="mt-6 space-y-2 rounded-lg border border-white/10 p-3 text-left" data-testid="mfa-recovery-cancel">
            <p className="text-sm font-medium">{t("cancelTitle")}</p>
            <p className="text-slate-300 text-sm">{t("cancelDescription")}</p>
            <Button
              type="button"
              variant="outline"
              onClick={() => void handleCancel()}
              disabled={busy !== null}
              data-testid="mfa-recovery-cancel-button"
            >
              {t("cancel")}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
