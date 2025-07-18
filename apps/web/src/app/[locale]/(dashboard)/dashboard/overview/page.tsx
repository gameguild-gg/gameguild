import React from 'react';
import { DashboardStats } from '@/components/dashboard/analytics/dashboard-stats';
import { RecentActivity } from '@/components/dashboard/analytics/recent-activity';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { BookOpen, DollarSign, Plus, TrendingUp, Users } from 'lucide-react';

export default async function Page() {
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

          {/* Quick Stats */}
          <DashboardStats />
        </div>
      </section>

      {/* Main Content Grid */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {/* Recent Activity */}
        <div className="lg:col-span-2">
          <RecentActivity />
        </div>

        {/* Quick Actions & Summary */}
        <div className="space-y-6">
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

          {/* System Status */}
          <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-base text-white">System Status</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm text-slate-300">API Status</span>
                <div className="flex items-center gap-2">
                  <div className="h-2 w-2 rounded-full bg-green-500" />
                  <span className="text-sm text-green-400">Online</span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-slate-300">Database</span>
                <div className="flex items-center gap-2">
                  <div className="h-2 w-2 rounded-full bg-green-500" />
                  <span className="text-sm text-green-400">Connected</span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-slate-300">Payment Gateway</span>
                <div className="flex items-center gap-2">
                  <div className="h-2 w-2 rounded-full bg-green-500" />
                  <span className="text-sm text-green-400">Active</span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-slate-300">Email Service</span>
                <div className="flex items-center gap-2">
                  <div className="h-2 w-2 rounded-full bg-yellow-500" />
                  <span className="text-sm text-yellow-400">Limited</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
