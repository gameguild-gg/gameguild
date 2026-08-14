import { LaunchPadApplicationForm } from '@/components/launch-pad/launch-pad-application-form';
import { Link } from '@/i18n/navigation';
import { registerLaunchPadSlotForm } from '@/lib/launch-pad/actions';
import { getMyLaunchPadApplications, getMyLaunchPadRegistrations, getPublicLaunchPadEvent } from '@/lib/launch-pad/queries';
import { getTestingProjectVersionOptions } from '@/lib/testing-lab/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { notFound } from 'next/navigation';

export default async function LaunchPadEventDetailPage({ params, searchParams }: { params: Promise<{ eventId: string }>; searchParams?: Promise<{ projectId?: string }> }) {
  const { eventId } = await params;
  const { projectId } = searchParams ? await searchParams : {};
  const detail = await getPublicLaunchPadEvent(eventId);
  if (!detail) notFound();
  const [versions, applications, registrations] = await Promise.all([
    getTestingProjectVersionOptions(), getMyLaunchPadApplications(), getMyLaunchPadRegistrations(),
  ]);
  const currentApplication = applications.find((application) => application.eventId === eventId);
  const registrationIds = new Set(registrations.map((registration) => registration.slotId));

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-14 text-white">
      <div className="mx-auto max-w-6xl space-y-8">
        <Link href="/launch-pad/events" className="text-sm text-sky-200">← Launch Pad events</Link>
        <header><div className="flex flex-wrap items-center gap-3"><h1 className="text-4xl font-semibold">{detail.event.name}</h1><Badge variant="outline">{detail.event.status}</Badge></div><p className="mt-3 max-w-3xl text-slate-300">{detail.event.description}</p></header>
        <div className="grid gap-6 lg:grid-cols-2">
          <Card className="border-white/10 bg-white/[0.04] text-white"><CardHeader><CardTitle>Project application</CardTitle></CardHeader><CardContent>
            {currentApplication ? <div className="space-y-2"><Badge>{currentApplication.status}</Badge><p className="text-sm text-slate-400">This application belongs to Project {currentApplication.projectId}, not to the original submitter.</p></div> : detail.event.status === 'ApplicationsOpen' ? <LaunchPadApplicationForm eventId={eventId} versions={versions} initialProjectId={projectId} /> : <p className="text-sm text-slate-400">Applications are closed.</p>}
          </CardContent></Card>
          <Card className="border-white/10 bg-white/[0.04] text-white"><CardHeader><CardTitle>Individual participation</CardTitle></CardHeader><CardContent className="space-y-3">
            {detail.slots.length === 0 ? <p className="text-sm text-slate-400">No participant slot configured.</p> : detail.slots.map((slot) => (
              <div key={slot.id} className="rounded-xl border border-white/10 p-3"><div className="flex justify-between gap-3"><div><p className="font-medium">{slot.name}</p><p className="text-xs text-slate-400">{slot.reservedCount}/{slot.capacity} reserved</p></div><Badge variant="outline">{slot.role}</Badge></div>
                {registrationIds.has(slot.id) ? <p className="mt-3 text-sm text-emerald-300">Registered</p> : <form action={registerLaunchPadSlotForm} className="mt-3"><input type="hidden" name="eventId" value={eventId} /><input type="hidden" name="slotId" value={slot.id} /><Button size="sm" type="submit">Register</Button></form>}
              </div>
            ))}
          </CardContent></Card>
        </div>
      </div>
    </main>
  );
}
