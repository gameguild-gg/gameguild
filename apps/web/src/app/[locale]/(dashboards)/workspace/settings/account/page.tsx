import { auth, getToken } from '@/auth';
import {
  ConnectedAccountsCard,
  type LinkedAccount,
  type SettingsBanner,
} from '@/components/connected-accounts-card';
import { PasswordCard } from '@/components/password-card';
import { redirect } from '@/i18n/navigation';
import { createServerClient } from '@game-guild/client';
import { getTranslations } from 'next-intl/server';
import React from 'react';

// Must live server-side: a 'use client' export would arrive here as a
// client-reference proxy whose lookups silently return undefined.
const ERROR_BANNER_KEYS: Record<
  string,
  'conflict' | 'lastSignInMethod' | 'stateMismatch' | 'generic'
> = {
  conflict: 'conflict',
  'last-method': 'lastSignInMethod',
  state_mismatch: 'stateMismatch',
};

/**
 * Account settings — Connected Accounts card for Google + Discord link /
 * unlink. Authentication is guarded here (mirroring the dashboard layout) and
 * the linked-provider metadata is fetched server-side with the session's
 * bearer via HEAD /v1/auth/external-logins (no body; providers arrive in the
 * X-Linked-Providers header as 'provider=iso-timestamp' pairs).
 */
function parseLinkedProviders(header: string): LinkedAccount[] {
  return header
    .split(',')
    .filter(Boolean)
    .map((pair) => {
      const [provider, linkedAt] = pair.split('=');
      return { provider, linkedAt: linkedAt ?? '' };
    })
    .filter((row) => row.provider !== '' && row.linkedAt !== '');
}
export default async function AccountSettingsPage({
  params,
  searchParams,
}: { params: Promise<{ locale: string }>; searchParams: Promise<{ linked?: string; error?: string }> }): Promise<React.JSX.Element> {
  const [{ locale }, query] = await Promise.all([params, searchParams]);
  const t = await getTranslations({ locale, namespace: 'settings' });

  const session = await auth();
  if (!session || typeof session === 'function') {
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: '/workspace/settings/account' } }, locale });
    throw new Error('Unauthenticated dashboard access');
  }

  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });

  let linkedAccounts: LinkedAccount[] = [];
  const result = await client.requestRaw<void>({
    method: 'HEAD',
    path: '/v1/auth/external-logins',
    requiresAuth: true,
  });
  if (result.ok) {
    linkedAccounts = parseLinkedProviders(
      result.data.headers.get('x-linked-providers') ?? '',
    ).filter((row) => row.provider === 'google' || row.provider === 'discord');
  }

  let banner: SettingsBanner = null;
  if (query?.linked === 'discord') {
    banner = { kind: 'linked', provider: 'discord' };
  } else if (typeof query?.error === 'string') {
    const code = ERROR_BANNER_KEYS[query.error];
    if (code) banner = { kind: 'error', code };
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t('accountTitle')}</h1>
        <p className="text-muted-foreground">{t('accountDescription')}</p>
      </header>
      <ConnectedAccountsCard linkedAccounts={linkedAccounts} banner={banner} />
      <PasswordCard />
    </div>
  );
}
