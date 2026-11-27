import React from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import { AuthErrorNotice } from '@/components/auth/auth-error-notice';
import { VerifyPageContent, type VerifyPageState } from '@/components/auth/verify-page-content';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';
import { verifyEmailToken } from '@/lib/auth/email-verification';

export default async function Page({
  params,
  searchParams,
}: PageProps<'/[locale]/verify'>): Promise<React.JSX.Element> {
  const [, query] = await Promise.all([params, searchParams]);
  const errorCode = typeof query?.error === 'string' ? query.error : undefined;
  const token = typeof query?.token === 'string' ? query.token : '';
  const email = typeof query?.email === 'string' ? query.email.trim() : '';
  const redirectTo = resolveAllowedAuthRedirect(
    typeof query?.redirectTo === 'string' ? query.redirectTo : undefined,
  );

  // #1298 J1: the API's artifact is the token inside the emailed link, so the
  // link is the primary way in. The token is consumed here, before anything
  // renders, so opening the link is all the visitor has to do; the outcome
  // decides what happens next.
  let state: VerifyPageState;
  if (token) {
    const verification = await verifyEmailToken(token);
    if (verification.ok) {
      // The token is single-use and spent: land on an address that no longer
      // carries it, so it stays out of the address bar and of anything that
      // reads the page URL, and a reload never replays it.
      redirect(`/verify?${new URLSearchParams({ verified: '1' })}`);
    }
    state = { kind: 'failed', email };
  } else if (query?.verified === '1') {
    // Where a successful verification lands (above). It only re-renders the
    // success message; it changes nothing on its own.
    state = { kind: 'verified', email, signInAgain: query?.signIn === '1' };
  } else if (email) {
    // Sign-up sends the visitor here with `new`, an interrupted sign-in with
    // `signIn`; anything else is a return visit.
    const origin =
      query?.new === '1' ? 'sign-up' : query?.signIn === '1' ? 'sign-in' : 'returning';
    state = { kind: 'pending', email, origin };
  } else {
    redirect('/sign-in');
  }

  const session = await auth();
  return (
    <>
      <AuthErrorNotice errorCode={errorCode} />
      <VerifyPageContent state={state} redirectTo={redirectTo} signedIn={session !== null} />
    </>
  );
}
