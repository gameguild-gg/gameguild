import type { WorkspaceNavGroup } from '@/components/app/app-shell-sidebar';
import type { PublicNavEntry } from '@/components/app/public-website-nav';
import { Bookmark, Compass, FlaskConical, Home } from 'lucide-react';

/**
 * Social sidebar navigation in the shared `WorkspaceNavGroup[]` shape so the
 * community shell renders through the same `AppShellSidebar` as the workspace.
 * `match` preserves the social active-state rules (query-param tabs, prefixes).
 */
export const socialNavigationData: WorkspaceNavGroup[] = [
  {
    label: '',
    items: [
      {
        title: 'Home',
        url: '/',
        icon: Home,
        activeOnTab: '',
      },
      {
        title: 'Explore',
        url: '/projects',
        icon: Compass,
        activeOnPath: '/projects',
      },
      {
        title: 'Testing Lab',
        url: '/workspace/testing-lab',
        icon: FlaskConical,
        activeOnPath: '/workspace/testing-lab',
      },
      {
        title: 'Saved',
        url: '/?tab=saved',
        icon: Bookmark,
        activeOnTab: 'saved',
      },
    ],
  },
];

/** Desktop header navigation rendered in the social shell's `AppShellHeader`. */
export const socialDesktopNav = [
  { label: 'Community', href: '/community' },
  {
    label: 'Learn',
    items: [
      { label: 'Courses', href: '/courses' },
      { label: 'Programs', href: '/programs' },
    ],
  },
  {
    label: 'Build',
    items: [
      { label: 'Workspace', href: '/workspace' },
      { label: 'Projects', href: '/projects' },
    ],
  },
  {
    label: 'Test & Launch',
    items: [
      { label: 'Testing Lab', href: '/testing-lab' },
      { label: 'Launch Pad', href: '/launch-pad' },
    ],
  },
] as const satisfies readonly PublicNavEntry[];
