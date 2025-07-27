import React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { DollarSign, Users, BookOpen, CreditCard, Package, Trophy, Target, TrendingUp } from 'lucide-react';

interface KeyMetricsProps {
  totalRevenue: number;
  totalUsers: number;
  totalPrograms: number;
  activeSubscriptions: number;
  totalProducts: number;
  totalAchievements: number;
  totalEnrollments: number;
  weeklyGrowth: number;
  monthlyRevenue: number;
  usersCreatedThisMonth: number;
  publishedPrograms: number;
  activeProducts: number;
  userAchievements: number;
  averageRating: number;
  activeSubscriptionPercentage: number;
}

export function DashboardKeyMetrics({
  totalRevenue,
  totalUsers,
  totalPrograms,
  activeSubscriptions,
  totalProducts,
  totalAchievements,
  totalEnrollments,
  weeklyGrowth,
  monthlyRevenue,
  usersCreatedThisMonth,
  publishedPrograms,
  activeProducts,
  userAchievements,
  averageRating,
  activeSubscriptionPercentage,
}: KeyMetricsProps) {
  return (
    <>
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
            <p className="text-xs text-muted-foreground">${monthlyRevenue.toLocaleString()} this month</p>
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
            <p className="text-xs text-muted-foreground">+{usersCreatedThisMonth} this month</p>
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
            <p className="text-xs text-muted-foreground">{publishedPrograms} published</p>
          </CardContent>
        </Card>

        {/* Active Subscriptions */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Active Subscriptions</p>
              <CreditCard className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{activeSubscriptions}</div>
            <p className="text-xs text-muted-foreground">{activeSubscriptionPercentage.toFixed(1)}% active</p>
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
            <p className="text-xs text-muted-foreground">{activeProducts} active</p>
          </CardContent>
        </Card>

        {/* Achievements */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Achievements</p>
              <Trophy className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{totalAchievements}</div>
            <p className="text-xs text-muted-foreground">{userAchievements} earned</p>
          </CardContent>
        </Card>

        {/* Enrollments */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">Total Enrollments</p>
              <Target className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{totalEnrollments}</div>
            <p className="text-xs text-muted-foreground">Avg rating: {averageRating.toFixed(1)}</p>
          </CardContent>
        </Card>

        {/* Growth */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between space-y-0 pb-2">
              <p className="text-sm font-medium">This Week</p>
              <TrendingUp className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="text-2xl font-bold">{weeklyGrowth}</div>
            <p className="text-xs text-muted-foreground">New users & programs</p>
          </CardContent>
        </Card>
      </div>
    </>
  );
}
