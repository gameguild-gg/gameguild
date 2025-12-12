'use server';

// STUB: Payment actions are stubbed; endpoints are unavailable in current SDK.

export type PaymentStatus = any;

export async function getUserPaymentMethods(): Promise<any> {
  throw new Error('Not implemented (STUB): getUserPaymentMethods');
}

export async function createPaymentIntent(_paymentData: any): Promise<any> {
  throw new Error('Not implemented (STUB): createPaymentIntent');
}

export async function getPaymentStatistics(): Promise<any> {
  throw new Error('Not implemented (STUB): getPaymentStatistics');
}

export async function getMyPayments(_params?: { skip?: number; take?: number; status?: PaymentStatus; fromDate?: string; toDate?: string }): Promise<any> {
  throw new Error('Not implemented (STUB): getMyPayments');
}

export async function processPayment(_paymentId: string, _paymentData: any): Promise<any> {
  throw new Error('Not implemented (STUB): processPayment');
}

export async function refundPayment(_paymentId: string, _refundData: any): Promise<any> {
  throw new Error('Not implemented (STUB): refundPayment');
}

export async function getPaymentById(_paymentId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getPaymentById');
}

export async function getPaymentsByUserId(_userId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getPaymentsByUserId');
}

export async function createPayment(_paymentData: any): Promise<any> {
  throw new Error('Not implemented (STUB): createPayment');
}

export async function getPaymentDetails(_paymentId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getPaymentDetails');
}

export async function getAllPaymentsByUserId(_userId: string, _params?: { skip?: number; take?: number; status?: PaymentStatus }): Promise<any> {
  throw new Error('Not implemented (STUB): getAllPaymentsByUserId');
}

export async function getPaymentsByProductId(_productId: string, _params?: { skip?: number; take?: number; status?: PaymentStatus }): Promise<any> {
  throw new Error('Not implemented (STUB): getPaymentsByProductId');
}

export async function processPaymentAlternative(_paymentId: string, _paymentData: any): Promise<any> {
  throw new Error('Not implemented (STUB): processPaymentAlternative');
}

export async function refundPaymentAlternative(_paymentId: string, _refundData: any): Promise<any> {
  throw new Error('Not implemented (STUB): refundPaymentAlternative');
}

export async function cancelPayment(_paymentId: string): Promise<any> {
  throw new Error('Not implemented (STUB): cancelPayment');
}

export async function getComprehensivePaymentStats(): Promise<any> {
  throw new Error('Not implemented (STUB): getComprehensivePaymentStats');
}

export async function getRevenueReport(_params?: { fromDate?: string; toDate?: string }): Promise<any> {
  throw new Error('Not implemented (STUB): getRevenueReport');
}
