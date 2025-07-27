'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getApiSubscriptionMe,
  getApiSubscriptionMeActive,
  getApiSubscriptionById,
  getApiSubscription,
  postApiSubscription,
  postApiSubscriptionByIdCancel,
  postApiSubscriptionByIdResume,
  putApiSubscriptionByIdPaymentMethod,
  getApiProductByIdSubscriptionPlans,
} from '@/lib/api/generated/sdk.gen';

// Get my subscriptions
export async function getMySubscriptions() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiSubscriptionMe();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get my subscriptions:', error);
    return { success: false, error: 'Failed to get my subscriptions' };
  }
}

// Get my active subscriptions
export async function getMyActiveSubscriptions() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiSubscriptionMeActive();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get active subscriptions:', error);
    return { success: false, error: 'Failed to get active subscriptions' };
  }
}

// Get subscription by ID
export async function getSubscriptionById(subscriptionId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getApiSubscriptionById({
      path: { id: subscriptionId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get subscription:', error);
    return { success: false, error: 'Failed to get subscription' };
  }
}

// Get all subscriptions
export async function getAllSubscriptions() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiSubscription();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get subscriptions:', error);
    return { success: false, error: 'Failed to get subscriptions' };
  }
}

// Create subscription
export async function createSubscription(data?: { planId?: string; paymentMethodId?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiSubscription({
      body: data,
    });

    if (result.data) {
      revalidateTag('subscriptions');
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to create subscription:', error);
    return { success: false, error: 'Failed to create subscription' };
  }
}

// Cancel subscription
export async function cancelSubscription(subscriptionId: string, data?: { reason?: string; cancelAtPeriodEnd?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiSubscriptionByIdCancel({
      path: { id: subscriptionId },
      body: data,
    });

    if (result.data) {
      revalidateTag('subscriptions');
      revalidateTag(`subscription-${subscriptionId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to cancel subscription:', error);
    return { success: false, error: 'Failed to cancel subscription' };
  }
}

// Resume subscription
export async function resumeSubscription(subscriptionId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiSubscriptionByIdResume({
      path: { id: subscriptionId },
    });

    if (result.data) {
      revalidateTag('subscriptions');
      revalidateTag(`subscription-${subscriptionId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to resume subscription:', error);
    return { success: false, error: 'Failed to resume subscription' };
  }
}

// Update subscription payment method
export async function updateSubscriptionPaymentMethod(subscriptionId: string, data?: { paymentMethodId?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await putApiSubscriptionByIdPaymentMethod({
      path: { id: subscriptionId },
      body: data,
    });

    if (result.data) {
      revalidateTag('subscriptions');
      revalidateTag(`subscription-${subscriptionId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to update subscription payment method:', error);
    return { success: false, error: 'Failed to update subscription payment method' };
  }
}

// Get subscription plans for a product
export async function getProductSubscriptionPlans(productId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getApiProductByIdSubscriptionPlans({
      path: { id: productId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get product subscription plans:', error);
    return { success: false, error: 'Failed to get product subscription plans' };
  }
}
