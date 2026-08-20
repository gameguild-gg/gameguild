'use client';

import { type FormEvent, useState } from 'react';
import { useSession } from '@game-guild/client/react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  changePasswordAction,
  type PasswordChangeActionResult,
} from '@/lib/auth/password-change-action';
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Loader2 } from 'lucide-react';
import { useTranslations } from 'next-intl';

/**
 * Password change card for the account settings page. Session refresh after
 * the server's TokenVersion bump goes through useSession().update() — this
 * component must never call signIn.
 */
export function PasswordCard() {
  const t = useTranslations('passwordChange');
  const { update } = useSession();
  const [pending, setPending] = useState(false);
  const [mismatch, setMismatch] = useState(false);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const currentPassword = (formData.get('currentPassword') as string) ?? '';
    const newPassword = (formData.get('newPassword') as string) ?? '';
    const confirmPassword = (formData.get('confirmPassword') as string) ?? '';
    const revokeOtherSessions = formData.get('revokeOtherSessions') === 'on';
    const form = e.currentTarget;

    if (newPassword !== confirmPassword) {
      setMismatch(true);
      return;
    }
    setMismatch(false);
    setPending(true);

    let result: PasswordChangeActionResult;
    try {
      result = await changePasswordAction({
        currentPassword,
        newPassword,
        confirmPassword,
        revokeOtherSessions,
      });
    } catch {
      result = { success: false, status: 'error' };
    } finally {
      setPending(false);
    }

    if (result.success) {
      toast.success(t('success'));
      form.reset();
      await update();
      return;
    }

    switch (result.status) {
      case 'wrongCurrent':
        toast.error(result.message ?? t('errors.wrongCurrent'));
        break;
      case 'weakPassword':
        toast.error(result.message ?? t('errors.weakPassword'));
        break;
      case 'unauthorized':
      case 'error':
        toast.error(t('errors.generic'));
        break;
    }
  }

  return (
    <Card data-testid="password-card">
      <CardHeader>
        <CardTitle>{t('title')}</CardTitle>
        <CardDescription>{t('description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} noValidate>
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="currentPassword">{t('currentLabel')}</FieldLabel>
              <Input
                id="currentPassword"
                name="currentPassword"
                type="password"
                autoComplete="current-password"
                disabled={pending}
              />
              <FieldDescription>{t('currentHelp')}</FieldDescription>
            </Field>
            <Field>
              <FieldLabel htmlFor="newPassword">{t('newLabel')}</FieldLabel>
              <Input
                id="newPassword"
                name="newPassword"
                type="password"
                autoComplete="new-password"
                required
                disabled={pending}
                aria-invalid={mismatch}
                onChange={() => mismatch && setMismatch(false)}
              />
              <FieldDescription>{t('hintLine')}</FieldDescription>
            </Field>
            <Field>
              <FieldLabel htmlFor="confirmPassword">{t('confirmLabel')}</FieldLabel>
              <Input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                required
                disabled={pending}
                aria-invalid={mismatch}
                onChange={() => mismatch && setMismatch(false)}
              />
              {mismatch && <FieldError>{t('errors.mismatch')}</FieldError>}
            </Field>
            <Field>
              <div className="flex items-center gap-2">
                <Checkbox id="revokeOtherSessions" name="revokeOtherSessions" defaultChecked disabled={pending} />
                <Label htmlFor="revokeOtherSessions" className="font-normal">
                  {t('revokeLabel')}
                </Label>
              </div>
            </Field>
            <Field>
              <Button type="submit" disabled={pending}>
                {pending ? <Loader2 className="size-4 animate-spin" /> : null}
                {t('submit')}
              </Button>
            </Field>
          </FieldGroup>
        </form>
      </CardContent>
    </Card>
  );
}
