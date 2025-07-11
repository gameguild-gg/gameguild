'use client';

export default function TracksError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div className="flex flex-col items-center justify-center min-h-[400px] space-y-4">
      <h2 className="text-xl font-semibold">Something went wrong!</h2>
      <p className="text-muted-foreground">Failed to load tracks</p>
      <button onClick={reset} className="px-4 py-2 bg-primary text-primary-foreground rounded hover:bg-primary/90">
        Try again
      </button>
    </div>
  );
}
