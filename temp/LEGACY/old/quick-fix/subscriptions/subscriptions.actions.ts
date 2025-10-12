import { revalidateTag, unstable_cache } from 'next/cache';
import { auth } from '@/auth';

// Types for Subscriptions
export interface Subscription {
  id: string;
  userId: string;
  productId?: string;
  planName: string;
  status: 'Active' | 'Cancelled' | 'Expired' | 'Pending';
  startDate: string;
  endDate?: string;
  nextBillingDate?: string;
  amount: number;
  currency: string;
  interval: 'monthly' | 'yearly' | 'lifetime';
  trialEndDate?: string;
  isActive: boolean;
  metadata?: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
  tenantId: string;
}

export interface SubscriptionPlan {
  id: string;
  name: string;
  description?: string;
  price: number;
  currency: string;
  interval: 'monthly' | 'yearly' | 'lifetime';
  features: string[];
  isActive: boolean;
  isPopular?: boolean;
  trialDays?: number;
  metadata?: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
  tenantId: string;
}

export interface SubscriptionStatistics {
  totalSubscriptions: number;
  activeSubscriptions: number;
  cancelledSubscriptions: number;
  expiredSubscriptions: number;
  totalRevenue: number;
  monthlyRevenue: number;
  averageSubscriptionValue: number;
  churnRate: number;
  newSubscriptionsToday: number;
  newSubscriptionsThisWeek: number;
  newSubscriptionsThisMonth: number;
}

export interface SubscriptionsResponse {
  success: boolean;
  data?: Subscription[];
  error?: string;
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export interface SubscriptionPlansResponse {
  success: boolean;
  data?: SubscriptionPlan[];
  error?: string;
}

// Cache configuration
const CACHE_TAGS = {
  SUBSCRIPTIONS: 'subscriptions',
  SUBSCRIPTION_PLANS: 'subscription-plans',
  SUBSCRIPTION_STATISTICS: 'subscription-statistics',
  USER_SUBSCRIPTION: 'user-subscription',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get user's subscriptions
 */
export async function getUserSubscriptions(userId?: string, page: number = 1, limit: number = 20): Promise<SubscriptionsResponse> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const targetUserId = userId || session.userId;
    const endpoint = targetUserId ? `/api/subscription/user/${targetUserId}` : '/api/subscription/me';

    const queryParams = new URLSearchParams({
      page: page.toString(),
      limit: limit.toString(),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}${endpoint}?${queryParams}`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.SUBSCRIPTIONS, targetUserId ? `user-subscription-${targetUserId}` : CACHE_TAGS.USER_SUBSCRIPTION],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const result = await response.json();
    const subscriptions = Array.isArray(result) ? result : result.data || [];

    return {
      success: true,
      data: subscriptions,
      pagination: {
        page,
        limit,
        total: result.total || subscriptions.length,
        totalPages: Math.ceil((result.total || subscriptions.length) / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching user subscriptions:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get active subscription for current user
 */
export async function getActiveSubscription(): Promise<{ success: boolean; data?: Subscription; error?: string }> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/me/active`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.USER_SUBSCRIPTION],
      },
    });

    if (response.status === 404) {
      return { success: true, data: undefined }; // No active subscription
    }

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const subscription: Subscription = await response.json();
    return { success: true, data: subscription };
  } catch (error) {
    console.error('Error fetching active subscription:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get all subscriptions (admin only)
 */
export async function getAllSubscriptions(page: number = 1, limit: number = 20, status?: string): Promise<SubscriptionsResponse> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const queryParams = new URLSearchParams({
      page: page.toString(),
      limit: limit.toString(),
      ...(status && { status }),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/all?${queryParams}`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.SUBSCRIPTIONS],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const result = await response.json();
    const subscriptions = Array.isArray(result) ? result : result.data || [];

    return {
      success: true,
      data: subscriptions,
      pagination: {
        page,
        limit,
        total: result.total || subscriptions.length,
        totalPages: Math.ceil((result.total || subscriptions.length) / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching all subscriptions:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get subscription statistics
 */
export async function getSubscriptionStatistics(): Promise<{ success: boolean; data?: SubscriptionStatistics; error?: string }> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/statistics`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.SUBSCRIPTION_STATISTICS],
      },
    });

    if (!response.ok) {
      // If endpoint doesn't exist, return mock data
      if (response.status === 404) {
        return {
          success: true,
          data: {
            totalSubscriptions: 0,
            activeSubscriptions: 0,
            cancelledSubscriptions: 0,
            expiredSubscriptions: 0,
            totalRevenue: 0,
            monthlyRevenue: 0,
            averageSubscriptionValue: 0,
            churnRate: 0,
            newSubscriptionsToday: 0,
            newSubscriptionsThisWeek: 0,
            newSubscriptionsThisMonth: 0,
          },
        };
      }

      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const statistics: SubscriptionStatistics = await response.json();
    return { success: true, data: statistics };
  } catch (error) {
    console.error('Error fetching subscription statistics:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Cancel subscription
 */
export async function cancelSubscription(subscriptionId: string): Promise<{ success: boolean; error?: string }> {
  'use server';

  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/${subscriptionId}/cancel`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    // Revalidate cache
    revalidateTag(CACHE_TAGS.SUBSCRIPTIONS);
    revalidateTag(CACHE_TAGS.USER_SUBSCRIPTION);
    revalidateTag(CACHE_TAGS.SUBSCRIPTION_STATISTICS);

    return { success: true };
  } catch (error) {
    console.error('Error cancelling subscription:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Reactivate subscription
 */
export async function reactivateSubscription(subscriptionId: string): Promise<{ success: boolean; error?: string }> {
  'use server';

  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/${subscriptionId}/reactivate`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    // Revalidate cache
    revalidateTag(CACHE_TAGS.SUBSCRIPTIONS);
    revalidateTag(CACHE_TAGS.USER_SUBSCRIPTION);
    revalidateTag(CACHE_TAGS.SUBSCRIPTION_STATISTICS);

    return { success: true };
  } catch (error) {
    console.error('Error reactivating subscription:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Server-only function to refresh subscriptions cache
 */
export async function refreshSubscriptions(): Promise<void> {
  'use server';
  revalidateTag(CACHE_TAGS.SUBSCRIPTIONS);
  revalidateTag(CACHE_TAGS.USER_SUBSCRIPTION);
  revalidateTag(CACHE_TAGS.SUBSCRIPTION_STATISTICS);
  revalidateTag(CACHE_TAGS.SUBSCRIPTION_PLANS);
}

/**
 * Cached version of getUserSubscriptions for better performance
 */
export const getCachedUserSubscriptions = unstable_cache(
  async (userId?: string, page: number = 1, limit: number = 20) => {
    return getUserSubscriptions(userId, page, limit);
  },
  ['user-subscriptions'],
  {
    tags: [CACHE_TAGS.SUBSCRIPTIONS],
    revalidate: REVALIDATION_TIME,
  },
);
