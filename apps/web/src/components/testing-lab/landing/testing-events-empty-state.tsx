import { Button } from '@/components/ui/button';
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
      <div className="mx-auto max-w-2xl rounded-2xl border border-slate-600/40 bg-slate-900/50 p-12 text-center">
        <Search className="mx-auto size-8 text-blue-400" aria-hidden="true" />
        <h2 className="mt-5 text-2xl font-semibold text-white">No events match your filters</h2>
        <p className="mt-3 text-slate-400">Adjust your search or filters to see other testing events.</p>
        <Button type="button" variant="outline" className="mt-6" onClick={clearFilters}>
          Clear filters
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl rounded-lg border border-slate-700 bg-gradient-to-br from-slate-900/60 to-slate-800/50 p-12 text-center">
      <h2 className="text-2xl font-semibold text-white">No events available</h2>
      <p className="mt-3 text-slate-400">
        You can prepare a Project now, then apply as soon as project applications open.
      </p>
      <div className="mt-6 flex flex-wrap justify-center gap-2">
        <Button asChild>
          <Link href="/workspace/projects">Prepare a project</Link>
        </Button>
        <Button asChild variant="outline">
          <Link href="/testing-lab">Back to Testing Lab</Link>
        </Button>
      </div>
    </div>
  );
}
