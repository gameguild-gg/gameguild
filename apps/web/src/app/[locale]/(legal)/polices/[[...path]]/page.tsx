import { redirect } from 'next/navigation';
import React from 'react';

/** Legacy /polices (typo) and /polices/* links forward to /legal/*. */
export default async function LegacyPolicesRedirectPage({
  params,
}: {
  params: Promise<{ locale: string; path?: string[] }>;
}): Promise<never> {
  const { locale, path } = await params;
  const segments = Array.isArray(path) ? path : [];
  const target = segments.length ? `/${locale}/legal/${segments.join('/')}` : `/${locale}/legal`;
  redirect(target);
}
