import React from 'react';
import { SignInForm } from '@/components/sign-in-form';
import { GoogleOneTap } from '@/components/google-one-tap';
import { GoogleSignInButton } from '@/components/google-sign-in-button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';
import { auth } from '@/auth';

export default async function Page({
  searchParams,
}: PageProps<'/[locale]/sign-in'>): Promise<React.JSX.Element> {
  const params = await searchParams;
  const session = await auth();
  // authenticated suppresses the One Tap prompt; signed-in users landing
  // on /sign-in don't get pestered, and GIS forbids prompt() in that case.
  return (
    <>
      <GoogleOneTap authenticated={Boolean(session)} />
      <Card className="border-white/10 bg-slate-900/85 text-white shadow-2xl shadow-sky-950/30 backdrop-blur">
        <CardContent className="flex flex-col items-center gap-4 pt-6">
          <GoogleSignInButton />
          <div className="flex w-full items-center gap-3 text-xs text-slate-400">
            <div className="h-px flex-1 bg-white/10" />
            <span>or with email</span>
            <div className="h-px flex-1 bg-white/10" />
          </div>
        </CardContent>
      </Card>
      <SignInForm
        redirectTo={resolveAllowedAuthRedirect(params?.redirectTo, {
          learningOrigin:
            process.env.LEARNING_PUBLIC_URL ||
            process.env.NEXT_PUBLIC_LEARNING_APP_URL ||
            'https://learning.gameguild.gg',
          webOrigin:
            process.env.WEB_PUBLIC_URL ||
            process.env.NEXT_PUBLIC_APP_URL ||
            'https://gameguild.gg',
        })}
      />
    </>
  );
}
