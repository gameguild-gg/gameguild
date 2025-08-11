export interface Track {
  id: string;
  title: string;
  description: string;
  area: string;
  tools: string[];
  image: string;
  knowledges: string[];
  obtained?: string;
  level: string;
  progress?: number;
  slug: string; // Made required since we use it for routing
}

export interface ToolsByArea extends Record<string, string[]> {
  programming: string[];
  art: string[];
  design: string[];
}

export interface TracksData {
  tracks: Track[];
  toolsByArea: ToolsByArea;
}

export type TrackArea = 'programming' | 'art' | 'design';

export type TrackLevel = '0' | '1' | '2' | '3';

export const TRACK_LEVELS = {
  '0': 'Postulant',
  '1': 'Apprentice',
  '2': 'Partner',
  '3': 'Master',
} as const;

export const TRACK_LEVEL_COLORS = {
  '0': 'bg-gray-500',
  '1': 'bg-green-500',
  '2': 'bg-blue-500',
  '3': 'bg-purple-500',
} as const;
