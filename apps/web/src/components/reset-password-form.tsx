'use client';

import { type FormEvent, useState } from 'react';
import Link from 'next/link';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Input } from '@game-guild/ui/components/input';

type ActionResult = { success: true; data: void } | { success: false; error: string };

interface ResetPasswordFormProps {
  token: string;
  onReset: (token: string, newPassword: string, confirmPassword: string) => Promise<ActionResult>;
}

export function ResetPasswordForm({ token, onReset }: ResetPasswordFormProps) {
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isComplete, setIsComplete] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const result = await onReset(token, newPassword, confirmPassword);
      if (!result.success) {
        setError(result.error);
        return;
      }
      setIsComplete(true);
    } finally {
      setIsLoading(false);
    }
  }

  if (isComplete) {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle>Password updated</CardTitle>
          <CardDescription>You can now sign in and review your workspace invitations.</CardDescription>
        </CardHeader>
        <CardContent>
          <Button asChild className="w-full">
            <Link href="/sign-in?callbackUrl=/invitations">Continue to sign in</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="text-center">
        <CardTitle>Choose a new password</CardTitle>
        <CardDescription>Use at least 8 characters for your GameGuild account.</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} noValidate>
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="new-password">New password</FieldLabel>
              <Input id="new-password" type="password" autoComplete="new-password" required minLength={8} disabled={isLoading} value={newPassword} onChange={(event) => setNewPassword(event.target.value)} />
            </Field>
            <Field>
              <FieldLabel htmlFor="confirm-password">Confirm password</FieldLabel>
              <Input id="confirm-password" type="password" autoComplete="new-password" required minLength={8} disabled={isLoading} value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} aria-invalid={Boolean(error)} />
              {error ? <FieldError>{error}</FieldError> : null}
            </Field>
            <Field>
              <Button type="submit" disabled={isLoading || !token}>
                {isLoading ? 'Updating...' : 'Update password'}
              </Button>
              {!token ? <FieldError>This reset link is missing its security token.</FieldError> : null}
              <FieldDescription className="text-center"><Link href="/forgot-password">Request a new link</Link></FieldDescription>
            </Field>
          </FieldGroup>
        </form>
      </CardContent>
    </Card>
  );
}
