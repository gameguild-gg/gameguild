'use server';

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

export async function getTrackBySlug(slug: string): Promise<Track | null> {
  // Mock implementation - replace with actual API call
  console.log(`Getting track by slug: ${slug}`);

  const mockTrack: Track = {
    id: 1,
    title: 'Sample Track',
    description: 'A sample track description',
    slug,
    area: 'programming',
    level: 1,
    tools: ['Unity', 'C#'],
    estimatedHours: 40,
    coursesCount: 5,
  };

  return mockTrack;
}

export async function getTracks(): Promise<Track[]> {
  // Mock implementation - replace with actual API call
  console.log('Getting all tracks');
  return [];
}
