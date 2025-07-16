import React from 'react';
import { DashboardCharts } from '@/components/dashboard/analytics/dashboard-charts';
import { DashboardStats } from '@/components/dashboard/analytics/dashboard-stats';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Button } from '@game-guild/ui/components';
import { Download, RefreshCw, Calendar } from 'lucide-react';

export default async function AnalyticsPage() {
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold">Analytics Dashboard</h1>
          <p className="text-muted-foreground">Comprehensive insights into your platform performance, user engagement, and revenue metrics.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline">
            <Calendar className="mr-2 h-4 w-4" />
            Date Range
          </Button>
          <Button variant="outline">
            <RefreshCw className="mr-2 h-4 w-4" />
            Refresh
          </Button>
          <Button>
            <Download className="mr-2 h-4 w-4" />
            Export
          </Button>
        </div>
      </div>

      {/* Key Metrics */}
      <DashboardStats />

      {/* Charts and Visualizations */}
      <DashboardCharts />

      {/* Additional Insights */}
      <div className="grid gap-6 md:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Performance Insights</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Best performing day:</span>
                <span className="font-medium">Wednesday</span>
              </div>
            </div>
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Peak hours:</span>
                <span className="font-medium">2 PM - 4 PM</span>
              </div>
            </div>
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Avg. session duration:</span>
                <span className="font-medium">24 minutes</span>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">User Behavior</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Bounce rate:</span>
                <span className="font-medium text-green-600">18.5%</span>
              </div>
            </div>
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Return rate:</span>
                <span className="font-medium text-blue-600">72.3%</span>
              </div>
            </div>
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Course completion:</span>
                <span className="font-medium text-purple-600">68%</span>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Revenue Metrics</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">ARPU:</span>
                <span className="font-medium">$45.20</span>
              </div>
            </div>
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">LTV:</span>
                <span className="font-medium">$312.80</span>
              </div>
            </div>
            <div className="text-sm">
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Churn rate:</span>
                <span className="font-medium text-red-600">5.2%</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
