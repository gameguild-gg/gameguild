import { redirect } from '@/i18n/navigation';

interface TracksPageProps {
  readonly params: Promise<{ locale: string }>;
}

export default async function TracksPage({ params }: TracksPageProps) {
  const { locale } = await params;
  redirect({ href: '/programs', locale });
}
