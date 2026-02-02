import { cache } from 'react';

// =============================================================================
// COURSE LISTING / STORE QUERIES
// =============================================================================
// Data for the public-facing course catalog/store page configuration.
// These control how the course appears to potential students.
// =============================================================================

/**
 * Course listing basic info
 */
export interface CourseListingInfo {
  id: string;
  courseId: string;
  headline: string;           // Short tagline
  description: string;        // Full description (rich text/markdown)
  objectives: string[];       // What students will learn
  requirements: string[];     // Prerequisites
  targetAudience: string[];   // Who this course is for
  language: string;           // Primary language
  subtitles: string[];        // Available subtitle languages
  level: 'beginner' | 'intermediate' | 'advanced' | 'all-levels';
  estimatedDuration: number;  // Total hours
  lastUpdated: string;
  updatedAt: string;
}

/**
 * Course media assets for listing
 */
export interface CourseListingMedia {
  id: string;
  courseId: string;
  coverImage: {
    url: string;
    alt: string;
    width: number;
    height: number;
  } | null;
  promoVideo: {
    url: string;
    duration: number;
    thumbnailUrl: string;
  } | null;
  gallery: Array<{
    id: string;
    type: 'image' | 'video';
    url: string;
    thumbnailUrl?: string;
    caption?: string;
    order: number;
  }>;
  updatedAt: string;
}

/**
 * Student testimonial/review
 */
export interface CourseTestimonial {
  id: string;
  courseId: string;
  studentId: string;
  studentName: string;
  studentAvatar?: string;
  studentTitle?: string;      // e.g., "Software Engineer at Google"
  rating: number;             // 1-5
  title: string;
  content: string;
  featured: boolean;          // Show on listing page
  verified: boolean;          // Verified purchase
  helpful: number;            // Helpful votes count
  createdAt: string;
  updatedAt: string;
}

export interface CourseTestimonials {
  testimonials: CourseTestimonial[];
  total: number;
  averageRating: number;
  ratingDistribution: Record<1 | 2 | 3 | 4 | 5, number>;
}

/**
 * FAQ item
 */
export interface CourseFaqItem {
  id: string;
  courseId: string;
  question: string;
  answer: string;
  order: number;
  category?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CourseFaq {
  items: CourseFaqItem[];
  total: number;
}

/**
 * Pricing tier
 */
export interface CoursePricingTier {
  id: string;
  courseId: string;
  name: string;               // e.g., "Basic", "Pro", "Enterprise"
  description: string;
  price: number;
  currency: string;
  interval?: 'one-time' | 'monthly' | 'yearly';
  features: string[];         // What's included
  highlighted: boolean;       // Featured tier
  maxSeats?: number;          // For team/enterprise tiers
  order: number;
  createdAt: string;
  updatedAt: string;
}

export interface CoursePricing {
  tiers: CoursePricingTier[];
  discounts: Array<{
    id: string;
    code: string;
    type: 'percentage' | 'fixed';
    value: number;
    validFrom: string;
    validUntil: string;
    maxUses?: number;
    usedCount: number;
  }>;
  refundPolicy: string;
  hasFreeTrial: boolean;
  trialDays?: number;
}

// =============================================================================
// FETCH FUNCTIONS
// =============================================================================

/**
 * Fetch course listing info.
 * Cache: revalidate 300s (stable, infrequent edits)
 */
export const getCourseListingInfo = cache(async (courseId: string): Promise<CourseListingInfo | null> => {
  void courseId;
  return null;
});

/**
 * Fetch course listing media.
 * Cache: revalidate 300s (stable)
 */
export const getCourseListingMedia = cache(async (courseId: string): Promise<CourseListingMedia | null> => {
  void courseId;
  return null;
});

/**
 * Fetch course testimonials.
 * Cache: revalidate 120s (moderate changes)
 */
export const getCourseTestimonials = cache(async (courseId: string): Promise<CourseTestimonials> => {
  void courseId;
  return { testimonials: [], total: 0, averageRating: 0, ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 } };
});

/**
 * Fetch single testimonial for editing.
 * Cache: revalidate 120s
 */
export const getCourseTestimonial = cache(async (testimonialId: string): Promise<CourseTestimonial | null> => {
  void testimonialId;
  return null;
});

/**
 * Fetch course FAQ.
 * Cache: revalidate 300s (stable)
 */
export const getCourseFaq = cache(async (courseId: string): Promise<CourseFaq> => {
  void courseId;
  return { items: [], total: 0 };
});

/**
 * Fetch single FAQ item for editing.
 * Cache: revalidate 300s
 */
export const getCourseFaqItem = cache(async (faqId: string): Promise<CourseFaqItem | null> => {
  void faqId;
  return null;
});

/**
 * Fetch course pricing (conditional: hasPricing).
 * Cache: revalidate 120s
 */
export const getCoursePricing = cache(async (courseId: string): Promise<CoursePricing> => {
  void courseId;
  return { tiers: [], discounts: [], refundPolicy: '', hasFreeTrial: false };
});
