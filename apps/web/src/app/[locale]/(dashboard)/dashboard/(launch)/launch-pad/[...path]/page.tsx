import { redirect } from 'next/navigation';
import React from 'react';

/**
 * Legacy /dashboard/community/launch-pad/* redirect. The Launch Pad console moved
 * under Community Management (/dashboard/community/launch-pad/*).
 */
export default async function LegacyLaunchPadRedirectPage({
  params,
}: {
  params: Promise<{ locale: string; path?: string[] }>;
}): Promise<never> {
  const { locale, path } = await params;
  const segments = Array.isArray(path) ? path.map((segment) => encodeURIComponent(segment)) : [];
  const suffix = segments.length ? `/${segments.join('/')}` : '';
  redirect(`/${locale}/dashboard/community/launch-pad${suffix}`);
}
