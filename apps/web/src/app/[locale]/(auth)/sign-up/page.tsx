import React from 'react';
import { getTranslations } from 'next-intl/server';
import { SignupForm } from "@/components/signup-form"
import { GoogleOneTap } from '@/components/google-one-tap';
import { GoogleSignInButton } from '@/components/google-sign-in-button';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';
import { auth } from '@/auth';
import { AuthErrorNotice } from '@/components/auth/auth-error-notice';

const GUEST_SESSION_ID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export default async function Page({
  searchParams,
}: PageProps<'/[locale]/sign-up'>): Promise<React.JSX.Element> {
  const [params, t] = await Promise.all([searchParams, getTranslations('signIn')]);
  const session = await auth();
  const redirectTo = resolveAllowedAuthRedirect(params?.redirectTo);
  const errorCode = typeof params?.error === 'string' ? params.error : undefined;
  // Guest upgrade entry (issue #213): the guest banner's CTA routes here with
  // the guest-session id; the form presents the matching guest bearer so the
  // backend upgrade links the new account to that session
  // (UpgradedFromGuestSessionId). Anything that is not a GUID is ignored.
  const guestSessionId =
    typeof params?.guestSessionId === 'string' &&
    GUEST_SESSION_ID_PATTERN.test(params.guestSessionId)
      ? params.guestSessionId
      : undefined;

  return (
    <>
      <GoogleOneTap authenticated={Boolean(session)} redirectTo={redirectTo} />
      <AuthErrorNotice errorCode={errorCode} />
      <SignupForm
        providers={<GoogleSignInButton options={{ text: 'signup_with' }} redirectTo={redirectTo} />}
        redirectTo={redirectTo}
        guestSessionId={guestSessionId}
        guestUpgradeNotice={
          guestSessionId ? t('guest.signUpNotice') : undefined
        }
      />
    </>
  );
}
