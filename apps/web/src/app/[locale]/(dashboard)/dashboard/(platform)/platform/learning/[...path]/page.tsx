import { redirect } from 'next/navigation';

export default async function LegacyPlatformLearningSectionPage({ params }: { params: Promise<{ locale: string; path: string[] }> }): Promise<never> {
  const { locale, path } = await params;
  const suffix = path.map((segment) => encodeURIComponent(segment)).join('/');
  redirect(`/${locale}/workspace/learning/${suffix}`);
}
