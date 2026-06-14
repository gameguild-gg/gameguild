import { getTrackProgramHref, TRACK_CATALOG } from '@/lib/tracks/catalog';
import { redirect } from '@/i18n/navigation';

interface TrackDetailPageProps {
  readonly params: Promise<{ locale: string; track: string }>;
}

export function generateStaticParams() {
  return TRACK_CATALOG.map((track) => ({ track: track.slug }));
}

export default async function TrackDetailPage({ params }: TrackDetailPageProps) {
  const { locale, track } = await params;
  redirect({ href: getTrackProgramHref(track), locale });
}
