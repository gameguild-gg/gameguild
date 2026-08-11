"use client";

import { Link, usePathname } from "@/i18n/navigation";
import {
  BookOpen,
  ChartNoAxesColumn,
  ClipboardList,
  LayoutDashboard,
  MessagesSquare,
} from "lucide-react";

export function CourseLearnerNav({ slug }: { slug: string }) {
  const pathname = usePathname();
  const courseHref = `/learn/courses/${slug}`;
  const items = [
    {
      href: courseHref,
      label: "Overview",
      icon: LayoutDashboard,
      exact: true,
    },
    { href: `${courseHref}/content`, label: "Content", icon: BookOpen },
    {
      href: `${courseHref}/activities`,
      label: "Activities",
      icon: ClipboardList,
    },
    {
      href: `${courseHref}/grades`,
      label: "Grades",
      icon: ChartNoAxesColumn,
    },
    {
      href: `${courseHref}/community`,
      label: "Community",
      icon: MessagesSquare,
    },
  ];

  return (
    <nav
      aria-label="Course navigation"
      className="mb-8 flex min-w-0 gap-1 overflow-x-auto border-b"
    >
      {items.map(({ exact, href, icon: Icon, label }) => {
        const active = exact
          ? pathname === href
          : pathname === href || pathname.startsWith(`${href}/`);
        return (
          <Link
            key={href}
            href={href}
            aria-current={active ? "page" : undefined}
            className={`flex h-11 shrink-0 items-center gap-2 border-b-2 px-3 text-sm ${
              active
                ? "border-primary text-foreground"
                : "border-transparent text-muted-foreground hover:text-foreground"
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
