'use client';

import type { Route } from 'next';
import { useRouter } from 'next/navigation';

import { getTrackProgramHref } from '@/lib/tracks/catalog';
import { useFilteredTracks } from '@/lib/tracks/use-tracks';
import { TrackCard } from './track-card';

export function TrackGrid() {
  const router = useRouter();
  const { tracks: filteredTracks } = useFilteredTracks();

  if (filteredTracks.length === 0) {
    return <div className="text-center py-10 text-muted-foreground">No tracks found matching your criteria. Try adjusting your filters.</div>;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mt-8">
      {filteredTracks.map((track) => (
        <TrackCard
          key={track.id}
          track={track}
          onClick={() => {
            router.push(getTrackProgramHref(track.slug) as Route);
          }}
        />
      ))}
    </div>
  );
}
