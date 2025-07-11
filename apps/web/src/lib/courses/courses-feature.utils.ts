import { Course, CourseFilters } from './types';

export function filterCourses(courses: Course[], filters: CourseFilters): Course[] {
  let filtered = [...courses];

  // Apply search filter
  if (filters.search) {
    const searchLower = filters.search.toLowerCase();
    filtered = filtered.filter(
      (course) =>
        course.title.toLowerCase().includes(searchLower) ||
        course.description.toLowerCase().includes(searchLower) ||
        course.instructor.name.toLowerCase().includes(searchLower) ||
        course.skills?.some((skill) => skill.toLowerCase().includes(searchLower)),
    );
  }

  // Apply category filter
  if (filters.category && filters.category !== 'All') {
    filtered = filtered.filter((course) => course.category === filters.category);
  }

  // Apply level filter
  if (filters.level && filters.level !== 'All') {
    filtered = filtered.filter((course) => course.level === filters.level);
  }

  // Apply price range filter
  if (filters.priceRange) {
    const { min, max } = filters.priceRange;
    filtered = filtered.filter((course) => {
      if (min !== undefined && course.price < min) return false;
      if (max !== undefined && course.price > max) return false;
      return true;
    });
  }

  // Apply rating filter
  if (filters.rating) {
    filtered = filtered.filter((course) => course.rating >= filters.rating!);
  }

  // Apply sorting
  filtered.sort((a, b) => {
    switch (filters.sortBy) {
      case 'title':
        return a.title.localeCompare(b.title);
      case 'price':
        return a.price - b.price;
      case 'rating':
        return b.rating - a.rating;
      case 'students':
        return b.enrolledStudents - a.enrolledStudents;
      case 'newest':
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      case 'popular':
      default:
        // Sort by popularity (featured, then by enrollment)
        if (a.isPopular && !b.isPopular) return -1;
        if (!a.isPopular && b.isPopular) return 1;
        return b.enrolledStudents - a.enrolledStudents;
    }
  });

  return filtered;
}

export function searchCourses(courses: Course[], query: string): Course[] {
  if (!query.trim()) return courses;

  const searchLower = query.toLowerCase();
  return courses.filter((course) => {
    const searchableText = [
      course.title,
      course.description,
      course.instructor.name,
      course.category,
      course.level,
      ...(course.skills || []),
      ...(course.outcomes || []),
    ]
      .join(' ')
      .toLowerCase();

    return searchableText.includes(searchLower);
  });
}

export function getCoursesByCategory(courses: Course[], category: string): Course[] {
  if (category === 'All') return courses;
  return courses.filter((course) => course.category === category);
}

export function getPopularCourses(courses: Course[], limit = 6): Course[] {
  return courses
    .filter((course) => course.isPopular)
    .sort((a, b) => b.enrolledStudents - a.enrolledStudents)
    .slice(0, limit);
}

export function getFeaturedCourses(courses: Course[], limit = 3): Course[] {
  return courses
    .filter((course) => course.isFeatured)
    .sort((a, b) => b.rating - a.rating)
    .slice(0, limit);
}

export function getRecommendedCourses(courses: Course[], userInterests: string[], completedCourses: string[] = [], limit = 6): Course[] {
  return courses
    .filter((course) => !completedCourses.includes(course.id))
    .filter((course) =>
      userInterests.some(
        (interest) =>
          course.category.toLowerCase().includes(interest.toLowerCase()) ||
          course.skills?.some((skill) => skill.toLowerCase().includes(interest.toLowerCase())),
      ),
    )
    .sort((a, b) => b.rating - a.rating)
    .slice(0, limit);
}

export function calculateCourseScore(course: Course): number {
  // Calculate a composite score based on rating, enrollment, and other factors
  const ratingScore = course.rating * 20; // Max 100
  const enrollmentScore = Math.min(course.enrolledStudents / 1000, 1) * 20; // Max 20
  const certificationBonus = course.certification ? 10 : 0;
  const popularBonus = course.isPopular ? 5 : 0;
  const featuredBonus = course.isFeatured ? 10 : 0;

  return ratingScore + enrollmentScore + certificationBonus + popularBonus + featuredBonus;
}
