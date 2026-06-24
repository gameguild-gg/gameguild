import React from 'react';
import { SignInForm } from '@/components/sign-in-form';

function resolveRedirectTo(value: unknown): string {
  const redirectTo = typeof value === 'string' ? value : '';

  if (!redirectTo.startsWith('/') || redirectTo.startsWith('//')) {
    return '/dashboard';
  }

  return redirectTo;
}

export default async function Page({
  searchParams,
}: PageProps<'/[locale]/sign-in'>): Promise<React.JSX.Element> {
  const params = await searchParams;
  return <SignInForm redirectTo={resolveRedirectTo(params?.redirectTo)} />;
}
