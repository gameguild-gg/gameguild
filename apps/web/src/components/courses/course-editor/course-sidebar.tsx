'use client';

import { useParams, usePathname } from 'next/navigation';
import Link from 'next/link';
import { ArrowLeft, Award, BookOpen, Calendar, DollarSign, Eye, FileText, HelpCircle, Image, Play, Save, Search, Settings } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarMenu, SidebarMenuButton, SidebarMenuItem } from '@/components/ui/sidebar';
import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { cn } from '@/lib/utils';

const COURSE_SECTIONS = [
  {
    id: 'overview',
    label: 'Overview',
    icon: Eye,
    path: '',
    description: 'Course dashboard, quick actions',
  },
  {
    id: 'details',
    label: 'Course Details',
    icon: FileText,
    path: '/details',
    description: 'Title, description, category',
  },
  {
    id: 'content',
    label: 'Course Content',
    icon: BookOpen,
    path: '/content',
    description: 'Lessons, modules, materials',
  },
  {
    id: 'media',
    label: 'Media & Assets',
    icon: Image,
    path: '/media',
    description: 'Thumbnail, videos, images',
  },
  {
    id: 'delivery',
    label: 'Delivery & Schedule',
    icon: Calendar,
    path: '/delivery',
    description: 'Course type, sessions, schedule',
  },
  {
    id: 'certificates',
    label: 'Certificates & Skills',
    icon: Award,
    path: '/certificates',
    description: 'Skills, prerequisites, certificates',
  },
  {
    id: 'seo',
    label: 'SEO & Metadata',
    icon: Search,
    path: '/seo',
    description: 'Search optimization, social media',
  },
  {
    id: 'publish',
    label: 'Preview & Publish',
    icon: Play,
    path: '/publish',
    description: 'Publishing, versions, live preview',
  },
  {
    id: 'pricing',
    label: 'Pricing & Sales',
    icon: DollarSign,
    path: '/pricing',
    description: 'Products, pricing, enrollment',
  },
  {
    id: 'settings',
    label: 'Settings',
    icon: Settings,
    path: '/settings',
    description: 'Publishing, access, advanced',
  },
  {
    id: 'help',
    label: 'Help & Guidance',
    icon: HelpCircle,
    path: '/help',
    description: 'Setup guide, tips, resources',
  },
];

export function CourseSidebar() {
  const pathname = usePathname();
  const params = useParams();
  const slug = params.slug as string;
  const { state, validate } = useCourseEditor();

  const basePath = `/dashboard/courses/${slug}`;

  const handleSave = async () => {
    validate();

    if (!state.isValid) {
      // Scroll to first error or show toast
      return;
    }

    try {
      // TODO: Implement API call to save course
      console.log('Saving course:', state);
    } catch (error) {
      console.error('Failed to save course:', error);
      // Show error toast
    }
  };

  const handlePreview = () => {
    // TODO: Open preview in new tab
    window.open(`/courses/${state.slug}`, '_blank');
  };

  return (
    <Sidebar collapsible="none">
      <SidebarHeader className="border-b border-border">
        <div className="p-4">
          <Link href="/dashboard/courses">
            <Button variant="ghost" size="sm" className="mb-4">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Courses
            </Button>
          </Link>

          <div>
            <h2 className="font-semibold text-lg text-foreground truncate">{state.title || 'Untitled Course'}</h2>
            {state.slug && <p className="text-sm text-muted-foreground truncate">/{state.slug}</p>}
          </div>

          {/* Status Badge */}
          <div
            className={cn(
              'mt-3 px-3 py-1 rounded-full text-xs font-medium inline-block',
              state.status === 'published'
                ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                : state.status === 'draft'
                  ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400'
                  : 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400',
            )}
          >
            {state.status.charAt(0).toUpperCase() + state.status.slice(1)}
          </div>
        </div>
      </SidebarHeader>

      <SidebarContent className="p-4">
        <SidebarMenu>
          {COURSE_SECTIONS.map((section) => {
            const isActive = pathname === `${basePath}${section.path}` || (section.path === '' && pathname === basePath);

            return (
              <SidebarMenuItem key={section.id}>
                <SidebarMenuButton asChild>
                  <Link
                    href={`${basePath}${section.path}`}
                    className={cn(
                      'flex flex-col items-start p-3 rounded-lg transition-colors group',
                      isActive ? 'bg-primary text-primary-foreground' : 'hover:bg-muted text-foreground',
                    )}
                  >
                    <div className="flex items-center gap-3 w-full">
                      <section.icon className="h-5 w-5 flex-shrink-0" />
                      <div className="flex-1 min-w-0">
                        <div className="font-medium text-sm truncate">{section.label}</div>
                        <div className={cn('text-xs truncate', isActive ? 'text-primary-foreground/80' : 'text-muted-foreground')}>{section.description}</div>
                      </div>
                    </div>
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            );
          })}
        </SidebarMenu>

        {/* Validation Errors */}
        {Object.keys(state.errors).length > 0 && (
          <div className="mt-6 p-3 bg-red-50/50 border border-red-200 rounded-lg">
            <h4 className="font-medium text-sm text-red-800 mb-2">⚠️ Validation Errors</h4>
            <ul className="space-y-1 text-xs text-red-700">
              {Object.entries(state.errors)
                .slice(0, 3)
                .map(([field, error]) => (
                  <li key={field} className="truncate">
                    • {error}
                  </li>
                ))}
              {Object.keys(state.errors).length > 3 && <li className="text-red-600">+{Object.keys(state.errors).length - 3} more errors</li>}
            </ul>
          </div>
        )}
      </SidebarContent>

      <SidebarFooter className="border-t border-border p-4">
        <div className="space-y-2">
          {/* Save Button */}
          <Button
            onClick={handleSave}
            disabled={!state.isValid}
            className="w-full bg-gradient-to-r from-primary to-chart-2 hover:from-primary/90 hover:to-chart-2/90"
            size="sm"
          >
            <Save className="h-4 w-4 mr-2" />
            Save Changes
          </Button>

          {/* Preview Button */}
          <Button variant="outline" onClick={handlePreview} disabled={!state.slug} className="w-full" size="sm">
            <Eye className="h-4 w-4 mr-2" />
            Preview
          </Button>
        </div>
      </SidebarFooter>
    </Sidebar>
  );
}
