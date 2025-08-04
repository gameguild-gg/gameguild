import { createClient } from '@/lib/api/generated/client';
import { environment } from '@/configs/environment';

// Product Types
export interface Product {
  id: string;
  name: string;
  shortDescription?: string;
  imageUrl?: string;
  type: ProductType;
  isBundle: boolean;
  creatorId: string;
  bundleItems?: string[];
  referralCommissionPercentage: number;
  maxAffiliateDiscount: number;
  affiliateCommissionPercentage: number;
  createdAt: string;
  updatedAt: string;
}

export enum ProductType {
  Program = 'Program',
  Course = 'Course',
  Subscription = 'Subscription',
  Digital = 'Digital',
  Physical = 'Physical',
}

// Subscription Types
export interface UserSubscription {
  id: string;
  userId: string;
  subscriptionPlanId: string;
  status: SubscriptionStatus;
  externalSubscriptionId?: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  canceledAt?: string;
  endsAt?: string;
  trialEndsAt?: string;
  lastPaymentAt?: string;
  nextBillingAt?: string;
  subscriptionPlan: SubscriptionPlan;
}

export interface SubscriptionPlan {
  id: string;
  name: string;
  description?: string;
  price: number;
  currency: string;
  intervalType: SubscriptionInterval;
  intervalCount: number;
  features: string[];
}

export enum SubscriptionStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  Canceled = 'Canceled',
  PastDue = 'PastDue',
  Trialing = 'Trialing',
  Paused = 'Paused',
}

export enum SubscriptionInterval {
  Day = 'Day',
  Week = 'Week',
  Month = 'Month',
  Year = 'Year',
}

// Payment Types
export interface FinancialTransaction {
  id: string;
  fromUserId?: string;
  toUserId?: string;
  type: TransactionType;
  amount: number;
  currency: string;
  status: TransactionStatus;
  externalTransactionId?: string;
  paymentMethodId?: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

export enum TransactionType {
  Payment = 'Payment',
  Refund = 'Refund',
  Subscription = 'Subscription',
  Commission = 'Commission',
  Withdrawal = 'Withdrawal',
}

export enum TransactionStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Canceled = 'Canceled',
  Refunded = 'Refunded',
}

// API Client helper
function createAuthenticatedClient(accessToken: string) {
  return createClient({
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
  });
}

// Products API
export const productsApi = {
  // Get all published products (public)
  async getPublishedProducts(): Promise<Product[]> {
    // Since the endpoint exists but might not be in generated client yet,
    // we'll use a direct fetch for now
    try {
      const response = await fetch(`${environment.apiBaseUrl}/api/product/published`);
      if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
      return await response.json();
    } catch (error) {
      console.error('Error fetching published products:', error);
      // Return mock data for now
      return [
        {
          id: '1',
          name: 'Game Development Bootcamp',
          shortDescription: 'Complete game development course',
          imageUrl: null,
          type: ProductType.Course,
          isBundle: false,
          creatorId: 'creator-1',
          referralCommissionPercentage: 30,
          maxAffiliateDiscount: 20,
          affiliateCommissionPercentage: 30,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        },
        {
          id: '2',
          name: 'Pro Membership',
          shortDescription: 'Access to all premium features',
          imageUrl: null,
          type: ProductType.Subscription,
          isBundle: false,
          creatorId: 'creator-1',
          referralCommissionPercentage: 30,
          maxAffiliateDiscount: 20,
          affiliateCommissionPercentage: 30,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        },
      ];
    }
  },

  // Get products by type
  async getProductsByType(type: ProductType, accessToken: string): Promise<Product[]> {
    try {
      const client = createAuthenticatedClient(accessToken);
      const response = await fetch(`${environment.apiBaseUrl}/api/product/type/${type}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
      });
      if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
      return await response.json();
    } catch (error) {
      console.error(`Error fetching products by type ${type}:`, error);
      return [];
    }
  },
};

// Subscriptions API
export const subscriptionsApi = {
  // Get user's current subscription
  async getUserSubscription(accessToken: string): Promise<UserSubscription | null> {
    try {
      // This endpoint might not exist yet, so we'll implement mock data
      console.log('Fetching user subscription...');

      // Mock subscription data based on user
      return {
        id: 'sub-1',
        userId: 'user-1',
        subscriptionPlanId: 'plan-1',
        status: SubscriptionStatus.Active,
        currentPeriodStart: new Date().toISOString(),
        currentPeriodEnd: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days from now
        subscriptionPlan: {
          id: 'plan-1',
          name: 'Pro Plan',
          description: 'Access to all premium features',
          price: 29.99,
          currency: 'USD',
          intervalType: SubscriptionInterval.Month,
          intervalCount: 1,
          features: ['Unlimited projects', 'Advanced analytics', 'Priority support', 'Custom domains'],
        },
      };
    } catch (error) {
      console.error('Error fetching user subscription:', error);
      return null;
    }
  },

  // Get available subscription plans
  async getSubscriptionPlans(): Promise<SubscriptionPlan[]> {
    try {
      // Mock subscription plans
      return [
        {
          id: 'plan-free',
          name: 'Free Trial',
          description: 'Try our platform for free',
          price: 0,
          currency: 'USD',
          intervalType: SubscriptionInterval.Month,
          intervalCount: 1,
          features: ['5 projects', 'Basic support'],
        },
        {
          id: 'plan-pro',
          name: 'Pro Plan',
          description: 'For serious developers',
          price: 29.99,
          currency: 'USD',
          intervalType: SubscriptionInterval.Month,
          intervalCount: 1,
          features: ['Unlimited projects', 'Advanced analytics', 'Priority support', 'Custom domains'],
        },
        {
          id: 'plan-enterprise',
          name: 'Enterprise',
          description: 'For teams and organizations',
          price: 99.99,
          currency: 'USD',
          intervalType: SubscriptionInterval.Month,
          intervalCount: 1,
          features: ['Everything in Pro', 'Team management', 'SSO integration', 'Dedicated support'],
        },
      ];
    } catch (error) {
      console.error('Error fetching subscription plans:', error);
      return [];
    }
  },
};

// Payments API
export const paymentsApi = {
  // Get user's transaction history
  async getUserTransactions(accessToken: string): Promise<FinancialTransaction[]> {
    try {
      // Mock transaction data
      return [
        {
          id: 'tx-1',
          fromUserId: 'user-1',
          toUserId: 'game-guild',
          type: TransactionType.Subscription,
          amount: 29.99,
          currency: 'USD',
          status: TransactionStatus.Completed,
          description: 'Pro Plan Monthly Subscription',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        },
      ];
    } catch (error) {
      console.error('Error fetching user transactions:', error);
      return [];
    }
  },
};
