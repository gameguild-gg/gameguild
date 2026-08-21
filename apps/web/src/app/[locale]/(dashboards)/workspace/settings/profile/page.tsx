import { auth } from '@/auth';
import { ProfileForm } from '@/components/settings/profile-form';
import { getProfile } from '@/lib/user-settings/queries';
import { getTranslations } from 'next-intl/server';
import React from 'react';

export default async function ProfileSettingsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<React.JSX.Element> {
  const { locale } = await params;
  const [t, session, profile] = await Promise.all([
    getTranslations({ locale, namespace: 'settings.profile' }),
    auth(),
    getProfile(),
  ]);
  const accountName = session && typeof session !== 'function'
    ? session.user.name?.trim() || session.user.email?.split('@')[0] || t('fallbackName')
    : t('fallbackName');
  const accountEmail = session && typeof session !== 'function'
    ? session.user.email ?? t('fallbackEmail')
    : t('fallbackEmail');

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
        <p className="text-muted-foreground">{t('description')}</p>
      </header>
      <ProfileForm
        accountName={accountName}
        accountEmail={accountEmail}
        defaultValues={{
          displayName: profile?.displayName ?? accountName,
          bio: profile?.bio ?? '',
          location: profile?.location ?? '',
          website: profile?.website ?? '',
          jobTitle: profile?.jobTitle ?? '',
          company: profile?.company ?? '',
        }}
      />
    </div>
  );
}
