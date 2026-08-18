import { redirect } from 'next/navigation';
import React from 'react';

export default async function LegacyLicensesRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/legal/licenses`);
}
