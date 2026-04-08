import React from 'react';
import { getCommunityStats } from '@/lib/community';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Users, UserPlus, MessageSquare, ShieldAlert } from 'lucide-react';

export default async function Page(): Promise<React.JSX.Element> {
  const stats = await getCommunityStats();

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
            <CardDescription>Latest community interactions</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">No recent activity to display.</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Growth Trend</CardTitle>
            <CardDescription>Member registration over time</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">No data available yet.</p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
