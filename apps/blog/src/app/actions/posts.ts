'use server';

export interface Tag {
  name: string;
  slug: string;
  parents?: string[]; // Parent tag slugs for hierarchical structure
  children?: string[]; // Child tag slugs for hierarchical structure
}

export interface Post {
  slug: string;
  title: string;
  publishedAt: Date;
  tags: string[]; // Array of tag slugs
}

// Tag registry - defines the graph structure of all tags
const tagRegistry: { [slug: string]: Tag } = {
  // Root categories
  'cgi-3d': {
    name: 'CGI & 3D',
    slug: 'cgi-3d',
    children: ['modeling', 'rendering', 'animation', 'texturing', 'lighting'],
  },
  'game-development': {
    name: 'Game Development',
    slug: 'game-development',
    children: ['game-engines', 'gameplay-programming', 'game-design', 'game-optimization'],
  },
  'digital-art': {
    name: 'Digital Art',
    slug: 'digital-art',
    children: ['concept-art', 'character-design', 'environment-art', 'vfx'],
  },
  'technical-pipeline': {
    name: 'Technical Pipeline',
    slug: 'technical-pipeline',
    children: ['pipeline-automation', 'asset-management', 'version-control', 'workflow-optimization'],
  },

  // 3D/CGI Framework tags
  modeling: {
    name: '3D Modeling',
    slug: 'modeling',
    parents: ['cgi-3d'],
    children: ['blender', 'maya', 'max', 'zbrush'],
  },
  rendering: {
    name: 'Rendering',
    slug: 'rendering',
    parents: ['cgi-3d'],
    children: ['cycles', 'arnold', 'vray', 'octane', 'path-tracing'],
  },
  animation: {
    name: 'Animation',
    slug: 'animation',
    parents: ['cgi-3d'],
    children: ['rigging', 'keyframing', 'mocap', 'procedural-animation'],
  },
  texturing: {
    name: 'Texturing',
    slug: 'texturing',
    parents: ['cgi-3d'],
    children: ['substance-painter', 'mari', 'pbr-materials'],
  },
  lighting: {
    name: 'Lighting',
    slug: 'lighting',
    parents: ['cgi-3d'],
    children: ['hdri', 'global-illumination', 'volumetrics'],
  },

  // Game Development Framework tags
  'game-engines': {
    name: 'Game Engines',
    slug: 'game-engines',
    parents: ['game-development'],
    children: ['unity', 'unreal-engine', 'godot', 'custom-engines'],
  },
  'gameplay-programming': {
    name: 'Gameplay Programming',
    slug: 'gameplay-programming',
    parents: ['game-development'],
    children: ['csharp', 'cpp', 'blueprints', 'scripting'],
  },
  'game-design': {
    name: 'Game Design',
    slug: 'game-design',
    parents: ['game-development'],
    children: ['level-design', 'mechanics', 'ui-ux', 'narrative'],
  },

  // Software/Tool tags
  blender: {
    name: 'Blender',
    slug: 'blender',
    parents: ['modeling'],
  },
  unity: {
    name: 'Unity',
    slug: 'unity',
    parents: ['game-engines'],
  },
  'unreal-engine': {
    name: 'Unreal Engine',
    slug: 'unreal-engine',
    parents: ['game-engines'],
  },
  'substance-painter': {
    name: 'Substance Painter',
    slug: 'substance-painter',
    parents: ['texturing'],
  },

  // Concept tags
  'real-time': {
    name: 'Real-time',
    slug: 'real-time',
  },
  photorealism: {
    name: 'Photorealism',
    slug: 'photorealism',
  },
  stylized: {
    name: 'Stylized',
    slug: 'stylized',
  },
  optimization: {
    name: 'Optimization',
    slug: 'optimization',
  },
  workflow: {
    name: 'Workflow',
    slug: 'workflow',
  },

  // Other specialized tags
  beginner: { name: 'Beginner', slug: 'beginner' },
  intermediate: { name: 'Intermediate', slug: 'intermediate' },
  advanced: { name: 'Advanced', slug: 'advanced' },
  tutorial: { name: 'Tutorial', slug: 'tutorial' },
  'tips-tricks': { name: 'Tips & Tricks', slug: 'tips-tricks' },
  industry: { name: 'Industry', slug: 'industry' },
  freelance: { name: 'Freelance', slug: 'freelance' },
  portfolio: { name: 'Portfolio', slug: 'portfolio' },
  career: { name: 'Career', slug: 'career' },
  tools: { name: 'Tools', slug: 'tools' },
  hardware: { name: 'Hardware', slug: 'hardware' },
  performance: { name: 'Performance', slug: 'performance' },
  shaders: { name: 'Shaders', slug: 'shaders' },
  procedural: { name: 'Procedural', slug: 'procedural' },
  'indie-dev': { name: 'Indie Development', slug: 'indie-dev' },
  'aaa-dev': { name: 'AAA Development', slug: 'aaa-dev' },
  'mobile-dev': { name: 'Mobile Development', slug: 'mobile-dev' },
};

// Mock data for published posts
const mockPosts: Post[] = [
  {
    slug: 'blender-modeling-fundamentals',
    title: 'Blender 3D Modeling Fundamentals for Game Assets',
    publishedAt: new Date('2024-01-15'),
    tags: ['modeling', 'blender', 'game-development', 'beginner', 'tutorial', 'workflow'],
  },
  {
    slug: 'unity-shader-optimization',
    title: 'Optimizing Unity Shaders for Mobile Performance',
    publishedAt: new Date('2024-02-28'),
    tags: ['unity', 'shaders', 'optimization', 'mobile-dev', 'performance', 'advanced'],
  },
  {
    slug: 'substance-painter-workflow',
    title: 'PBR Texturing Workflow in Substance Painter',
    publishedAt: new Date('2024-03-10'),
    tags: ['texturing', 'substance-painter', 'pbr-materials', 'workflow', 'intermediate', 'tutorial'],
  },
  {
    slug: 'unreal-engine-lighting-setup',
    title: 'Professional Lighting Setup in Unreal Engine 5',
    publishedAt: new Date('2024-04-05'),
    tags: ['unreal-engine', 'lighting', 'real-time', 'photorealism', 'advanced', 'tips-tricks'],
  },
  {
    slug: 'indie-game-development-pipeline',
    title: 'Building an Efficient Pipeline for Indie Game Development',
    publishedAt: new Date('2023-12-20'),
    tags: ['technical-pipeline', 'indie-dev', 'workflow-optimization', 'asset-management', 'career'],
  },
  {
    slug: 'procedural-animation-techniques',
    title: 'Procedural Animation Techniques for Dynamic Gameplay',
    publishedAt: new Date('2023-11-08'),
    tags: ['animation', 'procedural-animation', 'gameplay-programming', 'advanced', 'techniques'],
  },
];

export async function getPublishedPosts(): Promise<Post[]> {
  // Simulate API delay
  await new Promise((resolve) => setTimeout(resolve, 100));
  return mockPosts;
}

export async function getPostsByYear(year: number): Promise<Post[]> {
  const posts = await getPublishedPosts();
  return posts.filter((post) => post.publishedAt.getFullYear() === year);
}

export async function getPostsByMonth(year: number, month: number): Promise<Post[]> {
  const posts = await getPublishedPosts();
  return posts.filter((post) => post.publishedAt.getFullYear() === year && post.publishedAt.getMonth() + 1 === month);
}

export async function getPostsByDay(year: number, month: number, day: number): Promise<Post[]> {
  const posts = await getPublishedPosts();
  return posts.filter((post) => post.publishedAt.getFullYear() === year && post.publishedAt.getMonth() + 1 === month && post.publishedAt.getDate() === day);
}

export async function getPostBySlug(slug: string): Promise<Post | null> {
  const posts = await getPublishedPosts();
  return posts.find((post) => post.slug === slug) || null;
}

export async function getAvailableYears(): Promise<number[]> {
  const posts = await getPublishedPosts();
  const years = [...new Set(posts.map((post) => post.publishedAt.getFullYear()))];
  return years.sort((a, b) => b - a);
}

export async function getAvailableMonths(year: number): Promise<number[]> {
  const posts = await getPostsByYear(year);
  const months = [...new Set(posts.map((post) => post.publishedAt.getMonth() + 1))];
  return months.sort((a, b) => a - b);
}

export async function getAvailableDays(year: number, month: number): Promise<number[]> {
  const posts = await getPostsByMonth(year, month);
  const days = [...new Set(posts.map((post) => post.publishedAt.getDate()))];
  return days.sort((a, b) => a - b);
}

// Tag Graph functions
export async function getTag(slug: string): Promise<Tag | null> {
  return tagRegistry[slug] || null;
}

export async function getAllTags(): Promise<Tag[]> {
  return Object.values(tagRegistry);
}

export async function getPostsByTag(tagSlug: string): Promise<Post[]> {
  const posts = await getPublishedPosts();
  return posts.filter((post) => post.tags.includes(tagSlug));
}

export async function getPostsByTags(tagSlugs: string[]): Promise<Post[]> {
  const posts = await getPublishedPosts();
  return posts.filter((post) => tagSlugs.some((tagSlug) => post.tags.includes(tagSlug)));
}

export async function getTagsByPost(post: Post): Promise<Tag[]> {
  const tags = [];
  for (const tagSlug of post.tags) {
    const tag = await getTag(tagSlug);
    if (tag) tags.push(tag);
  }
  return tags;
}

export async function getRootTags(): Promise<Tag[]> {
  const allTags = await getAllTags();
  return allTags.filter((tag) => !tag.parents || tag.parents.length === 0);
}

export async function getChildTags(parentSlug: string): Promise<Tag[]> {
  const parentTag = await getTag(parentSlug);
  if (!parentTag?.children) return [];

  const children = [];
  for (const childSlug of parentTag.children) {
    const childTag = await getTag(childSlug);
    if (childTag) children.push(childTag);
  }
  return children;
}

export async function getParentTags(childSlug: string): Promise<Tag[]> {
  const childTag = await getTag(childSlug);
  if (!childTag?.parents) return [];

  const parents = [];
  for (const parentSlug of childTag.parents) {
    const parentTag = await getTag(parentSlug);
    if (parentTag) parents.push(parentTag);
  }
  return parents;
}

export async function getTagHierarchy(): Promise<{ [key: string]: Tag[] }> {
  const hierarchy: { [key: string]: Tag[] } = {};
  const allTags = await getAllTags();

  // Group tags by parent
  allTags.forEach((tag) => {
    if (!tag.parents || tag.parents.length === 0) {
      // Root level tags
      if (!hierarchy['root']) {
        hierarchy['root'] = [];
      }
      hierarchy['root'].push(tag);
    } else {
      // Child tags
      tag.parents.forEach((parentSlug) => {
        if (!hierarchy[parentSlug]) {
          hierarchy[parentSlug] = [];
        }
        hierarchy[parentSlug].push(tag);
      });
    }
  });

  return hierarchy;
}

export async function getPrimaryTag(post: Post): Promise<Tag | null> {
  // Get the first tag that has no parents (root category)
  for (const tagSlug of post.tags) {
    const tag = await getTag(tagSlug);
    if (tag && (!tag.parents || tag.parents.length === 0)) {
      return tag;
    }
  }

  return null;
}
