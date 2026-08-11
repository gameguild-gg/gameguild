import { Link } from '@/i18n/navigation';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@game-guild/ui/components/tooltip';
import { BarChart3, CalendarDays, FolderKanban, Settings, Users } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

const operations = [
  {
    href: '/dashboard/testing-lab/events',
    label: 'Events',
    Icon: CalendarDays,
  },
  {
    href: '/dashboard/testing-lab/projects',
    label: 'Projects',
    Icon: FolderKanban,
  },
  {
    href: '/dashboard/testing-lab/participants',
    label: 'Participants',
    Icon: Users,
  },
  {
    href: '/dashboard/testing-lab/analytics',
    label: 'Analytics',
    Icon: BarChart3,
  },
  {
    href: '/dashboard/testing-lab/settings/general',
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

export function TestingLabPageHeader({
  icon: Icon,
  title,
  description,
  actions,
  navigation,
  headingLevel = 1,
}: {
  icon: LucideIcon;
  title: string;
  description: string;
  actions?: ReactNode;
  navigation?: ReactNode;
  headingLevel?: 1 | 2;
}) {
  const Heading = headingLevel === 2 ? 'h2' : 'h1';

  return (
    <header className="space-y-4 border-b pb-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex min-w-0 items-start gap-3">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-md border bg-muted/40">
            <Icon className="size-5" aria-hidden="true" />
          </div>
          <div className="min-w-0">
            <Heading className="text-2xl font-semibold">{title}</Heading>
            <p className="mt-1 max-w-3xl text-sm text-muted-foreground">{description}</p>
          </div>
        </div>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </div>
      {navigation ? <div className="border-t pt-3">{navigation}</div> : null}
    </header>
  );
}
