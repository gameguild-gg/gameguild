import { Suspense } from 'react';
import { getTracksData } from '@/actions/tracks';
import { TrackProvider } from '@/contexts/track-context';
import { TrackFilters } from '@/components/tracks/track-filters';
import { TrackGrid } from '@/components/tracks/track-grid';

export const dynamic = 'force-dynamic';

async function TracksContent() {
  try {
    const tracksData = await getTracksData();

    return (
      <TrackProvider initialData={tracksData}>
        <div className="container mx-auto px-4 py-8">
          <div className="text-center mb-12">
            <h1 className="text-4xl font-bold mb-4 text-primary">Explore Game Development Tracks</h1>
            <p className="text-xl text-muted-foreground">Discover comprehensive learning paths in game development.</p>
          </div>

          <Suspense fallback={<div className="text-center py-10">Loading tracks...</div>}>
            <TrackFilters />
            <TrackGrid />
          </Suspense>
        </div>
      </TrackProvider>
    );
  } catch (error) {
    console.error('Error loading tracks:', error);
    return <div className="text-center py-10 text-red-500">Failed to load tracks. Please try again later.</div>;
  }
}

export default function TracksPage() {
  return (
    <div className="min-h-screen">
      <TracksContent />
    </div>
  );
}
