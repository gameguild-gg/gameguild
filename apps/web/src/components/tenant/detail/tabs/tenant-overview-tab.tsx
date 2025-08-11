'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Separator } from '@/components/ui/separator';
import { 
  Users, 
  Calendar, 
  Activity, 
  BarChart3, 
  TrendingUp, 
  TrendingDown,
  FileText,
  Clock,
  Zap
} from 'lucide-react';
import type { Tenant } from '@/lib/api/generated/types.gen';

interface TenantOverviewTabProps {
  tenant: Tenant;
}

interface TenantStats {
  totalUsers: number;
  activeUsers: number;
  totalProjects: number;
  activeProjects: number;
  storageUsed: number;
  storageLimit: number;
  apiCalls: number;
  apiLimit: number;
  monthlyGrowth: number;
  lastActivity: string;
}

interface ActivityItem {
  id: string;
  type: 'user_joined' | 'project_created' | 'content_published' | 'settings_updated';
  description: string;
  timestamp: string;
  user?: string;
}

export function TenantOverviewTab({ tenant }: TenantOverviewTabProps) {
  const [stats, setStats] = useState<TenantStats | null>(null);
  const [recentActivity, setRecentActivity] = useState<ActivityItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadTenantOverview = async () => {
      setIsLoading(true);
      try {
        // Mock data - replace with actual API calls
        const mockStats: TenantStats = {
          totalUsers: 12,
          activeUsers: 8,
          totalProjects: 25,
          activeProjects: 15,
          storageUsed: 2.4, // GB
          storageLimit: 10, // GB
          apiCalls: 15420,
          apiLimit: 50000,
          monthlyGrowth: 12.5,
          lastActivity: new Date().toISOString(),
        };

        const mockActivity: ActivityItem[] = [
          {
            id: '1',
            type: 'user_joined',
            description: 'John Doe joined the tenant',
            timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
            user: 'System'
          },
          {
            id: '2',
            type: 'project_created',
            description: 'New project "Mobile App v2" created',
            timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
            user: 'Jane Smith'
          },
          {
            id: '3',
            type: 'content_published',
            description: 'Content "API Documentation" published',
            timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
            user: 'Bob Wilson'
          },
          {
            id: '4',
            type: 'settings_updated',
            description: 'Tenant settings updated',
            timestamp: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(),
            user: 'Admin'
          },
        ];

        setStats(mockStats);
        setRecentActivity(mockActivity);
      } catch (error) {
        console.error('Failed to load tenant overview:', error);
      } finally {
        setIsLoading(false);
      }
    };

    if (tenant.id) {
      loadTenantOverview();
    }
  }, [tenant.id]);

  const getActivityIcon = (type: ActivityItem['type']) => {
    switch (type) {
      case 'user_joined':
        return <Users className="h-4 w-4 text-blue-500" />;
      case 'project_created':
        return <FileText className="h-4 w-4 text-green-500" />;
      case 'content_published':
        return <Activity className="h-4 w-4 text-purple-500" />;
      case 'settings_updated':
        return <Zap className="h-4 w-4 text-orange-500" />;
      default:
        return <Activity className="h-4 w-4 text-gray-500" />;
    }
  };

  const formatRelativeTime = (timestamp: string) => {
    const now = new Date();
    const time = new Date(timestamp);
    const diffInHours = Math.floor((now.getTime() - time.getTime()) / (1000 * 60 * 60));
    
    if (diffInHours < 1) {
      return 'Less than an hour ago';
    } else if (diffInHours < 24) {
      return `${diffInHours} hour${diffInHours > 1 ? 's' : ''} ago`;
    } else {
      const diffInDays = Math.floor(diffInHours / 24);
      return `${diffInDays} day${diffInDays > 1 ? 's' : ''} ago`;
    }
  };

  if (isLoading || !stats) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <Card key={i} className="animate-pulse">
              <CardContent className="p-6">
                <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
                <div className="h-8 bg-gray-200 rounded w-1/2"></div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Users</p>
                <p className="text-2xl font-bold">{stats.totalUsers}</p>
                <p className="text-xs text-gray-500">
                  {stats.activeUsers} active
                </p>
              </div>
              <Users className="h-8 w-8 text-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Projects</p>
                <p className="text-2xl font-bold">{stats.totalProjects}</p>
                <p className="text-xs text-gray-500">
                  {stats.activeProjects} active
                </p>
              </div>
              <FileText className="h-8 w-8 text-green-600" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Growth</p>
                <div className="flex items-center">
                  <p className="text-2xl font-bold">{stats.monthlyGrowth}%</p>
                  {stats.monthlyGrowth > 0 ? (
                    <TrendingUp className="h-4 w-4 text-green-500 ml-1" />
                  ) : (
                    <TrendingDown className="h-4 w-4 text-red-500 ml-1" />
                  )}
                </div>
                <p className="text-xs text-gray-500">This month</p>
              </div>
              <BarChart3 className="h-8 w-8 text-purple-600" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Last Activity</p>
                <p className="text-sm font-bold">{formatRelativeTime(stats.lastActivity)}</p>
                <p className="text-xs text-gray-500">
                  Status: <Badge variant="default" className="text-xs">Active</Badge>
                </p>
              </div>
              <Clock className="h-8 w-8 text-orange-600" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Resource Usage */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Storage Usage</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex justify-between text-sm">
                <span>Used: {stats.storageUsed} GB</span>
                <span>Limit: {stats.storageLimit} GB</span>
              </div>
              <Progress 
                value={(stats.storageUsed / stats.storageLimit) * 100} 
                className="w-full"
              />
              <p className="text-xs text-gray-500">
                {((stats.storageLimit - stats.storageUsed) / stats.storageLimit * 100).toFixed(1)}% available
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>API Usage</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex justify-between text-sm">
                <span>Used: {stats.apiCalls.toLocaleString()}</span>
                <span>Limit: {stats.apiLimit.toLocaleString()}</span>
              </div>
              <Progress 
                value={(stats.apiCalls / stats.apiLimit) * 100} 
                className="w-full"
              />
              <p className="text-xs text-gray-500">
                {(stats.apiLimit - stats.apiCalls).toLocaleString()} calls remaining this month
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
        </CardHeader>
        <CardContent>
          {recentActivity.length === 0 ? (
            <div className="text-center py-8">
              <Activity className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No recent activity</h3>
              <p className="text-gray-500">Activity will appear here as users interact with the tenant.</p>
            </div>
          ) : (
            <div className="space-y-4">
              {recentActivity.map((activity, index) => (
                <div key={activity.id}>
                  <div className="flex items-start space-x-3">
                    <div className="flex-shrink-0">
                      {getActivityIcon(activity.type)}
                    </div>
                    <div className="flex-grow min-w-0">
                      <p className="text-sm font-medium text-gray-900">
                        {activity.description}
                      </p>
                      <div className="flex items-center mt-1 text-xs text-gray-500">
                        <span>{activity.user}</span>
                        <span className="mx-1">•</span>
                        <span>{formatRelativeTime(activity.timestamp)}</span>
                      </div>
                    </div>
                  </div>
                  {index < recentActivity.length - 1 && (
                    <Separator className="my-4" />
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Button variant="outline" className="justify-start">
              <Users className="h-4 w-4 mr-2" />
              Manage Users
            </Button>
            <Button variant="outline" className="justify-start">
              <FileText className="h-4 w-4 mr-2" />
              View Projects
            </Button>
            <Button variant="outline" className="justify-start">
              <BarChart3 className="h-4 w-4 mr-2" />
              View Analytics
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
