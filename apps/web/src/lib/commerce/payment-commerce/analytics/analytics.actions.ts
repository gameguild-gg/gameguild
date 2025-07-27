'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getApiPaymentStats,
  getApiPaymentsMyPayments,
  getApiPaymentsRevenueReport,
  getApiSubscription,
  getApiProduct,
  getApiUsers,
  getApiProductAnalyticsCount,
  getApiProductByIdAnalyticsUserCount,
  getApiProductByIdAnalyticsRevenue,
} from '@/lib/api/generated/sdk.gen';

/**
 * Get payment statistics for analytics
 */
export async function getPaymentAnalytics() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentStats();
    return response.data;
  } catch (error) {
    console.error('Error fetching payment analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch payment analytics');
  }
}

/**
 * Get revenue analytics using the revenue report endpoint
 */
export async function getRevenueAnalytics(fromDate?: string, toDate?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsRevenueReport({
      query: {
        fromDate,
        toDate,
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching revenue analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch revenue analytics');
  }
}

/**
 * Get user's payment analytics
 */
export async function getMyPaymentAnalytics(fromDate?: string, toDate?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiPaymentsMyPayments({
      query: {
        fromDate,
        toDate,
      },
    });

    if (!response.data || !Array.isArray(response.data)) {
      throw new Error('Invalid payment data');
    }

    // Calculate user-specific metrics
    const payments = response.data;
    const totalPayments = payments.length;
    const totalAmount = payments.reduce((sum: number, payment) => {
      return sum + (typeof payment === 'object' && payment && 'amount' in payment ? (payment.amount as number) || 0 : 0);
    }, 0);

    const analytics = {
      totalPayments,
      totalAmount,
      averagePayment: totalPayments > 0 ? totalAmount / totalPayments : 0,
      lastPaymentDate: payments.length > 0 ? payments[payments.length - 1] : null,
    };

    return analytics;
  } catch (error) {
    console.error('Error fetching user payment analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user payment analytics');
  }
}

/**
 * Get subscription analytics
 */
export async function getSubscriptionAnalytics() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiSubscription({
      query: {
        take: 1000, // Get a large number for analytics
      },
    });

    if (!response.data || !Array.isArray(response.data)) {
      throw new Error('Invalid subscription data');
    }

    const subscriptions = response.data;
    const activeSubscriptions = subscriptions.filter((sub) => typeof sub === 'object' && sub && 'status' in sub && sub.status === 1);
    const canceledSubscriptions = subscriptions.filter((sub) => typeof sub === 'object' && sub && 'status' in sub && sub.status === 4);

    const analytics = {
      totalSubscriptions: subscriptions.length,
      activeSubscriptions: activeSubscriptions.length,
      canceledSubscriptions: canceledSubscriptions.length,
      churnRate: subscriptions.length > 0 ? (canceledSubscriptions.length / subscriptions.length) * 100 : 0,
    };

    return analytics;
  } catch (error) {
    console.error('Error fetching subscription analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch subscription analytics');
  }
}

/**
 * Get product analytics
 */
export async function getProductAnalytics() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProduct();

    if (!response.data) {
      throw new Error('Failed to fetch product data');
    }

    const products = Array.isArray(response.data) ? response.data : [];

    const analytics = {
      totalProducts: products.length,
      publishedProducts: products.filter((product) => typeof product === 'object' && product && 'isPublished' in product && product.isPublished).length,
    };

    return analytics;
  } catch (error) {
    console.error('Error fetching product analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product analytics');
  }
}

/**
 * Get user analytics and statistics
 */
export async function getUserAnalytics() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsers({
      query: {
        take: 1000,
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch user data');
    }

    const users = Array.isArray(response.data) ? response.data : [];

    const analytics = {
      totalUsers: users.length,
      activeUsers: users.filter((user) => typeof user === 'object' && user && 'isActive' in user && user.isActive).length,
    };

    return analytics;
  } catch (error) {
    console.error('Error fetching user analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user analytics');
  }
}

/**
 * Get comprehensive commerce dashboard analytics
 */
export async function getCommerceDashboardAnalytics(dateRange?: { from: string; to: string }) {
  await configureAuthenticatedClient();

  try {
    const [paymentStats, revenueAnalytics, subscriptionAnalytics, productAnalytics, userAnalytics] = await Promise.all([
      getPaymentAnalytics(),
      getRevenueAnalytics(dateRange?.from, dateRange?.to),
      getSubscriptionAnalytics(),
      getProductAnalytics(),
      getUserAnalytics(),
    ]);

    const dashboard = {
      payments: paymentStats,
      revenue: revenueAnalytics,
      subscriptions: subscriptionAnalytics,
      products: productAnalytics,
      users: userAnalytics,
      lastUpdated: new Date().toISOString(),
    };

    return dashboard;
  } catch (error) {
    console.error('Error fetching dashboard analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch dashboard analytics');
  }
}

/**
 * Get basic analytics summary
 */
export async function getAnalyticsSummary() {
  await configureAuthenticatedClient();

  try {
    const [paymentStats, myPayments] = await Promise.all([getPaymentAnalytics(), getMyPaymentAnalytics()]);

    const summary = {
      paymentStats: paymentStats,
      myPayments: myPayments,
      generatedAt: new Date().toISOString(),
    };

    return summary;
  } catch (error) {
    console.error('Error fetching analytics summary:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch analytics summary');
  }
}

/**
 * Export analytics data in a simple CSV format
 */
export async function exportAnalyticsData(type: 'payments' | 'subscriptions') {
  await configureAuthenticatedClient();

  try {
    let csvData = '';

    if (type === 'payments') {
      const myPayments = await getMyPaymentAnalytics();
      if (myPayments) {
        csvData = `Type,Total Payments,Total Amount,Average Payment\n`;
        csvData += `My Payments,${myPayments.totalPayments},${myPayments.totalAmount},${myPayments.averagePayment}\n`;
      }
    } else if (type === 'subscriptions') {
      const subscriptions = await getSubscriptionAnalytics();
      if (subscriptions) {
        csvData = `Type,Total,Active,Canceled,Churn Rate\n`;
        csvData += `Subscriptions,${subscriptions.totalSubscriptions},${subscriptions.activeSubscriptions},${subscriptions.canceledSubscriptions},${subscriptions.churnRate}%\n`;
      }
    }

    return csvData;
  } catch (error) {
    console.error('Error exporting analytics data:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to export analytics data');
  }
}

/**
 * Get product analytics count
 */
export async function getProductAnalyticsCount() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductAnalyticsCount();
    return response.data;
  } catch (error) {
    console.error('Error fetching product analytics count:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product analytics count');
  }
}

/**
 * Get product user count analytics by product ID
 */
export async function getProductUserCountAnalytics(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAnalyticsUserCount({
      path: { id: productId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching product user count analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product user count analytics');
  }
}

/**
 * Get product revenue analytics by product ID
 */
export async function getProductRevenueAnalytics(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAnalyticsRevenue({
      path: { id: productId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching product revenue analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product revenue analytics');
  }
}
