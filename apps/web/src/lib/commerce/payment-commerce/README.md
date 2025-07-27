# Payment & Commerce System

A comprehensive server actions module for payment processing, subscription management, and analytics for the Game Guild platform.

## 📁 Directory Structure

```
/lib/payment-commerce/
├── index.ts                        # Main exports
├── payments/
│   └── payments.actions.ts          # Payment processing actions (15 functions)
├── subscriptions/
│   └── subscriptions.actions.ts     # Subscription management actions (13 functions)
└── analytics/
    └── analytics.actions.ts         # Analytics and reporting actions (8 functions)
```

## 🚀 Features

### Payments Module (15 Functions)
- **Payment Methods**: Get user payment methods, create/delete payment methods
- **Payment Processing**: Create payment intents, process payments, handle refunds
- **Payment Retrieval**: Get payments by ID, user payments, product payments
- **Payment Statistics**: Get payment stats and analytics
- **Payment Management**: Cancel payments and track payment status

### Subscriptions Module (13 Functions)
- **User Subscriptions**: Get user's subscriptions, active subscriptions
- **Subscription Management**: Create, cancel, resume subscriptions
- **Payment Method Updates**: Update subscription payment methods
- **Subscription Plans**: Manage product subscription plans
- **Subscription Analytics**: Check subscription status and usage

### Analytics Module (8 Functions)
- **Payment Analytics**: Payment statistics and revenue reporting
- **Subscription Analytics**: Subscription metrics and churn analysis
- **Product Analytics**: Product performance tracking
- **User Analytics**: User statistics and insights
- **Dashboard Analytics**: Comprehensive commerce dashboard
- **Data Export**: Export analytics data in CSV format

## 🛠️ Installation & Usage

### Import Functions

```typescript
// Import all functions
import * from '@/lib/payment-commerce';

// Import specific modules
import { getUserPaymentMethods, processPayment } from '@/lib/payment-commerce/payments/payments.actions';
import { getMySubscriptions, cancelSubscription } from '@/lib/payment-commerce/subscriptions/subscriptions.actions';
import { getPaymentAnalytics, getCommerceDashboardAnalytics } from '@/lib/payment-commerce/analytics/analytics.actions';
```

### Example Usage

#### Payment Processing
```typescript
// Get user's payment methods
const { data: paymentMethods, error } = await getUserPaymentMethods();

// Create a payment intent
const { data: paymentIntent, error } = await createPaymentIntent({
  amount: 9.99,
  currency: 'USD',
  productId: 'product-123',
  paymentMethodId: 'pm-123'
});

// Process a payment
const { data: payment, error } = await processPayment({
  paymentIntentId: 'pi-123',
  providerTransactionId: 'txn-456',
  providerMetadata: { gateway: 'stripe' }
});
```

#### Subscription Management
```typescript
// Get user's active subscriptions
const { data: subscriptions, error } = await getMyActiveSubscriptions();

// Create a new subscription
const { data: subscription, error } = await createSubscription({
  planId: 'plan-123',
  paymentMethodId: 'pm-456'
});

// Cancel a subscription
const { data: canceledSub, error } = await cancelSubscription('sub-123');
```

#### Analytics & Reporting
```typescript
// Get payment analytics
const { data: paymentStats, error } = await getPaymentAnalytics();

// Get comprehensive dashboard data
const { data: dashboard, error } = await getCommerceDashboardAnalytics({
  from: '2024-01-01',
  to: '2024-12-31'
});

// Export analytics data
const { data: csvData, error } = await exportAnalyticsData('payments');
```

## 🔧 API Functions Reference

### Payment Actions

| Function | Description | Parameters |
|----------|-------------|------------|
| `getUserPaymentMethods()` | Get user's payment methods | None |
| `createPaymentMethod(data)` | Create new payment method | PaymentMethodData |
| `deletePaymentMethod(id)` | Delete payment method | paymentMethodId: string |
| `createPaymentIntent(data)` | Create payment intent | PaymentIntentData |
| `processPayment(data)` | Process a payment | ProcessPaymentData |
| `refundPayment(id, data)` | Refund a payment | paymentId: string, RefundData |
| `getPaymentById(id)` | Get payment by ID | paymentId: string |
| `getPaymentStatistics()` | Get payment statistics | None |
| `getMyPayments(params)` | Get user's payments | Optional filters |
| `getUserPayments(userId, params)` | Get payments by user | userId: string, filters |
| `getProductPayments(productId, params)` | Get payments by product | productId: string, filters |
| `cancelPayment(id)` | Cancel a payment | paymentId: string |
| `getPaymentStatus(id)` | Get payment status | paymentId: string |
| `updatePaymentMetadata(id, data)` | Update payment metadata | paymentId: string, metadata |
| `validatePayment(id)` | Validate payment | paymentId: string |

### Subscription Actions

| Function | Description | Parameters |
|----------|-------------|------------|
| `getMySubscriptions()` | Get user's subscriptions | None |
| `getMyActiveSubscriptions()` | Get user's active subscriptions | None |
| `getSubscriptionById(id)` | Get subscription by ID | subscriptionId: string |
| `getAllSubscriptions(params)` | Get all subscriptions (admin) | Optional filters |
| `createSubscription(data)` | Create new subscription | SubscriptionData |
| `cancelSubscription(id)` | Cancel subscription | subscriptionId: string |
| `resumeSubscription(id)` | Resume subscription | subscriptionId: string |
| `updateSubscriptionPaymentMethod(id, data)` | Update payment method | subscriptionId: string, PaymentMethodData |
| `getProductSubscriptionPlans(productId)` | Get product's subscription plans | productId: string |
| `createProductSubscriptionPlan(productId, data)` | Create subscription plan | productId: string, PlanData |
| `getSubscriptionPlanById(planId)` | Get subscription plan details | planId: string |
| `hasActiveSubscriptionForProduct(productId)` | Check active subscription | productId: string |
| `getSubscriptionUsage(id)` | Get subscription usage | subscriptionId: string |

### Analytics Actions

| Function | Description | Parameters |
|----------|-------------|------------|
| `getPaymentAnalytics()` | Get payment statistics | None |
| `getRevenueAnalytics(from, to)` | Get revenue analytics | Optional date range |
| `getMyPaymentAnalytics(from, to)` | Get user payment analytics | Optional date range |
| `getSubscriptionAnalytics()` | Get subscription metrics | None |
| `getProductAnalytics()` | Get product analytics | None |
| `getUserAnalytics()` | Get user statistics | None |
| `getCommerceDashboardAnalytics(range)` | Get comprehensive dashboard | Optional date range |
| `getAnalyticsSummary()` | Get analytics summary | None |
| `exportAnalyticsData(type)` | Export data as CSV | 'payments' \| 'subscriptions' |

## 🔒 Authentication

All functions automatically handle authentication through the `configureAuthenticatedClient()` helper. Ensure users are properly authenticated before calling these functions.

## ⚡ Performance Features

- **Automatic Cache Revalidation**: Functions automatically revalidate relevant cache tags
- **Error Handling**: Comprehensive error handling with user-friendly messages
- **Type Safety**: Full TypeScript support with proper typing
- **Optimized Queries**: Efficient API calls with minimal data transfer

## 🚨 Error Handling

All functions return a consistent response format:
```typescript
{
  data: T | null,
  error: string | null
}
```

Always check for errors before using the data:
```typescript
const { data, error } = await getUserPaymentMethods();
if (error) {
  console.error('Payment methods error:', error);
  return;
}
// Use data safely
console.log('Payment methods:', data);
```

## 🎯 Business Value

This payment & commerce system provides:

1. **Revenue Generation**: Complete payment processing pipeline
2. **Subscription Management**: Recurring revenue through subscriptions
3. **Analytics & Insights**: Data-driven business decisions
4. **User Experience**: Streamlined payment and subscription flows
5. **Scalability**: Built for high-volume commerce operations

## 🔄 Cache Management

The system automatically manages cache invalidation for:
- Payment data: `payments`, `payment-{id}`
- Subscription data: `subscriptions`, `subscription-{id}`
- Analytics data: Cache-friendly with reasonable TTL

## 📊 Analytics Metrics

### Payment Metrics
- Total payments and revenue
- Success rates and conversion
- Payment method performance
- Revenue trends over time

### Subscription Metrics
- Active vs canceled subscriptions
- Churn rate analysis
- Monthly/Annual recurring revenue
- Subscription lifecycle tracking

### Product Metrics
- Product-specific revenue
- Conversion rates by product
- Product performance comparison

## 🚀 Next Steps

1. **Integration**: Import and use functions in your components
2. **UI Components**: Build UI components that consume these actions
3. **Error Handling**: Implement proper error boundaries
4. **Testing**: Add unit tests for critical payment flows
5. **Monitoring**: Set up analytics dashboards using the provided functions

---

*This payment & commerce system is production-ready and follows Next.js server actions best practices with full TypeScript support.*
