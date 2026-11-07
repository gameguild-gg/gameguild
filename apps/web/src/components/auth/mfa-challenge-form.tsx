"use client";

import { type FormEvent, useEffect, useRef, useState, useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { QRCodeSVG } from "qrcode.react";
import { Check, Copy } from "lucide-react";
import { useAuth } from "@game-guild/client/react";
import { recoverFromStaleServerAction } from "@/lib/auth/server-action-errors";
import { completeMfaEnrollmentAction, sendMfaSmsChallengeAction } from "@/lib/auth/mfa-sign-in-actions";
import { Link } from "@/i18n/navigation";
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
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@game-guild/ui/components/field";
import { Input } from "@game-guild/ui/components/input";

const subscribeToHydration = () => () => undefined;
const methodNames = ["Totp", "BackupCode", "Sms"] as const;
type SignInMfaMethod = (typeof methodNames)[number];

/**
 * MFA challenge form for the dedicated /sign-in/2fa route. The challenge
 * (mfaToken, availableMethods, optional mfaEnrollment) is persisted by useAuth
 * in sessionStorage so it survives the navigation from /sign-in; without one
 * (direct visit or post-expiry refresh) a back-to-sign-in notice renders.
 *
 * A challenge carrying mfaEnrollment renders the first-time authenticator
 * setup (QR + setup key + fixed TOTP code) and, after completion, the fresh
 * recovery codes exactly once before continuing to the redirect target.
 */
export function MfaChallengeForm({
  className,
  redirectTo = "/",
  ...props
}: React.ComponentProps<"div"> & {
  redirectTo?: string;
}) {
  const { signIn, isLoading, error, mfaChallenge, clearMfa } = useAuth();
  const t = useTranslations("signIn.mfa");
  const [enrolling, setEnrolling] = useState(false);
  const [enrollError, setEnrollError] = useState<string | null>(null);
  const [backupCodes, setBackupCodes] = useState<string[] | null>(null);
  const [copied, setCopied] = useState<"secret" | "codes" | null>(null);
  const [copyError, setCopyError] = useState<string | null>(null);
  const isHydrated = useSyncExternalStore(
    subscribeToHydration,
    () => true,
    () => false,
  );

  const enrollment = mfaChallenge?.mfaEnrollment;
  const isEnrollment = enrollment != null;
  const availableMethods = [...new Set(
    (mfaChallenge?.availableMethods ?? [])
      .map((method) => methodNames.find((supported) => supported.toLowerCase() === method.toLowerCase()))
      .filter((method): method is SignInMfaMethod => method !== undefined),
  )];
  const [selection, setSelection] = useState<{ token: string; method: SignInMfaMethod } | null>(null);
  const selectedMethod = isEnrollment ? "Totp" :
    selection !== null && selection.token === mfaChallenge?.mfaToken && availableMethods.includes(selection.method)
      ? selection.method : availableMethods[0];
  const [smsDelivery, setSmsDelivery] = useState<{
    token: string;
    status: "sending" | "sent" | "failed";
  } | null>(null);
  const currentToken = useRef(mfaChallenge?.mfaToken);
  useEffect(() => {
    currentToken.current = mfaChallenge?.mfaToken;
    return () => { currentToken.current = undefined; };
  }, [mfaChallenge?.mfaToken]);
  const sendingToken = useRef<string | null>(null);
  const smsStatus = smsDelivery !== null && smsDelivery.token === mfaChallenge?.mfaToken ? smsDelivery.status : undefined;

  async function handleSendSms() {
    if (!mfaChallenge || isEnrollment || selectedMethod !== "Sms") return;
    const token = mfaChallenge.mfaToken;
    if (sendingToken.current === token) return;
    sendingToken.current = token;
    setSmsDelivery({ token, status: "sending" });
    try {
      const result = await sendMfaSmsChallengeAction(token);
      if (currentToken.current === token) {
        setSmsDelivery({ token, status: result.success ? "sent" : "failed" });
      }
    } catch (sendError) {
      if (currentToken.current !== token) return;
      if (recoverFromStaleServerAction(sendError)) return;
      if (currentToken.current === token) setSmsDelivery({ token, status: "failed" });
    } finally {
      if (sendingToken.current === token) sendingToken.current = null;
    }
  }

  async function copyText(text: string, kind: "secret" | "codes") {
    setCopied(null);
    setCopyError(null);
    if (!navigator.clipboard?.writeText) {
      setCopyError(t("copyFailed"));
      return;
    }
    try {
      await navigator.clipboard.writeText(text);
      setCopied(kind);
      window.setTimeout(() => setCopied(null), 1500);
    } catch {
      setCopyError(t("copyFailed"));
    }
  }

  function handleStartAgain() {
    setBackupCodes(null);
    clearMfa();
  }

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!mfaChallenge) return;
    const form = e.currentTarget;
    const data = new FormData(form);
    const code = data.get("code") as string;

    if (isEnrollment) {
      setEnrolling(true);
      setEnrollError(null);
      try {
        const result = await completeMfaEnrollmentAction(
          mfaChallenge.mfaToken,
          code,
        );
        if (!result.success) {
          setEnrollError(result.message ?? t("enrollFailed"));
          return;
        }
        clearMfa();
        form.reset();
        if (result.backupCodes && result.backupCodes.length > 0) {
          setBackupCodes(result.backupCodes);
        } else {
          window.location.href = redirectTo;
        }
      } catch (submitError) {
        if (recoverFromStaleServerAction(submitError)) return;
        setEnrollError(t("enrollFailed"));
      } finally {
        setEnrolling(false);
      }
      return;
    }

    if (!selectedMethod) return;
    try {
      await signIn("credentials", {
        mfaToken: mfaChallenge.mfaToken,
        method: selectedMethod,
        code,
        redirectTo,
      });
      form.reset();
    } catch {
      /* The hook keeps the current challenge and exposes the error. */
    }
  }

  if (backupCodes) {
    return (
      <div className={className} {...props}>
        <Card className="border-white/10 bg-slate-900/85 text-white shadow-2xl shadow-sky-950/30 backdrop-blur">
          <CardHeader className="text-center">
            <CardTitle className="text-2xl">
              <h1>{t("backupCodesTitle")}</h1>
            </CardTitle>
            <CardDescription className="text-slate-300">
              {t("backupCodesDescription")}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4" data-testid="mfa-enrollment-backup-codes">
              <div className="grid grid-cols-2 gap-1 font-mono text-sm sm:grid-cols-3">
                {backupCodes.map((backupCode) => (
                  <span key={backupCode} className="text-slate-100">
                    {backupCode}
                  </span>
                ))}
              </div>
              <div className="flex flex-col gap-2">
                <Button
                  type="button"
                  onClick={() => {
                    setBackupCodes(null);
                    window.location.href = redirectTo;
                  }}
                  data-testid="mfa-backup-codes-continue"
                >
                  {t("backupCodesContinue")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="border-white/10 text-slate-200 hover:bg-white/10 hover:text-white"
                  onClick={() => {
                    void copyText(backupCodes.join("\n"), "codes");
                  }}
                  data-testid="mfa-backup-codes-copy"
                >
                  {copied === "codes" ? (
                    <Check className="size-4" aria-hidden />
                  ) : (
                    <Copy className="size-4" aria-hidden />
                  )}
                  {copied === "codes" ? t("copied") : t("copyBackupCodes")}
                </Button>
              </div>
              {copyError && <FieldError>{copyError}</FieldError>}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!mfaChallenge) {
    return (
      <Card className={className} {...props}>
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            <h1>{t("title")}</h1>
          </CardTitle>
          <CardDescription className="text-slate-300">
            {t("noChallenge")}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Link
            href="/sign-in"
            className="text-sky-200 underline-offset-4 hover:underline"
          >
            {t("backToSignIn")}
          </Link>
        </CardContent>
      </Card>
    );
  }

  const submitting = isEnrollment ? enrolling : isLoading;

  return (
    <div className={className} {...props}>
      <Card className="border-white/10 bg-slate-900/85 text-white shadow-2xl shadow-sky-950/30 backdrop-blur">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            <h1>{t("title")}</h1>
          </CardTitle>
          <CardDescription className="text-slate-300">
            {isEnrollment ? t("enrollDescription") : t("description")}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form
            method="post"
            onSubmit={(event) => {
              void handleSubmit(event);
            }}
            data-auth-ready={isHydrated ? "true" : "false"}
          >
            <FieldGroup>
              {isEnrollment && (
                <Field>
                  <div className="mx-auto w-fit rounded-lg bg-white p-3">
                    <QRCodeSVG
                      value={enrollment.qrCodeUri}
                      size={168}
                      marginSize={2}
                      title={t("enrollQrLabel")}
                      data-testid="mfa-enrollment-qr"
                    />
                  </div>
                </Field>
              )}
              {isEnrollment && (
                <Field>
                  <FieldLabel htmlFor="mfa-secret">
                    {t("secretLabel")}
                  </FieldLabel>
                  <div className="flex items-center gap-2">
                    <code
                      id="mfa-secret"
                      data-testid="mfa-enrollment-secret"
                      className="min-w-0 flex-1 break-all rounded border border-white/10 bg-white/5 px-3 py-2 font-mono text-xs text-slate-100"
                    >
                      {enrollment.secretKey}
                    </code>
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      className="size-10 shrink-0 border-white/10 text-slate-200 hover:bg-white/10 hover:text-white"
                      aria-label={t("copySecret")}
                      disabled={enrolling}
                      onClick={() => {
                        if (enrollment?.secretKey) {
                          void copyText(enrollment.secretKey, "secret");
                        }
                      }}
                    >
                      {copied === "secret" ? (
                        <Check className="size-4" aria-hidden />
                      ) : (
                        <Copy className="size-4" aria-hidden />
                      )}
                    </Button>
                  </div>
                </Field>
              )}
              {!isEnrollment && availableMethods.length > 1 && (
                <Field>
                  <FieldLabel htmlFor="mfa-method">{t("methodLabel")}</FieldLabel>
                  <select
                    id="mfa-method"
                    name="method"
                    value={selectedMethod}
                    onChange={(event) => {
                      const method = availableMethods.find((value) => value === event.target.value);
                      if (method) setSelection({ token: mfaChallenge.mfaToken, method });
                    }}
                    disabled={isLoading}
                    className="h-10 rounded border border-white/10 bg-slate-900 px-3"
                  >
                    {availableMethods.map((method) => (
                      <option key={method} value={method}>{t(`method${method}`)}</option>
                    ))}
                  </select>
                </Field>
              )}
              {!isEnrollment && !selectedMethod && <FieldError>{t("noSupportedMethod")}</FieldError>}
              {!isEnrollment && selectedMethod === "Sms" && (
                <Field>
                  <Button
                    type="button"
                    variant="outline"
                    disabled={isLoading || smsStatus === "sending" || !isHydrated}
                    onClick={() => { void handleSendSms(); }}
                  >
                    {smsStatus === "sending" ? t("smsSending") : smsStatus === "sent" ? t("smsResend") : t("smsSend")}
                  </Button>
                  {smsStatus === "sent" && <p role="status" className="text-sm text-slate-300">{t("smsSent")}</p>}
                  {smsStatus === "failed" && <FieldError>{t("smsFailed")}</FieldError>}
                </Field>
              )}
              <Field>
                <FieldLabel htmlFor="mfa-code">{t("codeLabel")}</FieldLabel>
                <Input
                  id="mfa-code"
                  name="code"
                  autoComplete="one-time-code"
                  inputMode={isEnrollment ? "numeric" : undefined}
                  maxLength={isEnrollment ? 6 : undefined}
                  required
                  disabled={submitting}
                  className="h-10 border-white/10 bg-white/5 text-white"
                />
              </Field>
              {isEnrollment && enrollError && (
                <FieldError>{enrollError}</FieldError>
              )}
              {copyError && <FieldError>{copyError}</FieldError>}
              {error &&
                !isEnrollment &&
                !("type" in error && error.type === "MfaRequired") && (
                  <FieldError>{error.message}</FieldError>
                )}
              <Button type="submit" disabled={submitting || !isHydrated || (!isEnrollment && !selectedMethod)}>
                {isEnrollment ? t("enrollSubmit") : t("submit")}
              </Button>
              <Button
                type="button"
                variant="ghost"
                disabled={submitting}
                className="text-slate-200 hover:bg-white/10 hover:text-white"
                onClick={handleStartAgain}
              >
                {t("startAgain")}
              </Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
