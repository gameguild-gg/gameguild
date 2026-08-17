"use client";

import { Link, usePathname } from "@/i18n/navigation";
import { cn } from "@game-guild/ui/lib/utils";
import { MapPin, Settings, ShieldCheck } from "lucide-react";

const sections = (basePath: string) => [
  {
    label: "General",
    href: `${basePath}/settings/general`,
    icon: Settings,
  },
  {
    label: "Locations",
    href: `${basePath}/settings/locations`,
    icon: MapPin,
  },
  {
    label: "Access",
    href: `${basePath}/settings/access`,
    icon: ShieldCheck,
  },
] as const;

export function TestingLabSettingsNav({
  basePath = "/dashboard/testing-lab",
}: {
  basePath?: string;
}) {
  const pathname = usePathname() ?? "";

  return (
    <nav
      aria-label="Testing Lab settings"
      className="grid grid-cols-3 gap-1 lg:sticky lg:top-20 lg:grid-cols-1"
    >
      {sections(basePath).map((section) => {
        const Icon = section.icon;
        const current =
          pathname === section.href || pathname.startsWith(section.href + "/");

        return (
          <Link
            key={section.href}
            href={section.href}
            aria-current={current ? "page" : undefined}
            className={cn(
              "flex h-10 min-w-0 items-center gap-2 rounded-sm px-3 text-sm font-medium transition-colors",
              current
                ? "bg-muted text-foreground"
                : "text-muted-foreground hover:bg-muted/60 hover:text-foreground",
            )}
          >
            <Icon aria-hidden="true" className="size-4 shrink-0" />
            <span className="truncate">{section.label}</span>
          </Link>
        );
      })}
    </nav>
  );
}
