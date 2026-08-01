import React from 'react';
import { SignInForm } from '@/components/sign-in-form';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';

export default async function Page({
  searchParams,
}: PageProps<'/[locale]/sign-in'>): Promise<React.JSX.Element> {
  const params = await searchParams;
  return (
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
  );
}
