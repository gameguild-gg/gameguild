'use client';

import { type FormEvent, useState } from 'react';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Input } from '@game-guild/ui/components/input';

type ActionResult<T> = { success: true; data: T } | { success: false; error: string };

interface ForgotPasswordFormProps extends React.ComponentProps<'div'> {
  onRequestReset?: (email: string) => Promise<ActionResult<void>>;
  initialEmail?: string;
}

export function ForgotPasswordForm({ className, initialEmail = '', onRequestReset, ...props }: ForgotPasswordFormProps) {
  const [email, setEmail] = useState(initialEmail);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitted, setIsSubmitted] = useState(false);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);

    if (!email.trim()) {
      setError('Email is required.');
      return;
    }

    setIsLoading(true);

    try {
      if (onRequestReset) {
        const result = await onRequestReset(email.trim());
        if (!result.success) {
          throw new Error(result.error);
        }
      }
      setIsSubmitted(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.');
    } finally {
      setIsLoading(false);
    }
  }

  if (isSubmitted) {
    return (
      <div className={cn('flex flex-col gap-6', className)} {...props}>
        <Card>
          <CardHeader className="text-center">
            <CardTitle className="text-xl">Check your email</CardTitle>
            <CardDescription>
              If an account with that email exists, we&apos;ve sent password reset instructions to <span className="font-medium">{email}</span>.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <FieldGroup>
              <Field>
                <Button variant="outline" asChild>
                  <Link href="/sign-in">Back to Sign In</Link>
                </Button>
              </Field>
              <FieldDescription className="text-center">
                Didn&apos;t receive the email?{' '}
                <button type="button" className="underline underline-offset-4 hover:text-primary" onClick={() => setIsSubmitted(false)}>
                  Try again
                </button>
              </FieldDescription>
            </FieldGroup>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className={cn('flex flex-col gap-6', className)} {...props}>
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Reset your password</CardTitle>
          <CardDescription>Enter your email address and we&apos;ll send you a link to reset your password.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} noValidate>
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="email">Email</FieldLabel>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  placeholder="m@example.com"
                  autoComplete="email"
                  required
                  disabled={isLoading}
                  aria-invalid={!!error}
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value);
                    if (error) setError(null);
                  }}
                />
                {error && <FieldError>{error}</FieldError>}
              </Field>
              <Field>
                <Button type="submit" disabled={isLoading}>
                  {isLoading ? 'Sending...' : 'Send Reset Link'}
                </Button>
                <FieldDescription className="text-center">
                  Remember your password? <Link href="/sign-in">Sign in</Link>
                </FieldDescription>
              </Field>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
