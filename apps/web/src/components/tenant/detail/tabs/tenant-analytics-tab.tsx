'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { 
  BarChart3,
  TrendingUp,
  TrendingDown,
  Users,
  Activity,
  Clock,
  Calendar,
  Download,
  RefreshCw,
  Loader2
} from 'lucide-react';
import type { Tenant } from '@/lib/api/generated/types.gen';

interface TenantAnalyticsTabProps {
  tenant: Tenant;
}

interface AnalyticsData {
  userActivity: {
    period: string;
    activeUsers: number;
    newUsers: number;
    sessions: number;
  }[];
  
  usage: {
    totalApiCalls: number;
    apiCallsGrowth: number;
    averageResponseTime: number;
    errorRate: number;
    mostUsedEndpoints: {
      endpoint: string;
      calls: number;
      percentage: number;
    }[];
  };
  
  storage: {
    totalUsed: number;
    limit: number;
    fileCount: number;
    growth: number;
    breakdown: {
      category: string;
      size: number;
      percentage: number;
    }[];
  };
  
  summary: {
    totalUsers: number;
    activeUsers: number;
    totalSessions: number;
    averageSessionDuration: number;
    bounceRate: number;
  };
}

export function TenantAnalyticsTab({ tenant }: TenantAnalyticsTabProps) {
  const [analyticsData, setAnalyticsData] = useState<AnalyticsData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [timeRange, setTimeRange] = useState('30d');
  const [isRefreshing, setIsRefreshing] = useState(false);

  useEffect(() => {
    const loadAnalytics = async () => {
      setIsLoading(true);
      try {
        // Mock data - replace with actual API call
        const mockData: AnalyticsData = {
          userActivity: [
            { period: '2024-01-01', activeUsers: 45, newUsers: 5, sessions: 120 },
            { period: '2024-01-02', activeUsers: 52, newUsers: 8, sessions: 135 },
            { period: '2024-01-03', activeUsers: 48, newUsers: 3, sessions: 128 },
            { period: '2024-01-04', activeUsers: 61, newUsers: 12, sessions: 150 },
            { period: '2024-01-05', activeUsers: 55, newUsers: 7, sessions: 142 },
          ],
          
          usage: {
            totalApiCalls: 125430,
            apiCallsGrowth: 15.2,
            averageResponseTime: 234, // ms
            errorRate: 2.1, // percentage
            mostUsedEndpoints: [
              { endpoint: '/api/projects', calls: 25680, percentage: 20.5 },
              { endpoint: '/api/users', calls: 18920, percentage: 15.1 },
              { endpoint: '/api/content', calls: 15340, percentage: 12.2 },
              { endpoint: '/api/auth', calls: 12180, percentage: 9.7 },
            ],
          },
          
          storage: {
            totalUsed: 2.8, // GB
            limit: 10, // GB
            fileCount: 1247,
            growth: 8.3, // percentage
            breakdown: [
              { category: 'Documents', size: 1.2, percentage: 42.8 },
              { category: 'Images', size: 0.9, percentage: 32.1 },
              { category: 'Videos', size: 0.5, percentage: 17.9 },
              { category: 'Other', size: 0.2, percentage: 7.2 },
            ],
          },
          
          summary: {
            totalUsers: 87,
            activeUsers: 62,
            totalSessions: 1456,
            averageSessionDuration: 18.5, // minutes
            bounceRate: 32.1, // percentage
          },
        };
        
        setAnalyticsData(mockData);
      } catch (error) {
        console.error('Failed to load analytics:', error);
      } finally {
        setIsLoading(false);
      }
    };

    if (tenant.id) {
      loadAnalytics();
    }
  }, [tenant.id, timeRange]);

  const handleRefresh = async () => {
    setIsRefreshing(true);
    // Simulate refresh delay
    await new Promise(resolve => setTimeout(resolve, 1000));
    setIsRefreshing(false);
  };

  const formatNumber = (num: number) => {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    } else if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'k';
    }
    return num.toString();
  };

  const formatFileSize = (gb: number) => {
    if (gb < 1) {
      return `${(gb * 1024).toFixed(0)} MB`;
    }
    return `${gb.toFixed(1)} GB`;
  };

  const formatDuration = (minutes: number) => {
    const hours = Math.floor(minutes / 60);
    const mins = Math.floor(minutes % 60);
    
    if (hours > 0) {
      return `${hours}h ${mins}m`;
    }
    return `${mins}m`;
  };

  if (isLoading || !analyticsData) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
        <span className="ml-2 text-gray-600">Loading analytics...</span>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header Controls */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-medium">Analytics Overview</h3>
          <p className="text-sm text-gray-600">Usage statistics and insights for {tenant.name}</p>
        </div>
        <div className="flex items-center space-x-2">
          <Select value={timeRange} onValueChange={setTimeRange}>
            <SelectTrigger className="w-32">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="7d">Last 7 days</SelectItem>
              <SelectItem value="30d">Last 30 days</SelectItem>
              <SelectItem value="90d">Last 3 months</SelectItem>
              <SelectItem value="1y">Last year</SelectItem>
            </SelectContent>
          </Select>
          <Button variant="outline" size="sm" onClick={handleRefresh} disabled={isRefreshing}>
            <RefreshCw className={`h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`} />
          </Button>
          <Button variant="outline" size="sm">
            <Download className="h-4 w-4 mr-2" />
            Export
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Users</p>
                <p className="text-2xl font-bold">{analyticsData.summary.totalUsers}</p>
              </div>
              <Users className="h-8 w-8 text-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Active Users</p>
                <p className="text-2xl font-bold">{analyticsData.summary.activeUsers}</p>
                <p className="text-xs text-green-600">
                  {((analyticsData.summary.activeUsers / analyticsData.summary.totalUsers) * 100).toFixed(1)}% active
                </p>
              </div>
              <Activity className="h-8 w-8 text-green-600" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Sessions</p>
                <p className="text-2xl font-bold">{formatNumber(analyticsData.summary.totalSessions)}</p>
              </div>
              <BarChart3 className="h-8 w-8 text-purple-600" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Avg. Session</p>
                <p className="text-2xl font-bold">{formatDuration(analyticsData.summary.averageSessionDuration)}</p>
              </div>
              <Clock className="h-8 w-8 text-orange-600" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Bounce Rate</p>
                <p className="text-2xl font-bold">{analyticsData.summary.bounceRate}%</p>
              </div>
              <TrendingDown className="h-8 w-8 text-red-600" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* API Usage */}
      <Card>
        <CardHeader>
          <CardTitle>API Usage</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <div className="text-center p-4 border rounded-lg">
              <p className="text-2xl font-bold text-blue-600">{formatNumber(analyticsData.usage.totalApiCalls)}</p>
              <p className="text-sm text-gray-600">Total API Calls</p>
              <div className="flex items-center justify-center mt-1">
                <TrendingUp className="h-4 w-4 text-green-500 mr-1" />
                <span className="text-xs text-green-600">+{analyticsData.usage.apiCallsGrowth}%</span>
              </div>
            </div>
            
            <div className="text-center p-4 border rounded-lg">
              <p className="text-2xl font-bold text-green-600">{analyticsData.usage.averageResponseTime}ms</p>
              <p className="text-sm text-gray-600">Avg. Response Time</p>
              <Badge variant="outline" className="mt-1">Good</Badge>
            </div>
            
            <div className="text-center p-4 border rounded-lg">
              <p className="text-2xl font-bold text-orange-600">{analyticsData.usage.errorRate}%</p>
              <p className="text-sm text-gray-600">Error Rate</p>
              <Badge variant="outline" className="mt-1">Normal</Badge>
            </div>
            
            <div className="text-center p-4 border rounded-lg">
              <p className="text-2xl font-bold text-purple-600">{analyticsData.usage.mostUsedEndpoints.length}</p>
              <p className="text-sm text-gray-600">Active Endpoints</p>
            </div>
          </div>
          
          <div>
            <h4 className="font-medium mb-3">Most Used Endpoints</h4>
            <div className="space-y-2">
              {analyticsData.usage.mostUsedEndpoints.map((endpoint, index) => (
                <div key={index} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                  <div>
                    <p className="font-mono text-sm">{endpoint.endpoint}</p>
                    <p className="text-xs text-gray-500">{formatNumber(endpoint.calls)} calls</p>
                  </div>
                  <Badge variant="outline">{endpoint.percentage}%</Badge>
                </div>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Storage Usage */}
      <Card>
        <CardHeader>
          <CardTitle>Storage Usage</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
            <div className="text-center p-4 border rounded-lg">
              <p className="text-2xl font-bold text-blue-600">{formatFileSize(analyticsData.storage.totalUsed)}</p>
              <p className="text-sm text-gray-600">Used of {formatFileSize(analyticsData.storage.limit)}</p>
              <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                <div 
                  className="bg-blue-600 h-2 rounded-full"
                  style={{ width: `${(analyticsData.storage.totalUsed / analyticsData.storage.limit) * 100}%` }}
                ></div>
              </div>
            </div>
            
            <div className="text-center p-4 border rounded-lg">
              <p className="text-2xl font-bold text-green-600">{analyticsData.storage.fileCount.toLocaleString()}</p>
              <p className="text-sm text-gray-600">Total Files</p>
            </div>
            
            <div className="text-center p-4 border rounded-lg">
              <p className="text-2xl font-bold text-purple-600">+{analyticsData.storage.growth}%</p>
              <p className="text-sm text-gray-600">Growth Rate</p>
            </div>
          </div>
          
          <div>
            <h4 className="font-medium mb-3">Storage Breakdown</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {analyticsData.storage.breakdown.map((category, index) => (
                <div key={index} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                  <div>
                    <p className="font-medium">{category.category}</p>
                    <p className="text-sm text-gray-500">{formatFileSize(category.size)}</p>
                  </div>
                  <Badge variant="outline">{category.percentage}%</Badge>
                </div>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* User Activity Chart Placeholder */}
      <Card>
        <CardHeader>
          <CardTitle>User Activity Trends</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8">
            <BarChart3 className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">Activity Chart</h3>
            <p className="text-gray-500 mb-4">Interactive charts showing user activity trends over time will be displayed here.</p>
            <Button variant="outline">
              <BarChart3 className="h-4 w-4 mr-2" />
              View Full Analytics
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
