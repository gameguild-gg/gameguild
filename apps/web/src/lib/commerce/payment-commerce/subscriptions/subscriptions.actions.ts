'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getApiProductByIdSubscriptionPlans,
  getApiProductSubscriptionPlansByPlanId,
  getApiSubscription,
  getApiSubscriptionById,
  getApiSubscriptionMe,
  getApiSubscriptionMeActive,
  postApiProductByIdSubscriptionPlans,
  postApiSubscription,
  postApiSubscriptionByIdCancel,
  postApiSubscriptionByIdResume,
  putApiSubscriptionByIdPaymentMethod,
} from '@/lib/api/generated/sdk.gen';
import type { SubscriptionStatus } from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

/**
 * Get current user's subscriptions
 */
export async function getMySubscriptions() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiSubscriptionMe();
    return response.data;
  } catch (error) {
    console.error('Error fetching my subscriptions:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch my subscriptions');
  }
}

/**
 * Get current user's active subscriptions
 */
export async function getMyActiveSubscriptions() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiSubscriptionMeActive();
    return response.data;
  } catch (error) {
    console.error('Error fetching active subscriptions:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch active subscriptions');
  }
}

/**
 * Get subscription by ID
 */
export async function getSubscriptionById(subscriptionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiSubscriptionById({ path: { id: subscriptionId } });
    return response.data;
  } catch (error) {
    console.error('Error fetching subscription:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch subscription');
  }
}

/**
 * Get all subscriptions (admin view)
 */
export async function getAllSubscriptions(params?: { skip?: number; take?: number; status?: SubscriptionStatus }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiSubscription({
      query: {
        skip: params?.skip,
        take: params?.take,
        status: params?.status,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching subscriptions:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch subscriptions');
  }
}

/**
 * Create a new subscription
 */
export async function createSubscription(subscriptionData: { planId: string; userId?: string; paymentMethodId?: string; startDate?: string; metadata?: Record<string, unknown> }) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiSubscription({ body: subscriptionData });
    revalidateTag('subscriptions');
    return response.data;
  } catch (error) {
    console.error('Error creating subscription:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create subscription');
  }
}

/**
 * Cancel a subscription
 */
export async function cancelSubscription(subscriptionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiSubscriptionByIdCancel({
      path: { id: subscriptionId },
    });
    revalidateTag('subscriptions');
    revalidateTag(`subscription-${subscriptionId}`);
    return response.data;
  } catch (error) {
    console.error('Error canceling subscription:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to cancel subscription');
  }
}

/**
 * Resume a subscription
 */
export async function resumeSubscription(subscriptionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiSubscriptionByIdResume({
      path: { id: subscriptionId },
    });
    revalidateTag('subscriptions');
    revalidateTag(`subscription-${subscriptionId}`);
    return response.data;
  } catch (error) {
    console.error('Error resuming subscription:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to resume subscription');
  }
}

/**
 * Update subscription payment method
 */
export async function updateSubscriptionPaymentMethod(subscriptionId: string, paymentMethodData: { paymentMethodId: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiSubscriptionByIdPaymentMethod({
      path: { id: subscriptionId },
      body: paymentMethodData,
    });
    revalidateTag('subscriptions');
    revalidateTag(`subscription-${subscriptionId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating payment method:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update payment method');
  }
}

/**
 * Get subscription plans for a product
 */
export async function getProductSubscriptionPlans(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdSubscriptionPlans({ path: { id: productId } });
    return response.data;
  } catch (error) {
    console.error('Error fetching product subscription plans:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product subscription plans');
  }
}

/**
 * Create a subscription plan for a product
 */
export async function createProductSubscriptionPlan(
  productId: string,
  planData: {
    name: string;
    description?: string;
    price: number;
    currency: string;
    interval: string;
    intervalCount?: number;
    trialPeriodDays?: number;
    features?: string[];
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdSubscriptionPlans({
      path: { id: productId },
      body: { ...planData, productId },
    });
    revalidateTag('subscription-plans');
    revalidateTag(`product-${productId}-plans`);
    return response.data;
  } catch (error) {
    console.error('Error creating subscription plan:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create subscription plan');
  }
}

/**
 * Get subscription plan details
 */
export async function getSubscriptionPlanById(planId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductSubscriptionPlansByPlanId({ path: { planId } });
    return response.data;
  } catch (error) {
    console.error('Error fetching subscription plan:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch subscription plan');
  }
}

/**
 * Check if user has active subscription for a product
 */
export async function hasActiveSubscriptionForProduct(productId: string) {
  await configureAuthenticatedClient();

  try {
    const activeSubscriptions = await getMyActiveSubscriptions();

    if (!activeSubscriptions) {
      return false;
    }

    // Check if any active subscription is for the specified product
    const hasSubscription = Array.isArray(activeSubscriptions) && activeSubscriptions.some((sub: { productId?: string }) => sub.productId === productId);

    return hasSubscription;
  } catch (error) {
    console.error('Error checking subscription status:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to check subscription status');
  }
}

/**
 * Get subscription usage and billing information
 */
export async function getSubscriptionUsage(subscriptionId: string) {
  await configureAuthenticatedClient();

  try {
    const subscription = await getSubscriptionById(subscriptionId);

    if (!subscription) {
      throw new Error('Subscription not found');
    }

    // Return subscription with usage data
    return subscription;
  } catch (error) {
    console.error('Error fetching subscription usage:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch subscription usage');
  }
}
