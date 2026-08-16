import { redirect } from 'next/navigation';

export default async function LegacyPlatformLearningPage({ params }: { params: Promise<{ locale: string }> }): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/workspace/learning`);
}
