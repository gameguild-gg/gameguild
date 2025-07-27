import React from 'react';
import { DashboardStatsLoading } from '@/components/dashboard/analytics/server-dashboard-stats';
import { RecentActivityLoading } from '@/components/dashboard/analytics/server-recent-activity';

export function DashboardOverviewLoading() {
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
