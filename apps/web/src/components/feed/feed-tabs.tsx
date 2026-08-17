import { cn } from '@game-guild/ui/lib/utils';
import { Link } from '@/i18n/navigation';

export const FEED_TABS = [
  { id: 'foryou', label: 'For You' },
  { id: 'following', label: 'Following' },
  { id: 'discover', label: 'Discover' },
  { id: 'trending', label: 'Trending' },
] as const;

export type FeedTab = (typeof FEED_TABS)[number]['id'];

export function isFeedTab(value: string | undefined): value is FeedTab {
  return FEED_TABS.some((tab) => tab.id === value);
}

export function FeedTabs({ active }: { active: FeedTab }): React.JSX.Element {
  return (
    <nav
      aria-label="Feed tabs"
      className="sticky top-16 z-20 -mx-4 mb-5 flex gap-2 overflow-x-auto border-b bg-background/95 px-4 py-2.5 backdrop-blur"
    >
      {FEED_TABS.map((tab) => {
        const isActive = tab.id === active;
        return (
          <Link
            key={tab.id}
            href={tab.id === 'foryou' ? '/' : `/?tab=${tab.id}`}
            aria-current={isActive ? 'page' : undefined}
            className={cn(
              'whitespace-nowrap rounded-full px-4 py-1.5 text-sm font-semibold transition-colors',
              isActive
                ? 'bg-primary text-primary-foreground'
                : 'text-muted-foreground hover:bg-muted hover:text-foreground',
            )}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
