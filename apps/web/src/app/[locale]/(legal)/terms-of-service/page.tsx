import { redirect } from 'next/navigation';
import React from 'react';

export default async function LegacyTermsOfServiceRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/legal/terms-of-service`);
}
