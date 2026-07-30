'use client';

import { useAuth } from '@game-guild/client/react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Input } from '@game-guild/ui/components/input';
import Link from 'next/link';
import { useLayoutEffect, useRef, useState, type FormEvent } from 'react';

export function SignUpForm({ redirectTo }: { redirectTo: string }) {
  const { signUp, isLoading, error, clearError } = useAuth();
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const hydratedFieldsetRef = useRef<HTMLFieldSetElement>(null);

  useLayoutEffect(() => {
    if (hydratedFieldsetRef.current) hydratedFieldsetRef.current.disabled = false;
  });

  function clearFieldError(field: string) {
    setFieldErrors((current) => {
      if (!current[field]) {
        return current;
      }

      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    clearError();
    setFieldErrors({});

    const formData = new FormData(event.currentTarget);
    const name = String(formData.get('name') || '').trim();
    const email = String(formData.get('email') || '').trim();
    const password = String(formData.get('password') || '');
    const confirmPassword = String(formData.get('confirm-password') || '');

    const errors: Record<string, string> = {};

    if (!name) errors.name = 'Full name is required.';
    if (!email) errors.email = 'Email is required.';
    if (!password) errors.password = 'Password is required.';
    else if (password.length < 8) errors.password = 'Password must be at least 8 characters.';
    if (!confirmPassword) errors['confirm-password'] = 'Please confirm your password.';
    else if (password !== confirmPassword) errors['confirm-password'] = 'Passwords do not match.';

    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }

    const [firstName, ...lastNameParts] = name.split(/\s+/);

    try {
      await signUp({
        username: email.split('@')[0],
        email,
        password,
        firstName,
        lastName: lastNameParts.join(' ') || undefined,
        redirectTo,
      });
    } catch {
      // useAuth exposes the error state for rendering.
    }
  }

  return (
    <Card className="border-slate-800 bg-slate-900/80 text-slate-100 shadow-2xl shadow-slate-950/40">
      <CardHeader className="text-center">
        <CardTitle><h1 className="text-2xl">Create your learner account</h1></CardTitle>
        <CardDescription className="text-slate-300">Sign up here so you can continue directly into the student classroom.</CardDescription>
      </CardHeader>
      <CardContent>
        <form method="post" onSubmit={handleSubmit} noValidate>
          <fieldset ref={hydratedFieldsetRef} disabled className="contents">
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="name">Full name</FieldLabel>
                <Input
                  id="name"
                  name="name"
                  autoComplete="name"
                  disabled={isLoading}
                  aria-invalid={!!fieldErrors.name}
                  onChange={() => clearFieldError('name')}
                />
                {fieldErrors.name ? <FieldError>{fieldErrors.name}</FieldError> : null}
              </Field>
              <Field>
                <FieldLabel htmlFor="email">Email</FieldLabel>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  autoComplete="email"
                  disabled={isLoading}
                  aria-invalid={!!fieldErrors.email}
                  onChange={() => clearFieldError('email')}
                />
                {fieldErrors.email ? <FieldError>{fieldErrors.email}</FieldError> : null}
              </Field>
              <Field>
                <FieldLabel htmlFor="password">Password</FieldLabel>
                <Input
                  id="password"
                  name="password"
                  type="password"
                  autoComplete="new-password"
                  disabled={isLoading}
                  aria-invalid={!!fieldErrors.password}
                  onChange={() => clearFieldError('password')}
                />
                {fieldErrors.password ? <FieldError>{fieldErrors.password}</FieldError> : null}
              </Field>
              <Field>
                <FieldLabel htmlFor="confirm-password">Confirm password</FieldLabel>
                <Input
                  id="confirm-password"
                  name="confirm-password"
                  type="password"
                  autoComplete="new-password"
                  disabled={isLoading}
                  aria-invalid={!!fieldErrors['confirm-password']}
                  onChange={() => clearFieldError('confirm-password')}
                />
                {fieldErrors['confirm-password'] ? <FieldError>{fieldErrors['confirm-password']}</FieldError> : null}
              </Field>
              {error ? <FieldError>{error.message}</FieldError> : null}
              <Field>
                <Button type="submit" disabled={isLoading} className="bg-sky-600 text-white hover:bg-sky-500">
                  {isLoading ? 'Creating account...' : 'Create account'}
                </Button>
                <FieldDescription className="text-center text-slate-400">
                  Already enrolled?{' '}
                  <Link href={`/sign-in?redirectTo=${encodeURIComponent(redirectTo)}`} className="text-sky-300 hover:text-sky-200">
                    Sign in
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
