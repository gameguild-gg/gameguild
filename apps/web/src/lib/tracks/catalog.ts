export interface TrackCatalogItem {
  id: number;
  title: string;
  description: string;
  slug: string;
  area: 'programming' | 'art' | 'design';
  level: number;
  tools: string[];
  estimatedHours: number;
  coursesCount: number;
  knowledges: string[];
  image?: string;
}

export const TRACK_CATALOG: TrackCatalogItem[] = [
  {
    id: 1,
    title: 'Game Programming Fundamentals',
    description: 'Build reliable gameplay systems, engine-facing code, and production-ready game features.',
    slug: 'game-programming-fundamentals',
    area: 'programming',
    level: 1,
    tools: ['Unity', 'C#', 'Git', 'Visual Studio'],
    estimatedHours: 120,
    coursesCount: 8,
    knowledges: ['Game Loop', 'Gameplay Architecture', 'Debugging', 'Build Pipelines'],
  },
  {
    id: 2,
    title: 'Digital Art for Games',
    description: 'Create polished game assets, environment art, and visual production pipelines.',
    slug: 'digital-art-for-games',
    area: 'art',
    level: 2,
    tools: ['Blender', 'Photoshop', 'Substance Painter', 'Aseprite'],
    estimatedHours: 96,
    coursesCount: 6,
    knowledges: ['Shape Language', 'Texturing', 'Lighting', 'Asset Delivery'],
  },
  {
    id: 3,
    title: 'Game Design Principles',
    description: 'Design engaging systems, levels, economies, and player experiences with measurable feedback loops.',
    slug: 'game-design-principles',
    area: 'design',
    level: 1,
    tools: ['Figma', 'Miro', 'Unity', 'Google Sheets'],
    estimatedHours: 72,
    coursesCount: 5,
    knowledges: ['Game Mechanics', 'Level Design', 'Player Feedback', 'Balancing'],
  },
];

export function getTrackCatalogItem(slug: string): TrackCatalogItem | null {
  return TRACK_CATALOG.find((track) => track.slug === slug) ?? null;
}

const TRACK_PROGRAM_SLUGS: Record<string, string> = {
  'game-programming-fundamentals': 'game-programming-foundations',
};

export function getTrackProgramHref(slug: string): string {
  const programSlug = TRACK_PROGRAM_SLUGS[slug];
  return programSlug ? `/programs/${programSlug}` : '/programs';
}
