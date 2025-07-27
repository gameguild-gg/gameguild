'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type { PaymentStatus } from '@/lib/api/generated/types.gen';
import {
  getApiPaymentMethodsMe,
  postApiPaymentIntent,
  getApiPaymentStats,
  getApiPaymentsMyPayments,
  postApiPaymentByIdProcess,
  postApiPaymentByIdRefund,
  getApiPaymentById,
  getApiPaymentUserByUserId,
  postApiPayments,
  getApiPaymentsById,
  getApiPaymentsUsersByUserId,
  getApiPaymentsProductsByProductId,
  postApiPaymentsByIdProcess,
  postApiPaymentsByIdRefund,
  postApiPaymentsByIdCancel,
  getApiPaymentsStats,
  getApiPaymentsRevenueReport,
} from '@/lib/api/generated/sdk.gen';

/**
 * Get user's payment methods
 */
export async function getUserPaymentMethods() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentMethodsMe();
    return response.data;
  } catch (error) {
    console.error('Error fetching payment methods:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch payment methods');
  }
}

/**
 * Create a payment intent
 */
export async function createPaymentIntent(paymentData: {
  amount: number;
  currency: string;
  productId?: string;
  subscriptionId?: string;
  metadata?: Record<string, unknown>;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiPaymentIntent({
      body: paymentData,
    });
    revalidateTag('payments');
    return response.data;
  } catch (error) {
    console.error('Error creating payment intent:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create payment intent');
  }
}

/**
 * Get payment statistics
 */
export async function getPaymentStatistics() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentStats();
    return response.data;
  } catch (error) {
    console.error('Error fetching payment statistics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch payment statistics');
  }
}

/**
 * Get current user's payments
 */
export async function getMyPayments(params?: { skip?: number; take?: number; status?: PaymentStatus; fromDate?: string; toDate?: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsMyPayments({
      query: {
        skip: params?.skip,
        take: params?.take,
        status: params?.status,
        fromDate: params?.fromDate,
        toDate: params?.toDate,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching my payments:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch my payments');
  }
}

/**
 * Process a payment by ID
 */
export async function processPayment(paymentId: string, paymentData: { providerTransactionId?: string; providerMetadata?: Record<string, unknown> }) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiPaymentByIdProcess({
      path: { id: paymentId },
      body: paymentData,
    });
    
    revalidateTag('payments');
    revalidateTag('payment-analytics');
    return response.data;
  } catch (error) {
    console.error('Error processing payment:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to process payment');
  }
}

/**
 * Refund a payment by ID
 */
export async function refundPayment(paymentId: string, refundData: { amount?: number; reason?: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiPaymentByIdRefund({
      path: { id: paymentId },
      body: refundData,
    });
    
    revalidateTag('payments');
    revalidateTag('payment-analytics');
    return response.data;
  } catch (error) {
    console.error('Error refunding payment:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to refund payment');
  }
}

/**
 * Get payment by ID
 */
export async function getPaymentById(paymentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentById({
      path: { id: paymentId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching payment by ID:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch payment');
  }
}

/**
 * Get payments by user ID
 */
export async function getPaymentsByUserId(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentUserByUserId({
      path: { userId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching payments by user ID:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user payments');
  }
}

/**
 * Create a new payment
 */
export async function createPayment(paymentData: {
  amount: number;
  currency: string;
  productId?: string;
  subscriptionId?: string;
  paymentMethodId?: string;
  metadata?: Record<string, unknown>;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiPayments({
      body: paymentData,
    });
    
    revalidateTag('payments');
    revalidateTag('payment-analytics');
    return response.data;
  } catch (error) {
    console.error('Error creating payment:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create payment');
  }
}

/**
 * Get payment details by ID (alternative endpoint)
 */
export async function getPaymentDetails(paymentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsById({
      path: { id: paymentId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching payment details:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch payment details');
  }
}

/**
 * Get all payments by user ID with filters
 */
export async function getAllPaymentsByUserId(userId: string, params?: { skip?: number; take?: number; status?: PaymentStatus }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsUsersByUserId({
      path: { userId },
      query: {
        skip: params?.skip,
        take: params?.take,
        status: params?.status,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching user payments:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user payments');
  }
}

/**
 * Get payments by product ID
 */
export async function getPaymentsByProductId(productId: string, params?: { skip?: number; take?: number; status?: PaymentStatus }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsProductsByProductId({
      path: { productId },
      query: {
        skip: params?.skip,
        take: params?.take,
        status: params?.status,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching product payments:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product payments');
  }
}

/**
 * Process payment (alternative endpoint)
 */
export async function processPaymentAlternative(
  paymentId: string,
  paymentData: { providerTransactionId?: string; providerMetadata?: Record<string, unknown> },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiPaymentsByIdProcess({
      path: { id: paymentId },
      body: paymentData,
    });
    
    revalidateTag('payments');
    revalidateTag('payment-analytics');
    return response.data;
  } catch (error) {
    console.error('Error processing payment:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to process payment');
  }
}

/**
 * Refund payment (alternative endpoint)
 */
export async function refundPaymentAlternative(paymentId: string, refundData: { amount?: number; reason?: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiPaymentsByIdRefund({
      path: { id: paymentId },
      body: refundData,
    });
    
    revalidateTag('payments');
    revalidateTag('payment-analytics');
    return response.data;
  } catch (error) {
    console.error('Error refunding payment:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to refund payment');
  }
}

/**
 * Cancel a payment
 */
export async function cancelPayment(paymentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiPaymentsByIdCancel({
      path: { id: paymentId },
    });
    
    revalidateTag('payments');
    revalidateTag('payment-analytics');
    return response.data;
  } catch (error) {
    console.error('Error canceling payment:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to cancel payment');
  }
}

/**
 * Get comprehensive payment statistics
 */
export async function getComprehensivePaymentStats() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsStats();
    return response.data;
  } catch (error) {
    console.error('Error fetching comprehensive payment stats:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch payment statistics');
  }
}

/**
 * Get revenue report
 */
export async function getRevenueReport(params?: { fromDate?: string; toDate?: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsRevenueReport({
      query: {
        fromDate: params?.fromDate,
        toDate: params?.toDate,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching revenue report:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch revenue report');
  }
}
