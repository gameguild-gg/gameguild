'use client';

import { Link } from '@/i18n/navigation';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@game-guild/ui/components/tooltip';
import { BarChart3, CalendarDays, FolderKanban, Settings, Users } from 'lucide-react';

const operations = [
  {
    href: '/console/community/testing-lab/events',
    label: 'Events',
    Icon: CalendarDays,
  },
  {
    href: '/console/community/testing-lab/projects',
    label: 'Projects',
    Icon: FolderKanban,
  },
  {
    href: '/console/community/testing-lab/participants',
    label: 'Participants',
    Icon: Users,
  },
  {
    href: '/console/community/testing-lab/analytics',
    label: 'Analytics',
    Icon: BarChart3,
  },
  {
    href: '/console/community/testing-lab/settings/general',
    label: 'Settings',
    Icon: Settings,
  },
] as const;

export function TestingLabOperationsNavigation({ activeHref }: { activeHref?: string }) {
  return (
    <TooltipProvider delayDuration={250}>
      <nav aria-label="Testing Lab operations" className="flex flex-wrap items-center gap-1">
        {operations.map(({ href, label, Icon }) => {
          const active = activeHref === href;
          return (
            <Tooltip key={href}>
              <TooltipTrigger asChild>
                <Link
                  href={href}
                  aria-label={`${label} workspace`}
                  aria-current={active ? 'page' : undefined}
                  className={`inline-flex size-9 items-center justify-center rounded-md border transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
                    active
                      ? 'border-foreground/20 bg-muted text-foreground'
                      : 'border-transparent text-muted-foreground hover:border-border hover:bg-muted/50 hover:text-foreground'
                  }`}
                >
                  <Icon className="size-4" aria-hidden="true" />
                </Link>
              </TooltipTrigger>
              <TooltipContent side="bottom">{label}</TooltipContent>
            </Tooltip>
          );
        })}
      </nav>
    </TooltipProvider>
  );
}
