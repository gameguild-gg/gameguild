import { Link } from "@/i18n/navigation";
import { cn } from "@game-guild/ui/lib/utils";

// Tab labels are localized via the `feedTabs` message namespace (issue #1381):
// the default tab is honestly named "Recent" — the feed is timestamp-ordered
// (FeedScope.ForYou), not interest-ranked, so "For you" would overpromise
// (#1234 owner decision; reintroduce a personalized label only with a real
// recommendation MVP). This component is deliberately synchronous and
// presentational: the async server SocialFeed resolves the translations and
// passes them in, so nested-promise elements never reach a render tree.
export const SOCIAL_FEED_TABS = [
  { id: "foryou", labelKey: "recent" },
  { id: "following", labelKey: "following" },
  { id: "community", labelKey: "community" },
  { id: "saved", labelKey: "saved" },
] as const;

export type SocialFeedTab = (typeof SOCIAL_FEED_TABS)[number]["id"];

export type SocialFeedTabLabels = {
  ariaLabel: string;
  tab: Record<SocialFeedTab, string>;
};

export function isSocialFeedTab(
  value: string | undefined,
): value is SocialFeedTab {
  return SOCIAL_FEED_TABS.some((tab) => tab.id === value);
}

export function SocialFeedTabs({
  active,
  labels,
}: {
  active: SocialFeedTab;
  labels: SocialFeedTabLabels;
}): React.JSX.Element {
  return (
    <nav
      aria-label={labels.ariaLabel}
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
            {labels.tab[tab.id]}
            {isActive ? (
              <span className="absolute inset-x-0 bottom-0 h-0.5 rounded-full bg-primary" />
            ) : null}
          </Link>
        );
      })}
    </nav>
  );
}
