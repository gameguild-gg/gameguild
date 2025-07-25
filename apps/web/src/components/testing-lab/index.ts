// Enhanced testing lab components with type safety
export { EnhancedTestingLabSessions } from './sessions/enhanced-testing-lab-sessions';
export { EnhancedTestingLabFilters } from './filters/enhanced-testing-lab-filter-controls';
export type { TestingLabSession } from './filters/enhanced-testing-lab-filter-controls';

// Legacy components (maintain backward compatibility)
export { TestingLabSessions } from './sessions/testing-lab-sessions';
export { TestingLabFilterControls } from './filters/testing-lab-filter-controls';

// Detail components
export { TestingSessionDetails } from './sessions/testing-session-details';
export { TestingRequestDetails } from '@/components/testing-lab/requests/testing-request-details';
export { TestingFeedbackDetails } from './feedback/testing-feedback-details';

// List components
export { TestingRequestList } from '@/components/testing-lab/requests/testing-request-list';
export { TestingFeedbackList } from './feedback/testing-feedback-list';
