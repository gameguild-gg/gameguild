export interface Track {
  id: string;
  title: string;
  description: string;
  slug: string;
  difficulty: 'beginner' | 'intermediate' | 'advanced';
  duration: number; // in hours
  courseCount: number;
  enrolledCount: number;
  rating: number;
  imageUrl: string;
  category: string;
  tags: string[];
  prerequisites: string[];
  completionRate: number;
  createdAt: Date;
  updatedAt: Date;
  published: boolean;
  featured: boolean;
  price: number;
  instructorId: string;
  instructorName: string;
  instructorAvatar: string;
  courses: Course[];
}

export interface Course {
  id: string;
  title: string;
  description: string;
  slug: string;
  duration: number;
  order: number;
  completed: boolean;
  locked: boolean;
}

export interface TrackFilters {
  category?: string;
  difficulty?: Track['difficulty'];
  tags?: string[];
  priceRange?: {
    min: number;
    max: number;
  };
  rating?: number;
  duration?: {
    min: number;
    max: number;
  };
  featured?: boolean;
  free?: boolean;
}

export interface TrackProgress {
  trackId: string;
  userId: string;
  completedCourses: string[];
  currentCourseId: string | null;
  progressPercentage: number;
  timeSpent: number;
  lastAccessedAt: Date;
  startedAt: Date;
  completedAt: Date | null;
  certificateUrl: string | null;
}

export interface TrackCardProps {
  track: Track;
  variant?: 'default' | 'compact' | 'featured';
  showProgress?: boolean;
  progress?: TrackProgress;
  onEnroll?: (trackId: string) => void;
  onContinue?: (trackId: string, courseId: string) => void;
}

export interface TrackGridProps {
  tracks: Track[];
  loading?: boolean;
  filters?: TrackFilters;
  onFilterChange?: (filters: TrackFilters) => void;
  onTrackSelect?: (track: Track) => void;
}

export interface TrackFiltersProps {
  filters: TrackFilters;
  onFiltersChange: (filters: TrackFilters) => void;
  categories: string[];
  tags: string[];
  maxPrice: number;
  maxDuration: number;
}
