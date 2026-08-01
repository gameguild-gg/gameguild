import { Link } from "@/i18n/navigation";
import { MapPin, Settings, ShieldCheck } from "lucide-react";
import type { ReactNode } from "react";

const sections = [
  {
    label: "General",
    href: "/dashboard/testing-lab/settings/general",
    icon: Settings,
  },
  {
    label: "Locations",
    href: "/dashboard/testing-lab/settings/locations",
    icon: MapPin,
  },
  {
    label: "Access",
    href: "/dashboard/testing-lab/settings/access",
    icon: ShieldCheck,
  },
];

export default function TestingLabSettingsLayout({
  children,
}: {
  children: ReactNode;
}) {
  return (
    <div className="grid min-w-0 lg:grid-cols-[13rem_minmax(0,1fr)]">
      <aside className="border-b p-4 lg:min-h-[calc(100dvh-4rem)] lg:border-b-0 lg:border-r lg:p-5">
        <nav
          aria-label="Testing Lab settings"
          className="grid grid-cols-3 gap-1 lg:sticky lg:top-20 lg:grid-cols-1"
        >
          {sections.map((section) => {
            const Icon = section.icon;
            return (
              <Link
                key={section.href}
                href={section.href}
                className="flex h-10 min-w-0 items-center gap-2 rounded-sm px-3 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
              >
                <Icon aria-hidden="true" className="size-4 shrink-0" />
                <span className="truncate">{section.label}</span>
              </Link>
            );
          })}
        </nav>
      </aside>
      <div className="min-w-0">{children}</div>
    </div>
  );
}
