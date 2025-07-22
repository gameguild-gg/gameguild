import React, { Suspense } from 'react';
import { getUserSubscriptions, getAllSubscriptions, getSubscriptionStatistics } from '@/lib/subscriptions/subscriptions.actions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Plus, CreditCard, Users, DollarSign, TrendingUp, Eye, X, RotateCcw } from 'lucide-react';
import Link from 'next/link';

interface SubscriptionsPageProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

// Loading component
function SubscriptionsLoading() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div className="h-8 w-48 bg-gray-200 rounded animate-pulse" />
        <div className="h-10 w-32 bg-gray-200 rounded animate-pulse" />
      </div>

      {/* Statistics skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i}>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <div className="h-4 w-24 bg-gray-200 rounded animate-pulse" />
                <div className="h-6 w-6 bg-gray-200 rounded animate-pulse" />
              </div>
              <div className="h-8 w-16 bg-gray-200 rounded animate-pulse" />
              <div className="h-3 w-20 bg-gray-200 rounded animate-pulse mt-2" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

// Error component
function SubscriptionsError({ error }: { error: string }) {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="text-center">
          <CreditCard className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Unable to load subscriptions</h3>
          <p className="text-gray-600 mb-4">{error}</p>
          <Button variant="outline" asChild>
            <Link href="/dashboard/subscriptions">Try again</Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

// Helper functions
function getStatusColor(status: string) {
  switch (status) {
    case 'Active':
      return 'default';
    case 'Cancelled':
      return 'destructive';
    case 'Expired':
      return 'secondary';
    case 'Pending':
      return 'outline';
    default:
      return 'secondary';
  }
}

function getIntervalDisplay(interval: string) {
  switch (interval) {
    case 'monthly':
      return 'Monthly';
    case 'yearly':
      return 'Yearly';
    case 'lifetime':
      return 'Lifetime';
    default:
      return interval;
  }
}

// Main subscriptions content
async function SubscriptionsContent({ searchParams }: SubscriptionsPageProps) {
  const params = await searchParams;
  const page = typeof params.page === 'string' ? parseInt(params.page) : 1;
  const limit = typeof params.limit === 'string' ? parseInt(params.limit) : 20;
  const tab = typeof params.tab === 'string' ? params.tab : 'all';

  // Fetch data based on tab
  const [allSubscriptionsResult, userSubscriptionsResult, statisticsResult] = await Promise.all([
    tab === 'all' ? getAllSubscriptions(page, limit) : { success: true, data: [] },
    tab === 'user' ? getUserSubscriptions(undefined, page, limit) : { success: true, data: [] },
    getSubscriptionStatistics(),
  ]);

  const subscriptions = (tab === 'all' ? allSubscriptionsResult.data : userSubscriptionsResult.data) || [];
  const statistics = statisticsResult.data;

  if ((tab === 'all' && !allSubscriptionsResult.success) || (tab === 'user' && !userSubscriptionsResult.success)) {
    const error = 'Failed to load subscriptions';
    return <SubscriptionsError error={error} />;
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Subscriptions</h1>
          <p className="text-gray-600">Manage user subscriptions and billing</p>
        </div>
        <Button asChild>
          <Link href="/dashboard/subscriptions/plans">
            <Plus className="h-4 w-4 mr-2" />
            Manage Plans
          </Link>
        </Button>
      </div>

      {/* Statistics */}
      {statistics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Total Subscriptions</p>
                <CreditCard className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.totalSubscriptions}</div>
              <p className="text-xs text-muted-foreground">+{statistics.newSubscriptionsThisMonth} this month</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Active Subscriptions</p>
                <Users className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.activeSubscriptions}</div>
              <p className="text-xs text-muted-foreground">{statistics.cancelledSubscriptions} cancelled</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Monthly Revenue</p>
                <DollarSign className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">${statistics.monthlyRevenue.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">Total: ${statistics.totalRevenue.toLocaleString()}</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Churn Rate</p>
                <TrendingUp className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.churnRate.toFixed(1)}%</div>
              <p className="text-xs text-muted-foreground">Avg value: ${statistics.averageSubscriptionValue.toFixed(0)}</p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Tabs */}
      <Tabs value={tab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="all" asChild>
            <Link href="/dashboard/subscriptions?tab=all">All Subscriptions</Link>
          </TabsTrigger>
          <TabsTrigger value="user" asChild>
            <Link href="/dashboard/subscriptions?tab=user">My Subscriptions</Link>
          </TabsTrigger>
        </TabsList>

        {/* All Subscriptions Tab */}
        <TabsContent value="all" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>All Subscriptions</CardTitle>
            </CardHeader>
            <CardContent>
              {subscriptions.length === 0 ? (
                <div className="text-center py-12">
                  <CreditCard className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">No subscriptions found</h3>
                  <p className="text-gray-600 mb-4">Subscriptions will appear here when users subscribe.</p>
                </div>
              ) : (
                <div className="space-y-4">
                  {subscriptions.map((subscription) => (
                    <div key={subscription.id} className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                      <div className="flex items-center space-x-4">
                        <div className="h-12 w-12 rounded-lg bg-blue-100 flex items-center justify-center">
                          <CreditCard className="h-6 w-6 text-blue-600" />
                        </div>
                        <div>
                          <h3 className="font-semibold text-gray-900">{subscription.planName}</h3>
                          <p className="text-sm text-gray-600">User ID: {subscription.userId}</p>
                          <div className="flex items-center space-x-2 mt-1">
                            <span className="text-sm font-medium text-gray-900">
                              {subscription.amount} {subscription.currency}
                            </span>
                            <span className="text-sm text-gray-500">{getIntervalDisplay(subscription.interval)}</span>
                            {subscription.nextBillingDate && (
                              <span className="text-xs text-gray-500">Next: {new Date(subscription.nextBillingDate).toLocaleDateString()}</span>
                            )}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center space-x-2">
                        <Badge variant={getStatusColor(subscription.status)}>{subscription.status}</Badge>
                        <Button variant="ghost" size="sm" asChild>
                          <Link href={`/dashboard/subscriptions/${subscription.id}`}>
                            <Eye className="h-4 w-4" />
                          </Link>
                        </Button>
                        {subscription.status === 'Active' && (
                          <Button variant="ghost" size="sm">
                            <X className="h-4 w-4" />
                          </Button>
                        )}
                        {subscription.status === 'Cancelled' && (
                          <Button variant="ghost" size="sm">
                            <RotateCcw className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* User Subscriptions Tab */}
        <TabsContent value="user" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>My Subscriptions</CardTitle>
            </CardHeader>
            <CardContent>
              {subscriptions.length === 0 ? (
                <div className="text-center py-12">
                  <CreditCard className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">No subscriptions yet</h3>
                  <p className="text-gray-600 mb-4">Your subscriptions will appear here.</p>
                  <Button asChild>
                    <Link href="/pricing">
                      <Plus className="h-4 w-4 mr-2" />
                      View Plans
                    </Link>
                  </Button>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {subscriptions.map((subscription) => (
                    <Card key={subscription.id} className="hover:shadow-md transition-shadow">
                      <CardContent className="p-6">
                        <div className="flex items-start justify-between mb-4">
                          <div>
                            <h3 className="font-semibold text-gray-900">{subscription.planName}</h3>
                            <p className="text-2xl font-bold text-blue-600">
                              {subscription.amount} {subscription.currency}
                            </p>
                            <p className="text-sm text-gray-500">{getIntervalDisplay(subscription.interval)}</p>
                          </div>
                          <Badge variant={getStatusColor(subscription.status)}>{subscription.status}</Badge>
                        </div>

                        <div className="space-y-2 mb-4">
                          <div className="flex justify-between text-sm">
                            <span className="text-gray-600">Started:</span>
                            <span>{new Date(subscription.startDate).toLocaleDateString()}</span>
                          </div>
                          {subscription.nextBillingDate && (
                            <div className="flex justify-between text-sm">
                              <span className="text-gray-600">Next billing:</span>
                              <span>{new Date(subscription.nextBillingDate).toLocaleDateString()}</span>
                            </div>
                          )}
                          {subscription.endDate && (
                            <div className="flex justify-between text-sm">
                              <span className="text-gray-600">Ends:</span>
                              <span>{new Date(subscription.endDate).toLocaleDateString()}</span>
                            </div>
                          )}
                        </div>

                        <div className="flex space-x-2">
                          <Button variant="outline" size="sm" asChild className="flex-1">
                            <Link href={`/dashboard/subscriptions/${subscription.id}`}>View Details</Link>
                          </Button>
                          {subscription.status === 'Active' && (
                            <Button variant="outline" size="sm">
                              Cancel
                            </Button>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default async function SubscriptionsPage({ searchParams }: SubscriptionsPageProps) {
  return (
    <Suspense fallback={<SubscriptionsLoading />}>
      <SubscriptionsContent searchParams={searchParams} />
    </Suspense>
  );
}
