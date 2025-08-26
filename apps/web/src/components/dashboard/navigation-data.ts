// This is sample data structured for the sidebar
import {BookOpen, Building2, FileText, LayoutDashboard, MessageSquare, Package, Settings, Users} from 'lucide-react';

export const navigationData = {
  primary: [
    {
      title: 'Overview',
      url: '/dashboard/overview',
      icon: LayoutDashboard,
    },
    {
      title: 'Tenants',
      url: '/dashboard/tenant',
      icon: Building2,
    },
    {
      title: 'Users',
      url: '/dashboard/users',
      icon: Users,
    },
    {
      title: 'Achievements',
      url: '/dashboard/achievements',
      icon: FileText,
    },
  ],
  content: [
    {
      name: 'Projects',
      url: '/dashboard/projects',
      icon: Package,
    },
    {
      name: 'Courses',
      url: '/dashboard/courses',
      icon: BookOpen,
    },
  ],
  testingLab: [
    {
      name: 'Overview',
      url: '/dashboard/testing-lab/overview',
      icon: LayoutDashboard,
    },
    {
      name: 'Sessions',
      url: '/dashboard/testing-lab/sessions',
      icon: Users,
    },
    {
      name: 'Requests',
      url: '/dashboard/testing-lab/requests',
      icon: FileText,
    },
    {
      name: 'Feedbacks',
      url: '/dashboard/testing-lab/feedbacks',
      icon: MessageSquare,
    },
    {
      name: 'Reports',
      url: '/dashboard/testing-lab/reports',
      icon: FileText,
    },
    {
      name: 'Settings',
      url: '/dashboard/testing-lab/settings',
      icon: Settings,
    },
  ],
};
