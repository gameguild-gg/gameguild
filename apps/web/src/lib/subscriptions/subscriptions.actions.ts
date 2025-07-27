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
} from '@/lib/api/generated/sdk.gen';

import type {
  GetApiSubscriptionMeData,
  GetApiSubscriptionMeActiveData,
  GetApiSubscriptionByIdData,
  GetApiSubscriptionData,
  PostApiSubscriptionData,
  PostApiSubscriptionByIdCancelData,
  PostApiSubscriptionByIdResumeData,
  PutApiSubscriptionByIdPaymentMethodData,
} from '@/lib/api/generated/types.gen';

/**
 * Get current user's subscriptions
 */
export async function getMySubscriptionsAction(params?: GetApiSubscriptionMeData) {
  await configureAuthenticatedClient();
  const result = await getApiSubscriptionMe({
    ...params,
  });

  revalidateTag('my-subscriptions');
  return result;
}

/**
 * Get current user's active subscriptions
 */
export async function getMyActiveSubscriptionsAction(params?: GetApiSubscriptionMeActiveData) {
  await configureAuthenticatedClient();
  const result = await getApiSubscriptionMeActive({
    ...params,
  });

  revalidateTag('my-subscriptions');
  revalidateTag('my-active-subscriptions');
  return result;
}

/**
 * Get a subscription by ID
 */
export async function getSubscriptionByIdAction(data: GetApiSubscriptionByIdData) {
  await configureAuthenticatedClient();
  const result = await getApiSubscriptionById({
    path: { id: data.path.id },
  });

  revalidateTag(`subscription-${data.path.id}`);
  return result;
}

/**
 * Get all subscriptions
 */
export async function getSubscriptionsAction(params?: GetApiSubscriptionData) {
  await configureAuthenticatedClient();
  const result = await getApiSubscription({
    ...params,
  });

  revalidateTag('subscriptions');
  return result;
}

/**
 * Create a new subscription
 */
export async function createSubscriptionAction(data: PostApiSubscriptionData) {
  await configureAuthenticatedClient();
  const result = await postApiSubscription({
    body: data.body,
  });

  revalidateTag('subscriptions');
  revalidateTag('my-subscriptions');
  return result;
}

/**
 * Cancel a subscription
 */
export async function cancelSubscriptionAction(data: PostApiSubscriptionByIdCancelData) {
  await configureAuthenticatedClient();
  const result = await postApiSubscriptionByIdCancel({
    path: { id: data.path.id },
    body: data.body,
  });

  revalidateTag('subscriptions');
  revalidateTag('my-subscriptions');
  revalidateTag('my-active-subscriptions');
  revalidateTag(`subscription-${data.path.id}`);
  return result;
}

/**
 * Resume a cancelled subscription
 */
export async function resumeSubscriptionAction(data: PostApiSubscriptionByIdResumeData) {
  await configureAuthenticatedClient();
  const result = await postApiSubscriptionByIdResume({
    path: { id: data.path.id },
  });

  revalidateTag('subscriptions');
  revalidateTag('my-subscriptions');
  revalidateTag('my-active-subscriptions');
  revalidateTag(`subscription-${data.path.id}`);
  return result;
}

/**
 * Update payment method for a subscription
 */
export async function updateSubscriptionPaymentMethodAction(data: PutApiSubscriptionByIdPaymentMethodData) {
  await configureAuthenticatedClient();
  const result = await putApiSubscriptionByIdPaymentMethod({
    path: { id: data.path.id },
    body: data.body,
  });

  revalidateTag('subscriptions');
  revalidateTag('my-subscriptions');
  revalidateTag(`subscription-${data.path.id}`);
  return result;
}
