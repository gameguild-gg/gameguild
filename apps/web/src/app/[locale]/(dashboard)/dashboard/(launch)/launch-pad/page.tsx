import { redirect } from 'next/navigation';
import React from 'react';

export default async function LegacyLaunchPadRedirectPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/dashboard/community/launch-pad`);
}
