'use server';

import { ToolsByArea, Track, TracksData } from '@/components/legacy/types/tracks';

// Helper function to map Program API categories to track areas
function mapProgramCategoryToTrackArea(category: number): string {
  // Based on ProgramCategory enum from API:
  // Programming = 0, GameDevelopment = 4, Design = 10
  const categoryMap: { [key: number]: string } = {
    0: 'programming', // Programming
    4: 'programming', // GameDevelopment (can be programming)
    10: 'design', // Design
    14: 'art', // CreativeArts
  };
  return categoryMap[category] || 'programming';
}

// Helper function to map Program API difficulty to track levels
function mapProgramDifficultyToTrackLevel(difficulty: number): string {
  // Based on ProgramDifficulty enum: Beginner = 0, Intermediate = 1, Advanced = 2, Expert = 3
  const levelMap: { [key: number]: string } = {
    0: '1', // Beginner
    1: '2', // Intermediate
    2: '3', // Advanced
    3: '4', // Expert
  };
  return levelMap[difficulty] || '1';
}

// Helper function to extract tools from program (placeholder implementation)
function extractToolsFromProgram(program: { category: number; [key: string]: unknown }): string[] {
  // This could be enhanced to extract tools from program metadata, tags, or content
  // For now, return default tools based on category
  const category = mapProgramCategoryToTrackArea(program.category);
  const defaultTools: { [key: string]: string[] } = {
    programming: ['unity', 'c++', 'unreal-engine'],
    art: ['blender', 'maya', 'photoshop'],
    design: ['unity', 'figma', 'sketch'],
  };
  return defaultTools[category] || ['unity'];
}

// Helper function to sanitize image paths
function sanitizeImagePath(thumbnail: string | null | undefined): string {
  if (!thumbnail) return '/placeholder.svg';

  // If the thumbnail path starts with /images/ but the image doesn't exist, use placeholder
  if (thumbnail.startsWith('/images/')) {
    return '/placeholder.svg';
  }

  return thumbnail;
}

function getFallbackTracksData(): TracksData {
  return {
    tracks: [
      {
        id: 'fallback-web-dev-track',
        title: 'Web Development Track',
        description: 'Learn the fundamentals of web development from HTML/CSS to modern frameworks.',
        area: 'programming',
        tools: ['HTML', 'CSS', 'JavaScript', 'React', 'Node.js'],
        image: '/images/tracks/web-development.jpg',
        knowledges: ['HTML Fundamentals', 'CSS Styling', 'JavaScript Basics', 'React Framework', 'Backend Development'],
        obtained: '0',
        level: 'beginner',
        slug: 'web-development-track',
      },
      {
        id: 'fallback-game-dev-track',
        title: 'Game Development Track',
        description: 'Create games from concept to completion using industry-standard tools and techniques.',
        area: 'programming',
        tools: ['Unity', 'C#', 'Blender', 'Photoshop'],
        image: '/images/tracks/game-development.jpg',
        knowledges: ['Game Design', 'Unity Engine', 'C# Programming', '3D Modeling', 'Game Assets'],
        obtained: '0',
        level: 'intermediate',
        slug: 'game-development-track',
      },
      {
        id: 'fallback-design-track',
        title: 'UI/UX Design Track',
        description: 'Master the art of user interface and user experience design.',
        area: 'design',
        tools: ['Figma', 'Adobe XD', 'Photoshop', 'Illustrator'],
        image: '/images/tracks/ui-ux-design.jpg',
        knowledges: ['Design Principles', 'User Research', 'Prototyping', 'Visual Design', 'Interaction Design'],
        obtained: '0',
        level: 'beginner',
        slug: 'ui-ux-design-track',
      },
    ],
    toolsByArea: {
      programming: ['HTML', 'CSS', 'JavaScript', 'React', 'Node.js', 'Unity', 'C#'],
      art: ['Blender', 'Photoshop', 'Illustrator'],
      design: ['Figma', 'Adobe XD', 'Photoshop', 'Illustrator'],
    },
  };
}

export async function getTracksData(): Promise<TracksData> {
  try {
    // Use the existing Programs API to get all published programs
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'}/api/program/published`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      console.warn(`API error when fetching tracks: ${response.status}. Using fallback data.`);
      return getFallbackTracksData();
    }

    const allPrograms = await response.json();

    // Filter for tracks: programs with "track" in slug or title
    const trackPrograms = allPrograms.filter(
      (program: { slug?: string; title?: string }) => program.slug?.includes('-track-') || program.title?.includes('Track'),
    );

    // Transform track programs to tracks format
    const tracks: Track[] = await Promise.all(
      trackPrograms.map(
        async (program: {
          id: string;
          title: string;
          description?: string;
          category: number;
          difficulty: number;
          thumbnail?: string;
          slug: string;
          estimatedHours?: number;
        }) => {
          // For now, use fallback knowledge indicators since the content endpoint requires authentication
          // TODO: Either make the content endpoint public or implement authentication
          const fallbackKnowledges = {
            'game-development-track': ['Game Design', 'Programming', 'Art & Animation', 'Sound Design', 'Testing'],
            'web-development-track': ['HTML/CSS', 'JavaScript', 'React', 'Backend APIs', 'Deployment'],
            'data-science-track': ['Python', 'Statistics', 'Machine Learning', 'Data Visualization', 'Analytics'],
            'ai-development-track': ['Neural Networks', 'Deep Learning', 'NLP', 'Computer Vision', 'Ethics'],
          };

          const trackKnowledges = fallbackKnowledges[program.slug as keyof typeof fallbackKnowledges] || [
            'Fundamentals',
            'Intermediate',
            'Advanced',
            'Projects',
            'Portfolio',
          ];

          return {
            id: program.id,
            title: program.title,
            description: program.description || '',
            area: mapProgramCategoryToTrackArea(program.category),
            tools: extractToolsFromProgram(program),
            image: sanitizeImagePath(program.thumbnail),
            knowledges: trackKnowledges,
            obtained: '0', // Could be mapped from user progress
            level: mapProgramDifficultyToTrackLevel(program.difficulty),
            slug: program.slug,
          };
        },
      ),
    );

    // Generate tools by area from the tracks
    const toolsByArea: ToolsByArea = {
      programming: [],
      art: [],
      design: [],
    };

    tracks.forEach((track) => {
      if (track.area && toolsByArea[track.area as keyof ToolsByArea]) {
        track.tools.forEach((tool) => {
          if (!toolsByArea[track.area as keyof ToolsByArea].includes(tool)) {
            toolsByArea[track.area as keyof ToolsByArea].push(tool);
          }
        });
      }
    });

    return {
      tracks,
      toolsByArea,
    };
  } catch (error) {
    console.error('Error fetching tracks data:', error);
    return getFallbackTracksData();
  }
}

export async function getTrackBySlug(slug: string): Promise<Track | null> {
  try {
    const { tracks } = await getTracksData();
    const track = tracks.find((t) => t.slug === slug);
    return track || null;
  } catch (error) {
    console.error('Error fetching track by slug:', error);
    // Try fallback data
    const fallbackData = getFallbackTracksData();
    const track = fallbackData.tracks.find((t) => t.slug === slug);
    return track || null;
  }
}
