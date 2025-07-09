'use client';

import { useEffect, useMemo } from 'react';
import { useTrackContext } from '@/contexts/track-context';

export function useFilteredTracks() {
  const { state, dispatch } = useTrackContext();

  // Update available tools when area changes
  useEffect(() => {
    if (state.area === 'all') {
      const allTools = Object.values(state.toolsByArea).flat();
      dispatch({ type: 'SET_AVAILABLE_TOOLS', payload: allTools });
    } else {
      const areaTools = state.toolsByArea[state.area] || [];
      dispatch({ type: 'SET_AVAILABLE_TOOLS', payload: areaTools });
    }
    dispatch({ type: 'SET_TOOL', payload: 'all' });
  }, [state.area, state.toolsByArea, dispatch]);

  // Memoized filtered tracks
  const filteredTracks = useMemo(() => {
    return state.tracks.filter(
      (track) =>
        (state.area === 'all' || track.area === state.area) &&
        (state.tool === 'all' || track.tools.includes(state.tool)) &&
        (state.level === 'all' || track.level === state.level) &&
        (!state.searchTerm ||
          track.title.toLowerCase().includes(state.searchTerm.toLowerCase()) ||
          track.description.toLowerCase().includes(state.searchTerm.toLowerCase())),
    );
  }, [state.area, state.tool, state.level, state.searchTerm, state.tracks]);

  return filteredTracks;
}

export function useTrackFilters() {
  const { state, dispatch } = useTrackContext();

  const setArea = (area: string) => dispatch({ type: 'SET_AREA', payload: area });
  const setTool = (tool: string) => dispatch({ type: 'SET_TOOL', payload: tool });
  const setLevel = (level: string) => dispatch({ type: 'SET_LEVEL', payload: level });
  const setSearchTerm = (searchTerm: string) => dispatch({ type: 'SET_SEARCH_TERM', payload: searchTerm });

  return {
    area: state.area,
    tool: state.tool,
    level: state.level,
    searchTerm: state.searchTerm,
    availableTools: state.availableTools,
    setArea,
    setTool,
    setLevel,
    setSearchTerm,
  };
}
