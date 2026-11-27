'use client';

import {
  completeEmailEnrollmentAction,
  completeSmsEnrollmentAction,
  completeTotpEnrollmentAction,
  disableMfaAction,
  regenerateBackupCodesAction,
  startEmailEnrollmentAction,
  startSmsEnrollmentAction,
  startTotpEnrollmentAction,
  type MfaPageData,
} from '@/lib/auth/mfa-actions';
import { BackupCodesReveal } from '@/components/auth/backup-codes-reveal';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@game-guild/ui/components/card';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@game-guild/ui/components/alert-dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { KeyRound, Mail, MessageSquare, ShieldCheck, Smartphone } from 'lucide-react';
import { useTranslations } from 'next-intl';
import * as React from 'react';
import { toast } from 'sonner';

/**
 * MFA method-management surface (settings/security, issue #244): enrolled-method list with
 * add flows for the authenticator app, SMS and email codes, recovery-code status with the
 * one-time reveal, and password-verified disable. All mutations go through server actions
 * (mfa-actions.ts); revalidatePath refreshes the server-rendered data after each action.
 */

type MethodState =
  | { kind: 'idle' }
  /** `backupCodes`: the first recovery set, issued with the setup and revealed once (issue #1298 J2). */
  | { kind: 'totp-active'; secretKey: string; qrCodeUri: string; backupCodes: readonly string[] }
  | { kind: 'sms-active'; maskedPhone: string }
  | { kind: 'email-active'; maskedEmail: string; expiresInSeconds: number };

/** Where a revealed set came from; decides the reveal's copy. */
type FreshCodes = { codes: readonly string[]; source: 'issued' | 'regenerated' };

/** The server throttles email codes to one a minute; the panel's countdown mirrors it. */
const EMAIL_CODE_RESEND_COOLDOWN_MS = 60_000;

export function MfaMethodsManager({ data }: { data: MfaPageData }) {
  const t = useTranslations('settings.security.mfa');
  const tShared = useTranslations('settings.security');
  const [methodState, setMethodState] = React.useState<MethodState>({ kind: 'idle' });
  const [code, setCode] = React.useState('');
  const [phone, setPhone] = React.useState('');
  const [smsPassword, setSmsPassword] = React.useState('');
  const [password, setPassword] = React.useState('');
  const [disableOpen, setDisableOpen] = React.useState(false);
  const [regenerateOpen, setRegenerateOpen] = React.useState(false);
  const [busy, setBusy] = React.useState(false);
  const [freshCodes, setFreshCodes] = React.useState<FreshCodes | null>(null);
  const [emailError, setEmailError] = React.useState<string | null>(null);
  const [emailCooldownUntil, setEmailCooldownUntil] = React.useState<number | null>(null);
  const [now, setNow] = React.useState(() => Date.now());

  const configuration = data.configuration;
  const isEnabled = configuration?.isEnabled === true;
  const methods = data.methods?.methods ?? [];
  const backupCodes = data.backupCodes;

  const totp = methods.find((m) => m.method === 'Totp');
  const sms = methods.find((m) => m.method === 'Sms');
  const email = methods.find((m) => m.method === 'Email');
  const backupCode = methods.find((m) => m.method === 'BackupCode');

  // Email codes are offered as an additional factor only. The web sign-in challenge does
  // not offer them yet, so an account left with email as its sole factor could not sign in.
  const anotherFactorEnrolled = totp?.isEnabled === true || sms?.isEnabled === true;
  const emailUnenrolled = email?.isAvailable === true && email.isEnabled === false;
  const canAddEmail = emailUnenrolled && anotherFactorEnrolled && methodState.kind !== 'email-active';

  const emailCooldownActive = emailCooldownUntil !== null && emailCooldownUntil > now;
  const emailSecondsLeft = emailCooldownUntil === null ? 0 : Math.max(0, Math.ceil((emailCooldownUntil - now) / 1000));

  React.useEffect(() => {
    if (!emailCooldownActive) {
      return;
    }
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [emailCooldownActive]);

  const reportFailure = (message?: string) => {
    toast.error(message ?? tShared('actionFailed'));
  };

  function startEmailCooldown() {
    setEmailCooldownUntil(Date.now() + EMAIL_CODE_RESEND_COOLDOWN_MS);
    setNow(Date.now());
  }

  function closeEmailPanel() {
    setMethodState({ kind: 'idle' });
    setCode('');
    setEmailError(null);
    setEmailCooldownUntil(null);
  }

  async function handleStartEmail() {
    setBusy(true);
    try {
      const result = await startEmailEnrollmentAction();
      if (result.success && result.emailSetup) {
        setMethodState({
          kind: 'email-active',
          maskedEmail: result.emailSetup.emailMasked ?? '',
          expiresInSeconds: result.emailSetup.expiresInSeconds ?? 600,
        });
        setCode('');
        setEmailError(null);
        startEmailCooldown();
        toast.success(t('email.sent'));
      } else {
        reportFailure(result.message ?? t('email.startFailed'));
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleResendEmail() {
    setBusy(true);
    try {
      const result = await startEmailEnrollmentAction();
      if (result.success) {
        setEmailError(null);
        startEmailCooldown();
        toast.success(t('email.sent'));
      } else {
        // The server's throttle is the authority: a refused request restarts the countdown.
        if (result.status === 'error') {
          startEmailCooldown();
        }
        reportFailure(result.message ?? t('email.startFailed'));
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleCompleteEmail() {
    setBusy(true);
    try {
      const result = await completeEmailEnrollmentAction(code);
      if (result.success) {
        closeEmailPanel();
        toast.success(t('email.completed'));
      } else {
        // The panel stays open: the code may simply be wrong. The server's message says so.
        setEmailError(result.message ?? t('email.invalidCode'));
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleStartTotp() {
    setSmsPassword('');
    setBusy(true);
    try {
      const result = await startTotpEnrollmentAction();
      if (result.success && result.setup?.secretKey) {
        // The setup also issues the first recovery set (#1195). Keep it for the reveal
        // once enrollment completes; it is not shown before the factor is verified.
        setMethodState({
          kind: 'totp-active',
          secretKey: result.setup.secretKey,
          qrCodeUri: result.setup.qrCodeUri ?? '',
          backupCodes: result.setup.backupCodes ?? [],
        });
        setCode('');
        toast.success(t('totp.started'));
      } else {
        reportFailure(result.message);
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleCompleteTotp() {
    if (methodState.kind !== 'totp-active') {
      return;
    }
    setBusy(true);
    try {
      const result = await completeTotpEnrollmentAction(code, methodState.secretKey);
      if (result.success) {
        const issued = methodState.backupCodes;
        setMethodState({ kind: 'idle' });
        setCode('');
        if (issued.length > 0) {
          setFreshCodes({ codes: issued, source: 'issued' });
        }
        toast.success(t('totp.completed'));
      } else {
        reportFailure(result.message);
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleStartSms() {
    if (smsPassword.length === 0) {
      return;
    }
    setBusy(true);
    try {
      const result = await startSmsEnrollmentAction(phone, smsPassword);
      if (result.success && result.smsSetup) {
        setMethodState({ kind: 'sms-active', maskedPhone: result.smsSetup.phoneNumberMasked ?? phone });
        setCode('');
        toast.success(t('sms.sent', { phone: result.smsSetup.phoneNumberMasked ?? '' }));
      } else {
        reportFailure(result.message);
      }
    } finally {
      setSmsPassword('');
      setBusy(false);
    }
  }

  async function handleCompleteSms() {
    setBusy(true);
    try {
      const result = await completeSmsEnrollmentAction(code);
      if (result.success) {
        setMethodState({ kind: 'idle' });
        setCode('');
        setPhone('');
        toast.success(t('sms.completed'));
      } else {
        reportFailure(result.message);
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleRegenerate() {
    setBusy(true);
    try {
      // Issue #1153: regeneration re-verifies the account password server-side; the
      // dialog collects it before the codes are rotated.
      const result = await regenerateBackupCodesAction(password);
      if (result.success) {
        setRegenerateOpen(false);
        setPassword('');
        setFreshCodes({ codes: result.codes ?? [], source: 'regenerated' });
        toast.success(t('recovery.regenerated'));
      } else if (result.emptySet) {
        // An empty answer is a failure too (issue #1298 J2): never a success toast over an empty grid.
        reportFailure(t('recovery.notRegenerated'));
      } else {
        reportFailure(result.message ?? t('recovery.wrongPassword'));
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleDisable() {
    setBusy(true);
    try {
      const result = await disableMfaAction(password);
      if (result.success) {
        setDisableOpen(false);
        setPassword('');
        setMethodState({ kind: 'idle' });
        toast.success(t('disable.success'));
      } else {
        reportFailure(result.message ?? t('disable.wrongPassword'));
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ShieldCheck className="size-5" aria-hidden />
            {t('title')}
          </CardTitle>
          <CardDescription>{t('description')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">{t('status.label')}</p>
              <p className="text-muted-foreground text-sm">
                {isEnabled ? t('status.enabled', { date: configuration?.enabledAt ?? '' }) : t('status.disabled')}
              </p>
            </div>
            <Badge variant={isEnabled ? 'default' : 'secondary'} data-testid="mfa-status-badge">
              {isEnabled ? t('status.badgeEnabled') : t('status.badgeDisabled')}
            </Badge>
          </div>

          <ul className="divide-y rounded-lg border" data-testid="mfa-method-list">
            {methods.map((method) => (
              <li key={method.method} className="flex items-center justify-between gap-4 p-4">
                <div className="flex items-start gap-3">
                  {method.method === 'Totp' ? (
                    <KeyRound className="text-muted-foreground mt-0.5 size-5" aria-hidden />
                  ) : method.method === 'Sms' ? (
                    <MessageSquare className="text-muted-foreground mt-0.5 size-5" aria-hidden />
                  ) : method.method === 'Email' ? (
                    <Mail className="text-muted-foreground mt-0.5 size-5" aria-hidden />
                  ) : (
                    <Smartphone className="text-muted-foreground mt-0.5 size-5" aria-hidden />
                  )}
                  <div>
                    <p className="text-sm font-medium">{method.name}</p>
                    <p className="text-muted-foreground text-sm">{method.description}</p>
                    {!method.isAvailable && method.method === 'Sms' ? <p className="text-muted-foreground text-xs">{t('sms.unavailable')}</p> : null}
                  </div>
                </div>
                <Badge variant={method.isEnabled ? 'default' : 'outline'}>
                  {method.isEnabled ? t('methods.enrolled') : method.isAvailable ? t('methods.available') : t('methods.unavailable')}
                </Badge>
              </li>
            ))}
          </ul>

          <div className="flex flex-wrap gap-2">
            {!totp?.isEnabled && (
              <Button onClick={() => void handleStartTotp()} disabled={busy} data-testid="mfa-totp-start">
                {t('totp.start')}
              </Button>
            )}
            {!sms?.isEnabled && sms?.isAvailable && methodState.kind !== 'sms-active' && (
              <Button variant="outline" onClick={() => { setSmsPassword(''); setMethodState({ kind: 'sms-active', maskedPhone: '' }); }} disabled={busy} data-testid="mfa-sms-start">
                {t('sms.start')}
              </Button>
            )}
            {canAddEmail && (
              <Button
                variant="outline"
                onClick={() => void handleStartEmail()}
                disabled={busy}
                data-testid="mfa-email-start"
              >
                {t('email.start')}
              </Button>
            )}
            {isEnabled && (
              <Button variant="destructive" onClick={() => setDisableOpen(true)} disabled={busy} data-testid="mfa-disable">
                {t('disable.title')}
              </Button>
            )}
          </div>

          {emailUnenrolled && !anotherFactorEnrolled && methodState.kind !== 'email-active' && (
            <p className="text-muted-foreground text-xs" data-testid="mfa-email-prerequisite">
              {t('email.prerequisite')}
            </p>
          )}

          {methodState.kind === 'totp-active' && (
            <div className="space-y-3 rounded-lg border p-4" data-testid="mfa-totp-panel">
              <p className="text-sm">{t('totp.scan')}</p>
              <p className="break-all rounded bg-muted p-2 font-mono text-xs" data-testid="mfa-totp-secret">
                {methodState.secretKey}
              </p>
              {methodState.qrCodeUri ? (
                <p className="break-all text-muted-foreground text-xs">{methodState.qrCodeUri}</p>
              ) : null}
              <div className="grid gap-2">
                <Label htmlFor="mfa-totp-code">{t('totp.codeLabel')}</Label>
                <Input
                  id="mfa-totp-code"
                  value={code}
                  onChange={(event) => setCode(event.target.value)}
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  placeholder="123456"
                />
              </div>
              <div className="flex gap-2">
                <Button onClick={() => void handleCompleteTotp()} disabled={busy || code.length < 6}>
                  {t('totp.complete')}
                </Button>
                <Button variant="ghost" onClick={() => setMethodState({ kind: 'idle' })} disabled={busy}>
                  {t('methods.cancel')}
                </Button>
              </div>
            </div>
          )}

          {methodState.kind === 'sms-active' && (
            <div className="space-y-3 rounded-lg border p-4" data-testid="mfa-sms-panel">
              <div className="grid gap-2">
                <Label htmlFor="mfa-sms-phone">{t('sms.phoneLabel')}</Label>
                <Input
                  id="mfa-sms-phone"
                  value={phone}
                  onChange={(event) => setPhone(event.target.value)}
                  inputMode="tel"
                  placeholder="+15551234567"
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="mfa-sms-password">{t('disable.passwordLabel')}</Label>
                <Input
                  id="mfa-sms-password"
                  data-testid="mfa-sms-password"
                  type="password"
                  value={smsPassword}
                  onChange={(event) => setSmsPassword(event.target.value)}
                  autoComplete="current-password"
                />
              </div>
              <div className="flex gap-2">
                <Button onClick={() => void handleStartSms()} disabled={busy || phone.trim().length < 9 || smsPassword.length === 0}>
                  {t('sms.send')}
                </Button>
                <Button variant="ghost" onClick={() => { setSmsPassword(''); setMethodState({ kind: 'idle' }); }} disabled={busy}>
                  {t('methods.cancel')}
                </Button>
              </div>
              {methodState.maskedPhone ? (
                <div className="grid gap-2">
                  <Label htmlFor="mfa-sms-code">{t('sms.codeLabel')}</Label>
                  <Input
                    id="mfa-sms-code"
                    value={code}
                    onChange={(event) => setCode(event.target.value)}
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    placeholder="123456"
                  />
                  <Button onClick={() => void handleCompleteSms()} disabled={busy || code.length < 6}>
                    {t('sms.complete')}
                  </Button>
                </div>
              ) : null}
            </div>
          )}

          {methodState.kind === 'email-active' && (
            <div className="space-y-3 rounded-lg border p-4" data-testid="mfa-email-panel">
              <p className="text-sm" data-testid="mfa-email-destination">
                {methodState.maskedEmail ? t('email.sentTo', { email: methodState.maskedEmail }) : t('email.sent')}
              </p>
              <p className="text-muted-foreground text-xs">
                {t('email.expiresNote', { minutes: Math.max(1, Math.round(methodState.expiresInSeconds / 60)) })}
              </p>
              <div className="grid gap-2">
                <Label htmlFor="mfa-email-code">{t('email.codeLabel')}</Label>
                <Input
                  id="mfa-email-code"
                  value={code}
                  onChange={(event) => setCode(event.target.value)}
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  placeholder="123456"
                  aria-invalid={emailError ? true : undefined}
                  aria-describedby={emailError ? 'mfa-email-error' : undefined}
                />
                {emailError && (
                  <p id="mfa-email-error" role="alert" className="text-destructive text-sm">
                    {emailError}
                  </p>
                )}
              </div>
              <div className="flex flex-wrap gap-2">
                <Button
                  onClick={() => void handleCompleteEmail()}
                  disabled={busy || code.length < 6}
                  data-testid="mfa-email-complete"
                >
                  {t('email.complete')}
                </Button>
                <Button
                  variant="outline"
                  onClick={() => void handleResendEmail()}
                  disabled={busy || emailCooldownActive}
                  data-testid="mfa-email-resend"
                >
                  {emailCooldownActive ? t('email.resendCountdown', { seconds: emailSecondsLeft }) : t('email.resend')}
                </Button>
                <Button variant="ghost" onClick={closeEmailPanel} disabled={busy}>
                  {t('methods.cancel')}
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <KeyRound className="size-5" aria-hidden />
            {t('recovery.title')}
          </CardTitle>
          <CardDescription>{t('recovery.description')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {backupCodes?.hasBackupCodes ? (
            <p className="text-muted-foreground text-sm" data-testid="mfa-recovery-status">
              {t('recovery.remaining', { remaining: backupCodes.remainingCount, total: backupCodes.totalCount })}
            </p>
          ) : (
            <p className="text-muted-foreground text-sm">{t('recovery.none')}</p>
          )}

          {freshCodes ? (
            <BackupCodesReveal
              testId="mfa-recovery-codes"
              codes={freshCodes.codes}
              title={freshCodes.source === 'issued' ? t('recovery.firstSetTitle') : t('recovery.freshWarning')}
              description={freshCodes.source === 'issued' ? t('recovery.firstSetDescription') : undefined}
              copyLabel={t('recovery.copy')}
              copiedLabel={t('recovery.copied')}
              copyFailedLabel={t('recovery.copyFailed')}
              dismissLabel={t('recovery.hide')}
              onDismiss={() => setFreshCodes(null)}
            />
          ) : (
            <Button variant="outline" onClick={() => setRegenerateOpen(true)} disabled={busy} data-testid="mfa-recovery-regenerate">
              {backupCode?.isEnabled ? t('recovery.regenerate') : t('recovery.generate')}
            </Button>
          )}
        </CardContent>
      </Card>

      <AlertDialog open={disableOpen} onOpenChange={setDisableOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('disable.title')}</AlertDialogTitle>
            <AlertDialogDescription>{t('disable.description')}</AlertDialogDescription>
          </AlertDialogHeader>
          <form
            onSubmit={(event) => {
              event.preventDefault();
              if (!busy && password.length > 0) {
                void handleDisable();
              }
            }}
          >
            <div className="grid gap-2">
              <Label htmlFor="mfa-disable-password">{t('disable.passwordLabel')}</Label>
              <Input
                id="mfa-disable-password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="current-password"
              />
            </div>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={() => setPassword('')}>{t('disable.cancel')}</AlertDialogCancel>
              {/* Issue #864: preventDefault keeps the dialog open while the password is
                  verified — Radix closes it on click by default, which threw the user out
                  on every wrong password and forced retyping from scratch. Success closes
                  it via setDisableOpen(false) in handleDisable. */}
              <AlertDialogAction
                type="submit"
                onClick={(event) => {
                  event.preventDefault();
                  void handleDisable();
                }}
                disabled={busy || password.length === 0}
                data-testid="mfa-disable-confirm"
              >
                {t('disable.confirm')}
              </AlertDialogAction>
            </AlertDialogFooter>
          </form>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={regenerateOpen} onOpenChange={setRegenerateOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('recovery.confirmTitle')}</AlertDialogTitle>
            <AlertDialogDescription>{t('recovery.confirmDescription')}</AlertDialogDescription>
          </AlertDialogHeader>
          <form
            onSubmit={(event) => {
              event.preventDefault();
              if (!busy && password.length > 0) {
                void handleRegenerate();
              }
            }}
          >
            <div className="grid gap-2">
              <Label htmlFor="mfa-regenerate-password">{t('recovery.passwordLabel')}</Label>
              <Input
                id="mfa-regenerate-password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="current-password"
              />
            </div>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={() => setPassword('')}>{t('recovery.cancel')}</AlertDialogCancel>
              {/* Same #864 pattern as disable: preventDefault keeps the dialog open while
                  the password is verified so a wrong password doesn't force retyping. */}
              <AlertDialogAction
                type="submit"
                onClick={(event) => {
                  event.preventDefault();
                  void handleRegenerate();
                }}
                disabled={busy || password.length === 0}
                data-testid="mfa-recovery-confirm"
              >
                {t('recovery.regenerate')}
              </AlertDialogAction>
            </AlertDialogFooter>
          </form>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
