'use client';

import React, { useActionState } from 'react';
import { SignInFormState, signInWithEmailAndPassword } from '@/lib/auth/email-password-actions';
import { Button } from '@game-guild/ui/components';
import { Input } from '@game-guild/ui/components';
import { Label } from '@game-guild/ui/components';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Alert, AlertDescription } from '@game-guild/ui/components';

const initialState: SignInFormState = {
  success: false,
};

export function EmailPasswordSignInForm() {
  const [state, formAction, isPending] = useActionState(signInWithEmailAndPassword, initialState);

  return (
    <Card className="w-full max-w-md">
      <CardHeader>
        <CardTitle>Sign In</CardTitle>
        <CardDescription>Sign in with your email and password</CardDescription>
      </CardHeader>
      <CardContent>
        <form action={formAction} className="space-y-4">
          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          {state.success && state.data && (
            <Alert>
              <AlertDescription>Sign in successful! Welcome {state.data.user.email}</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input id="email" name="email" type="email" placeholder="Enter your email" required disabled={isPending} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <Input id="password" name="password" type="password" placeholder="Enter your password" required disabled={isPending} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="tenantId">Tenant ID (Optional)</Label>
            <Input id="tenantId" name="tenantId" type="text" placeholder="Enter tenant ID (optional)" disabled={isPending} />
          </div>

          <Button type="submit" className="w-full" disabled={isPending}>
            {isPending ? 'Signing in...' : 'Sign In'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
