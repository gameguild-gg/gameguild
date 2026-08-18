import { redirect } from '@/i18n/navigation';

export default async function TrackDetailPage({ params }: { params: Promise<{ locale: string; track: string }> }) {
  const { locale } = await params;
  redirect({ href: '/courses', locale });
}
