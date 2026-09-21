import type { WorkspaceNavGroup, WorkspaceNavSubItem } from '@/components/app/app-shell-sidebar';
import {
  BarChart3,
  BookOpen,
  CalendarDays,
  ClipboardList,
  CircleDollarSign,
  FileText,
  FlaskConical,
  FolderOpen,
  HeadphonesIcon,
  Home,
  LayoutDashboard,
  List,
  FolderKanban,
  Rocket,
  Settings,
  ShieldCheck,
  UserCog,
  Users,
} from 'lucide-react';

// Game Guild Dashboard navigation structure
// Routes map to: /[locale]/(dashboard)/dashboard/...
export const workspaceNavigationData: WorkspaceNavGroup[] = [
  {
    label: 'Workspace',
    items: [
      {
        title: 'Home',
        url: '/workspace',
        icon: Home,
      },
      {
        title: 'Projects',
        url: '/workspace/projects',
        icon: FolderKanban,
      },
      {
        title: 'Teams',
        url: '/workspace/teams',
        icon: Users,
      },
      {
        title: 'Learning',
        icon: BookOpen,
        subGroups: [
          { title: 'Overview', url: '/workspace/learning', icon: LayoutDashboard, items: [] },
          { title: 'Courses', url: '/workspace/learning/courses', icon: BookOpen, items: [] },
          { title: 'Tutorials', url: '/workspace/learning/tutorials', icon: FileText, items: [] },
          { title: 'Resources', url: '/workspace/learning/resources', icon: FolderOpen, items: [] },
        ],
      },
    ],
  },
  {
    label: 'Community Management',
    items: [
      {
        title: 'Calendar',
        url: '/workspace/calendar',
        icon: CalendarDays,
        requiredCapabilities: ['Community.Manage'],
      },
      {
        title: 'Members',
        url: '/console/community/members/users',
        icon: UserCog,
        requiredCapabilities: ['Community.ManageMembers'],
      },
      {
        title: 'Groups',
        url: '/console/community/members/groups',
        icon: Users,
        requiredCapabilities: ['Community.ManageMembers'],
      },
      {
        title: 'Support',
        url: '/console/community/members/support',
        icon: HeadphonesIcon,
        requiredCapabilities: ['Community.ManageSupport'],
      },
      {
        title: 'Teams',
        url: '/console/community/teams',
        icon: Users,
        requiredCapabilities: ['Community.ManageTeams'],
      },
      {
        title: 'Projects',
        url: '/console/community/projects',
        icon: FolderKanban,
        requiredCapabilities: ['Community.ManageProjects'],
      },
      {
        title: 'Testing Lab',
        icon: FlaskConical,
        requiredCapabilities: [
          'TestingLab.ManageEvents',
          'TestingLab.ReviewApplications',
          'TestingLab.ManageParticipants',
          'TestingLab.ManageFeedback',
          'TestingLab.ViewAnalytics',
          'TestingLab.ManageSettings',
        ],
        subGroups: [
          {
            title: 'Sessions',
            url: '/workspace/testing-lab/events',
            icon: List,
            items: [],
            requiredCapabilities: ['TestingLab.ManageEvents'],
          },
          {
            title: 'Settings',
            url: '/workspace/testing-lab/settings',
            icon: Settings,
            items: [],
            requiredCapabilities: [
              'TestingLab.ManageSettings',
              'TestingLab.ViewAnalytics',
            ],
          },
        ],
      },
      {
        title: 'Launch Pad',
        url: '/console/community/launch-pad',
        icon: Rocket,
        requiredCapabilities: [
          'LaunchPad.ManageEvents',
          'LaunchPad.ReviewApplications',
          'LaunchPad.ManageParticipants',
          'LaunchPad.ViewAnalytics',
          'LaunchPad.ManageSettings',
        ],
        subGroups: [
          {
            title: 'Overview',
            url: '/console/community/launch-pad',
            icon: LayoutDashboard,
            items: [],
            requiredCapabilities: [
              'LaunchPad.ManageEvents',
              'LaunchPad.ReviewApplications',
              'LaunchPad.ManageParticipants',
              'LaunchPad.ViewAnalytics',
              'LaunchPad.ManageSettings',
            ],
          },
          {
            title: 'Events',
            url: '/console/community/launch-pad/events',
            icon: Rocket,
            items: [],
            requiredCapabilities: ['LaunchPad.ManageEvents'],
          },
          {
            title: 'Applications',
            url: '/console/community/launch-pad/applications',
            icon: ClipboardList,
            items: [],
            requiredCapabilities: ['LaunchPad.ReviewApplications'],
          },
          {
            title: 'Participants',
            url: '/console/community/launch-pad/participants',
            icon: Users,
            items: [],
            requiredCapabilities: ['LaunchPad.ManageParticipants'],
          },
          {
            title: 'Analytics',
            url: '/console/community/launch-pad/analytics',
            icon: BarChart3,
            items: [],
            requiredCapabilities: ['LaunchPad.ViewAnalytics'],
          },
          {
            title: 'Settings',
            url: '/console/community/launch-pad/settings',
            icon: Settings,
            items: [],
            requiredCapabilities: ['LaunchPad.ManageSettings'],
          },
        ],
      },
      {
        title: 'Learning',
        icon: BookOpen,
        requiredCapabilities: ['Learning.Manage'],
        subGroups: [
          {
            title: 'Overview',
            url: '/console/learning',
            icon: LayoutDashboard,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
          {
            title: 'Courses',
            url: '/console/learning/courses',
            icon: BookOpen,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
          {
            title: 'Tutorials',
            url: '/console/learning/tutorials',
            icon: FileText,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
          {
            title: 'Resources',
            url: '/console/learning/resources',
            icon: FolderOpen,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
        ],
      },
    ],
  },
  {
    label: 'Platform Management',
    items: [
      {
        title: 'Economy',
        icon: CircleDollarSign,
        requiredCapabilities: ['Economy.ManagePayouts'],
        subGroups: [
          {
            title: 'Payout review',
            url: '/console/economy/payout-reviews',
            icon: ClipboardList,
            items: [],
            requiredCapabilities: ['Economy.ManagePayouts'],
          },
        ],
      },
      {
        title: 'Roles',
        url: '/console/platform/roles',
        icon: ShieldCheck,
        requiredCapabilities: ['Platform.ManageRoles'],
      },
    ],
  },
];

function hasAnyCapability(
  requiredCapabilities: readonly string[] | undefined,
  capabilities: ReadonlySet<string>,
): boolean {
  return (
    !requiredCapabilities?.length ||
    requiredCapabilities.some((capability) => capabilities.has(capability))
  );
}

export function filterWorkspaceNavigation(
  groups: WorkspaceNavGroup[],
  actorCapabilities: readonly string[],
): WorkspaceNavGroup[] {
  const capabilities = new Set(actorCapabilities);

  return groups.flatMap((group) => {
    const items = group.items.flatMap((item) => {
      if (!hasAnyCapability(item.requiredCapabilities, capabilities)) return [];

      const nestedItems = item.items?.filter((nested) =>
        hasAnyCapability(nested.requiredCapabilities, capabilities),
      );
      const subGroups = item.subGroups?.filter((nested) =>
        hasAnyCapability(nested.requiredCapabilities, capabilities),
      );

      if (item.items?.length && !nestedItems?.length) return [];
      if (item.subGroups?.length && !subGroups?.length) return [];

      return [{ ...item, items: nestedItems, subGroups }];
    });

    return items.length ? [{ ...group, items }] : [];
  });
}

export function flattenWorkspaceNavigationItems(groups: WorkspaceNavGroup[] = workspaceNavigationData): WorkspaceNavSubItem[] {
  const items: WorkspaceNavSubItem[] = [];

  for (const group of groups) {
    for (const item of group.items) {
      if (item.url && item.icon) {
        items.push({ title: item.title, url: item.url, icon: item.icon });
      }

      if (item.items?.length) {
        items.push(...item.items);
      }

      if (item.subGroups?.length) {
        for (const subGroup of item.subGroups) {
          if (subGroup.url && subGroup.icon) {
            items.push({ title: subGroup.title, url: subGroup.url, icon: subGroup.icon });
          }

          items.push(...subGroup.items);
        }
      }
    }
  }

  return items;
}
