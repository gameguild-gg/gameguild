import { redirect } from '@/i18n/navigation';
import React from 'react';

/** The settings hub root redirects to the profile section. */
export default async function SettingsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<React.JSX.Element> {
  const { locale } = await params;

  redirect({ href: '/workspace/settings/profile', locale });
  throw new Error('unreachable');
}
