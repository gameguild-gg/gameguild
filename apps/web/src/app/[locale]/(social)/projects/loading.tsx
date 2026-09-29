import { Skeleton } from '@game-guild/ui/components/skeleton';

export default function ProjectsLoading() {
  return (
    <main aria-label="Loading projects" aria-busy="true" className="mx-auto flex w-full max-w-[1560px] flex-col gap-8 px-4 py-7 sm:px-6 lg:px-8">
      <div className="space-y-3">
        <Skeleton className="h-4 w-36" />
        <Skeleton className="h-10 w-56 max-w-full" />
        <Skeleton className="h-5 w-full max-w-2xl" />
      </div>
      <Skeleton className="h-64 w-full rounded-xl sm:h-80 lg:h-96" />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }, (_, index) => (
          <div key={index} className="overflow-hidden rounded-xl border border-border">
            <Skeleton className="aspect-[16/10] w-full rounded-none" />
            <div className="space-y-3 p-4">
              <Skeleton className="h-5 w-2/3" />
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-10 w-full" />
            </div>
          </div>
        ))}
      </div>
    </main>
  );
}
