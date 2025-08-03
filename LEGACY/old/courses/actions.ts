// Legacy course actions - backed up due to migration
'use server';

export interface CourseLegacy {
  id: number;
  title: string;
  category: string;
  description: string;
  image: string;
  slug: string;
  level?: string;
  duration?: string;
  seatsLeft?: number;
}

// Mock data for development/demo purposes. Remove or replace with real data source as needed

// Helper function to generate slug from title
function generateSlug(title: string): string {
  return title
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .trim()
    .replace(/^-+|-+$/g, '');
}

const mockCourses: CourseLegacy[] = [
  {
    id: 1,
    title: 'Concept Art Fundamentals',
    slug: generateSlug('Concept Art Fundamentals'),
    category: 'Art',
    description: 'Learn the basics of concept art for games and film.',
    image: '/images/concept-art.jpg',
    level: 'Beginner',
    duration: '8 weeks',
    seatsLeft: 4,
  },
  {
    id: 2,
    title: '3D Character Modeling',
    slug: generateSlug('3D Character Modeling'),
    category: '3D',
    description: 'Create stunning 3D characters from scratch.',
    image: '/images/3d-character.jpg',
    level: 'Intermediate',
    duration: '9 weeks',
  },
  {
    id: 3,
    title: '2D Animation Essentials',
    slug: generateSlug('2D Animation Essentials'),
    category: 'Animation',
    description: 'A 9-week course where students will learn the fundamentals of character animation, importance of emotions, and more.',
    image: '/images/2d-animation.jpg',
    level: 'Intermediate',
    duration: '9 weeks',
  },
  {
    id: 4,
    title: '2D Motion Graphics in After Effects',
    slug: generateSlug('2D Motion Graphics in After Effects'),
    category: 'Motion Graphics',
    description: 'An 8-week course diving into how to create compelling visual elements and animations using Adobe After Effects.',
    image: '/images/2d-motion.jpg',
    level: 'Beginner - Intermediate',
    duration: '8 weeks',
  },
  {
    id: 5,
    title: '3D for 2D artists',
    slug: generateSlug('3D for 2D artists'),
    category: '3D',
    description: 'A 9-week course where artists learn to integrate easy 3D techniques into their 2D concept workflow.',
    image: '/images/3d-for-2d.jpg',
    level: 'Intermediate to Advanced',
    duration: '9 weeks',
  },
  {
    id: 6,
    title: 'Absolute Beginners',
    slug: generateSlug('Absolute Beginners'),
    category: 'Art',
    description: 'An 8-week course for beginning artists looking to develop a strong foundation using different tools in art.',
    image: '/images/absolute-beginners.jpg',
    level: 'Beginner',
    duration: '8 weeks',
  },
  {
    id: 7,
    title: 'Acting for Visual Storytellers',
    slug: generateSlug('Acting for Visual Storytellers'),
    category: 'Storytelling',
    description: 'A 5-week course where students will learn simple acting techniques to better communicate character and emotion.',
    image: '/images/acting-storytellers.jpg',
    level: 'All Levels',
    duration: '5 weeks',
    seatsLeft: 4,
  },
  {
    id: 8,
    title: 'Analytical Figure Drawing',
    slug: generateSlug('Analytical Figure Drawing'),
    category: 'Drawing Foundation',
    description: 'An 8-week foundation course focused on the analysis of the figure starting from skeleton, to muscle, to surface.',
    image: '/images/analytical-figure.jpg',
    level: 'All Levels',
    duration: '8 weeks',
  },
  {
    id: 9,
    title: 'Anatomy of Clothing',
    slug: generateSlug('Anatomy of Clothing'),
    category: 'Drawing Foundation',
    description: 'An 8-week course on the foundation of visually communicating clothing and fabric, and how different materials behave.',
    image: '/images/anatomy-clothing.jpg',
    level: 'All Levels',
    duration: '8 weeks',
  },
  {
    id: 10,
    title: 'Animal Drawing',
    slug: generateSlug('Animal Drawing'),
    category: 'Drawing Foundation',
    description: 'An 8-week course focused on understanding and illustrating animals by breaking down animal anatomy.',
    image: '/images/animal-drawing.jpg',
    level: 'All Levels',
    duration: '8 weeks',
  },
  {
    id: 11,
    title: 'C++ Fundamentals',
    slug: generateSlug('C++ Fundamentals'),
    category: 'CPP',
    description: 'Master the basics of C++ for software, games, and embedded systems.',
    image: '/images/cpp-fundamentals.jpg',
    level: 'Beginner',
    duration: '11 hours',
  },
  {
    id: 12,
    title: 'C++ for Programmers',
    slug: generateSlug('C++ for Programmers'),
    category: 'CPP',
    description: 'Take this course meant for experienced programmers and learn about C++, one of the world’s most popular languages.',
    image: '/images/cpp-for-programmers.jpg',
    level: 'Intermediate',
    duration: '3 hours',
  },
  {
    id: 13,
    title: 'C++: Introduction',
    slug: generateSlug('C++: Introduction'),
    category: 'CPP',
    description: 'Dive into C++, a flexible and well-supported language that’s still widely used now, over 40 years after its conception.',
    image: '/images/cpp-intro.jpg',
    level: 'Beginner Friendly',
    duration: '4 hours',
  },
  {
    id: 14,
    title: 'Object-Oriented Programming (OOP) with C++',
    slug: generateSlug('Object-Oriented Programming (OOP) with C++'),
    category: 'CPP',
    description: 'Learn OOP concepts in C++ including classes, inheritance, and polymorphism.',
    image: '/images/cpp-oop.jpg',
    level: 'Intermediate',
    duration: '8 hours',
  },
  {
    id: 15,
    title: 'Intermediate C++',
    slug: generateSlug('Intermediate C++'),
    category: 'CPP',
    description: 'Learn intermediate C++ concepts like variable scope, storage classes, and more.',
    image: '/images/cpp-intermediate.jpg',
    level: 'Intermediate',
    duration: '7 hours',
  },
  {
    id: 16,
    title: 'C++: References and Pointers',
    slug: generateSlug('C++: References and Pointers'),
    category: 'CPP',
    description: 'References and pointers are some of the most important concepts in C++. Master them in this course.',
    image: '/images/cpp-references.jpg',
    level: 'Intermediate',
    duration: '5 hours',
  },
  {
    id: 17,
    title: 'Advanced C++ Templates',
    slug: generateSlug('Advanced C++ Templates'),
    category: 'CPP',
    description: 'Explore advanced template programming in C++ for generic and reusable code.',
    image: '/images/cpp-templates.jpg',
    level: 'Advanced',
    duration: '6 hours',
  },
  {
    id: 18,
    title: 'C++ for Game Development',
    slug: generateSlug('C++ for Game Development'),
    category: 'CPP',
    description: 'Apply C++ to real-world game development scenarios and build your own game engine modules.',
    image: '/images/cpp-game-dev.jpg',
    level: 'Advanced',
    duration: '12 hours',
  },
  // ...add more as needed...
];

let courses: CourseLegacy[] = [...mockCourses];

export async function fetchCourses(): Promise<CourseLegacy[]> {
  return courses;
}

export async function getCourse(id: number): Promise<CourseLegacy | undefined> {
  return courses.find((c) => c.id === id);
}

export async function createCourse(course: Omit<CourseLegacy, 'id'>): Promise<CourseLegacy> {
  const newCourse: CourseLegacy = { ...course, id: Date.now() };
  courses.push(newCourse);
  return newCourse;
}

export async function updateCourse(id: number, updates: Partial<Omit<CourseLegacy, 'id'>>): Promise<CourseLegacy | undefined> {
  const idx = courses.findIndex((c) => c.id === id);
  if (idx === -1) return undefined;
  courses[idx] = { ...courses[idx], ...updates };
  return courses[idx];
}

export async function deleteCourse(id: number): Promise<boolean> {
  const prevLen = courses.length;
  courses = courses.filter((c) => c.id !== id);
  return courses.length < prevLen;
}
