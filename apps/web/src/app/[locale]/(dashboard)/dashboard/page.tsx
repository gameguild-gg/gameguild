import { Link } from '@/i18n/navigation';
import { getCommunityStats } from '@/lib/community';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Activity, BookOpen, FlaskConical, GraduationCap, Rocket, Users } from 'lucide-react';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const communityStats = await getCommunityStats();
  const sections = [
    {
      title: 'Community',
      description: 'Members, groups, and engagement',
      icon: Users,
      href: '/dashboard/community',
      stats: `${communityStats.totalMembers} members · ${communityStats.activeMembers} active`,
    },
    {
      title: 'Learning',
      description: 'Courses, tutorials, and resources',
      icon: BookOpen,
      href: '/dashboard/learning',
      stats: 'Manage courses & content',
    },
    {
      title: 'Activity',
      description: 'Recent platform activity',
      icon: Activity,
      href: '/dashboard/learning/overview',
      stats: 'View engagement trends',
    },
    {
      title: 'Instructor',
      description: 'Teaching tools and analytics',
      icon: GraduationCap,
      href: '/dashboard/learning/courses',
      stats: 'Manage your courses',
    },
    {
      title: 'Launch Pad',
      description: 'Prepare projects for release',
      icon: Rocket,
      href: '/dashboard/launch-pad',
      stats: 'Track readiness & channels',
    },
    {
      title: 'Testing Lab',
      description: 'Run moderated project tests',
      icon: FlaskConical,
      href: '/dashboard/testing-lab',
      stats: 'Manage sessions & feedback',
    },
  ] as const;

  return (
    <div className="flex min-w-0 flex-col gap-6">
      <div className="min-w-0">
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Welcome to the Game Guild management dashboard.</p>
      </div>

      <div className="grid min-w-0 grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Members</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{communityStats.totalMembers}</div>
            <CardDescription>{communityStats.activeMembers} active in the current tenant</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Groups</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{communityStats.totalGroups}</div>
            <CardDescription>Community spaces loaded from the API</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Support</CardTitle>
            <Activity className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{communityStats.openTickets}</div>
            <CardDescription>Open member support requests</CardDescription>
          </CardContent>
        </Card>
      </div>

      <div className="grid min-w-0 grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {sections.map((section) => (
          <Link key={section.title} href={section.href}>
            <Card className="h-full transition-colors hover:bg-muted/50">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">{section.title}</CardTitle>
                <section.icon className="size-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-lg font-semibold">{section.description}</div>
                <CardDescription>{section.stats}</CardDescription>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
