import { redirect } from 'next/navigation';
import React from 'react';

export default async function LegacyFerpaWaiverRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/legal/ferpa-waiver`);
}
