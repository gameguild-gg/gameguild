'use server';

import { revalidateTag } from 'next/cache';
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
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  GetApiPaymentMethodsMeData,
  PostApiPaymentIntentData,
  PostApiPaymentByIdProcessData,
  PostApiPaymentByIdRefundData,
  GetApiPaymentByIdData,
  GetApiPaymentUserByUserIdData,
  GetApiPaymentStatsData,
  PostApiPaymentsData,
  GetApiPaymentsByIdData,
  GetApiPaymentsMyPaymentsData,
  GetApiPaymentsUsersByUserIdData,
  GetApiPaymentsProductsByProductIdData,
  PostApiPaymentsByIdProcessData,
  PostApiPaymentsByIdRefundData,
  PostApiPaymentsByIdCancelData,
  GetApiPaymentsStatsData,
  GetApiPaymentsRevenueReportData,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// PAYMENT METHODS & SETUP
// =============================================================================

/**
 * Get user's payment methods
 */
export async function getMyPaymentMethods(data?: GetApiPaymentMethodsMeData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentMethodsMe({
    query: data?.query,
  });
}

/**
 * Create payment intent
 */
export async function createPaymentIntent(data?: PostApiPaymentIntentData) {
  await configureAuthenticatedClient();
  
  return postApiPaymentIntent({
    body: data?.body,
  });
}

// =============================================================================
// PAYMENT PROCESSING
// =============================================================================

/**
 * Process a payment
 */
export async function processPayment(data: PostApiPaymentByIdProcessData) {
  await configureAuthenticatedClient();
  
  const result = await postApiPaymentByIdProcess({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate payments cache
  revalidateTag('payments');
  
  return result;
}

/**
 * Refund a payment
 */
export async function refundPayment(data: PostApiPaymentByIdRefundData) {
  await configureAuthenticatedClient();
  
  const result = await postApiPaymentByIdRefund({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate payments cache
  revalidateTag('payments');
  
  return result;
}

/**
 * Create a new payment
 */
export async function createPayment(data?: PostApiPaymentsData) {
  await configureAuthenticatedClient();
  
  const result = await postApiPayments({
    body: data?.body,
  });
  
  // Revalidate payments cache
  revalidateTag('payments');
  
  return result;
}

/**
 * Process payment (alternative API)
 */
export async function processPaymentById(data: PostApiPaymentsByIdProcessData) {
  await configureAuthenticatedClient();
  
  const result = await postApiPaymentsByIdProcess({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate payments cache
  revalidateTag('payments');
  
  return result;
}

/**
 * Refund payment (alternative API)
 */
export async function refundPaymentById(data: PostApiPaymentsByIdRefundData) {
  await configureAuthenticatedClient();
  
  const result = await postApiPaymentsByIdRefund({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate payments cache
  revalidateTag('payments');
  
  return result;
}

/**
 * Cancel a payment
 */
export async function cancelPayment(data: PostApiPaymentsByIdCancelData) {
  await configureAuthenticatedClient();
  
  const result = await postApiPaymentsByIdCancel({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate payments cache
  revalidateTag('payments');
  
  return result;
}

// =============================================================================
// PAYMENT RETRIEVAL
// =============================================================================

/**
 * Get a specific payment by ID
 */
export async function getPaymentById(data: GetApiPaymentByIdData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentById({
    path: data.path,
  });
}

/**
 * Get payments by payment ID (alternative API)
 */
export async function getPaymentsById(data: GetApiPaymentsByIdData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentsById({
    path: data.path,
  });
}

/**
 * Get current user's payments
 */
export async function getMyPayments(data?: GetApiPaymentsMyPaymentsData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentsMyPayments({
    query: data?.query,
  });
}

/**
 * Get payments for a specific user
 */
export async function getPaymentsByUser(data: GetApiPaymentUserByUserIdData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentUserByUserId({
    path: data.path,
  });
}

/**
 * Get payments for a user (alternative API)
 */
export async function getPaymentsByUserId(data: GetApiPaymentsUsersByUserIdData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentsUsersByUserId({
    path: data.path,
  });
}

/**
 * Get payments for a specific product
 */
export async function getPaymentsByProduct(data: GetApiPaymentsProductsByProductIdData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentsProductsByProductId({
    path: data.path,
  });
}

// =============================================================================
// PAYMENT ANALYTICS & REPORTING
// =============================================================================

/**
 * Get payment statistics
 */
export async function getPaymentStatistics(data?: GetApiPaymentStatsData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentStats({
    query: data?.query,
  });
}

/**
 * Get detailed payment statistics
 */
export async function getDetailedPaymentStats(data?: GetApiPaymentsStatsData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentsStats({
    query: data?.query,
  });
}

/**
 * Get revenue report
 */
export async function getRevenueReport(data?: GetApiPaymentsRevenueReportData) {
  await configureAuthenticatedClient();
  
  return getApiPaymentsRevenueReport({
    query: data?.query,
  });
}
