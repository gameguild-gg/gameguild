import { redirect } from 'next/navigation';
import React from 'react';

/**
 * Legacy /dashboard/platform/learning/* redirect. The learning console moved under
 * Platform Management (/dashboard/platform/learning/*).
 */
export default async function LegacyLearningRedirectPage({
  params,
}: {
  params: Promise<{ locale: string; path?: string[] }>;
}): Promise<never> {
  const { locale, path } = await params;
  const segments = Array.isArray(path) ? path.map((segment) => encodeURIComponent(segment)) : [];
  const suffix = segments.length ? `/${segments.join('/')}` : '';
  redirect(`/${locale}/dashboard/platform/learning${suffix}`);
}
