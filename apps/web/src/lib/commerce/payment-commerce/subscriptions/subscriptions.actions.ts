'use server';

// STUB: Subscription actions are stubbed; endpoints are unavailable in current SDK.

export type SubscriptionStatus = any;

export async function getMySubscriptions(): Promise<any> {
  throw new Error('Not implemented (STUB): getMySubscriptions');
}

export async function getMyActiveSubscriptions(): Promise<any> {
  throw new Error('Not implemented (STUB): getMyActiveSubscriptions');
}

export async function getSubscriptionById(_subscriptionId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getSubscriptionById');
}

export async function getAllSubscriptions(_params?: { skip?: number; take?: number; status?: SubscriptionStatus }): Promise<any> {
  throw new Error('Not implemented (STUB): getAllSubscriptions');
}

export async function createSubscription(_subscriptionData: any): Promise<any> {
  throw new Error('Not implemented (STUB): createSubscription');
}

export async function cancelSubscription(_subscriptionId: string): Promise<any> {
  throw new Error('Not implemented (STUB): cancelSubscription');
}

export async function resumeSubscription(_subscriptionId: string): Promise<any> {
  throw new Error('Not implemented (STUB): resumeSubscription');
}

export async function updateSubscriptionPaymentMethod(_subscriptionId: string, _paymentMethodData: any): Promise<any> {
  throw new Error('Not implemented (STUB): updateSubscriptionPaymentMethod');
}

export async function getProductSubscriptionPlans(_productId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getProductSubscriptionPlans');
}

export async function createProductSubscriptionPlan(_productId: string, _planData: any): Promise<any> {
  throw new Error('Not implemented (STUB): createProductSubscriptionPlan');
}

export async function getSubscriptionPlanById(_planId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getSubscriptionPlanById');
}

export async function hasActiveSubscriptionForProduct(_productId: string): Promise<any> {
  throw new Error('Not implemented (STUB): hasActiveSubscriptionForProduct');
}

export async function getSubscriptionUsage(_subscriptionId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getSubscriptionUsage');
}
