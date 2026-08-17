import { redirect } from '@/i18n/navigation';
import React from 'react';

export default async function LegacyProgramsRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect({ href: '/courses', locale });
  throw new Error('unreachable');
}
