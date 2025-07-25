import React, { Suspense } from 'react';
import { getUserStatistics } from '@/lib/users/users.actions';
import { ServerDashboardStats, DashboardStatsLoading, DashboardStatsError } from '@/components/dashboard/server-dashboard-stats';
import { SystemStatus } from '@/components/dashboard/system-status';
import { RefreshButton } from '@/components/dashboard/refresh-button';
import { DashboardFilters } from '@/components/dashboard/dashboard-filters';
import { ServerRecentActivity, RecentActivityLoading } from '@/components/dashboard/server-recent-activity';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { BookOpen, DollarSign, Plus, TrendingUp, Users } from 'lucide-react';

interface DashboardContentProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

async function DashboardContent({ searchParams }: DashboardContentProps) {
  const params = await searchParams;

  // Extract search parameters for filtering
  const fromDate = typeof params.fromDate === 'string' ? params.fromDate : undefined;
  const toDate = typeof params.toDate === 'string' ? params.toDate : undefined;
  const includeDeleted = params.includeDeleted === 'true';

  // Fetch dashboard data using server action
  const dashboardResult = await getUserStatistics(fromDate, toDate, includeDeleted);

  if (!dashboardResult.success) {
    return (
      <div className="space-y-8">
        <section className="relative overflow-hidden rounded-2xl">
          <div className="absolute inset-0 bg-gradient-to-br from-blue-600/20 via-purple-500/10 to-transparent">
            <div className="absolute bottom-0 right-0 w-96 h-96 rounded-full bg-gradient-to-t from-blue-600/30 via-purple-500/20 to-transparent blur-3xl"></div>
          </div>
          <div className="relative p-8 md:p-12">
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between mb-8">
              <div>
                <h1 className="text-4xl md:text-5xl font-bold text-white mb-4">Dashboard Overview</h1>
                <p className="text-slate-300 text-lg">Welcome to your Game Guild dashboard. Monitor your users, courses, and revenue in real-time.</p>
              </div>
              <RefreshButton />
            </div>
            <DashboardStatsError error={dashboardResult.error || 'Failed to load dashboard data'} />
          </div>
        </section>
      </div>
    );
  }

  const { data: userStatistics } = dashboardResult;

  return (
    <div className="space-y-8">
      {/* Hero Section with gradient background */}
      <section className="relative overflow-hidden rounded-2xl">
        <div className="absolute inset-0 bg-gradient-to-br from-blue-600/20 via-purple-500/10 to-transparent">
          <div className="absolute bottom-0 right-0 w-96 h-96 rounded-full bg-gradient-to-t from-blue-600/30 via-purple-500/20 to-transparent blur-3xl"></div>
        </div>

        <div className="relative p-8 md:p-12">
          {/* Header */}
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between mb-8">
            <div>
              <h1 className="text-4xl md:text-5xl font-bold text-white mb-4">Dashboard Overview</h1>
              <p className="text-slate-300 text-lg">Welcome to your Game Guild dashboard. Monitor your users, courses, and revenue in real-time.</p>
            </div>
            <div className="flex gap-3">
              <RefreshButton />
              <Button className="bg-white text-black hover:bg-slate-100 font-medium">
                <Plus className="mr-2 h-4 w-4" />
                Add User
              </Button>
              <Button variant="outline" className="border-slate-600 text-white hover:bg-slate-800/50 hover:border-slate-500">
                <Plus className="mr-2 h-4 w-4" />
                Create Course
              </Button>
            </div>
          </div>

          {/* Dashboard Stats with SSR */}
          <ServerDashboardStats data={userStatistics} />
        </div>
      </section>

      {/* Main Content Grid */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {/* Recent Activity */}
        <div className="lg:col-span-2">
          <Suspense fallback={<RecentActivityLoading />}>
            <ServerRecentActivity limit={15} />
          </Suspense>
        </div>

        {/* Sidebar with Filters, Quick Actions & System Status */}
        <div className="space-y-6">
          {/* Dashboard Filters */}
          <DashboardFilters />

          {/* Quick Actions */}
          <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-base text-white">Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
                <Users className="mr-2 h-4 w-4" />
                Manage Users
              </Button>
              <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
                <BookOpen className="mr-2 h-4 w-4" />
                Course Management
              </Button>
              <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
                <DollarSign className="mr-2 h-4 w-4" />
                Revenue Reports
              </Button>
              <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
                <TrendingUp className="mr-2 h-4 w-4" />
                Analytics
              </Button>
            </CardContent>
          </Card>

          {/* System Status with Server Data */}
          {/* <SystemStatus systemStatus={data.systemStatus} /> */}
        </div>
      </div>
    </div>
  );
}

export default async function Page({ searchParams }: { searchParams: Promise<{ [key: string]: string | string[] | undefined }> }) {
  return (
    <Suspense fallback={<DashboardPageLoading />}>
      <DashboardContent searchParams={searchParams} />
    </Suspense>
  );
}

function DashboardPageLoading() {
  return (
    <div className="space-y-8">
      <section className="relative overflow-hidden rounded-2xl">
        <div className="absolute inset-0 bg-gradient-to-br from-blue-600/20 via-purple-500/10 to-transparent">
          <div className="absolute bottom-0 right-0 w-96 h-96 rounded-full bg-gradient-to-t from-blue-600/30 via-purple-500/20 to-transparent blur-3xl"></div>
        </div>
        <div className="relative p-8 md:p-12">
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between mb-8">
            <div>
              <h1 className="text-4xl md:text-5xl font-bold text-white mb-4">Dashboard Overview</h1>
              <p className="text-slate-300 text-lg">Loading your dashboard data...</p>
            </div>
          </div>
          <DashboardStatsLoading />
        </div>
      </section>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <RecentActivityLoading />
        </div>
        <div className="space-y-6">
          <div className="h-64 bg-slate-800/50 border border-slate-700/50 rounded-lg animate-pulse" />
          <div className="h-48 bg-slate-800/50 border border-slate-700/50 rounded-lg animate-pulse" />
          <div className="h-32 bg-slate-800/50 border border-slate-700/50 rounded-lg animate-pulse" />
        </div>
      </div>
    </div>
  );
}
