import { AppearanceForm } from '@/components/settings/appearance-form';
import { getGeneralPreferences } from '@/lib/user-settings/queries';
import { getTranslations } from 'next-intl/server';
import React from 'react';

export default async function AppearanceSettingsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<React.JSX.Element> {
  const { locale } = await params;
  const [t, preferences] = await Promise.all([
    getTranslations({ locale, namespace: 'settings.appearance' }),
    getGeneralPreferences(),
  ]);

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
        <p className="text-muted-foreground">{t('description')}</p>
      </header>
      <AppearanceForm initialTheme={preferences.theme} />
    </div>
  );
}
