'use client';

import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import { linkGoogleAccount } from '@/lib/auth/external-logins-actions';
import { useTranslations } from 'next-intl';
import {
  useGoogleIdentityService,
  type GisRenderButtonOptions,
} from './use-google-identity-service';

const DEFAULT_BUTTON_OPTIONS: GisRenderButtonOptions = {
  type: 'standard',
  theme: 'outline',
  size: 'large',
  // 'continue_with' is the closest GIS label for account linking — there is
  // no "link_with" text in the GIS button catalog.
  text: 'continue_with',
  shape: 'rectangular',
  width: 300,
};

/**
 * Link-mode Google Identity Services button for the Connected Accounts card.
 *
 * CRITICAL (plan M1): unlike GoogleSignInButton, the credential callback
 * routes the ID token to the `linkGoogleAccount` SERVER ACTION — never to
 * signIn("google"), which would re-issue/clobber the current session.
 */
export function GoogleLinkButton() {
  const t = useTranslations('connectedAccounts');
  const containerRef = useRef<HTMLDivElement>(null);
  const [pending, setPending] = useState(false);
  const { status, renderButton } = useGoogleIdentityService({
    onCredential: (credential) => {
      // credential is an untrusted ID token — the backend verifies it and
      // attaches the Google identity to the CURRENT session's user.
      setPending(true);
      linkGoogleAccount(credential)
        .then((result) => {
          if (result.success) {
            toast.success(t('google.linkSuccess'));
          } else if (result.status === 'conflict') {
            toast.error(t('errors.conflict'));
          } else {
            toast.error(t('errors.generic'));
          }
        })
        .catch(() => toast.error(t('errors.generic')))
        .finally(() => setPending(false));
    },
  });

  useEffect(() => {
    if (status === 'ready' && containerRef.current) {
      renderButton(containerRef.current, DEFAULT_BUTTON_OPTIONS);
    }
  }, [status, renderButton]);

  if (status === 'error') {
    return (
      <div role="alert" className="text-sm text-destructive">
        {t('google.unavailable')}
      </div>
    );
  }

  return (
    <div
      ref={containerRef}
      data-testid="google-link-button"
      aria-busy={pending}
      // Reserve layout while GIS hydrates the button to prevent CLS.
      style={{ minHeight: 40, display: 'flex', justifyContent: 'center' }}
    />
  );
}
