'use server';

/**
 * Payment actions - STUB implementations.
 * The Commerce/Payments module is disabled in GameGuild.Production.sln
 */

// Type stubs for payment data
type PaymentMethodData = { query?: Record<string, unknown> };
type PaymentIntentData = { body?: Record<string, unknown> };
type PaymentProcessData = { path: { id: string }; body?: Record<string, unknown> };
type PaymentRefundData = { path: { id: string }; body?: Record<string, unknown> };
type PaymentCreateData = { body?: Record<string, unknown> };
type PaymentCancelData = { path: { id: string }; body?: Record<string, unknown> };
type PaymentByIdData = { path: { id: string } };
type PaymentByUserData = { path: { userId: string } };
type PaymentByProductData = { path: { productId: string } };
type PaymentStatsData = { query?: Record<string, unknown> };
type PaymentReportData = { query?: Record<string, unknown> };
type PaymentsMyData = { query?: Record<string, unknown> };

// Standard stub response
const stubResponse = <T>(data?: T) => ({ data, error: undefined });
const stubError = (message: string) => ({ data: undefined, error: { message } });

// =============================================================================
// PAYMENT METHODS & SETUP
// =============================================================================

export async function getMyPaymentMethods(_data?: PaymentMethodData) {
  return stubResponse({ paymentMethods: [] });
}

export async function createPaymentIntent(_data?: PaymentIntentData) {
  return stubError('Payment processing is disabled');
}

// =============================================================================
// PAYMENT PROCESSING
// =============================================================================

export async function processPayment(_data: PaymentProcessData) {
  return stubError('Payment processing is disabled');
}

export async function refundPayment(_data: PaymentRefundData) {
  return stubError('Payment refunds are disabled');
}

export async function createPayment(_data?: PaymentCreateData) {
  return stubError('Payment creation is disabled');
}

export async function processPaymentById(_data: PaymentProcessData) {
  return stubError('Payment processing is disabled');
}

export async function refundPaymentById(_data: PaymentRefundData) {
  return stubError('Payment refunds are disabled');
}

export async function cancelPayment(_data: PaymentCancelData) {
  return stubError('Payment cancellation is disabled');
}

// =============================================================================
// PAYMENT RETRIEVAL
// =============================================================================

export async function getPaymentById(_data: PaymentByIdData) {
  return stubResponse(null);
}

export async function getPaymentsById(_data: PaymentByIdData) {
  return stubResponse(null);
}

export async function getMyPayments(_data?: PaymentsMyData) {
  return stubResponse({ payments: [], total: 0 });
}

export async function getPaymentsByUser(_data: PaymentByUserData) {
  return stubResponse({ payments: [], total: 0 });
}

export async function getPaymentsByUserId(_data: PaymentByUserData) {
  return stubResponse({ payments: [], total: 0 });
}

export async function getPaymentsByProduct(_data: PaymentByProductData) {
  return stubResponse({ payments: [], total: 0 });
}

// =============================================================================
// PAYMENT ANALYTICS & REPORTING
// =============================================================================

export async function getPaymentStatistics(_data?: PaymentStatsData) {
  return stubResponse({
    totalRevenue: 0,
    totalPayments: 0,
    averagePayment: 0,
    revenueByMonth: [],
  });
}

export async function getDetailedPaymentStats(_data?: PaymentStatsData) {
  return stubResponse({
    totalRevenue: 0,
    totalPayments: 0,
    averagePayment: 0,
    revenueByMonth: [],
  });
}

export async function getRevenueReport(_data?: PaymentReportData) {
  return stubResponse({
    totalRevenue: 0,
    periods: [],
  });
}
