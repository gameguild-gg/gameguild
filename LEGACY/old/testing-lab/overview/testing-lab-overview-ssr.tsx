import { TestingLabOverviewClient } from './testing-lab-overview-client';

interface TestingLabStats {
  totalRequests: number;
  activeRequests: number;
  totalSessions: number;
  upcomingSessions: number;
  totalFeedback: number;
  pendingFeedback: number;
  mySubmissions: number;
  myTestingAssignments: number;
}

interface TestingLabOverviewProps {
  userRole?: 'student' | 'professor' | 'admin';
}

// Server Component - handles data fetching
export async function TestingLabOverview({ userRole = 'student' }: TestingLabOverviewProps) {
  let stats: TestingLabStats = {
    totalRequests: 0,
    activeRequests: 0,
    totalSessions: 0,
    upcomingSessions: 0,
    totalFeedback: 0,
    pendingFeedback: 0,
    mySubmissions: 0,
    myTestingAssignments: 0,
  };

  try {
    // Fetch data on the server
    if (userRole === 'student') {
      // For students, we would normally fetch their specific data
      // For now, using mock data that would come from the API
      stats = {
        totalRequests: 8,
        activeRequests: 3,
        totalSessions: 12,
        upcomingSessions: 2,
        totalFeedback: 6,
        pendingFeedback: 1,
        mySubmissions: 4,
        myTestingAssignments: 8,
      };
    } else {
      // For professors/admins
      stats = {
        totalRequests: 120,
        activeRequests: 45,
        totalSessions: 24,
        upcomingSessions: 6,
        totalFeedback: 89,
        pendingFeedback: 12,
        mySubmissions: 0,
        myTestingAssignments: 0,
      };
    }
  } catch (error) {
    console.error('Failed to load testing lab stats:', error);
    // Use fallback stats
  }

  return <TestingLabOverviewClient initialStats={stats} userRole={userRole} />;
}
