'use server';

// STUB: Commerce analytics actions disabled when backend endpoints are unavailable
export async function getPaymentAnalytics() { throw new Error('Not implemented (STUB)'); }
export async function getRevenueAnalytics(_fromDate?: string, _toDate?: string) { throw new Error('Not implemented (STUB)'); }
export async function getMyPaymentAnalytics(_fromDate?: string, _toDate?: string) { throw new Error('Not implemented (STUB)'); }
export async function getSubscriptionAnalytics() { throw new Error('Not implemented (STUB)'); }
export async function getProductAnalytics() { throw new Error('Not implemented (STUB)'); }
export async function getUserAnalytics() { throw new Error('Not implemented (STUB)'); }
export async function getCommerceDashboardAnalytics(_dateRange?: { from: string; to: string }) { throw new Error('Not implemented (STUB)'); }
export async function getAnalyticsSummary() { throw new Error('Not implemented (STUB)'); }
export async function exportAnalyticsData(_type: 'payments' | 'subscriptions') { throw new Error('Not implemented (STUB)'); }
export async function getProductAnalyticsCount() { throw new Error('Not implemented (STUB)'); }
export async function getProductUserCountAnalytics(_productId: string) { throw new Error('Not implemented (STUB)'); }
export async function getProductRevenueAnalytics(_productId: string) { throw new Error('Not implemented (STUB)'); }
