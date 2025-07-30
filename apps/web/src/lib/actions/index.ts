'use server';

// Authentication actions
export * from '../auth/auth-actions';
export * from '../auth/server-actions';

// User management actions
export * from '../users/users.actions';

// Project actions
export * from '../projects/projects.actions';

// Achievement actions
export * from '../achievements/achievements.actions';

// Program actions
export * from '../programs/programs.actions';

// Dashboard actions
export * from '../dashboard/refresh-actions';

// Feed actions
export * from '../feed/feed.actions';

// Post actions
export * from '../posts/posts.actions';

// Product actions
export * from '../products/products.actions';

// Tenant actions
export * from '../tenants/tenant.actions';

// Subscription actions
export * from '../subscriptions/subscriptions.actions';

// Integration actions
export * from '../integrations/github/github.actions';

// Track actions
export * from '../tracks/actions/tracks.actions';

// Course actions - main courses
export * from '../courses/actions/courses.actions';

// Course enrollment actions
export {
  getCourseEnrollmentStatus,
  getProductsContainingCourse,
  enrollInFreeCourse,
  createPaymentIntent,
  processPaymentSuccess,
  checkCourseAccess,
} from '../courses/actions/enrollment.actions';

// Testing lab actions
export {
  getTestingRequestsData,
  createTestingRequest,
  updateTestingRequest,
  deleteTestingRequest,
  getTestingSessionsData,
  createTestingSession,
  updateTestingSession,
  deleteTestingSession,
} from '../testing-lab/testing-lab.actions';
