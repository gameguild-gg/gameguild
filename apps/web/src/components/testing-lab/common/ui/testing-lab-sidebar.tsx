'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import { TestTube, FileText, MessageSquare, Calendar, BarChart3, Settings, Upload, Download } from 'lucide-react';

const sidebarItems = [
  {
    title: 'Overview',
    href: '/dashboard/testing-lab',
    icon: BarChart3,
    description: 'Testing lab dashboard and statistics',
  },
  {
    title: 'My Requests',
    href: '/dashboard/testing-lab/requests',
    icon: FileText,
    description: 'Manage your testing requests',
  },
  {
    title: 'Testing Sessions',
    href: '#',
    icon: Calendar,
    description: 'View and join testing sessions',
    isGitHubIssue: true,
    route: '/dashboard/testing-lab/sessions',
  },
  {
    title: 'Feedback',
    href: '/dashboard/testing-lab/feedback',
    icon: MessageSquare,
    description: 'Submit and review feedback',
  },
  {
    title: 'Submit Project',
    href: '/dashboard/testing-lab/submit',
    icon: Upload,
    description: 'Submit your project for testing',
  },
  {
    title: 'Downloads',
    href: '/dashboard/testing-lab/downloads',
    icon: Download,
    description: 'Download testing materials',
  },
];

export function TestingLabSidebar() {
  const pathname = usePathname();

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="p-6 border-b border-border">
        <div className="flex items-center gap-2">
          <TestTube className="h-6 w-6 text-primary" />
          <h2 className="text-xl font-semibold">Testing Lab</h2>
        </div>
        <p className="text-sm text-muted-foreground mt-1">Game testing and feedback platform</p>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4">
        <ul className="space-y-2">
          {sidebarItems.map((item) => {
            const isActive = pathname === item.href;
            const Icon = item.icon;

            return (
              <li key={item.href}>
                {(item as any).isGitHubIssue ? (
                  <a
                    href={item.href}
                    data-github-issue="true"
                    data-route={(item as any).route}
                    className={cn('flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors hover:bg-accent hover:text-accent-foreground', isActive ? 'bg-accent text-accent-foreground font-medium' : 'text-muted-foreground')}
                  >
                    <Icon className="h-4 w-4" />
                    <div className="flex-1">
                      <div>{item.title}</div>
                      <div className="text-xs text-muted-foreground line-clamp-1">{item.description}</div>
                    </div>
                  </a>
                ) : (
                  <Link
                    href={item.href}
                    className={cn('flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors hover:bg-accent hover:text-accent-foreground', isActive ? 'bg-accent text-accent-foreground font-medium' : 'text-muted-foreground')}
                  >
                    <Icon className="h-4 w-4" />
                    <div className="flex-1">
                      <div>{item.title}</div>
                      <div className="text-xs text-muted-foreground line-clamp-1">{item.description}</div>
                    </div>
                  </Link>
                )}
              </li>
            );
          })}
        </ul>
      </nav>

      {/* Footer */}
      <div className="p-4 border-t border-border">
        <Link
          href="/dashboard/testing-lab/settings"
          className={cn(
            'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors hover:bg-accent hover:text-accent-foreground',
            pathname === '/dashboard/testing-lab/settings' ? 'bg-accent text-accent-foreground font-medium' : 'text-muted-foreground',
          )}
        >
          <Settings className="h-4 w-4" />
          <span>Settings</span>
        </Link>
      </div>
    </div>
  );
}
