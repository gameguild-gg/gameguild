import { Link } from '@/i18n/navigation';
import { cancelLaunchPadRegistrationForm, updateLaunchPadApplicationForm, withdrawLaunchPadApplicationForm } from '@/lib/launch-pad/actions';
import { getMyLaunchPadApplications, getMyLaunchPadRegistrations } from '@/lib/launch-pad/queries';
import { getTestingProjectVersionOptions } from '@/lib/testing-lab/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Textarea } from '@game-guild/ui/components/textarea';

export default async function LaunchPadParticipationPage() {
  const [applications, registrations, versions] = await Promise.all([getMyLaunchPadApplications(), getMyLaunchPadRegistrations(), getTestingProjectVersionOptions()]);
  return (
    <main className="min-h-screen bg-slate-950 px-4 py-14 text-white"><div className="mx-auto max-w-6xl space-y-8">
      <header className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-200">Community participation</p><h1 className="mt-2 text-4xl font-semibold">Your Launch Pad</h1><p className="mt-3 text-slate-300">Project-owned applications and your individual registrations.</p></div><Button asChild variant="outline"><Link href="/launch-pad/events">Discover events</Link></Button></header>
      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="border-white/10 bg-white/[0.04] text-white"><CardHeader><CardTitle>Project applications</CardTitle></CardHeader><CardContent className="space-y-3">{applications.length === 0 ? <p className="text-sm text-slate-400">No application.</p> : applications.map((application) => <div key={application.id} className="rounded-xl border border-white/10 p-3"><div className="flex justify-between"><span>Project {application.projectId}</span><Badge variant="outline">{application.status}</Badge></div>{String(application.status) === 'Submitted' && <form action={updateLaunchPadApplicationForm} className="mt-3 space-y-2"><input type="hidden" name="applicationId" value={application.id} /><input type="hidden" name="eventId" value={application.eventId} /><select name="projectVersionId" defaultValue={application.projectVersionId} className="h-10 w-full rounded-md border border-white/15 bg-slate-950 px-3 text-sm">{versions.filter((version) => version.projectId === application.projectId).map((version) => <option key={version.id} value={version.id}>{version.versionNumber}</option>)}</select><Textarea name="pitch" defaultValue={application.pitch ?? ''} placeholder="Pitch" /><Button size="sm">Update application</Button></form>}{['Submitted','UnderReview','Waitlisted'].includes(String(application.status)) ? <form action={withdrawLaunchPadApplicationForm} className="mt-3"><input type="hidden" name="applicationId" value={application.id} /><input type="hidden" name="eventId" value={application.eventId} /><Button size="sm" variant="outline">Withdraw</Button></form> : null}</div>)}</CardContent></Card>
        <Card className="border-white/10 bg-white/[0.04] text-white"><CardHeader><CardTitle>Individual registrations</CardTitle></CardHeader><CardContent className="space-y-3">{registrations.length === 0 ? <p className="text-sm text-slate-400">No registration.</p> : registrations.map((registration) => <div key={registration.id} className="rounded-xl border border-white/10 p-3"><div className="flex justify-between"><span>Slot {registration.slotId}</span><Badge variant="outline">{registration.status}</Badge></div>{['Registered','Waitlisted'].includes(String(registration.status)) ? <form action={cancelLaunchPadRegistrationForm} className="mt-3"><input type="hidden" name="registrationId" value={registration.id} /><Button size="sm" variant="outline">Cancel</Button></form> : null}</div>)}</CardContent></Card>
      </div>
    </div></main>
  );
}
