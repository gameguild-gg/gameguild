'use server';

import { getTrackCatalogItem, TRACK_CATALOG } from '../catalog';

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
  knowledges: string[];
  image?: string;
  obtained?: string;
  progress?: number;
}

export async function getTrackBySlug(slug: string): Promise<Track | null> {
  return getTrackCatalogItem(slug);
}

export async function getTracks(): Promise<Track[]> {
  return TRACK_CATALOG;
}
