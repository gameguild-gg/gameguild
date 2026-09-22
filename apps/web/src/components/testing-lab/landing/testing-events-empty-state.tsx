import { Button, buttonVariants } from '@game-guild/ui/components/button';
import { Link } from '@/i18n/navigation';
import { Search } from 'lucide-react';

interface TestingEventsEmptyStateProps {
  filtered: boolean;
  hasEvents: boolean;
  clearFilters: () => void;
}

export function TestingEventsEmptyState({ filtered, hasEvents, clearFilters }: TestingEventsEmptyStateProps) {
  if (filtered && hasEvents) {
    return (
      <div className="mx-auto max-w-2xl rounded-2xl border border-border bg-card p-12 text-center">
        <Search className="mx-auto size-8 text-muted-foreground" aria-hidden="true" />
        <h2 className="mt-5 text-2xl font-semibold">No events match your filters</h2>
        <p className="mt-3 text-muted-foreground">Adjust your search or filters to see other testing events.</p>
        <Button type="button" variant="outline" className="mt-6" onClick={clearFilters}>
          Clear filters
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl rounded-lg border border-border bg-card p-12 text-center">
      <h2 className="text-2xl font-semibold">No events available</h2>
      <p className="mt-3 text-muted-foreground">
        You can prepare a Project now, then apply as soon as project applications open.
      </p>
      <div className="mt-6 flex flex-wrap justify-center gap-2">
        <Link href="/workspace/projects" className={buttonVariants()}>Prepare a project</Link>
        <Link href="/testing-lab" className={buttonVariants({ variant: 'outline' })}>Back to Testing Lab</Link>
      </div>
    </div>
  );
}
