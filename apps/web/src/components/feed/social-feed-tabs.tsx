import { Link } from "@/i18n/navigation";
import { getTranslations } from "next-intl/server";
import { cn } from "@game-guild/ui/lib/utils";

// Tab labels are localized via the `feedTabs` message namespace (issue #1381):
// the default tab is honestly named "Recent" — the feed is timestamp-ordered
// (FeedScope.ForYou), not interest-ranked, so "For you" would overpromise
// (#1234 owner decision; reintroduce a personalized label only with a real
// recommendation MVP).
export const SOCIAL_FEED_TABS = [
  { id: "foryou", labelKey: "recent" },
  { id: "following", labelKey: "following" },
  { id: "community", labelKey: "community" },
  { id: "saved", labelKey: "saved" },
] as const;

export type SocialFeedTab = (typeof SOCIAL_FEED_TABS)[number]["id"];

export function isSocialFeedTab(
  value: string | undefined,
): value is SocialFeedTab {
  return SOCIAL_FEED_TABS.some((tab) => tab.id === value);
}

export async function SocialFeedTabs({
  active,
}: {
  active: SocialFeedTab;
}): Promise<React.JSX.Element> {
  const t = await getTranslations("feedTabs");
  return (
    <nav
      aria-label={t("ariaLabel")}
      className="sticky top-0 z-20 flex h-14 items-end gap-8 bg-background/95 px-4 backdrop-blur-xl sm:px-6"
    >
      {SOCIAL_FEED_TABS.map((tab) => {
        const isActive = tab.id === active;
        return (
          <Link
            key={tab.id}
            href={tab.id === "foryou" ? "/feed" : `/feed?tab=${tab.id}`}
            aria-current={isActive ? "page" : undefined}
            className={cn(
              "relative flex h-full items-center text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              isActive ? "text-foreground" : "text-muted-foreground hover:text-foreground",
            )}
          >
            {t(tab.labelKey)}
            {isActive ? (
              <span className="absolute inset-x-0 bottom-0 h-0.5 rounded-full bg-primary" />
            ) : null}
          </Link>
        );
      })}
    </nav>
  );
}
