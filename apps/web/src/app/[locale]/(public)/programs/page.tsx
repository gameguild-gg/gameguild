import { redirect } from '@/i18n/navigation';
import React from 'react';

/**
 * Programs are the same entity as courses: the unified catalog lives at
 * /courses and renders the program packages view via ?type=program.
 */
export default async function LegacyProgramsRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect({ href: { pathname: '/courses', query: { type: 'program' } }, locale });
  throw new Error('unreachable');
}
