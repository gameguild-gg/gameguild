'use client';

import React, { createContext, useContext, useState } from 'react';

export interface Track {
  id: number;
  title: string;
  description: string;
  slug: string;
  area: string;
  level: number;
  tools: string[];
  estimatedHours: number;
  coursesCount: number;
  image?: string;
}

interface TracksContextType {
  tracks: Track[];
  setTracks: (tracks: Track[]) => void;
  loading: boolean;
  setLoading: (loading: boolean) => void;
}

const TracksContext = createContext<TracksContextType | undefined>(undefined);

export function TracksProvider({ children }: { children: React.ReactNode }) {
  const [tracks, setTracks] = useState<Track[]>([]);
  const [loading, setLoading] = useState(false);

  return <TracksContext.Provider value={{ tracks, setTracks, loading, setLoading }}>{children}</TracksContext.Provider>;
}

// Alias for backward compatibility
export const TrackProvider = TracksProvider;

export function useTracksContext() {
  const context = useContext(TracksContext);
  if (context === undefined) {
    throw new Error('useTracksContext must be used within a TracksProvider');
  }
  return context;
}
