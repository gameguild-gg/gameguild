import { revalidateTag } from 'next/cache';
import { auth } from '@/auth';

export interface Request {
  id: string;
  title: string;
  description: string;
  status: 'pending' | 'approved' | 'rejected' | 'in_review';
  type: 'feature' | 'bug_report' | 'content' | 'partnership' | 'general';
  priority: 'low' | 'medium' | 'high' | 'urgent';
  submittedBy: {
    id: string;
    name: string;
    email: string;
    avatar?: string;
  };
  assignedTo?: {
    id: string;
    name: string;
    email: string;
  };
  createdAt: string;
  updatedAt: string;
  dueDate?: string;
  tags?: string[];
  attachments?: Array<{
    id: string;
    name: string;
    url: string;
    type: string;
  }>;
}

export interface RequestData {
  requests: Request[];
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export interface ActionState {
  success: boolean;
  error?: string;
}

export interface RequestActionState extends ActionState {
  request?: Request;
}

export interface RequestStatistics {
  totalRequests: number;
  pendingRequests: number;
  approvedRequests: number;
  rejectedRequests: number;
  inReviewRequests: number;
  requestsCreatedToday: number;
  requestsCreatedThisWeek: number;
  requestsCreatedThisMonth: number;
  averageProcessingTime: number;
}

// Cache configuration
const CACHE_TAGS = {
  REQUESTS: 'requests',
  REQUEST_DETAIL: 'request-detail',
  REQUEST_STATISTICS: 'request-statistics',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get requests data with authentication
 */
export async function getRequestsData(page: number = 1, limit: number = 20, search?: string): Promise<RequestData> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.accessToken) {
      return {
        requests: [],
        pagination: { page, limit, total: 0, totalPages: 0 },
      };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    const skip = (page - 1) * limit;

    if (search) {
      // Use search endpoint for text search
      const params = new URLSearchParams({
        searchTerm: search,
        skip: skip.toString(),
        take: limit.toString(),
      });

      const response = await fetch(`${apiUrl}/api/requests/search?${params}`, {
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          'Content-Type': 'application/json',
        },
        next: {
          revalidate: REVALIDATION_TIME,
          tags: [CACHE_TAGS.REQUESTS],
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to search requests: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return {
        requests: data.items,
        pagination: {
          page,
          limit,
          total: data.totalCount,
          totalPages: Math.ceil(data.totalCount / limit),
        },
      };
    }

    // Use regular requests endpoint
    const params = new URLSearchParams({
      skip: skip.toString(),
      take: limit.toString(),
    });

    const response = await fetch(`${apiUrl}/api/requests?${params}`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.REQUESTS],
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch requests: ${response.status} ${response.statusText}`);
    }

    const requests: Request[] = await response.json();

    return {
      requests,
      pagination: {
        page,
        limit,
        total: requests.length, // Note: This is the current page count, not total
        totalPages: Math.ceil(requests.length / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching requests:', error);

    // Return empty data for network errors
    if (error instanceof TypeError && error.message.includes('fetch')) {
      return {
        requests: [],
        pagination: { page, limit, total: 0, totalPages: 0 },
      };
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch requests');
  }
}

/**
 * Get request by ID with authentication
 */
export async function getRequestById(id: string): Promise<Request | null> {
  try {
    const session = await auth();

    if (!session?.accessToken) {
      throw new Error('Unauthorized');
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/requests/${id}`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.REQUEST_DETAIL, `request-${id}`],
      },
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch request: ${response.status} ${response.statusText}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching request by ID:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch request');
  }
}

/**
 * Get request statistics
 */
export async function getRequestStatistics(): Promise<RequestStatistics> {
  try {
    const session = await auth();

    if (!session?.accessToken) {
      throw new Error('Unauthorized');
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/requests/statistics`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.REQUEST_STATISTICS],
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch request statistics: ${response.status} ${response.statusText}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching request statistics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch request statistics');
  }
}

/**
 * Revalidate requests data cache
 */
export async function revalidateRequestsData(): Promise<void> {
  revalidateTag(CACHE_TAGS.REQUESTS);
  revalidateTag(CACHE_TAGS.REQUEST_STATISTICS);
}
