import type { Program, ProgramContent } from '@/lib/api/generated';

export type ProgramArea = 'ai-systems' | 'programming' | 'production' | 'portfolio' | 'data';

export interface PublicProgramPackage {
  slug: string;
  title: string;
  shortTitle: string;
  area: ProgramArea;
  summary: string;
  longDescription: string;
  audience: string;
  level: string;
  duration: string;
  format: string;
  image: string;
  accent: string;
  courseSlugs: string[];
  outcomes: string[];
  tools: string[];
  portfolioResult: string;
}

export interface CourseProjectShowcase {
  title: string;
  summary: string;
  image: string;
  skills: string[];
  deliverable: string;
  moduleLabel: string;
}

export interface CourseJourneyStep {
  label: string;
  title: string;
  body: string;
  checkpoint: string;
  projectTitle: string;
  minutes?: number | null;
  type?: number | null;
}

export interface CourseShowcase {
  slug: string;
  programSlug: string;
  headline: string;
  studioPrompt: string;
  projectResult: string;
  instructorModel: string;
  portfolioProof: string;
  outcomes: string[];
  prerequisites: string[];
  projects?: CourseProjectShowcase[];
  journey?: CourseJourneyStep[];
  faq: Array<{ question: string; answer: string }>;
}

export const PUBLIC_PROGRAM_PACKAGES: PublicProgramPackage[] = [
  {
    slug: 'game-ai-systems',
    title: 'Game AI & Systems Programming',
    shortTitle: 'AI & Systems',
    area: 'ai-systems',
    summary: 'Build believable agents, tactical decision systems, procedural tools, and runtime AI features for playable games.',
    longDescription:
      'A production-minded AI path for students who want to move beyond isolated algorithms and ship game-ready behavior. The package starts with AI fundamentals, then pushes into pathfinding, tactical reasoning, simulation, procedural content, and network-aware systems.',
    audience: 'Gameplay programmers, technical designers, AI engineers, and advanced students building portfolio-ready systems.',
    level: 'Intermediate to advanced',
    duration: '73 estimated hours',
    format: 'Project-based lessons, technical breakdowns, and capstone implementation work',
    image: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
    accent: 'cyan',
    courseSlugs: ['ai4games'],
    outcomes: [
      'Implement navigation, steering, tactical positioning, and agent decision loops.',
      'Design multiplayer-safe systems and data flows for real-time games.',
      'Produce a technical portfolio piece that demonstrates AI behavior in context.',
    ],
    tools: ['C++', 'Unity', 'C#', 'Pathfinding', 'Behavior Trees', 'Networking'],
    portfolioResult: 'A playable AI systems prototype with documented design decisions and implementation notes.',
  },
  {
    slug: 'game-programming-foundations',
    title: 'Game Programming Foundations',
    shortTitle: 'Programming',
    area: 'programming',
    summary: 'A structured foundation for students who need programming confidence before entering advanced game development work.',
    longDescription:
      'This package builds durable programming habits through Python, data structures, algorithms, databases, and introductory game programming. It is designed as the practical bridge between beginner coding and production game development courses.',
    audience: 'Beginners, career switchers, technical artists, designers, and students preparing for programming-heavy game courses.',
    level: 'Beginner to intermediate',
    duration: '142 estimated hours',
    format: 'Guided fundamentals, coding labs, technical assignments, and applied projects',
    image: 'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=1400&h=900&fit=crop',
    accent: 'blue',
    courseSlugs: ['intro2gpro'],
    outcomes: [
      'Write maintainable code using clear control flow, data structures, and debugging habits.',
      'Understand databases and data modeling for game and platform features.',
      'Build small gameplay systems that prepare students for engine-level work.',
    ],
    tools: ['Python', 'SQL', 'Algorithms', 'Data Structures', 'Game Loops'],
    portfolioResult: 'A small playable game/programming project plus supporting technical exercises.',
  },
];

export const SHOWCASE_BY_SLUG: Record<string, CourseShowcase> = {
  ai4games: {
    slug: 'ai4games',
    programSlug: 'game-ai-systems',
    headline: 'Build decision-making agents and procedural systems that make games feel alive.',
    studioPrompt: 'Students prototype AI behaviors, study pathfinding tradeoffs, and finish with an applied game AI system.',
    projectResult: 'A playable AI prototype with navigation, decision logic, and a documented final project.',
    instructorModel: 'Guided technical instruction with project checkpoints and implementation-focused feedback.',
    portfolioProof: 'Show recruiters and collaborators how you reason about behavior, simulation, and gameplay constraints.',
    outcomes: [
      'Build behavioral agents that react to player state, world context, and gameplay goals.',
      'Implement pathfinding and tactical movement choices that are readable inside a playable prototype.',
      'Use procedural generation techniques to create varied content without losing design control.',
      'Finish a documented AI prototype that explains implementation decisions and tradeoffs.',
    ],
    prerequisites: ['Comfort reading code', 'Basic programming fundamentals', 'Interest in gameplay systems'],
    faq: [
      {
        question: 'Is this only theory?',
        answer: 'No. The course is structured around practical implementation and finishes with a capstone-style AI project.',
      },
      {
        question: 'Which engine is required?',
        answer: 'The concepts transfer across engines. Course examples emphasize implementation patterns over engine lock-in.',
      },
    ],
  },
  intro2gpro: {
    slug: 'intro2gpro',
    programSlug: 'game-programming-foundations',
    headline: 'Move from programming basics into the structure, rhythm, and constraints of game programming.',
    studioPrompt: 'Students learn how gameplay systems are organized and how interactive programs become playable experiences.',
    projectResult: 'A small gameplay prototype or technical feature that demonstrates engine-facing programming habits.',
    instructorModel: 'Practical programming guidance grounded in game loops, systems, and implementation constraints.',
    portfolioProof: 'A first game-programming artifact that can anchor later AI, networking, or systems courses.',
    outcomes: [
      'Understand the game loop and how frame-by-frame updates shape interactive behavior.',
      'Build small gameplay systems that connect input, state, feedback, and rules.',
      'Debug interactive code by observing state changes instead of guessing at behavior.',
      'Finish a focused game-programming artifact that prepares you for advanced systems courses.',
    ],
    prerequisites: ['Basic programming experience', 'Comfort practicing with examples'],
    faq: [
      {
        question: 'Is this beginner-friendly?',
        answer: 'Yes, it is designed as the bridge from programming basics into game programming.',
      },
      {
        question: 'Will I finish a game?',
        answer: 'The goal is a focused playable prototype or system rather than a complete commercial game.',
      },
    ],
  },
};

export function getPublicProgramPackage(slug: string): PublicProgramPackage | null {
  return PUBLIC_PROGRAM_PACKAGES.find((program) => program.slug === slug) ?? null;
}

export function getProgramForCourse(courseSlug: string | null | undefined): PublicProgramPackage | null {
  if (!courseSlug) {
    return null;
  }

  return PUBLIC_PROGRAM_PACKAGES.find((program) => program.courseSlugs.includes(courseSlug)) ?? null;
}

export function getCourseShowcase(courseSlug: string | null | undefined): CourseShowcase | null {
  if (!courseSlug) {
    return null;
  }

  return SHOWCASE_BY_SLUG[courseSlug] ?? null;
}

export function getCoursesForProgram<T extends Pick<Program, 'slug'>>(program: PublicProgramPackage, courses: T[]): T[] {
  return program.courseSlugs.map((slug) => courses.find((course) => course.slug === slug)).filter((course): course is T => Boolean(course));
}

function getContentSortOrder(content: ProgramContent): number {
  const sortOrder = content.sortOrder;

  if (typeof sortOrder === 'number') {
    return sortOrder;
  }

  if (typeof sortOrder === 'string') {
    const parsed = Number(sortOrder);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  return 0;
}

export function listCourseContentPreview(contents: ProgramContent[] | null | undefined, limit = 6): ProgramContent[] {
  return [...(contents ?? [])]
    .filter((content) => !content.parentId)
    .sort((left, right) => getContentSortOrder(left) - getContentSortOrder(right))
    .slice(0, limit);
}
