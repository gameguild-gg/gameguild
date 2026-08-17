import { redirect } from 'next/navigation';

export default async function LegacyDashboardHomePage({ params }: { params: Promise<{ locale: string }> }): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/workspace`);
}
