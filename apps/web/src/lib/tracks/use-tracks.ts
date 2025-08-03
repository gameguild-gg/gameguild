'use client';

import { useState, useEffect, useMemo } from 'react';

export interface Track {
  id: number;
  title: string;
  description: string;
  area: string;
  level: number;
  tools: string[];
  estimatedHours: number;
  coursesCount: number;
  image?: string;
  slug: string;
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
      // Mock tracks data - replace with actual API call
      const mockTracks: Track[] = [
        {
          id: 1,
          title: 'Game Programming Fundamentals',
          description: 'Learn the core programming concepts for game development.',
          area: 'programming',
          level: 1,
          tools: ['Unity', 'C#', 'Visual Studio'],
          estimatedHours: 120,
          coursesCount: 8,
          slug: 'game-programming-fundamentals',
        },
        {
          id: 2,
          title: 'Digital Art for Games',
          description: 'Master digital art techniques for game assets.',
          area: 'art',
          level: 2,
          tools: ['Photoshop', 'Illustrator', 'Blender'],
          estimatedHours: 80,
          coursesCount: 6,
          slug: 'digital-art-for-games',
        },
        {
          id: 3,
          title: 'Game Design Principles',
          description: 'Understand the principles of effective game design.',
          area: 'design',
          level: 1,
          tools: ['Figma', 'Miro', 'GameMaker'],
          estimatedHours: 60,
          coursesCount: 5,
          slug: 'game-design-principles',
        },
      ];

      setTracks(mockTracks);

      // Extract unique tools from all tracks
      const tools = [...new Set(mockTracks.flatMap((track) => track.tools))];
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
