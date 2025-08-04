// Course level types with branded types for better type safety
export type CourseLevel = 1 | 2 | 3 | 4;
export type CourseArea = 'programming' | 'art' | 'design' | 'audio';

// Branded types for IDs to prevent mixing
export type CourseId = string & { readonly __brand: 'CourseId' };
export type ProductId = string & { readonly __brand: 'ProductId' };
export type UserId = string & { readonly __brand: 'UserId' };

// Course type with improved type safety
export interface Course {
  readonly id: CourseId;
  readonly title: string;
  readonly description: string;
  readonly area: CourseArea;
  readonly level: CourseLevel;
  readonly tools: readonly string[];
  readonly image: string;
  readonly slug: string;
  readonly progress?: number;
}

// Level configuration with const assertions
export const COURSE_LEVELS = {
  1: { name: 'Beginner', color: 'bg-green-500/10 border-green-500 text-green-400' },
  2: { name: 'Intermediate', color: 'bg-blue-500/10 border-blue-500 text-blue-400' },
  3: { name: 'Advanced', color: 'bg-orange-500/10 border-orange-500 text-orange-400' },
  4: { name: 'Arcane', color: 'bg-red-500/10 border-red-500 text-red-400' },
} as const;

// Result types for better error handling
export type Result<T, E = Error> = { success: true; data: T } | { success: false; error: E };

// Enrollment types
export interface EnrollmentStatus {
  readonly isEnrolled: boolean;
  readonly isFree: boolean;
  readonly price?: number;
  readonly productId?: ProductId;
  readonly enrollmentDate?: string;
  readonly progress?: number;
}

export interface Product {
  readonly id: ProductId;
  readonly title: string;
  readonly description: string;
  readonly price: number;
  readonly courses: readonly CourseId[];
  readonly type: 'course' | 'bundle' | 'track';
}

// Course content types
export interface CourseModule {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly duration: string;
  readonly completed: boolean;
  readonly lessons: readonly CourseLesson[];
}

export interface CourseLesson {
  readonly id: string;
  readonly title: string;
  readonly type: 'video' | 'text' | 'code' | 'exercise';
  readonly duration: string;
  readonly completed: boolean;
}

export interface CourseContent {
  readonly modules: readonly CourseModule[];
}
