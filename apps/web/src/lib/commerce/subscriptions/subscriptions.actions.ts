'use server';

/**
 * Subscription actions - STUB implementations.
 * The Commerce/Subscriptions module is disabled in GameGuild.Production.sln
 */

// Type stubs
type SubscriptionData = { query?: Record<string, unknown> };
type SubscriptionByIdData = { path: { id: string } };
type CreateSubscriptionData = { body?: Record<string, unknown> };
type UpdateSubscriptionData = { path: { id: string }; body?: Record<string, unknown> };

// Standard stub response
const stubResponse = <T>(data?: T) => ({ data, error: undefined });
const stubError = (message: string) => ({ data: undefined, error: { message } });

// =============================================================================
// SUBSCRIPTION CRUD
// =============================================================================

export async function getSubscriptions(_data?: SubscriptionData) {
  return stubResponse({ subscriptions: [], total: 0 });
}

export async function getSubscriptionById(_data: SubscriptionByIdData) {
  return stubResponse(null);
}

export async function createSubscription(_data?: CreateSubscriptionData) {
  return stubError('Subscription creation is disabled');
}

export async function updateSubscription(_data: UpdateSubscriptionData) {
  return stubError('Subscription updates are disabled');
}

export async function cancelSubscription(_data: SubscriptionByIdData) {
  return stubError('Subscription cancellation is disabled');
}

export async function renewSubscription(_data: SubscriptionByIdData) {
  return stubError('Subscription renewal is disabled');
}

// =============================================================================
// USER SUBSCRIPTIONS
// =============================================================================

export async function getMySubscriptions(_data?: SubscriptionData) {
  return stubResponse({ subscriptions: [], total: 0 });
}

export async function getMyActiveSubscription(_data?: SubscriptionData) {
  return stubResponse(null);
}

// =============================================================================
// SUBSCRIPTION PLANS
// =============================================================================

export async function getSubscriptionPlans(_data?: SubscriptionData) {
  return stubResponse({ plans: [], total: 0 });
}

export async function getSubscriptionPlanById(_data: SubscriptionByIdData) {
  return stubResponse(null);
}
