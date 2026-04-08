import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { BookOpen, Users, Activity, TrendingUp } from 'lucide-react';
import Link from 'next/link';

export default async function Page({ params }: PageProps<'/[locale]/dashboard'>): Promise<React.JSX.Element> {
  const { locale } = await params;
  void locale;

  const sections = [
    {
      title: 'Community',
      description: 'Members, groups, and engagement',
      icon: Users,
      href: '/dashboard/community',
      stats: 'Manage members & groups',
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
      href: '/dashboard/community',
      stats: 'View engagement trends',
    },
    {
      title: 'Growth',
      description: 'Platform growth metrics',
      icon: TrendingUp,
      href: '/dashboard/platform',
      stats: 'Track platform KPIs',
    },
  ];

  return (
    <div className="flex flex-col gap-6 p-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Welcome to the Game Guild management dashboard.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {sections.map((section) => (
          <Link key={section.title} href={section.href}>
            <Card className="transition-colors hover:bg-muted/50">
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
