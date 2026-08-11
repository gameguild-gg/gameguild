import React from 'react';
import { SignupForm } from "@/components/signup-form"
import { GoogleOneTap } from '@/components/google-one-tap';
import { GoogleSignInButton } from '@/components/google-sign-in-button';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';
import { auth } from '@/auth';

export default async function Page({
  searchParams,
}: PageProps<'/[locale]/sign-up'>): Promise<React.JSX.Element> {
  const params = await searchParams;
  const session = await auth();
  const redirectTo = resolveAllowedAuthRedirect(params?.redirectTo);

  return (
    <>
      <GoogleOneTap authenticated={Boolean(session)} redirectTo={redirectTo} />
      <SignupForm
        providers={<GoogleSignInButton options={{ text: 'signup_with' }} redirectTo={redirectTo} />}
        redirectTo={redirectTo}
      />
    </>
  );
}
