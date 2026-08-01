'use client';

import { useAuth } from '@game-guild/client/react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Input } from '@game-guild/ui/components/input';
import Link from 'next/link';
import { useLayoutEffect, useRef, useState, type FormEvent } from 'react';

export function SignInForm({ redirectTo }: { redirectTo: string }) {
  const { signIn, isLoading, error, clearError } = useAuth();
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const hydratedFieldsetRef = useRef<HTMLFieldSetElement>(null);

  useLayoutEffect(() => {
    if (hydratedFieldsetRef.current) hydratedFieldsetRef.current.disabled = false;
  });

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    clearError();
    setFieldErrors({});

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get('email') || '').trim();
    const password = String(formData.get('password') || '');

    if (!email) {
      setFieldErrors((current) => ({ ...current, email: 'Email is required.' }));
      return;
    }

    if (!password) {
      setFieldErrors((current) => ({ ...current, password: 'Password is required.' }));
      return;
    }

    try {
      await signIn('credentials', {
        email,
        password,
        redirectTo,
      });
    } catch {
      // useAuth exposes the error state for rendering.
    }
  }

  return (
    <Card className="border-slate-800 bg-slate-900/80 text-slate-100 shadow-2xl shadow-slate-950/40">
      <CardHeader className="text-center">
        <CardTitle><h1 className="text-2xl">Student sign in</h1></CardTitle>
        <CardDescription className="text-slate-300">Use your GameGuild account to continue studying in the dedicated learning app.</CardDescription>
      </CardHeader>
      <CardContent>
        <form method="post" onSubmit={handleSubmit} noValidate>
          <fieldset ref={hydratedFieldsetRef} disabled className="contents">
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="email">Email</FieldLabel>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  autoComplete="email"
                  placeholder="student@example.com"
                  disabled={isLoading}
                  aria-invalid={!!fieldErrors.email}
                  onChange={() => fieldErrors.email && setFieldErrors((current) => ({ ...current, email: '' }))}
                />
                {fieldErrors.email ? <FieldError>{fieldErrors.email}</FieldError> : null}
              </Field>
              <Field>
                <FieldLabel htmlFor="password">Password</FieldLabel>
                <Input
                  id="password"
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  disabled={isLoading}
                  aria-invalid={!!fieldErrors.password}
                  onChange={() => fieldErrors.password && setFieldErrors((current) => ({ ...current, password: '' }))}
                />
                {fieldErrors.password ? <FieldError>{fieldErrors.password}</FieldError> : null}
              </Field>
              {error ? <FieldError>{error.message}</FieldError> : null}
              <Field>
                <Button type="submit" disabled={isLoading} className="bg-sky-600 text-white hover:bg-sky-500">
                  {isLoading ? 'Signing in...' : 'Continue to classroom'}
                </Button>
                <FieldDescription className="text-center text-slate-400">
                  Need an account?{' '}
                  <Link href={`/sign-up?redirectTo=${encodeURIComponent(redirectTo)}`} className="text-sky-300 hover:text-sky-200">
                    Create one
                  </Link>
                </FieldDescription>
              </Field>
            </FieldGroup>
          </fieldset>
        </form>
      </CardContent>
    </Card>
  );
}
