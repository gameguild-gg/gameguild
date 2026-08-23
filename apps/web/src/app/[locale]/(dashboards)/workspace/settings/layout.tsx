import { SettingsNav } from '@/components/settings/settings-nav';
import { getTranslations } from 'next-intl/server';
import React from 'react';

/**
 * User Settings Hub Layout
 *
 * Shared shell for all user-level settings sections:
 * - /settings (redirect → /settings/profile)
 * - /settings/profile — display name, bio, links
 * - /settings/account — connected accounts (OAuth link/unlink)
 * - /settings/appearance — theme (synced to server preferences)
 * - /settings/localization — language, timezone, formats, currency
 * - /settings/privacy — profile visibility and data sharing
 * - /settings/accessibility — display and interaction preferences
 */
export default async function SettingsLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}): Promise<React.JSX.Element> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: 'settings' });

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-6 lg:flex-row lg:gap-10">
      <aside className="lg:w-56 lg:shrink-0">
        <h2 className="mb-3 px-3 text-sm font-semibold tracking-wide text-muted-foreground uppercase lg:px-3">
          {t('hubTitle')}
        </h2>
        <SettingsNav />
      </aside>
      <div className="min-w-0 flex-1 space-y-6">{children}</div>
    </div>
  );
}
