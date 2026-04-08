'use client';

import Link from 'next/link';
import { useParams, usePathname } from 'next/navigation';
import {
  ArrowLeft,
  BarChart3,
  BookOpen,
  Edit,
  Eye,
  FileText,
  GraduationCap,
  Info,
  LayoutDashboard,
  MessageSquare,
  MoreHorizontal,
  Settings,
  Share2,
  Trash2,
  Users,
} from 'lucide-react';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';

interface CourseNavProps {
  courseTitle: string;
  courseDescription: string;
  courseStatus: 'draft' | 'published' | 'archived';
  children: React.ReactNode;
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

const navItems = [
  { title: 'Overview', icon: LayoutDashboard, href: '/overview' },
  { title: 'Course Info', icon: Info, href: '/listing' },
  { title: 'Content', icon: BookOpen, href: '/content' },
  { title: 'Assessments', icon: GraduationCap, href: '/assessments' },
  { title: 'Students', icon: Users, href: '/students' },
  { title: 'Analytics', icon: BarChart3, href: '/analytics' },
  { title: 'Support', icon: MessageSquare, href: '/support' },
  { title: 'Settings', icon: Settings, href: '/settings' },
];

export function CourseNav({ courseTitle, courseDescription, courseStatus, children }: CourseNavProps) {
  const params = useParams();
  const pathname = usePathname();
  const locale = params.locale as string;
  const courseId = params.course as string;
  const basePath = `/${locale}/dashboard/learning/courses/${courseId}`;

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href={`/${locale}/dashboard/learning/courses`}>
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
          <Button variant="outline" size="sm">
            <Eye className="mr-2 size-4" />
            Preview
          </Button>
          <Button variant="outline" size="sm">
            <Share2 className="mr-2 size-4" />
            Share
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="icon">
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={`${basePath}/listing/info`}>
                  <Edit className="mr-2 size-4" />
                  Edit Course
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
            const href = `${basePath}${item.href}`;
            const isActive = pathname.startsWith(href);
            return (
              <Link
                key={item.title}
                href={href}
                className={`flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                  isActive ? 'bg-muted text-foreground' : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground'
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
              const href = `${basePath}${item.href}`;
              const isActive = pathname.startsWith(href);
              return (
                <Link
                  key={item.title}
                  href={href}
                  className={`flex shrink-0 items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                    isActive ? 'bg-muted text-foreground' : 'text-muted-foreground hover:text-foreground'
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
