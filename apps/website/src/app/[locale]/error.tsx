'use client';

export default function Error({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}): React.JSX.Element {
    return (
        <div className="flex min-h-screen flex-col items-center justify-center">
            <h2 className="text-2xl font-bold">Something went wrong!</h2>
            <p className="mt-2 text-muted-foreground">{error.message}</p>
            <button
                className="mt-4 rounded-md bg-primary px-4 py-2 text-primary-foreground hover:bg-primary/90"
                onClick={() => reset()}
            >
                Try again
            </button>
        </div>
    );
}
