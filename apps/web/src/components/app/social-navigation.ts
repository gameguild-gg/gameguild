import type { WorkspaceNavGroup } from '@/components/app/app-shell-sidebar';
import type { PublicNavEntry } from '@/components/app/public-website-nav';
import { Bookmark, Compass, FlaskConical, FolderKanban, Home, Rocket } from 'lucide-react';

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
        url: '/feed',
        icon: Home,
        activeOnTab: '',
      },
      {
        title: 'Explore',
        url: '/community',
        icon: Compass,
        activeOnPath: '/community',
      },
      {
        title: 'Projects',
        url: '/projects',
        icon: FolderKanban,
        activeOnPath: '/projects',
      },
      {
        title: 'Testing Lab',
        url: '/testing-lab',
        icon: FlaskConical,
        activeOnPath: '/testing-lab',
      },
      {
        title: 'Launch Pad',
        url: '/launch-pad',
        icon: Rocket,
        activeOnPath: '/launch-pad',
      },
      {
        title: 'Saved',
        url: '/feed?tab=saved',
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
] as const satisfies readonly PublicNavEntry[];
