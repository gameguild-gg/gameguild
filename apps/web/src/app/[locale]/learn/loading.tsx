import { Skeleton } from '@game-guild/ui/components/skeleton';

export default function LearningLoading() {
  return (
    <div aria-label="Loading learning workspace" className="space-y-6">
      <Skeleton className="h-5 w-32" />
      <Skeleton className="h-10 w-full max-w-xl" />
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <Skeleton className="h-52" />
        <Skeleton className="h-52" />
        <Skeleton className="h-52" />
      </div>
    </div>
  );
}
