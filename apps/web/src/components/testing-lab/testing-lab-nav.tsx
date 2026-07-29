'use client';

import { Link, usePathname } from '@/i18n/navigation';
import { cn } from '@game-guild/ui/lib/utils';
import { BarChart3, CalendarDays, ClipboardList, FlaskConical, MapPin, MessageSquareText, Settings, ShieldCheck, Users } from 'lucide-react';

const items = [
  { label: 'Overview', href: '/dashboard/testing-lab', icon: FlaskConical, exact: true },
  { label: 'Events', href: '/dashboard/testing-lab/events', icon: CalendarDays },
  { label: 'Requests', href: '/dashboard/testing-lab/requests', icon: ClipboardList },
  { label: 'Sessions', href: '/dashboard/testing-lab/sessions', icon: FlaskConical },
  { label: 'People', href: '/dashboard/testing-lab/people', icon: Users },
  { label: 'Feedback', href: '/dashboard/testing-lab/feedback', icon: MessageSquareText },
  { label: 'Reports', href: '/dashboard/testing-lab/reports', icon: BarChart3 },
  { label: 'Locations', href: '/dashboard/testing-lab/locations', icon: MapPin },
  { label: 'Settings', href: '/dashboard/testing-lab/settings', icon: Settings },
  { label: 'Access', href: '/dashboard/testing-lab/access', icon: ShieldCheck },
];

export function TestingLabNav() {
  const pathname = usePathname();

  return (
    <nav aria-label="Testing Lab sections" className="overflow-x-auto border-b">
      <div className="flex min-w-max gap-1 px-4 lg:px-6">
        {items.map((item) => {
          const active = item.exact ? pathname === item.href : pathname.startsWith(item.href);
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              aria-current={active ? 'page' : undefined}
              className={cn(
                'flex h-11 items-center gap-2 border-b-2 px-3 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground',
                active ? 'border-primary text-foreground' : 'border-transparent',
              )}
            >
              <Icon className="size-4" />
              {item.label}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
