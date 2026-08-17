import { redirect } from '@/i18n/navigation';
import React from 'react';

/** The feed lives at `/` for signed-in members; old /feed links forward there. */
export default async function LegacyFeedRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect({ href: '/', locale });
  throw new Error('unreachable');
}
