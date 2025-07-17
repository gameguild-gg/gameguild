'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { BarChart3, Bell, BookOpen, ChevronDown, FileText, LayoutDashboard, Menu, Plus, Search, Settings, Users, X } from 'lucide-react';

interface NavigationItem {
  title: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  badge?: string;
  children?: NavigationItem[];
}

const navigationItems: NavigationItem[] = [
  {
    title: 'Dashboard',
    href: '/dashboard',
    icon: LayoutDashboard,
  },
  {
    title: 'Users',
    href: '/dashboard/users',
    icon: Users,
    badge: 'New',
    children: [
      {
        title: 'All Users',
        href: '/dashboard/users',
        icon: Users,
      },
      {
        title: 'Manage Users',
        href: '/dashboard/users/manage',
        icon: Users,
      },
      {
        title: 'User Analytics',
        href: '/dashboard/users/analytics',
        icon: BarChart3,
      },
    ],
  },
  {
    title: 'Courses',
    href: '/dashboard/courses',
    icon: BookOpen,
    children: [
      {
        title: 'All Courses',
        href: '/dashboard/courses',
        icon: BookOpen,
      },
      {
        title: 'Manage Courses',
        href: '/dashboard/courses/manage',
        icon: BookOpen,
      },
      {
        title: 'Course Analytics',
        href: '/dashboard/courses/analytics',
        icon: BarChart3,
      },
    ],
  },
  {
    title: 'Analytics',
    href: '/dashboard/analytics',
    icon: BarChart3,
    children: [
      {
        title: 'Overview',
        href: '/dashboard/analytics',
        icon: BarChart3,
      },
      {
        title: 'Revenue',
        href: '/dashboard/analytics/revenue',
        icon: BarChart3,
      },
      {
        title: 'Engagement',
        href: '/dashboard/analytics/engagement',
        icon: BarChart3,
      },
    ],
  },
  {
    title: 'Reports',
    href: '/dashboard/reports',
    icon: FileText,
  },
  {
    title: 'Settings',
    href: '/dashboard/settings',
    icon: Settings,
  },
];

interface DashboardNavigationProps {
  className?: string;
}

export function DashboardNavigation({ className }: DashboardNavigationProps) {
  const pathname = usePathname();
  const [expandedItems, setExpandedItems] = useState<string[]>([]);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const toggleExpanded = (href: string) => {
    setExpandedItems((prev) => (prev.includes(href) ? prev.filter((item) => item !== href) : [...prev, href]));
  };

  const isActive = (href: string) => {
    if (href === '/dashboard') {
      return pathname === '/dashboard';
    }
    return pathname.startsWith(href);
  };

  const isExpanded = (href: string) => {
    return expandedItems.includes(href) || pathname.startsWith(href);
  };

  const NavigationList = () => (
    <nav className="space-y-2">
      {navigationItems.map((item) => {
        const hasChildren = item.children && item.children.length > 0;
        const expanded = isExpanded(item.href);

        return (
          <div key={item.href}>
            <div className="flex items-center">
              <Link
                href={hasChildren ? '#' : item.href}
                className={cn(
                  'flex-1 flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive(item.href) ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                )}
                onClick={(e) => {
                  if (hasChildren) {
                    e.preventDefault();
                    toggleExpanded(item.href);
                  } else {
                    setIsMobileMenuOpen(false);
                  }
                }}
              >
                <item.icon className="h-4 w-4" />
                <span className="flex-1">{item.title}</span>
                {item.badge && (
                  <Badge variant="secondary" className="text-xs">
                    {item.badge}
                  </Badge>
                )}
              </Link>
              {hasChildren && (
                <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={() => toggleExpanded(item.href)}>
                  <ChevronDown className={cn('h-4 w-4 transition-transform', expanded && 'rotate-180')} />
                </Button>
              )}
            </div>

            {hasChildren && expanded && (
              <div className="ml-6 mt-2 space-y-1">
                {item.children.map((child) => (
                  <Link
                    key={child.href}
                    href={child.href}
                    className={cn(
                      'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors',
                      isActive(child.href) ? 'bg-primary/10 text-primary font-medium' : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                    )}
                    onClick={() => setIsMobileMenuOpen(false)}
                  >
                    <child.icon className="h-3 w-3" />
                    {child.title}
                  </Link>
                ))}
              </div>
            )}
          </div>
        );
      })}
    </nav>
  );

  return (
    <>
      {/* Mobile Menu Button */}
      <div className="lg:hidden fixed top-4 left-4 z-50">
        <Button variant="outline" size="sm" onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)} className="bg-background">
          {isMobileMenuOpen ? <X className="h-4 w-4" /> : <Menu className="h-4 w-4" />}
        </Button>
      </div>

      {/* Mobile Menu Overlay */}
      {isMobileMenuOpen && <div className="lg:hidden fixed inset-0 z-40 bg-black/50" onClick={() => setIsMobileMenuOpen(false)} />}

      {/* Sidebar */}
      <div
        className={cn(
          'fixed inset-y-0 left-0 z-40 w-64 bg-background border-r transform transition-transform duration-200 ease-in-out lg:translate-x-0 lg:static lg:inset-0',
          isMobileMenuOpen ? 'translate-x-0' : '-translate-x-full',
          className,
        )}
      >
        <div className="flex h-full flex-col">
          {/* Header */}
          <div className="flex items-center gap-2 border-b px-6 py-4">
            <div className="flex items-center gap-2">
              <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center">
                <span className="text-primary-foreground font-bold text-sm">GG</span>
              </div>
              <div>
                <h1 className="font-semibold">Game Guild</h1>
                <p className="text-xs text-muted-foreground">Dashboard</p>
              </div>
            </div>
          </div>

          {/* Search */}
          <div className="p-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search..."
                className="w-full rounded-lg border bg-background pl-10 pr-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent"
              />
            </div>
          </div>

          {/* Navigation */}
          <div className="flex-1 overflow-auto px-4 pb-4">
            <NavigationList />
          </div>

          {/* Quick Actions */}
          <div className="border-t p-4">
            <div className="space-y-2">
              <Button size="sm" className="w-full justify-start" variant="outline">
                <Plus className="mr-2 h-4 w-4" />
                Add User
              </Button>
              <Button size="sm" className="w-full justify-start" variant="outline">
                <Plus className="mr-2 h-4 w-4" />
                Create Course
              </Button>
            </div>
          </div>

          {/* Notifications */}
          <div className="border-t p-4">
            <Button variant="ghost" size="sm" className="w-full justify-start text-muted-foreground">
              <Bell className="mr-2 h-4 w-4" />
              Notifications
              <Badge variant="destructive" className="ml-auto">
                3
              </Badge>
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
