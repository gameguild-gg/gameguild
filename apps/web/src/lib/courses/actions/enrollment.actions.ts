'use server';

import { auth } from '@/auth.ts';

export interface EnrollmentStatus {
  isEnrolled: boolean;
  isFree: boolean;
  price?: number;
  productId?: string;
  enrollmentDate?: string;
  progress?: number;
}

export interface Product {
  id: string;
  title: string;
  description: string;
  price: number;
  courses: string[]; // Course IDs
  type: 'course' | 'bundle' | 'track';
}

// Mock data for demonstration
const mockEnrollments = new Map<string, EnrollmentStatus>([['user1', { isEnrolled: true, isFree: true, enrollmentDate: '2024-01-15', progress: 45 }]]);

const mockProducts = new Map<string, Product>([
  [
    'game-dev-portfolio',
    {
      id: 'prod-game-dev-1',
      title: 'Game Development Portfolio Course',
      description: 'Build an impressive portfolio of games',
      price: 0, // Free course
      courses: ['game-dev-portfolio'],
      type: 'course',
    },
  ],
  [
    'game-jam-survival',
    {
      id: 'prod-game-jam-1',
      title: 'Game Jam Survival Guide',
      description: 'Master the art of game jams',
      price: 49.99,
      courses: ['game-jam-survival'],
      type: 'course',
    },
  ],
  [
    'retro-game-development',
    {
      id: 'prod-retro-1',
      title: 'Retro Game Development Masterclass',
      description: 'Create classic-style games',
      price: 79.99,
      courses: ['retro-game-development'],
      type: 'course',
    },
  ],
]);

export async function getCourseEnrollmentStatus(courseSlug: string): Promise<EnrollmentStatus> {
  const session = await auth();

  if (!session?.user) {
    return { isEnrolled: false, isFree: false };
  }

  const userId = session.user.id;
  const product = mockProducts.get(courseSlug);

  // If course is not in mock data, return default enrollment status
  if (!product) {
    return {
      isEnrolled: false,
      isFree: true, // Default to free for courses not in mock data
      price: 0,
      productId: `prod-${courseSlug}`,
    };
  }

  // Check if user is enrolled
  const enrollment = mockEnrollments.get(userId);

  return {
    isEnrolled: enrollment?.isEnrolled || false,
    isFree: product.price === 0,
    price: product.price,
    productId: product.id,
    enrollmentDate: enrollment?.enrollmentDate,
    progress: enrollment?.progress,
  };
}

export async function getProductsContainingCourse(courseSlug: string): Promise<Product[]> {
  const products = Array.from(mockProducts.values()).filter((product) => product.courses.includes(courseSlug));

  // If no products found, create a default product for the course
  if (products.length === 0) {
    return [
      {
        id: `prod-${courseSlug}`,
        title: `Course: ${courseSlug.replace(/-/g, ' ').replace(/\b\w/g, (l) => l.toUpperCase())}`,
        description: 'Course available for enrollment',
        price: 0, // Default to free
        courses: [courseSlug],
        type: 'course',
      },
    ];
  }

  return products;
}

export async function enrollInFreeCourse(courseSlug: string): Promise<{ success: boolean; message: string }> {
  const session = await auth();

  if (!session?.user) {
    return { success: false, message: 'Authentication required' };
  }

  const product = mockProducts.get(courseSlug);

  // If course is not in mock data, treat it as a free course and allow enrollment
  if (!product) {
    const userId = session.user.id;
    mockEnrollments.set(userId, {
      isEnrolled: true,
      isFree: true,
      enrollmentDate: new Date().toISOString(),
      progress: 0,
    });
    return { success: true, message: 'Successfully enrolled in course' };
  }

  if (product.price > 0) {
    return { success: false, message: 'This course requires payment' };
  }

  // Auto-enroll in free course
  const userId = session.user.id;
  mockEnrollments.set(userId, {
    isEnrolled: true,
    isFree: true,
    enrollmentDate: new Date().toISOString(),
    progress: 0,
  });

  return { success: true, message: 'Successfully enrolled in course' };
}

export async function createPaymentIntent(productId: string): Promise<{ success: boolean; paymentUrl?: string; message: string }> {
  const session = await auth();

  if (!session?.user) {
    return { success: false, message: 'Authentication required' };
  }

  // Mock payment intent creation
  // In a real app, this would integrate with Stripe, PayPal, etc.
  const product = Array.from(mockProducts.values()).find((p) => p.id === productId);

  if (!product) {
    return { success: false, message: 'Product not found' };
  }

  if (product.price === 0) {
    return { success: false, message: 'This product is free' };
  }

  // Mock payment URL
  const paymentUrl = `/payment/${productId}?amount=${product.price}`;

  return {
    success: true,
    paymentUrl,
    message: 'Payment intent created successfully',
  };
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function processPaymentSuccess(productId: string, _paymentIntentId: string): Promise<{ success: boolean; message: string }> {
  const session = await auth();

  if (!session?.user) {
    return { success: false, message: 'Authentication required' };
  }

  // Note: paymentIntentId would be used in real implementation to verify payment
  const product = Array.from(mockProducts.values()).find((p) => p.id === productId);

  if (!product) {
    return { success: false, message: 'Product not found' };
  }

  // Auto-enroll user in all courses in the product
  const userId = session.user.id;

  // For simplicity, we'll just enroll in the first course
  // In a real app, you'd enroll in all courses in the product
  if (product.courses.length > 0) {
    mockEnrollments.set(userId, {
      isEnrolled: true,
      isFree: false,
      enrollmentDate: new Date().toISOString(),
      progress: 0,
      productId: product.id,
    });
  }

  return { success: true, message: 'Payment processed and enrollment completed' };
}

export async function checkCourseAccess(courseSlug: string): Promise<{ hasAccess: boolean; reason?: string }> {
  const session = await auth();

  if (!session?.user) {
    return { hasAccess: false, reason: 'authentication_required' };
  }

  const enrollmentStatus = await getCourseEnrollmentStatus(courseSlug);

  if (!enrollmentStatus.isEnrolled) {
    return { hasAccess: false, reason: 'not_enrolled' };
  }

  return { hasAccess: true };
}
