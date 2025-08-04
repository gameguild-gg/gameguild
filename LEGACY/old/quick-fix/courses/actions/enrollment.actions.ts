'use server';

import { auth } from '@/auth';

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
const mockEnrollments = new Map<string, EnrollmentStatus>([
  [
    'user1',
    {
      isEnrolled: true,
      isFree: true,
      enrollmentDate: '2024-01-15',
      progress: 45,
    },
  ],
]);

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
  console.log('Getting enrollment status for course:', courseSlug);
  const session = await auth();
  console.log('Session:', session?.user?.id ? 'User logged in' : 'No user session');

  // Development mode - simulate user if not authenticated
  const isDevelopment = process.env.NODE_ENV === 'development';
  const mockUserId = 'dev-user-123';

  if (!session?.user && !isDevelopment) {
    console.log('No user session, returning not enrolled');
    return { isEnrolled: false, isFree: false };
  }

  const userId = session?.user?.id || (isDevelopment ? mockUserId : null);
  if (!userId) {
    return { isEnrolled: false, isFree: false };
  }

  const product = mockProducts.get(courseSlug);
  console.log('Product found:', product ? product.title : 'No product found for this slug');

  // If course is not in mock data, return default enrollment status
  if (!product) {
    console.log('Course not in mock data, returning default free course');
    return {
      isEnrolled: false,
      isFree: true, // Default to free for courses not in mock data
      price: 0,
      productId: `prod-${courseSlug}`,
    };
  }

  // Check if user is enrolled
  const enrollment = mockEnrollments.get(userId);
  console.log('User enrollment:', enrollment);

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
  console.log('Getting products for course:', courseSlug);
  const products = Array.from(mockProducts.values()).filter((product) => product.courses.includes(courseSlug));

  // If no products found, create a default product for the course
  if (products.length === 0) {
    console.log('No products found, creating default product');
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

  console.log(
    'Found products:',
    products.map((p) => p.title),
  );
  return products;
}

export async function enrollInFreeCourse(courseSlug: string): Promise<{ success: boolean; message: string }> {
  console.log('Enrolling in free course:', courseSlug);
  const session = await auth();
  console.log('Session:', session?.user?.id ? `User ${session.user.id} logged in` : 'No user session');

  // Development mode - simulate user if not authenticated
  const isDevelopment = process.env.NODE_ENV === 'development';
  const mockUserId = 'dev-user-123';

  if (!session?.user && !isDevelopment) {
    console.log('No user session, enrollment failed');
    return { success: false, message: 'Authentication required' };
  }

  const userId = session?.user?.id || (isDevelopment ? mockUserId : null);
  if (!userId) {
    return { success: false, message: 'Authentication required' };
  }

  const product = mockProducts.get(courseSlug);
  console.log('Product found:', product ? product.title : 'No product found, treating as free course');

  // If course is not in mock data, treat it as a free course and allow enrollment
  if (!product) {
    console.log('Enrolling user in default free course');
    mockEnrollments.set(userId, {
      isEnrolled: true,
      isFree: true,
      enrollmentDate: new Date().toISOString(),
      progress: 0,
    });
    console.log('Enrollment successful for free course');
    return { success: true, message: 'Successfully enrolled in course' };
  }

  if (product.price > 0) {
    console.log('Course requires payment, price:', product.price);
    return { success: false, message: 'This course requires payment' };
  }

  // Auto-enroll in free course
  console.log('Enrolling user in free course from products');
  mockEnrollments.set(userId, {
    isEnrolled: true,
    isFree: true,
    enrollmentDate: new Date().toISOString(),
    progress: 0,
  });

  console.log('Enrollment successful');
  return { success: true, message: 'Successfully enrolled in course' };
}

export async function createPaymentIntent(productId: string): Promise<{
  success: boolean;
  paymentUrl?: string;
  message: string;
}> {
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

export async function processPaymentSuccess(
  productId: string,
  _paymentIntentId: string,
): Promise<{
  success: boolean;
  message: string;
}> {
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
