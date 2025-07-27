import React from 'react';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';
import { RefreshButton } from '@/components/dashboard/analytics/refresh-button';
import { ServerDashboardStats } from '@/components/dashboard/analytics/server-dashboard-stats';
import { DashboardStatsError } from '@/components/dashboard/analytics/server-dashboard-stats';
import type { UserStatistics } from '@/lib/users/users.actions';

interface DashboardHeroSectionProps {
  userStatistics?: UserStatistics;
  error?: string;
}

export function DashboardHeroSection({ userStatistics, error }: DashboardHeroSectionProps) {
  return (
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

        {/* Dashboard Stats or Error */}
        {error ? <DashboardStatsError error={error} /> : userStatistics && <ServerDashboardStats data={userStatistics} />}
      </div>
    </section>
  );
}
