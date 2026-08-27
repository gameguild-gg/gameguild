import { redirect } from 'next/navigation';
import React from 'react';

// /login is not a route in this app: the auth entry is /sign-in. Browsers
// and old bookmarks still guess /login, so redirect instead of 404.
export default async function LoginRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/sign-in`);
}
