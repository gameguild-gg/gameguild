'use client';

import { useParams, usePathname } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowLeft,
  Award,
  BookOpen,
  Calendar,
  DollarSign,
  Eye,
  FileText,
  HelpCircle,
  Image,
  Play,
  Save,
  Search,
  Settings,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarMenu, SidebarMenuButton, SidebarMenuItem } from '@/components/ui/sidebar';
import { useCourseEditor } from '@/components/courses/editor/context/course-editor-provider';
import { cn } from '@/lib/utils';
import { useState } from 'react';

const OVERVIEW_SECTION = {
  id: 'overview',
  label: 'Overview',
  icon: Eye,
  path: '',
  description: 'Course dashboard, quick actions',
};

const COURSE_SECTION_GROUPS = [
  {
    label: 'Course Setup',
    defaultOpen: true,
    sections: [
      {
        id: 'details',
        label: 'Course Details',
        icon: FileText,
        path: '/details',
        description: 'Title, description, category',
      },
      {
        id: 'delivery',
        label: 'Delivery & Schedule',
        icon: Calendar,
        path: '/delivery',
        description: 'Course type, sessions, schedule',
      },
      {
        id: 'pricing',
        label: 'Pricing & Sales',
        icon: DollarSign,
        path: '/pricing',
        description: 'Products, pricing, enrollment',
      },
    ],
  },
  {
    label: 'Content & Media',
    defaultOpen: false,
    sections: [
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
    ],
  },
  {
    label: 'Certification & Optimization',
    defaultOpen: false,
    sections: [
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
    ],
  },
  {
    label: 'Publishing & Management',
    defaultOpen: false,
    sections: [
      {
        id: 'publish',
        label: 'Preview & Publish',
        icon: Play,
        path: '/publish',
        description: 'Publishing, versions, live preview',
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
    ],
  },
];

export function CourseEditorSidebar() {
  const pathname = usePathname();
  const params = useParams();
  const slug = params.slug as string;
  const { state, validate } = useCourseEditor();

  // Initialize collapsed state based on defaultOpen
  const [collapsedGroups, setCollapsedGroups] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {};
    COURSE_SECTION_GROUPS.forEach((group) => {
      initial[group.label] = !group.defaultOpen;
    });
    return initial;
  });

  const basePath = `/dashboard/courses/${slug}`;

  const toggleGroup = (groupLabel: string) => {
    setCollapsedGroups((prev) => ({
      ...prev,
      [groupLabel]: !prev[groupLabel],
    }));
  };

  const renderSection = (section: typeof OVERVIEW_SECTION) => {
    const isActive = pathname === `${basePath}${section.path}` || (section.path === '' && pathname === basePath);

    return (
      <SidebarMenuItem key={section.id}>
        <SidebarMenuButton asChild>
          <Link
            href={`${basePath}${section.path}`}
            className={cn(
              'flex items-center gap-3 p-3 rounded-lg transition-all duration-200 group border backdrop-blur-md',
              isActive
                ? 'bg-gradient-to-r from-blue-600/20 to-purple-600/20 text-white border-blue-500/30 shadow-lg shadow-blue-500/10'
                : 'bg-slate-800/30 text-slate-300 border-slate-700/30 hover:bg-slate-700/40 hover:text-white hover:border-slate-600/40',
            )}
          >
            <div
              className={cn(
                'p-2 rounded-lg transition-all duration-200',
                isActive ? 'bg-gradient-to-r from-blue-600 to-purple-600 shadow-lg' : 'bg-slate-700/50 group-hover:bg-slate-600/50',
              )}
            >
              <section.icon className="h-4 w-4 text-white" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="font-medium text-sm truncate">{section.label}</div>
              <div className={cn('text-xs truncate transition-colors duration-200', isActive ? 'text-blue-200' : 'text-slate-400 group-hover:text-slate-300')}>
                {section.description}
              </div>
            </div>
            {isActive && <div className="w-2 h-2 bg-blue-400 rounded-full animate-pulse flex-shrink-0" />}
          </Link>
        </SidebarMenuButton>
      </SidebarMenuItem>
    );
  };

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
    <Sidebar collapsible="none" className="border-r border-slate-700/50">
      <SidebarHeader className="border-b border-slate-700/50 bg-gradient-to-b from-slate-900/95 via-slate-900/90 to-slate-800/95 backdrop-blur-xl">
        <div className="p-4">
          <Link href="/dashboard/courses">
            <Button
              variant="ghost"
              size="sm"
              className="mb-4 bg-slate-900/30 backdrop-blur-md border border-slate-700/50 text-slate-200 hover:text-white hover:bg-slate-800/40 hover:border-slate-600/50 transition-all duration-200"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Courses
            </Button>
          </Link>

          <div className="space-y-3">
            <h2 className="font-semibold text-xl bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent truncate">
              {state.title || 'Untitled Course'}
            </h2>
            {state.slug && (
              <p className="text-sm text-slate-400 truncate font-mono bg-slate-800/30 px-2 py-1 rounded border border-slate-700/30">/{state.slug}</p>
            )}
          </div>

          {/* Status Badge */}
          <div className="mt-4">
            <div
              className={cn(
                'px-4 py-2 rounded-full text-sm font-medium inline-flex items-center gap-2 backdrop-blur-md border',
                state.status === 'published'
                  ? 'bg-green-900/30 text-green-400 border-green-700/50'
                  : state.status === 'draft'
                    ? 'bg-amber-900/30 text-amber-400 border-amber-700/50'
                    : 'bg-slate-900/30 text-slate-400 border-slate-700/50',
              )}
            >
              <div
                className={cn(
                  'w-2 h-2 rounded-full',
                  state.status === 'published' ? 'bg-green-400' : state.status === 'draft' ? 'bg-amber-400 animate-pulse' : 'bg-slate-400',
                )}
              />
              {state.status.charAt(0).toUpperCase() + state.status.slice(1)}
            </div>
          </div>
        </div>
      </SidebarHeader>

      <SidebarContent className="bg-gradient-to-b from-slate-900/95 via-slate-900/90 to-slate-800/95 backdrop-blur-xl p-4">
        {/* Overview Section - Always visible */}
        <div className="mb-6">
          <SidebarMenu className="space-y-1">{renderSection(OVERVIEW_SECTION)}</SidebarMenu>
        </div>

        {/* Collapsible Groups */}
        {COURSE_SECTION_GROUPS.map((group) => (
          <div key={group.label} className="mb-6">
            <div
              className="px-3 py-2 cursor-pointer hover:bg-slate-800/30 rounded-lg transition-all duration-200 flex items-center justify-between group border border-transparent hover:border-slate-700/30"
              onClick={() => toggleGroup(group.label)}
            >
              <span className="text-sm font-semibold bg-gradient-to-r from-slate-300 to-slate-400 bg-clip-text text-transparent group-hover:from-blue-300 group-hover:to-purple-300 transition-all duration-200">
                {group.label}
              </span>
              <div className="text-slate-400 group-hover:text-slate-300 transition-colors duration-200">
                {collapsedGroups[group.label] ? <ChevronRight className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
              </div>
            </div>

            {!collapsedGroups[group.label] && (
              <div className="mt-2 space-y-1 border-l-2 border-gradient-to-b border-slate-700/30 ml-3 pl-3">
                <SidebarMenu className="space-y-1">{group.sections.map((section) => renderSection(section))}</SidebarMenu>
              </div>
            )}
          </div>
        ))}

        {/* Validation Errors */}
        {Object.keys(state.errors).length > 0 && (
          <div className="mt-6 p-4 bg-gradient-to-r from-red-900/30 to-rose-900/30 backdrop-blur-md border border-red-700/50 rounded-lg">
            <h4 className="font-semibold text-sm text-red-300 mb-3 flex items-center gap-2">
              <div className="w-2 h-2 bg-red-400 rounded-full animate-pulse" />
              Validation Errors
            </h4>
            <ul className="space-y-2 text-sm text-red-200">
              {Object.entries(state.errors)
                .slice(0, 3)
                .map(([field, error]) => (
                  <li key={field} className="flex items-start gap-2">
                    <div className="w-1 h-1 bg-red-400 rounded-full mt-2 flex-shrink-0" />
                    <span className="truncate">{error}</span>
                  </li>
                ))}
              {Object.keys(state.errors).length > 3 && (
                <li className="text-red-300 font-medium text-xs">+{Object.keys(state.errors).length - 3} more errors</li>
              )}
            </ul>
          </div>
        )}
      </SidebarContent>

      <SidebarFooter className="border-t border-slate-700/50 bg-gradient-to-b from-slate-900/95 via-slate-900/90 to-slate-800/95 backdrop-blur-xl p-4">
        <div className="space-y-3">
          {/* Save Button */}
          <Button
            onClick={handleSave}
            disabled={!state.isValid}
            className="w-full bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white border-0 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30 transition-all duration-200 h-10"
            size="sm"
          >
            <Save className="h-4 w-4 mr-2" />
            Save Changes
          </Button>

          {/* Preview Button */}
          <Button
            variant="outline"
            onClick={handlePreview}
            disabled={!state.slug}
            className="w-full bg-slate-800/30 backdrop-blur-md border border-slate-700/50 text-slate-200 hover:text-white hover:bg-slate-700/40 hover:border-slate-600/50 transition-all duration-200"
            size="sm"
          >
            <Eye className="h-4 w-4 mr-2" />
            Preview
          </Button>
        </div>
      </SidebarFooter>
    </Sidebar>
  );
}
