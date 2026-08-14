import { Link } from '@/i18n/navigation';
import { getPublicLaunchPadEvents } from '@/lib/launch-pad/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CalendarDays, Rocket } from 'lucide-react';

export default async function LaunchPadEventsPage() {
  const events = await getPublicLaunchPadEvents();
  return (
    <main className="min-h-screen bg-slate-950 px-4 py-14 text-white">
      <div className="mx-auto max-w-7xl space-y-8">
        <header className="flex flex-wrap items-end justify-between gap-4">
          <div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-200">Community events</p><h1 className="mt-2 text-4xl font-semibold">Launch Pad events</h1><p className="mt-3 text-slate-300">Apply with a Team Project or register individually for available roles.</p></div>
          <Button asChild variant="outline"><Link href="/launch-pad/participation">Your participation</Link></Button>
        </header>
        <section className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {events.length === 0 ? <p className="text-slate-400">No public Launch Pad event is available.</p> : events.map((event) => (
            <Card key={event.id} className="border-white/10 bg-white/[0.04] text-white">
              <CardHeader><div className="flex justify-between gap-3"><CardTitle className="flex items-center gap-2"><Rocket className="size-5" /> {event.name}</CardTitle><Badge variant="outline">{event.status}</Badge></div></CardHeader>
              <CardContent className="space-y-4"><p className="text-sm text-slate-400">{event.description || 'Community launch event'}</p><p className="flex items-center gap-2 text-sm"><CalendarDays className="size-4" />{new Date(event.startsAt).toLocaleString()}</p><Button asChild><Link href={`/launch-pad/events/${event.id}`}>View event</Link></Button></CardContent>
            </Card>
          ))}
        </section>
      </div>
    </main>
  );
}
