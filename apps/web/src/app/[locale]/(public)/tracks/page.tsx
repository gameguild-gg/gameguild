import { redirect } from '@/i18n/navigation';

export default async function TracksPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  redirect({ href: '/courses', locale });
}
