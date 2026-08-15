import { redirect } from 'next/navigation';
import React from 'react';

/**
 * Legacy destination for OAuth sign-in errors.
 *
 * Errors now render inline on the auth routes themselves (see
 * `AuthErrorNotice`), so this page only exists to keep old links and
 * bookmarks working: it forwards the error code to /sign-in, which
 * displays it inline above the form.
 */
export default async function AuthErrorLegacyRedirectPage({
  params,
  searchParams,
}: PageProps<'/[locale]/auth-error'>): Promise<never> {
  const [{ locale }, query] = await Promise.all([params, searchParams]);
  const errorCode = typeof query?.error === 'string' ? query.error : '';
  const target = errorCode
    ? `/${locale}/sign-in?error=${encodeURIComponent(errorCode)}`
    : `/${locale}/sign-in`;
  redirect(target);
}
