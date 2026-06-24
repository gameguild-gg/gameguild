import React from 'react';
import { SignupForm } from "@/components/signup-form"

function resolveRedirectTo(value: unknown): string {
  const redirectTo = typeof value === 'string' ? value : '';

  if (!redirectTo.startsWith('/') || redirectTo.startsWith('//')) {
    return '/dashboard';
  }

  return redirectTo;
}

export default async function Page({
  searchParams,
}: PageProps<'/[locale]/sign-up'>): Promise<React.JSX.Element> {
  const params = await searchParams;
  return <SignupForm redirectTo={resolveRedirectTo(params?.redirectTo)} />;
}
