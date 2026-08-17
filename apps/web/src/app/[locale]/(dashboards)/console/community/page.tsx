import React from 'react';
import { getCommunityStats } from '@/lib/community';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Users, UserPlus, MessageSquare, ShieldAlert } from 'lucide-react';

export default async function Page(): Promise<React.JSX.Element> {
  const stats = await getCommunityStats();
  const activeRate = stats.totalMembers > 0 ? Math.round((stats.activeMembers / stats.totalMembers) * 100) : 0;

  const kpis = [
    { label: 'Total Members', value: stats.totalMembers, icon: Users, description: 'Registered community members' },
    { label: 'Active Members', value: stats.activeMembers, icon: Users, description: 'Active in the last 30 days' },
    { label: 'New This Month', value: stats.newMembersThisMonth, icon: UserPlus, description: 'Joined this month' },
    { label: 'Total Posts', value: stats.totalPosts, icon: MessageSquare, description: 'Community posts & discussions' },
    { label: 'Groups', value: stats.totalGroups, icon: Users, description: 'Community groups' },
    { label: 'Open Tickets', value: stats.openTickets, icon: ShieldAlert, description: 'Pending support requests' },
  ];

  return (
    <div className="flex flex-col gap-6 p-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Community Overview</h1>
        <p className="text-muted-foreground">Manage your community members, groups, and engagement.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {kpis.map((kpi) => (
          <Card key={kpi.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">{kpi.label}</CardTitle>
              <kpi.icon className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{kpi.value}</div>
              <CardDescription>{kpi.description}</CardDescription>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Recent Activity</CardTitle>
            <CardDescription>Live moderation and participation signals</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex items-center justify-between rounded-lg border p-3">
              <span className="text-sm text-muted-foreground">Open support requests</span>
              <span className="text-sm font-semibold">{stats.openTickets}</span>
            </div>
            <div className="flex items-center justify-between rounded-lg border p-3">
              <span className="text-sm text-muted-foreground">Published posts and discussions</span>
              <span className="text-sm font-semibold">{stats.totalPosts}</span>
            </div>
            <div className="flex items-center justify-between rounded-lg border p-3">
              <span className="text-sm text-muted-foreground">Active groups</span>
              <span className="text-sm font-semibold">{stats.totalGroups}</span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Growth Trend</CardTitle>
            <CardDescription>Current member base health</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <div className="mb-2 flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Active member rate</span>
                <span className="font-semibold">{activeRate}%</span>
              </div>
              <div className="h-2 rounded-full bg-muted">
                <div className="h-2 rounded-full bg-primary" style={{ width: `${activeRate}%` }} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">New this month</p>
                <p className="text-lg font-semibold">{stats.newMembersThisMonth}</p>
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Total members</p>
                <p className="text-lg font-semibold">{stats.totalMembers}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
