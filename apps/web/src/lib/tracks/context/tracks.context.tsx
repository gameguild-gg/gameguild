'use client';

import React, { createContext, useContext, useReducer, ReactNode } from 'react';
import { Track, TracksData } from '@/types/tracks';

// Track filter state
export interface TrackState {
  tracks: Track[];
  filteredTracks: Track[];
  area: string;
  tool: string;
  level: string;
  searchTerm: string;
  toolsByArea: Record<string, string[]>;
  availableTools: string[];
  isLoading: boolean;
  error: string | null;
}

// Track actions
export type TrackAction =
  | { type: 'SET_TRACKS'; payload: TracksData }
  | { type: 'SET_AREA'; payload: string }
  | { type: 'SET_TOOL'; payload: string }
  | { type: 'SET_LEVEL'; payload: string }
  | { type: 'SET_SEARCH_TERM'; payload: string }
  | { type: 'SET_AVAILABLE_TOOLS'; payload: string[] }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null };

// Initial state
const initialState: TrackState = {
  tracks: [],
  filteredTracks: [],
  area: 'all',
  tool: 'all',
  level: 'all',
  searchTerm: '',
  toolsByArea: {},
  availableTools: [],
  isLoading: false,
  error: null,
};

// Reducer function
function trackReducer(state: TrackState, action: TrackAction): TrackState {
  switch (action.type) {
    case 'SET_TRACKS':
      return {
        ...state,
        tracks: action.payload.tracks,
        toolsByArea: action.payload.toolsByArea,
        filteredTracks: action.payload.tracks,
      };
    case 'SET_AREA':
      return { ...state, area: action.payload };
    case 'SET_TOOL':
      return { ...state, tool: action.payload };
    case 'SET_LEVEL':
      return { ...state, level: action.payload };
    case 'SET_SEARCH_TERM':
      return { ...state, searchTerm: action.payload };
    case 'SET_AVAILABLE_TOOLS':
      return { ...state, availableTools: action.payload };
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    case 'SET_ERROR':
      return { ...state, error: action.payload };
    default:
      return state;
  }
}

// Context
const TrackContext = createContext<{
  state: TrackState;
  dispatch: React.Dispatch<TrackAction>;
} | null>(null);

// Provider component
export function TrackProvider({ children, initialData }: { children: ReactNode; initialData?: TracksData }) {
  const [state, dispatch] = useReducer(trackReducer, {
    ...initialState,
    ...(initialData && {
      tracks: initialData.tracks,
      toolsByArea: initialData.toolsByArea,
      filteredTracks: initialData.tracks,
    }),
  });

  return <TrackContext.Provider value={{ state, dispatch }}>{children}</TrackContext.Provider>;
}

// Hook to use track context
export function useTrackContext() {
  const context = useContext(TrackContext);
  if (!context) {
    throw new Error('useTrackContext must be used within a TrackProvider');
  }
  return context;
}
