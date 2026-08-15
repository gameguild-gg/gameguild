import { redirect } from 'next/navigation';
import React from 'react';

/**
 * Legacy /dashboard/community/testing-lab/* redirect. The Testing Lab console moved
 * under Community Management (/dashboard/community/testing-lab/*).
 */
export default async function LegacyTestingLabRedirectPage({
  params,
}: {
  params: Promise<{ locale: string; path?: string[] }>;
}): Promise<never> {
  const { locale, path } = await params;
  const segments = Array.isArray(path) ? path.map((segment) => encodeURIComponent(segment)) : [];
  const suffix = segments.length ? `/${segments.join('/')}` : '';
  redirect(`/${locale}/dashboard/community/testing-lab${suffix}`);
}
