import { cache } from 'react';
import { getCourse } from './course';
import { learningApiGet } from './http';

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

interface CourseMetadata {
  landingFaq?: Array<{
    question?: unknown;
    answer?: unknown;
    category?: unknown;
  }>;
}

function parseCourseMetadata(raw: string | null | undefined): CourseMetadata | null {
  if (!raw) return null;

  try {
    const parsed = JSON.parse(raw) as unknown;
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed as CourseMetadata : null;
  } catch {
    return null;
  }
}

function getMetadataFaqItems(course: {
  id: string;
  createdAt: string;
  updatedAt: string;
  metadata?: string | null;
}): CourseFaqItem[] | null {
  const metadata = parseCourseMetadata(course.metadata);
  const rawItems = Array.isArray(metadata?.landingFaq) ? metadata.landingFaq : [];

  const items = rawItems
    .map((item, index): CourseFaqItem | null => {
      const question = typeof item.question === 'string' ? item.question.trim() : '';
      const answer = typeof item.answer === 'string' ? item.answer.trim() : '';

      if (!question || !answer) return null;

      return {
        id: `${course.id}-faq-${index + 1}`,
        courseId: course.id,
        question,
        answer,
        order: index + 1,
        category: typeof item.category === 'string' && item.category.trim() ? item.category.trim() : 'Course details',
        createdAt: course.createdAt,
        updatedAt: course.updatedAt,
      };
    })
    .filter((item): item is CourseFaqItem => Boolean(item));

  return items.length > 0 ? items : null;
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
  const course = await getCourse(courseId);
  if (!course) return null;

  const skillsProvided = course.skillsProvided?.split(',').map((skill) => skill.trim()).filter(Boolean) ?? [];
  const skillsRequired = course.skillsRequired?.split(',').map((skill) => skill.trim()).filter(Boolean) ?? [];

  return {
    id: course.id,
    courseId: course.id,
    headline: course.title,
    description: course.description,
    objectives: skillsProvided.length > 0 ? skillsProvided : ['Complete the published course curriculum'],
    requirements: skillsRequired,
    targetAudience: [course.category],
    language: 'English',
    subtitles: [],
    level: course.difficulty.toLowerCase() === 'beginner'
      ? 'beginner'
      : course.difficulty.toLowerCase() === 'intermediate'
        ? 'intermediate'
        : course.difficulty.toLowerCase() === 'advanced'
          ? 'advanced'
          : 'all-levels',
    estimatedDuration: course.estimatedHours ?? 0,
    lastUpdated: course.updatedAt,
    updatedAt: course.updatedAt,
  };
});

/**
 * Fetch course listing media.
 * Cache: revalidate 300s (stable)
 */
export const getCourseListingMedia = cache(async (courseId: string): Promise<CourseListingMedia | null> => {
  const course = await getCourse(courseId);
  if (!course) return null;

  return {
    id: course.id,
    courseId: course.id,
    coverImage: course.thumbnail
      ? {
          url: course.thumbnail,
          alt: `${course.title} cover`,
          width: 1280,
          height: 720,
        }
      : null,
    promoVideo: course.videoShowcaseUrl
      ? {
          url: course.videoShowcaseUrl,
          duration: 0,
          thumbnailUrl: course.thumbnail ?? '',
        }
      : null,
    gallery: [],
    updatedAt: course.updatedAt,
  };
});

interface CourseReviewApiDto {
  id: string;
  courseId: string;
  userId: string;
  rating: number;
  title?: string | null;
  content?: string | null;
  isVerifiedPurchase?: boolean;
  helpfulCount?: number;
  isApproved?: boolean;
  isFeatured?: boolean;
  createdAt?: string;
}

function mapReviewToTestimonial(dto: CourseReviewApiDto): CourseTestimonial {
  const createdAt = dto.createdAt ?? new Date().toISOString();

  return {
    id: dto.id,
    courseId: dto.courseId,
    studentId: dto.userId,
    studentName: `Student ${dto.userId.slice(0, 8)}`,
    rating: dto.rating,
    title: dto.title ?? 'Course review',
    content: dto.content ?? '',
    featured: dto.isFeatured ?? false,
    verified: dto.isVerifiedPurchase ?? false,
    helpful: dto.helpfulCount ?? 0,
    createdAt,
    updatedAt: createdAt,
  };
}

/**
 * Fetch course testimonials.
 * Cache: revalidate 120s (moderate changes)
 */
export const getCourseTestimonials = cache(async (courseId: string): Promise<CourseTestimonials> => {
  const reviews = await learningApiGet<CourseReviewApiDto[]>(`/api/social/courses/${courseId}/reviews?skip=0&take=100&approvedOnly=false`, 120);
  const testimonials = (reviews ?? []).map(mapReviewToTestimonial);
  const ratingDistribution: Record<1 | 2 | 3 | 4 | 5, number> = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 };

  for (const testimonial of testimonials) {
    const rating = Math.min(5, Math.max(1, Math.round(testimonial.rating))) as 1 | 2 | 3 | 4 | 5;
    ratingDistribution[rating] += 1;
  }

  const averageRating = testimonials.length > 0
    ? testimonials.reduce((sum, testimonial) => sum + testimonial.rating, 0) / testimonials.length
    : 0;

  return { testimonials, total: testimonials.length, averageRating, ratingDistribution };
});

/**
 * Fetch single testimonial for editing.
 * Cache: revalidate 120s
 */
export const getCourseTestimonial = cache(async (testimonialId: string): Promise<CourseTestimonial | null> => {
  const review = await learningApiGet<CourseReviewApiDto>(`/api/social/reviews/${testimonialId}`, 120);
  return review ? mapReviewToTestimonial(review) : null;
});

/**
 * Fetch course FAQ.
 * Cache: revalidate 300s (stable)
 */
export const getCourseFaq = cache(async (courseId: string): Promise<CourseFaq> => {
  const course = await getCourse(courseId);
  if (!course) return { items: [], total: 0 };

  const metadataFaqItems = getMetadataFaqItems(course);
  if (metadataFaqItems) return { items: metadataFaqItems, total: metadataFaqItems.length };

  const createdAt = course.createdAt;
  const items: CourseFaqItem[] = [
    {
      id: `${course.id}-duration`,
      courseId: course.id,
      question: 'How long does the course take?',
      answer: course.estimatedHours ? `${course.estimatedHours} hours of estimated work.` : 'The duration depends on the published curriculum.',
      order: 1,
      category: 'Course details',
      createdAt,
      updatedAt: course.updatedAt,
    },
    {
      id: `${course.id}-level`,
      courseId: course.id,
      question: 'What level is this course?',
      answer: `${course.difficulty} level in ${course.category}.`,
      order: 2,
      category: 'Course details',
      createdAt,
      updatedAt: course.updatedAt,
    },
  ];

  return { items, total: items.length };
});

/**
 * Fetch single FAQ item for editing.
 * Cache: revalidate 300s
 */
export const getCourseFaqItem = cache(async (faqId: string): Promise<CourseFaqItem | null> => {
  const courseId = faqId.split('-').slice(0, -1).join('-');
  if (!courseId) return null;

  const faq = await getCourseFaq(courseId);
  return faq.items.find((item) => item.id === faqId) ?? null;
});

/**
 * Fetch course pricing (conditional: hasPricing).
 * Cache: revalidate 120s
 */
export const getCoursePricing = cache(async (courseId: string): Promise<CoursePricing> => {
  const pricing = await learningApiGet<{
    price?: number;
    currency?: string | null;
    isSubscription?: boolean;
    subscriptionDurationDays?: number | null;
    isMonetizationEnabled?: boolean;
  }>(`/v1/courses/${courseId}/pricing`, 120);

  if (!pricing?.isMonetizationEnabled) {
    return {
      tiers: [],
      discounts: [],
      refundPolicy: 'Free courses do not collect payment.',
      hasFreeTrial: false,
    };
  }

  const interval = pricing.isSubscription
    ? pricing.subscriptionDurationDays && pricing.subscriptionDurationDays >= 365
      ? 'yearly'
      : 'monthly'
    : 'one-time';

  return {
    tiers: [
      {
        id: `${courseId}-standard`,
        courseId,
        name: 'Standard access',
        description: 'Primary course access configured on the course pricing endpoint.',
        price: pricing.price ?? 0,
        currency: pricing.currency ?? 'USD',
        interval,
        features: ['Course content access', 'Assessments', 'Discussion access', 'Certificate eligibility'],
        highlighted: true,
        order: 1,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ],
    discounts: [],
    refundPolicy: 'Refund handling follows the platform billing policy configured for this workspace.',
    hasFreeTrial: false,
  };
});
