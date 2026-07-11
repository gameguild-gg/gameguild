'use client';

import { Link, usePathname } from '@/i18n/navigation';
import { buildDashboardCoursePath } from '@/lib/learning/course-route';
import type { CourseFeatures } from '@/lib/learning/types';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
import {
  ArrowLeft,
  Award,
  BarChart3,
  BookOpen,
  CalendarDays,
  Edit,
  Eye,
  GraduationCap,
  Info,
  LayoutDashboard,
  MessageSquare,
  MoreHorizontal,
  Settings,
  Share2,
  Trash2,
  Users,
  type LucideIcon,
} from 'lucide-react';
import { useState, type ReactNode } from 'react';

interface CourseNavProps {
  courseTitle: string;
  courseDescription: string;
  courseStatus: 'draft' | 'published' | 'archived';
  courseSlug: string | null;
  courseRouteParam: string;
  locale: string;
  features: CourseFeatures;
  children: ReactNode;
}

interface NavItem {
  title: string;
  icon: LucideIcon;
  segment: string;
  enabled: boolean;
}

function getStatusBadge(status: string) {
  switch (status) {
    case 'published':
      return <Badge className="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">Published</Badge>;
    case 'draft':
      return <Badge variant="secondary">Draft</Badge>;
    case 'archived':
      return <Badge variant="outline">Archived</Badge>;
    default:
      return null;
  }
}

function buildNavItems(features: CourseFeatures): NavItem[] {
  return [
    { title: 'Overview', icon: LayoutDashboard, segment: 'overview', enabled: true },
    { title: 'Listing', icon: Info, segment: 'listing', enabled: true },
    { title: 'Content', icon: BookOpen, segment: 'content', enabled: true },
    { title: 'Classes', icon: CalendarDays, segment: 'classes', enabled: features.hasClasses },
    { title: 'Assessments', icon: GraduationCap, segment: 'assessments', enabled: features.hasAssessments },
    { title: 'Certificates', icon: Award, segment: 'certificates', enabled: features.hasCertificate },
    { title: 'Students', icon: Users, segment: 'students', enabled: true },
    { title: 'Analytics', icon: BarChart3, segment: 'analytics', enabled: true },
    { title: 'Support', icon: MessageSquare, segment: 'support', enabled: true },
    { title: 'Settings', icon: Settings, segment: 'settings', enabled: true },
  ].filter((item) => item.enabled);
}

export function CourseNav({ courseTitle, courseDescription, courseStatus, courseSlug, courseRouteParam, locale, features, children }: CourseNavProps) {
  const pathname = usePathname() ?? '';
  const publicCourseHref = courseSlug?.trim() ? `/courses/${courseSlug.trim()}` : null;
  const previewHref = buildDashboardCoursePath(courseRouteParam, 'preview');
  const [shareLabel, setShareLabel] = useState('Share');

  const navItems = buildNavItems(features);

  async function copyPublicCourseUrl() {
    if (!publicCourseHref || !navigator.clipboard?.writeText) {
      return;
    }

    await navigator.clipboard.writeText(`${window.location.origin}${publicCourseHref}`);
    setShareLabel('Copied');
  }

  // Match the active segment after the dynamic course route param.
  const activeSegment = (() => {
    const marker = '/dashboard/learning/courses/';
    const idx = pathname.indexOf(marker);
    if (idx === -1) return '';
    const tail = pathname.slice(idx + marker.length);
    return tail.split('/')[1] ?? '';
  })();

  return (
    <div className="flex min-w-0 max-w-full flex-col gap-6 overflow-x-hidden">
      {/* Header */}
      <div className="flex min-w-0 max-w-full flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex min-w-0 items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/dashboard/learning/courses" locale={locale} prefetch={false}>
              <ArrowLeft className="size-5" />
            </Link>
          </Button>
          <div className="flex size-12 shrink-0 items-center justify-center rounded-lg bg-linear-to-br from-emerald-500 to-teal-600">
            <BookOpen className="size-6 text-white" />
          </div>
          <div className="min-w-0">
            <div className="flex min-w-0 flex-wrap items-center gap-2">
              <h1 className="min-w-0 break-words text-2xl font-bold tracking-tight">{courseTitle}</h1>
              {getStatusBadge(courseStatus)}
            </div>
            <p className="max-w-prose break-words text-sm text-muted-foreground">{courseDescription}</p>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2 sm:justify-end">
          <Button variant="outline" size="sm" asChild>
            <Link href={previewHref} locale={locale} prefetch={false}>
              <Eye className="mr-2 size-4" />
              Preview
            </Link>
          </Button>
          <Button variant="outline" size="sm" type="button" disabled={!publicCourseHref} onClick={copyPublicCourseUrl}>
            <Share2 className="mr-2 size-4" />
            {shareLabel}
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="icon">
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={buildDashboardCoursePath(courseRouteParam, 'listing')} locale={locale} prefetch={false}>
                  <Edit className="mr-2 size-4" />
                  Edit Listing
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={buildDashboardCoursePath(courseRouteParam, 'settings')} locale={locale} prefetch={false}>
                  <Settings className="mr-2 size-4" />
                  Settings
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem className="text-destructive" asChild>
                <Link href={buildDashboardCoursePath(courseRouteParam, 'settings/danger')} locale={locale} prefetch={false}>
                  <Trash2 className="mr-2 size-4" />
                  Delete Course
                </Link>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {/* Nav Tabs + Content */}
      <div className="flex min-w-0 flex-1 gap-6">
        {/* Sidebar Nav */}
        <nav className="hidden w-48 shrink-0 flex-col gap-1 lg:flex">
          {navItems.map((item) => {
            const isActive = activeSegment === item.segment;
            return (
              <Link
                key={item.segment}
                href={buildDashboardCoursePath(courseRouteParam, item.segment)}
                locale={locale}
                prefetch={false}
                className={`flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors ${isActive ? 'bg-muted text-foreground' : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground'
                  }`}
              >
                <item.icon className="size-4" />
                {item.title}
              </Link>
            );
          })}
        </nav>

        <div className="flex min-w-0 w-full flex-1 flex-col gap-6">
          {/* Mobile tabs - scrollable */}
          <div className="flex min-w-0 max-w-full gap-1 overflow-x-auto border-b pb-2 lg:hidden">
            {navItems.map((item) => {
              const isActive = activeSegment === item.segment;
              return (
                <Link
                  key={item.segment}
                  href={buildDashboardCoursePath(courseRouteParam, item.segment)}
                  locale={locale}
                  prefetch={false}
                  className={`flex shrink-0 items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${isActive ? 'bg-muted text-foreground' : 'text-muted-foreground hover:text-foreground'
                    }`}
                >
                  <item.icon className="size-3.5" />
                  {item.title}
                </Link>
              );
            })}
          </div>
          <div className="min-w-0 flex-1">{children}</div>
        </div>
      </div>
    </div>
  );
}
