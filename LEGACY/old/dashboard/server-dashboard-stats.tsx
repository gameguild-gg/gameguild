import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { formatCurrency, formatNumber } from '@/lib/utils';
import { Activity, BarChart3, BookOpen, Calendar, DollarSign, TrendingDown, TrendingUp, UserCheck, Users } from 'lucide-react';
import type { UserStatistics } from '@/lib/users/users.actions';

interface ServerDashboardStatsProps {
  data: UserStatistics;
  className?: string;
}

export function ServerDashboardStats({ data, className }: ServerDashboardStatsProps) {
  const userStatistics = data;

  // Calculate trends (mock calculation for demo - in real app this would come from API)
  const userGrowthTrend = userStatistics.usersCreatedThisMonth > 0 ? 'up' : 'down';

  const stats = [
    {
      title: 'Total Users',
      value: formatNumber(userStatistics.totalUsers),
      change: `+${userStatistics.usersCreatedThisMonth}`,
      trend: userGrowthTrend as 'up' | 'down',
      icon: Users,
      description: 'All registered users',
      subtitle: `${userStatistics.activeUsers} active`,
    },
    {
      title: 'Active Users',
      value: formatNumber(userStatistics.activeUsers),
      change: `${Math.round((userStatistics.activeUsers / userStatistics.totalUsers) * 100)}%`,
      trend: 'up' as const,
      icon: UserCheck,
      description: 'Currently active users',
      subtitle: `${userStatistics.inactiveUsers} inactive`,
    },
    {
      title: 'New This Week',
      value: formatNumber(userStatistics.usersCreatedThisWeek),
      change: `+${userStatistics.usersCreatedToday} today`,
      trend: userGrowthTrend as 'up' | 'down',
      icon: Calendar,
      description: 'New users this week',
      subtitle: `${userStatistics.usersCreatedThisMonth} this month`,
    },
    {
      title: 'Total Balance',
      value: formatCurrency(userStatistics.totalBalance),
      change: `Avg: ${formatCurrency(userStatistics.averageBalance)}`,
      trend: 'up' as const,
      icon: DollarSign,
      description: 'Total user balance',
      subtitle: 'Across all users',
    },
    {
      title: 'Users This Week',
      value: formatNumber(userStatistics.usersCreatedThisWeek),
      change: `+${userStatistics.usersCreatedToday} today`,
      trend: userGrowthTrend as 'up' | 'down',
      icon: Calendar,
      description: 'New users this week',
      subtitle: `${userStatistics.usersCreatedThisMonth} this month`,
    },
    {
      title: 'Inactive Users',
      value: formatNumber(userStatistics.inactiveUsers),
      change: `${userStatistics.deletedUsers} deleted`,
      trend: 'down' as const,
      icon: UserCheck,
      description: 'Inactive user accounts',
      subtitle: 'Need attention',
    },
  ];

  return (
    <div className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 ${className}`}>
      {stats.map((stat, index) => {
        const Icon = stat.icon;
        const trendColor = stat.trend === 'up' ? 'text-green-400' : 'text-red-400';
        const TrendIcon = stat.trend === 'up' ? TrendingUp : TrendingDown;
        const bgColor = stat.trend === 'up' ? 'bg-green-500/10' : 'bg-red-500/10';

        return (
          <Card key={index} className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm hover:bg-slate-800/70 transition-colors">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-300">{stat.title}</CardTitle>
              <div className={`p-2 rounded-full ${bgColor}`}>
                <Icon className="h-4 w-4 text-white" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white mb-1">{stat.value}</div>
              <div className="flex items-center justify-between">
                <p className="text-xs text-slate-400">{stat.description}</p>
                <div className="flex items-center space-x-1">
                  <TrendIcon className={`h-3 w-3 ${trendColor}`} />
                  <span className={`text-xs ${trendColor}`}>{stat.change}</span>
                </div>
              </div>
              <div className="text-xs text-slate-500 mt-1">{stat.subtitle}</div>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}

export function DashboardStatsLoading({ className }: { className?: string }) {
  return (
    <div className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 ${className}`}>
      {[...Array(8)].map((_, i) => (
        <Card key={i} className="bg-slate-800/50 border-slate-700/50">
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <Skeleton className="h-4 w-24 bg-slate-700" />
              <Skeleton className="h-8 w-8 rounded-full bg-slate-700" />
            </div>
          </CardHeader>
          <CardContent>
            <Skeleton className="h-8 w-20 mb-2 bg-slate-700" />
            <div className="flex items-center justify-between">
              <Skeleton className="h-3 w-24 bg-slate-700" />
              <Skeleton className="h-3 w-16 bg-slate-700" />
            </div>
            <Skeleton className="h-3 w-20 mt-1 bg-slate-700" />
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

export function DashboardStatsError({ error, retry }: { error: string; retry?: () => void }) {
  return (
    <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
      <CardContent className="pt-6">
        <div className="text-center">
          <Activity className="mx-auto h-8 w-8 mb-2 text-red-400" />
          <CardTitle className="text-lg text-white mb-2">Failed to load dashboard statistics</CardTitle>
          <CardDescription className="text-slate-400 mb-4">{error}</CardDescription>
          {retry && (
            <button onClick={retry} className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors">
              Try Again
            </button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
