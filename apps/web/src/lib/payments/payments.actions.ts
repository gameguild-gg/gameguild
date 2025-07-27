'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient, getAuthenticatedSession } from '@/lib/api/authenticated-client';
import {
  getApiPaymentMethodsMe,
  postApiPaymentIntent,
  postApiPaymentByIdProcess,
  postApiPaymentByIdRefund,
  getApiPaymentById,
  getApiPaymentUserByUserId,
  getApiPaymentStats,
  postApiPayments,
  getApiPaymentsById,
  getApiPaymentsMyPayments,
  getApiPaymentsUsersByUserId,
  getApiPaymentsProductsByProductId,
  postApiPaymentsByIdProcess,
  postApiPaymentsByIdRefund,
  postApiPaymentsByIdCancel,
  getApiPaymentsStats,
  getApiPaymentsRevenueReport,
} from '@/lib/api/generated/sdk.gen';

// Get my payment methods
export async function getMyPaymentMethods() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentMethodsMe();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payment methods:', error);
    return { success: false, error: 'Failed to get payment methods' };
  }
}

// Create payment intent
export async function createPaymentIntent(data?: { amount?: number; currency?: string; productId?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiPaymentIntent({
      body: data,
    });

    if (result.data) {
      revalidateTag('payments');
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to create payment intent:', error);
    return { success: false, error: 'Failed to create payment intent' };
  }
}

// Process payment by ID
export async function processPayment(paymentId: string, data?: { paymentMethodId?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiPaymentByIdProcess({
      path: { id: paymentId },
      body: data,
    });

    if (result.data) {
      revalidateTag('payments');
      revalidateTag(`payment-${paymentId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to process payment:', error);
    return { success: false, error: 'Failed to process payment' };
  }
}

// Refund payment by ID
export async function refundPayment(paymentId: string, data?: { amount?: number; reason?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiPaymentByIdRefund({
      path: { id: paymentId },
      body: data,
    });

    if (result.data) {
      revalidateTag('payments');
      revalidateTag(`payment-${paymentId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to refund payment:', error);
    return { success: false, error: 'Failed to refund payment' };
  }
}

// Get payment by ID
export async function getPaymentById(paymentId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentById({
      path: { id: paymentId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payment:', error);
    return { success: false, error: 'Failed to get payment' };
  }
}

// Get payments for a user
export async function getUserPayments(userId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentUserByUserId({
      path: { userId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get user payments:', error);
    return { success: false, error: 'Failed to get user payments' };
  }
}

// Get payment statistics
export async function getPaymentStats() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentStats();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payment stats:', error);
    return { success: false, error: 'Failed to get payment stats' };
  }
}

// Create payment
export async function createPayment(data?: { amount?: number; currency?: string; userId?: string; productId?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiPayments({
      body: data,
    });

    if (result.data) {
      revalidateTag('payments');
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to create payment:', error);
    return { success: false, error: 'Failed to create payment' };
  }
}

// Get payments by ID (alternative endpoint)
export async function getPaymentsById(paymentId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentsById({
      path: { id: paymentId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payments by ID:', error);
    return { success: false, error: 'Failed to get payments by ID' };
  }
}

// Get my payments
export async function getMyPayments() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentsMyPayments();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get my payments:', error);
    return { success: false, error: 'Failed to get my payments' };
  }
}

// Get payments by user ID (alternative endpoint)
export async function getPaymentsByUserId(userId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentsUsersByUserId({
      path: { userId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payments by user ID:', error);
    return { success: false, error: 'Failed to get payments by user ID' };
  }
}

// Get payments by product ID
export async function getPaymentsByProductId(productId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentsProductsByProductId({
      path: { productId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payments by product ID:', error);
    return { success: false, error: 'Failed to get payments by product ID' };
  }
}

// Process payment (alternative endpoint)
export async function processPaymentAlternative(paymentId: string, data?: { paymentMethodId?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiPaymentsByIdProcess({
      path: { id: paymentId },
      body: data,
    });

    if (result.data) {
      revalidateTag('payments');
      revalidateTag(`payment-${paymentId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to process payment (alternative):', error);
    return { success: false, error: 'Failed to process payment' };
  }
}

// Refund payment (alternative endpoint)
export async function refundPaymentAlternative(paymentId: string, data?: { amount?: number; reason?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiPaymentsByIdRefund({
      path: { id: paymentId },
      body: data,
    });

    if (result.data) {
      revalidateTag('payments');
      revalidateTag(`payment-${paymentId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to refund payment (alternative):', error);
    return { success: false, error: 'Failed to refund payment' };
  }
}

// Cancel payment
export async function cancelPayment(paymentId: string, data?: { reason?: string }) {
  await configureAuthenticatedClient();

  try {
    const result = await postApiPaymentsByIdCancel({
      path: { id: paymentId },
      body: data,
    });

    if (result.data) {
      revalidateTag('payments');
      revalidateTag(`payment-${paymentId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to cancel payment:', error);
    return { success: false, error: 'Failed to cancel payment' };
  }
}

// Get payments statistics (alternative endpoint)
export async function getPaymentsStatsAlternative() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentsStats();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payments stats:', error);
    return { success: false, error: 'Failed to get payments stats' };
  }
}

// Get payments revenue report
export async function getPaymentsRevenueReport() {
  await configureAuthenticatedClient();

  try {
    const result = await getApiPaymentsRevenueReport();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get payments revenue report:', error);
    return { success: false, error: 'Failed to get payments revenue report' };
  }
}
