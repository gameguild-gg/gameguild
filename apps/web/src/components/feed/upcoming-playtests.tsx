import { getPublicTestingEventsDirectory } from '@/lib/testing-lab/events-public-queries';
import { CalendarDays } from 'lucide-react';
import Link from 'next/link';
import React from 'react';

export async function UpcomingPlaytests(): Promise<React.JSX.Element> {
  const directory = await getPublicTestingEventsDirectory({ take: 12 });
  const upcoming = directory.events
    .filter((event) => event.startsAt && new Date(event.startsAt) > new Date())
    .sort((a, b) => String(a.startsAt).localeCompare(String(b.startsAt)))
    .slice(0, 5);

  return (
    <div className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
      <div className="mb-5 flex items-center gap-3">
        <CalendarDays className="size-5 text-sky-200" aria-hidden="true" />
        <h2 className="text-xl font-semibold">Upcoming playtests</h2>
      </div>
      <div className="space-y-3">
        {upcoming.length === 0 ? (
          <p className="text-sm text-slate-400">No upcoming playtests scheduled right now.</p>
        ) : (
          upcoming.map((event) => (
            <Link
              key={event.id}
              href={`/testing-lab/events/${event.id}`}
              className="block rounded-2xl border border-white/10 bg-white/[0.03] p-4 transition hover:border-white/20"
            >
              <p className="font-semibold text-white">{event.name ?? 'Playtest event'}</p>
              <p className="mt-1 text-sm text-slate-400">
                {new Date(String(event.startsAt)).toLocaleString()}
              </p>
            </Link>
          ))
        )}
      </div>
    </div>
  );
}
