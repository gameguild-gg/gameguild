'use client';

import { Link, usePathname } from '@/i18n/navigation';
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
import { useParams } from 'next/navigation';
import { useState, type ReactNode } from 'react';

interface CourseNavProps {
  courseTitle: string;
  courseDescription: string;
  courseStatus: 'draft' | 'published' | 'archived';
  courseSlug: string | null;
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

export function CourseNav({ courseTitle, courseDescription, courseStatus, courseSlug, features, children }: CourseNavProps) {
  const params = useParams();
  const pathname = usePathname() ?? '';
  const courseId = params.course as string;
  const basePath = `/dashboard/learning/courses/${courseId}`;
  const publicCourseHref = courseSlug?.trim() ? `/courses/${courseSlug.trim()}` : null;
  const previewHref = `${basePath}/preview`;
  const [shareLabel, setShareLabel] = useState('Share');

  const navItems = buildNavItems(features);

  async function copyPublicCourseUrl() {
    if (!publicCourseHref || !navigator.clipboard?.writeText) {
      return;
    }

    await navigator.clipboard.writeText(`${window.location.origin}${publicCourseHref}`);
    setShareLabel('Copied');
  }

  // Match the active segment after the courseId
  const activeSegment = (() => {
    const idx = pathname.indexOf(`/courses/${courseId}/`);
    if (idx === -1) return '';
    const tail = pathname.slice(idx + `/courses/${courseId}/`.length);
    return tail.split('/')[0] ?? '';
  })();

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/dashboard/learning/courses">
              <ArrowLeft className="size-5" />
            </Link>
          </Button>
          <div className="flex size-12 shrink-0 items-center justify-center rounded-lg bg-linear-to-br from-emerald-500 to-teal-600">
            <BookOpen className="size-6 text-white" />
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="truncate text-2xl font-bold tracking-tight">{courseTitle}</h1>
              {getStatusBadge(courseStatus)}
            </div>
            <p className="truncate text-sm text-muted-foreground">{courseDescription}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" asChild>
            <Link href={previewHref}>
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
                <Link href={`${basePath}/listing`}>
                  <Edit className="mr-2 size-4" />
                  Edit Listing
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={`${basePath}/settings`}>
                  <Settings className="mr-2 size-4" />
                  Settings
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem className="text-destructive" asChild>
                <Link href={`${basePath}/settings/danger`}>
                  <Trash2 className="mr-2 size-4" />
                  Delete Course
                </Link>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {/* Nav Tabs + Content */}
      <div className="flex flex-1 gap-6">
        {/* Sidebar Nav */}
        <nav className="hidden w-48 shrink-0 flex-col gap-1 lg:flex">
          {navItems.map((item) => {
            const isActive = activeSegment === item.segment;
            return (
              <Link
                key={item.segment}
                href={`${basePath}/${item.segment}`}
                className={`flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors ${isActive ? 'bg-muted text-foreground' : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground'
                  }`}
              >
                <item.icon className="size-4" />
                {item.title}
              </Link>
            );
          })}
        </nav>

        {/* Mobile tabs - scrollable */}
        <div className="flex w-full flex-col gap-6 lg:hidden">
          <div className="flex gap-1 overflow-x-auto border-b pb-2">
            {navItems.map((item) => {
              const isActive = activeSegment === item.segment;
              return (
                <Link
                  key={item.segment}
                  href={`${basePath}/${item.segment}`}
                  className={`flex shrink-0 items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${isActive ? 'bg-muted text-foreground' : 'text-muted-foreground hover:text-foreground'
                    }`}
                >
                  <item.icon className="size-3.5" />
                  {item.title}
                </Link>
              );
            })}
          </div>
          <div className="flex-1">{children}</div>
        </div>

        {/* Desktop Content */}
        <div className="hidden min-w-0 flex-1 flex-col lg:flex">{children}</div>
      </div>
    </div>
  );
}
