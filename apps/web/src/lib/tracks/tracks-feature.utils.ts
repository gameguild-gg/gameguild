import { Track, TrackFilters } from './types';

export function filterTracks(tracks: Track[], filters: TrackFilters): Track[] {
  return tracks.filter((track) => {
    // Category filter
    if (filters.category && track.category !== filters.category) {
      return false;
    }

    // Difficulty filter
    if (filters.difficulty && track.difficulty !== filters.difficulty) {
      return false;
    }

    // Tags filter (track must have at least one of the selected tags)
    if (filters.tags && filters.tags.length > 0) {
      const hasMatchingTag = filters.tags.some((tag) => track.tags.includes(tag));
      if (!hasMatchingTag) {
        return false;
      }
    }

    // Price range filter
    if (filters.priceRange) {
      const { min, max } = filters.priceRange;
      if (track.price < min || track.price > max) {
        return false;
      }
    }

    // Rating filter
    if (filters.rating && track.rating < filters.rating) {
      return false;
    }

    // Duration filter
    if (filters.duration) {
      const { min, max } = filters.duration;
      if (track.duration < min || track.duration > max) {
        return false;
      }
    }

    // Featured filter
    if (filters.featured !== undefined && track.featured !== filters.featured) {
      return false;
    }

    // Free filter
    if (filters.free !== undefined) {
      const isFree = track.price === 0;
      if (isFree !== filters.free) {
        return false;
      }
    }

    return true;
  });
}

export function searchTracks(tracks: Track[], query: string): Track[] {
  if (!query.trim()) {
    return tracks;
  }

  const searchTerm = query.toLowerCase();

  return tracks.filter((track) => {
    return (
      track.title.toLowerCase().includes(searchTerm) ||
      track.description.toLowerCase().includes(searchTerm) ||
      track.category.toLowerCase().includes(searchTerm) ||
      track.instructorName.toLowerCase().includes(searchTerm) ||
      track.tags.some((tag) => tag.toLowerCase().includes(searchTerm))
    );
  });
}

export function sortTracks(tracks: Track[], sortBy: string): Track[] {
  const sortedTracks = [...tracks];

  switch (sortBy) {
    case 'title':
      return sortedTracks.sort((a, b) => a.title.localeCompare(b.title));
    case 'rating':
      return sortedTracks.sort((a, b) => b.rating - a.rating);
    case 'enrolled':
      return sortedTracks.sort((a, b) => b.enrolledCount - a.enrolledCount);
    case 'duration':
      return sortedTracks.sort((a, b) => a.duration - b.duration);
    case 'price-low':
      return sortedTracks.sort((a, b) => a.price - b.price);
    case 'price-high':
      return sortedTracks.sort((a, b) => b.price - a.price);
    case 'newest':
      return sortedTracks.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    case 'featured':
      return sortedTracks.sort((a, b) => {
        if (a.featured && !b.featured) return -1;
        if (!a.featured && b.featured) return 1;
        return 0;
      });
    default:
      return sortedTracks;
  }
}

export function getDifficultyColor(difficulty: Track['difficulty']): string {
  switch (difficulty) {
    case 'beginner':
      return 'bg-green-100 text-green-800 border-green-200';
    case 'intermediate':
      return 'bg-yellow-100 text-yellow-800 border-yellow-200';
    case 'advanced':
      return 'bg-red-100 text-red-800 border-red-200';
    default:
      return 'bg-gray-100 text-gray-800 border-gray-200';
  }
}

export function formatDuration(hours: number): string {
  if (hours < 1) {
    return `${Math.round(hours * 60)}min`;
  }

  if (hours < 24) {
    return `${Math.round(hours)}h`;
  }

  const days = Math.floor(hours / 24);
  const remainingHours = Math.round(hours % 24);

  if (remainingHours === 0) {
    return `${days}d`;
  }

  return `${days}d ${remainingHours}h`;
}

export function formatPrice(price: number): string {
  if (price === 0) {
    return 'Free';
  }

  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(price);
}

export function calculateProgress(completedCourses: string[], totalCourses: number): number {
  if (totalCourses === 0) return 0;
  return Math.round((completedCourses.length / totalCourses) * 100);
}
