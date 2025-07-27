import React from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Activity, Plus } from 'lucide-react';

export function DashboardOverviewHeader() {
  return (
    <div className="flex justify-between items-center">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard Overview</h1>
        <p className="text-gray-600">Welcome back! Here&apos;s what&apos;s happening with your platform.</p>
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
  );
}
