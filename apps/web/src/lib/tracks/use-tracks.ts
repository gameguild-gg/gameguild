'use client';

import { useEffect, useMemo, useState } from 'react';
import { TRACK_CATALOG } from './catalog';

export interface Track {
  id: number | string;
  title: string;
  description: string;
  area: string;
  level: number | string;
  tools: string[];
  estimatedHours?: number;
  coursesCount?: number;
  image?: string;
  slug: string;
  knowledges: string[];
  obtained?: string;
  progress?: number;
}

export interface TrackFilters {
  area: string;
  tool: string;
  level: string;
  searchTerm: string;
}

/**
 * Hook for managing track filters and data
 */
export function useTrackFilters() {
  const [area, setArea] = useState('all');
  const [tool, setTool] = useState('all');
  const [level, setLevel] = useState('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [tracks, setTracks] = useState<Track[]>([]);
  const [availableTools, setAvailableTools] = useState<string[]>([]);

  // Load tracks data
  useEffect(() => {
    const loadTracks = async () => {
      const catalogTracks: Track[] = TRACK_CATALOG;

      setTracks(catalogTracks);

      // Extract unique tools from all tracks
      const tools = [...new Set(catalogTracks.flatMap((track) => track.tools))];
      setAvailableTools(tools);
    };

    loadTracks();
  }, []);

  // Filter tracks based on current filters
  const filteredTracks = useMemo(() => {
    let filtered = tracks;

    if (area !== 'all') {
      filtered = filtered.filter((track) => track.area === area);
    }

    if (tool !== 'all') {
      filtered = filtered.filter((track) => track.tools.includes(tool));
    }

    if (level !== 'all') {
      filtered = filtered.filter((track) => track.level.toString() === level);
    }

    if (searchTerm) {
      const search = searchTerm.toLowerCase();
      filtered = filtered.filter((track) => track.title.toLowerCase().includes(search) || track.description.toLowerCase().includes(search));
    }

    return filtered;
  }, [tracks, area, tool, level, searchTerm]);

  return {
    area,
    tool,
    level,
    searchTerm,
    tracks: filteredTracks,
    availableTools,
    setArea,
    setTool,
    setLevel,
    setSearchTerm,
  };
}

// Alias for backward compatibility
export const useFilteredTracks = useTrackFilters;
