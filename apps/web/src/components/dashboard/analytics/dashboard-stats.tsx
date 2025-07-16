'use client';

import { useEffect, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import { Skeleton } from '@game-guild/ui/components';
import { Progress } from '@game-guild/ui/components';
import { Users, UserCheck, UserX, GraduationCap, BookOpen, DollarSign, TrendingUp, TrendingDown, Activity, Eye, Calendar } from 'lucide-react';
import { fetchUserStatistics } from '@/lib/api/dashboard';

interface DashboardStatsProps {
  className?: string;
}

interface UserStats {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  deletedUsers: number;
  totalBalance: number;
  averageBalance: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  newUsersThisMonth: number;
}

interface ProgramStats {
  totalPrograms: number;
  publishedPrograms: number;
  draftPrograms: number;
  totalEnrollments: number;
  completionRate: number;
  totalRevenue: number;
  averageRating: number;
}

export function DashboardStats({ className }: DashboardStatsProps) {
  const [userStats, setUserStats] = useState<UserStats | null>(null);
  const [programStats, setProgramStats] = useState<ProgramStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchStats() {
      try {
        setIsLoading(true);

        // Fetch user statistics
        const userStatsResult = await fetchUserStatistics();
        if (userStatsResult.success && userStatsResult.statistics) {
          setUserStats(userStatsResult.statistics);
        }

        // TODO: Add program statistics when available
        // For now, using mock data
        setProgramStats({
          totalPrograms: 24,
          publishedPrograms: 18,
          draftPrograms: 6,
          totalEnrollments: 1250,
          completionRate: 72.5,
          totalRevenue: 45600,
          averageRating: 4.3,
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch statistics');
      } finally {
        setIsLoading(false);
      }
    }

    fetchStats();
  }, []);

  if (isLoading) {
    return (
      <div className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 ${className}`}>
        {[...Array(8)].map((_, i) => (
          <Card key={i}>
            <CardHeader className="pb-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-8 w-16" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-4 w-20" />
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <Card className={className}>
        <CardContent className="pt-6">
          <div className="text-center text-red-600">
            <Activity className="mx-auto h-8 w-8 mb-2" />
            <p>Failed to load dashboard statistics</p>
            <p className="text-sm text-gray-500">{error}</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const userActivityRate = userStats ? (userStats.activeUsers / userStats.totalUsers) * 100 : 0;
  const programPublishRate = programStats ? (programStats.publishedPrograms / programStats.totalPrograms) * 100 : 0;

  return (
    <div className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 ${className}`}>
      {/* Total Users */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Total Users</CardTitle>
          <Users className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">{userStats?.totalUsers.toLocaleString() || '0'}</div>
          <p className="text-xs text-muted-foreground">+{userStats?.newUsersThisMonth || 0} this month</p>
          <Progress value={userActivityRate} className="mt-2" />
          <p className="text-xs text-muted-foreground mt-1">{userActivityRate.toFixed(1)}% active</p>
        </CardContent>
      </Card>

      {/* Active Users */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Active Users</CardTitle>
          <UserCheck className="h-4 w-4 text-green-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold text-green-600">{userStats?.activeUsers.toLocaleString() || '0'}</div>
          <p className="text-xs text-muted-foreground">{userStats?.inactiveUsers || 0} inactive</p>
          <div className="flex items-center mt-2">
            <TrendingUp className="h-3 w-3 text-green-600 mr-1" />
            <span className="text-xs text-green-600">+12% from last month</span>
          </div>
        </CardContent>
      </Card>

      {/* Total Programs */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Programs</CardTitle>
          <BookOpen className="h-4 w-4 text-blue-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">{programStats?.totalPrograms || '0'}</div>
          <p className="text-xs text-muted-foreground">
            {programStats?.publishedPrograms || 0} published, {programStats?.draftPrograms || 0} drafts
          </p>
          <Progress value={programPublishRate} className="mt-2" />
          <p className="text-xs text-muted-foreground mt-1">{programPublishRate.toFixed(1)}% published</p>
        </CardContent>
      </Card>

      {/* Total Revenue */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Revenue</CardTitle>
          <DollarSign className="h-4 w-4 text-green-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">${(programStats?.totalRevenue || 0).toLocaleString()}</div>
          <p className="text-xs text-muted-foreground">${((userStats?.totalBalance || 0) / 100).toLocaleString()} in user balances</p>
          <div className="flex items-center mt-2">
            <TrendingUp className="h-3 w-3 text-green-600 mr-1" />
            <span className="text-xs text-green-600">+8.2% from last month</span>
          </div>
        </CardContent>
      </Card>

      {/* Enrollments */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Enrollments</CardTitle>
          <GraduationCap className="h-4 w-4 text-purple-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">{programStats?.totalEnrollments.toLocaleString() || '0'}</div>
          <p className="text-xs text-muted-foreground">{programStats?.completionRate || 0}% completion rate</p>
          <Badge variant="secondary" className="mt-2">
            {programStats?.averageRating || 0}★ avg rating
          </Badge>
        </CardContent>
      </Card>

      {/* New Users Today */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">New Today</CardTitle>
          <Calendar className="h-4 w-4 text-orange-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold text-orange-600">{userStats?.newUsersToday || 0}</div>
          <p className="text-xs text-muted-foreground">{userStats?.newUsersThisWeek || 0} this week</p>
          <div className="flex items-center mt-2">
            {(userStats?.newUsersToday || 0) > 5 ? (
              <>
                <TrendingUp className="h-3 w-3 text-green-600 mr-1" />
                <span className="text-xs text-green-600">Above average</span>
              </>
            ) : (
              <>
                <TrendingDown className="h-3 w-3 text-gray-400 mr-1" />
                <span className="text-xs text-gray-400">Below average</span>
              </>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Avg Balance */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Avg Balance</CardTitle>
          <DollarSign className="h-4 w-4 text-blue-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">${((userStats?.averageBalance || 0) / 100).toFixed(2)}</div>
          <p className="text-xs text-muted-foreground">Per user average</p>
          <div className="mt-2">
            <Badge variant="outline">${((userStats?.totalBalance || 0) / 100).toLocaleString()} total</Badge>
          </div>
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Quick Actions</CardTitle>
          <Eye className="h-4 w-4 text-indigo-600" />
        </CardHeader>
        <CardContent className="space-y-2">
          <button className="w-full text-left text-sm hover:bg-gray-50 p-2 rounded transition-colors">View All Users</button>
          <button className="w-full text-left text-sm hover:bg-gray-50 p-2 rounded transition-colors">Manage Programs</button>
          <button className="w-full text-left text-sm hover:bg-gray-50 p-2 rounded transition-colors">Export Reports</button>
        </CardContent>
      </Card>
    </div>
  );
}
