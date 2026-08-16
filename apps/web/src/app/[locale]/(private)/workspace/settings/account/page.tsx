import { auth, getToken } from '@/auth';
import {
  ConnectedAccountsCard,
  type LinkedAccount,
  type SettingsBanner,
} from '@/components/connected-accounts-card';
import { redirect } from '@/i18n/navigation';
import { createServerClient, GeneratedApi } from '@game-guild/client';
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
 * the linked-provider list is fetched server-side with the session's bearer
 * via the generated typed client.
 */
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
  const authModule = new GeneratedApi.AuthModule(
    createServerClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: () => getToken() },
    }),
  );

  let linkedAccounts: LinkedAccount[] = [];
  const result = await authModule.getAuthExternalLogins();
  if (result.ok) {
    linkedAccounts = result.data
      .filter((row) => row.provider === 'google' || row.provider === 'discord')
      .map((row) => ({ provider: row.provider as string, linkedAt: row.createdAt }));
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
    </div>
  );
}
