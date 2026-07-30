'use client';

import { createLearnerRoutes } from '@/lib/learner/routes';
import { BookOpen, ChartNoAxesColumn, ClipboardList, LayoutDashboard, MessagesSquare } from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

export function CourseLearnerNav({ slug }: { slug: string }) {
  const pathname = usePathname();
  const routes = createLearnerRoutes();
  const items = [
    { href: routes.course(slug), label: 'Overview', icon: LayoutDashboard, exact: true },
    { href: routes.content(slug), label: 'Content', icon: BookOpen },
    { href: routes.activities(slug), label: 'Activities', icon: ClipboardList },
    { href: `${routes.course(slug)}/grades`, label: 'Grades', icon: ChartNoAxesColumn },
    { href: routes.community(slug), label: 'Community', icon: MessagesSquare },
  ];

  return (
    <nav
      aria-label="Course navigation"
      className="mb-8 flex min-w-0 gap-1 overflow-x-auto border-b"
    >
      {items.map(({ exact, href, icon: Icon, label }) => {
        const active = exact ? pathname === href : pathname === href || pathname.startsWith(`${href}/`);
        return (
          <Link
            key={href}
            href={href}
            aria-current={active ? 'page' : undefined}
            className={`flex h-11 shrink-0 items-center gap-2 border-b-2 px-3 text-sm ${
              active
                ? 'border-primary text-foreground'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <Icon className="size-4" />
            {label}
          </Link>
        );
      })}
    </nav>
  );
}
