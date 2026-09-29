import { Link } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import { ArrowRight, Compass, House } from 'lucide-react';

export default function NotFound(): React.JSX.Element {
  return (
    <main className="relative isolate flex min-h-[calc(100svh-4rem)] items-center overflow-hidden bg-background text-foreground">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_18%_40%,color-mix(in_oklch,var(--primary)_13%,transparent),transparent_42%),radial-gradient(ellipse_at_84%_68%,color-mix(in_oklch,var(--highlight)_11%,transparent),transparent_38%)]"
      />

      <div className="relative mx-auto grid w-full max-w-6xl items-center gap-12 px-6 py-16 sm:px-10 lg:grid-cols-[1fr_0.9fr] lg:gap-20 lg:py-24">
        <section aria-labelledby="not-found-title" className="max-w-xl">
          <p className="mb-6 inline-flex items-center gap-3 text-xs font-semibold uppercase tracking-[0.2em] text-muted-foreground">
            <span className="rounded border border-primary/30 bg-primary/10 px-2.5 py-1 font-mono text-primary">404</span>
            <span>Waypoint missing</span>
          </p>

          <h1 id="not-found-title" className="text-balance text-4xl font-bold leading-[1.08] tracking-tight sm:text-5xl lg:text-6xl">
            This page isn&apos;t on the map.
          </h1>
          <p className="mt-6 max-w-lg text-pretty text-base leading-7 text-muted-foreground sm:text-lg">
            The link may be outdated, or the page may have moved. Pick a destination and keep going.
          </p>

          <nav aria-label="Suggested destinations" className="mt-9 flex flex-col gap-3 sm:flex-row">
            <Button nativeButton={false} render={<Link href="/" />} className="gap-2">
              <House aria-hidden="true" />
              Go to home
              <ArrowRight aria-hidden="true" />
            </Button>
            <Button
              nativeButton={false}
              variant="outline"
              render={<Link href="/courses" />}
              className="gap-2"
            >
              Explore courses
            </Button>
          </nav>

          <p className="mt-8 text-sm text-muted-foreground">
            If you typed the address, check it for a typo.
          </p>
        </section>

        <div
          aria-hidden="true"
          className="relative mx-auto aspect-[1.12] w-full max-w-[30rem] overflow-hidden rounded-2xl border border-border bg-card shadow-2xl shadow-primary/5"
        >
          <div className="absolute inset-0 bg-[linear-gradient(to_right,color-mix(in_oklch,var(--border)_75%,transparent)_1px,transparent_1px),linear-gradient(to_bottom,color-mix(in_oklch,var(--border)_75%,transparent)_1px,transparent_1px)] bg-[size:36px_36px] opacity-60" />
          <div className="absolute left-5 top-5 flex items-center gap-2 rounded-full border border-border bg-background/85 px-3 py-1.5 text-[0.65rem] font-medium uppercase tracking-[0.16em] text-muted-foreground backdrop-blur">
            <Compass className="size-3.5 text-primary" />
            <span>Guild map</span>
          </div>

          <svg viewBox="0 0 480 420" className="absolute inset-0 h-full w-full" fill="none">
            <path
              d="M66 324H151L205 270H277L326 220L326 151"
              stroke="var(--primary)"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="3"
            />
            <path
              d="M326 151L377 104"
              stroke="var(--highlight)"
              strokeDasharray="7 10"
              strokeLinecap="round"
              strokeWidth="3"
            />
            <circle cx="66" cy="324" r="8" fill="var(--success)" />
            <circle cx="151" cy="324" r="5" fill="var(--primary)" />
            <circle cx="205" cy="270" r="5" fill="var(--primary)" />
            <circle cx="277" cy="270" r="5" fill="var(--primary)" />
            <circle cx="326" cy="220" r="5" fill="var(--primary)" />
            <circle cx="326" cy="151" r="25" fill="color-mix(in_oklch,var(--highlight)_14%,transparent)" />
            <circle cx="326" cy="151" r="10" fill="var(--highlight)" />
            <circle cx="377" cy="104" r="21" stroke="var(--highlight)" strokeDasharray="3 5" strokeWidth="2" />
            <path d="M370 97L384 111M384 97L370 111" stroke="var(--highlight)" strokeLinecap="round" strokeWidth="2" />
          </svg>

          <span className="absolute bottom-[16%] left-[8%] rounded-md border border-success/25 bg-background/90 px-2.5 py-1 text-xs font-medium text-success shadow-sm">
            Home
          </span>
          <span className="absolute left-[61%] top-[32%] rounded-md border border-highlight/25 bg-background/90 px-2.5 py-1 text-xs font-medium text-highlight shadow-sm">
            You are here
          </span>
          <span className="absolute right-[8%] top-[15%] rounded-md border border-border bg-background/90 px-2.5 py-1 font-mono text-xs text-muted-foreground shadow-sm">
            404
          </span>
        </div>
      </div>
    </main>
  );
}
