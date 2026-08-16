'use client';

import { Link, usePathname, useRouter } from '@/i18n/navigation';
import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import {
  BarChart3,
  BookOpen,
  CalendarDays,
  ChevronDown,
  ClipboardCheck,
  LayoutDashboard,
  Settings,
  Users,
  type LucideIcon,
} from 'lucide-react';
import type { ReactNode } from 'react';

interface CohortWorkspaceNavProps {
  courseRoute: string;
  courseTitle: string;
  cohort: CourseCohortSummary;
  cohorts: CourseCohortSummary[];
  children: ReactNode;
}

interface WorkspaceSection {
  segment: 'overview' | 'schedule' | 'students' | 'assessments' | 'gradebook' | 'settings';
  label: string;
  icon: LucideIcon;
}

const sections: WorkspaceSection[] = [
  { segment: 'overview', label: 'Overview', icon: LayoutDashboard },
  { segment: 'schedule', label: 'Schedule & content', icon: CalendarDays },
  { segment: 'students', label: 'Students', icon: Users },
  { segment: 'assessments', label: 'Assessments', icon: ClipboardCheck },
  { segment: 'gradebook', label: 'Gradebook', icon: BarChart3 },
  { segment: 'settings', label: 'Settings', icon: Settings },
];

const periodFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' });

export function CohortWorkspaceNav({ courseRoute, courseTitle, cohort, cohorts, children }: CohortWorkspaceNavProps) {
  const pathname = usePathname() ?? '';
  const router = useRouter();
  const basePath = `/dashboard/platform/learning/courses/${courseRoute}/classes/${cohort.id}`;
  const activeSegment = sections.find((section) => pathname.includes(`/${section.segment}`))?.segment ?? 'schedule';

  return (
    <div className="min-w-0 space-y-5">
      <header className="rounded-lg border bg-card px-4 py-4 sm:px-5">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex min-w-0 items-start gap-3">
            <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-emerald-500/10 text-emerald-600 dark:text-emerald-300">
              <BookOpen className="size-5" />
            </div>
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                <Link href={`/dashboard/platform/learning/courses/${courseRoute}/classes`} className="hover:text-foreground hover:underline">{courseTitle}</Link>
                <span aria-hidden="true">/</span>
                <span>Classes</span>
              </div>
              <div className="mt-1 flex flex-wrap items-center gap-2">
                <h2 className="truncate text-lg font-semibold">{cohort.name}</h2>
                <Badge variant="outline" className="capitalize">{cohort.status}</Badge>
              </div>
              <p className="mt-1 text-sm text-muted-foreground">
                {periodFormatter.format(new Date(cohort.period.startsAt))} - {periodFormatter.format(new Date(cohort.period.endsAt))}
                {cohort.meetingPattern ? ` · ${cohort.meetingPattern}` : ''}
              </p>
            </div>
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" className="w-full justify-between sm:w-auto" aria-label="Switch class">
                Switch class <ChevronDown className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-72">
              {cohorts.map((item) => (
                <DropdownMenuItem
                  key={item.id}
                  disabled={item.id === cohort.id}
                  onSelect={() => router.push(`/dashboard/platform/learning/courses/${courseRoute}/classes/${item.id}/schedule`)}
                >
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-medium">{item.name}</span>
                    <span className="block truncate text-xs text-muted-foreground">{item.meetingPattern ?? 'Schedule not configured'}</span>
                  </span>
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>

      <div className="flex min-w-0 items-start gap-5">
        <nav className="sticky top-20 hidden w-52 shrink-0 flex-col gap-1 lg:flex" aria-label="Class workspace">
          {sections.map((section) => {
            const active = activeSegment === section.segment;
            return (
              <Link
                key={section.segment}
                href={`${basePath}/${section.segment}`}
                className={`flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors ${active ? 'bg-muted text-foreground' : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground'}`}
              >
                <section.icon className="size-4" />
                {section.label}
              </Link>
            );
          })}
        </nav>

        <div className="min-w-0 flex-1">
          <nav className="mb-4 flex min-w-0 gap-1 overflow-x-auto border-b pb-2 lg:hidden" aria-label="Class workspace mobile">
            {sections.map((section) => {
              const active = activeSegment === section.segment;
              return (
                <Link
                  key={section.segment}
                  href={`${basePath}/${section.segment}`}
                  className={`flex shrink-0 items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium ${active ? 'bg-muted text-foreground' : 'text-muted-foreground'}`}
                >
                  <section.icon className="size-3.5" />
                  {section.label}
                </Link>
              );
            })}
          </nav>
          {children}
        </div>
      </div>
    </div>
  );
}
