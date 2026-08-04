import React from 'react';
import { SignInForm } from '@/components/sign-in-form';
import { GoogleOneTap } from '@/components/google-one-tap';
import { GoogleSignInButton } from '@/components/google-sign-in-button';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';
import { auth } from '@/auth';

export default async function Page({
  searchParams,
}: PageProps<'/[locale]/sign-in'>): Promise<React.JSX.Element> {
  const params = await searchParams;
  const session = await auth();
  // authenticated suppresses the One Tap prompt; signed-in users landing
  // on /sign-in don't get pestered, and GIS forbids prompt() in that case.
  const redirectTo = resolveAllowedAuthRedirect(params?.redirectTo, {
    learningOrigin:
      process.env.LEARNING_PUBLIC_URL ||
      process.env.NEXT_PUBLIC_LEARNING_APP_URL ||
      'https://learning.gameguild.gg',
    webOrigin:
      process.env.WEB_PUBLIC_URL ||
      process.env.NEXT_PUBLIC_APP_URL ||
      'https://gameguild.gg',
  });

  return (
    <>
      <GoogleOneTap authenticated={Boolean(session)} redirectTo={redirectTo} />
      <SignInForm
        providers={<GoogleSignInButton redirectTo={redirectTo} />}
        redirectTo={redirectTo}
      />
    </>
  );
}
