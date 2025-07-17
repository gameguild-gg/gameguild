'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { motion } from 'framer-motion';
import { TrackCard } from './track-card';
import { useFilteredTracks } from '@/lib/tracks/use-tracks';

export function TrackGrid() {
  const router = useRouter();
  const filteredTracks = useFilteredTracks();

  if (filteredTracks.length === 0) {
    return (
      <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ duration: 0.5 }} className="text-center py-10 text-muted-foreground">
        No tracks found matching your criteria. Try adjusting your filters.
      </motion.div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mt-8">
      {filteredTracks.map((track) => (
        <TrackCard
          key={track.id}
          track={track}
          onClick={() => {
            router.push(`/tracks/${track.slug}`);
          }}
        />
      ))}
    </div>
  );
}
