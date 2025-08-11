export interface Course {
  id: string;
  title: string;
  description: string;
  longDescription?: string;
  category: string;
  level: 'Beginner' | 'Intermediate' | 'Advanced';
  duration: string;
  enrolledStudents: number;
  rating: number;
  reviewCount?: number;
  price: number;
  image: string;
  instructor: {
    name: string;
    avatar: string;
    title?: string;
    experience?: string;
  };
  isEnrolled?: boolean;
  progress?: number;
  certification?: boolean;
  skills?: string[];
  outcomes?: string[];
  prerequisites?: string[];
  isPopular?: boolean;
  isFeatured?: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CourseFilters {
  search: string;
  category: string;
  level: string;
  sortBy: string;
  priceRange?: {
    min?: number;
    max?: number;
  };
  rating?: number;
  duration?: string;
}

export interface CoursePagination {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export type CourseViewMode = 'grid' | 'list' | 'compact';

export interface CourseGridProps {
  courses: Course[];
  variant?: 'default' | 'compact' | 'featured';
  columns?: 1 | 2 | 3 | 4;
  loading?: boolean;
}
