import React, { Suspense } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Activity, BookOpen, Calendar, CreditCard, DollarSign, Package, Plus, Target, TrendingUp, Trophy, Users } from 'lucide-react';

// Import the individual action functions
import { getUserStatistics } from '@/lib/users/users.actions';
import { getProgramStatistics } from '../../../../../../../../../old/quick-fix/programs/programs.actions';
import { getProductStatistics } from '../../../../../../../../../old/quick-fix/products/products.actions';
import { getAchievementStatistics } from '@/lib/achievements/achievements.actions';
import { getSubscriptionStatistics } from '@/lib/subscriptions/subscriptions.actions';

interface DashboardOverviewProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

// Loading component
function DashboardOverviewLoading() {
  return (
    <div className="space-y-6">
      <div className="h-8 w-64 bg-gray-200 rounded animate-pulse" />

      {/* Statistics cards skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {Array.from({ length: 8 }).map((_, i) => (
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

      {/* Quick actions skeleton */}
      <Card>
        <CardHeader>
          <div className="h-6 w-32 bg-gray-200 rounded animate-pulse" />
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="h-20 bg-gray-200 rounded animate-pulse" />
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// Main dashboard content
async function DashboardOverviewContent({ searchParams }: DashboardOverviewProps) {
  const params = await searchParams;

  // Extract date filters
  const fromDate = typeof params.fromDate === 'string' ? params.fromDate : undefined;
  const toDate = typeof params.toDate === 'string' ? params.toDate : undefined;

  // Fetch all statistics in parallel
  const [userStats, programStats, productStats, achievementStats, subscriptionStats] = await Promise.all([
    getUserStatistics(fromDate, toDate),
    getProgramStatistics(),
    getProductStatistics(fromDate, toDate),
    getAchievementStatistics(),
    getSubscriptionStatistics(),
  ]);

  // Calculate totals and trends
  const totalRevenue = (productStats.data?.totalRevenue || 0) + (subscriptionStats.data?.totalRevenue || 0);
  const totalUsers = userStats.data?.totalUsers || 0;
  const totalPrograms = programStats.data?.totalPrograms || 0;
  const totalProducts = productStats.data?.totalProducts || 0;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard Overview</h1>
          <p className="text-gray-600">Welcome back! Here's what's happening with your platform.</p>
        </div>
        <div className="flex space-x-2">
          <Button variant="outline" asChild>
            <Link href="/dashboard/analytics">
              <Activity className="h-4 w-4 mr-2" />
              Analytics
            </Link>
          </Button>
          <Button asChild>
            <Link href="/dashboard/projects/create">
              <Plus className="h-4 w-4 mr-2" />
              New Project
            </Link>
          </Button>
        </div>
      </div>

      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {/* Total Revenue */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Total Revenue</p>
              <DollarSign className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">${totalRevenue.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">${(subscriptionStats.data?.monthlyRevenue || 0).toLocaleString()} this month</p>
          </CardContent>
        </Card>

        {/* Total Users */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Total Users</p>
              <Users className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{totalUsers.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">+{userStats.data?.usersCreatedThisMonth || 0} this month</p>
          </CardContent>
        </Card>

        {/* Programs & Courses */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Programs</p>
              <BookOpen className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{totalPrograms}</div>
            <p className="text-xs text-muted-foreground">{programStats.data?.publishedPrograms || 0} published</p>
          </CardContent>
        </Card>

        {/* Active Subscriptions */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Active Subscriptions</p>
              <CreditCard className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{subscriptionStats.data?.activeSubscriptions || 0}</div>
            <p className="text-xs text-muted-foreground">
              {(((subscriptionStats.data?.activeSubscriptions || 0) / Math.max(subscriptionStats.data?.totalSubscriptions || 1, 1)) * 100).toFixed(1)}% active
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Secondary Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {/* Products */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Products</p>
              <Package className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{totalProducts}</div>
            <p className="text-xs text-muted-foreground">{productStats.data?.activeProducts || 0} active</p>
          </CardContent>
        </Card>

        {/* Achievements */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Achievements</p>
              <Trophy className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{achievementStats.data?.totalAchievements || 0}</div>
            <p className="text-xs text-muted-foreground">{achievementStats.data?.userAchievements || 0} earned</p>
          </CardContent>
        </Card>

        {/* Enrollments */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Total Enrollments</p>
              <Target className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{programStats.data?.totalEnrollments || 0}</div>
            <p className="text-xs text-muted-foreground">Avg rating: {(programStats.data?.averageRating || 0).toFixed(1)}</p>
          </CardContent>
        </Card>

        {/* Growth */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">This Week</p>
              <TrendingUp className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{(userStats.data?.usersCreatedThisWeek || 0) + (programStats.data?.programsCreatedThisWeek || 0)}</div>
            <p className="text-xs text-muted-foreground">New users & programs</p>
          </CardContent>
        </Card>
      </div>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/users">
                <Users className="h-6 w-6 mb-2" />
                <span className="text-sm">Manage Users</span>
              </Link>
            </Button>

            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/programs">
                <BookOpen className="h-6 w-6 mb-2" />
                <span className="text-sm">Programs</span>
              </Link>
            </Button>

            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/products">
                <Package className="h-6 w-6 mb-2" />
                <span className="text-sm">Products</span>
              </Link>
            </Button>

            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/achievements">
                <Trophy className="h-6 w-6 mb-2" />
                <span className="text-sm">Achievements</span>
              </Link>
            </Button>

            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/subscriptions">
                <CreditCard className="h-6 w-6 mb-2" />
                <span className="text-sm">Subscriptions</span>
              </Link>
            </Button>

            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/projects">
                <Target className="h-6 w-6 mb-2" />
                <span className="text-sm">Projects</span>
              </Link>
            </Button>

            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/analytics">
                <Activity className="h-6 w-6 mb-2" />
                <span className="text-sm">Analytics</span>
              </Link>
            </Button>

            <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
              <Link href="/dashboard/testing-lab">
                <Calendar className="h-6 w-6 mb-2" />
                <span className="text-sm">Testing Lab</span>
              </Link>
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Status Overview */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Platform Health */}
        <Card>
          <CardHeader>
            <CardTitle>Platform Health</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Active Users</span>
                <div className="flex items-center space-x-2">
                  <Badge variant="default">{userStats.data?.activeUsers || 0}</Badge>
                  <span className="text-xs text-gray-500">
                    {userStats.data?.activeUsers && userStats.data?.totalUsers
                      ? `${((userStats.data.activeUsers / userStats.data.totalUsers) * 100).toFixed(1)}%`
                      : '0%'}
                  </span>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Published Programs</span>
                <div className="flex items-center space-x-2">
                  <Badge variant="default">{programStats.data?.publishedPrograms || 0}</Badge>
                  <span className="text-xs text-gray-500">{programStats.data?.draftPrograms || 0} drafts</span>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Active Products</span>
                <div className="flex items-center space-x-2">
                  <Badge variant="default">{productStats.data?.activeProducts || 0}</Badge>
                  <span className="text-xs text-gray-500">{productStats.data?.inactiveProducts || 0} inactive</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Recent Activity */}
        <Card>
          <CardHeader>
            <CardTitle>Recent Activity</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm">New users today</span>
                <Badge variant="secondary">{userStats.data?.usersCreatedToday || 0}</Badge>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-sm">New programs this week</span>
                <Badge variant="secondary">{programStats.data?.programsCreatedThisWeek || 0}</Badge>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-sm">New subscriptions this month</span>
                <Badge variant="secondary">{subscriptionStats.data?.newSubscriptionsThisMonth || 0}</Badge>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-sm">Recent achievements</span>
                <Badge variant="secondary">{achievementStats.data?.recentAchievements || 0}</Badge>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

export default async function DashboardOverviewPage({ searchParams }: DashboardOverviewProps) {
  return (
    <Suspense fallback={<DashboardOverviewLoading />}>
      <DashboardOverviewContent searchParams={searchParams} />
    </Suspense>
  );
}
