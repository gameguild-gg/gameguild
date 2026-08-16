import { Link } from '@/i18n/navigation';
import { getTestingParticipationOverview } from '@/lib/testing-lab/events-public-queries';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CalendarCheck2, ClipboardCheck, FolderKanban, MessageSquareWarning } from 'lucide-react';

export default async function TestingLabParticipationPage() {
  const participation = await getTestingParticipationOverview();

  if (!participation.isAuthenticated) {
    return (
      <main className="min-h-screen bg-slate-950 px-4 py-16 text-white">
        <div className="mx-auto max-w-3xl rounded-3xl border border-white/10 bg-white/[0.04] p-8">
          <h1 className="text-3xl font-semibold">Your Testing Lab participation</h1>
          <p className="mt-3 text-slate-300">Sign in to manage your tester registrations, Team project applications, and pending feedback.</p>
          <Button asChild className="mt-6"><Link href="/sign-in">Sign in</Link></Button>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-12 text-white">
      <div className="mx-auto max-w-7xl space-y-8">
        <header className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-200">Community participation</p>
            <h1 className="mt-2 text-4xl font-semibold">Your Testing Lab</h1>
            <p className="mt-3 text-slate-300">Individual testing and applications owned by Projects you can represent.</p>
          </div>
          <Button asChild variant="outline"><Link href="/testing-lab/events">Discover events</Link></Button>
        </header>

        {participation.accessIssues.length > 0 ? (
          <Alert variant="destructive"><AlertTitle>Some participation data could not be loaded</AlertTitle>
            <AlertDescription>{participation.accessIssues.join(' ')}</AlertDescription></Alert>
        ) : null}

        <section className="grid gap-5 lg:grid-cols-3">
          <Card className="border-white/10 bg-white/[0.04] text-white">
            <CardHeader><CardTitle className="flex items-center gap-2"><FolderKanban className="size-5" /> Project applications</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              {participation.applications.length === 0 ? <p className="text-sm text-slate-400">No Project application yet.</p> : participation.applications.map((application) => (
                <div key={application.id} className="rounded-xl border border-white/10 p-3">
                  <div className="flex justify-between gap-3"><span className="text-sm font-medium">Project {application.projectId}</span><Badge variant="outline">{application.status}</Badge></div>
                  <p className="mt-2 text-xs text-slate-400">Application {application.id}</p>
                </div>
              ))}
            </CardContent>
          </Card>

          <Card className="border-white/10 bg-white/[0.04] text-white">
            <CardHeader><CardTitle className="flex items-center gap-2"><CalendarCheck2 className="size-5" /> Tester schedule</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              {participation.registrations.length === 0 ? <p className="text-sm text-slate-400">No tester registration yet.</p> : participation.registrations.map((registration) => (
                <div key={registration.id} className="rounded-xl border border-white/10 p-3">
                  <div className="flex justify-between gap-3"><span className="text-sm font-medium">Slot {registration.slotId}</span><Badge variant="outline">{registration.status}</Badge></div>
                </div>
              ))}
            </CardContent>
          </Card>

          <Card className="border-white/10 bg-white/[0.04] text-white">
            <CardHeader><CardTitle className="flex items-center gap-2"><MessageSquareWarning className="size-5" /> Feedback pending</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              {participation.feedbackObligations.length === 0 ? <p className="text-sm text-slate-400">Nothing pending.</p> : participation.feedbackObligations.map((obligation) => (
                <div key={obligation.id} className="rounded-xl border border-amber-300/20 bg-amber-300/5 p-3">
                  <p className="text-sm font-medium">Feedback required</p>
                  <p className="mt-1 text-xs text-slate-400">Application {obligation.applicationId}</p>
                  <Button asChild size="sm" className="mt-3"><Link href={`/testing-lab/events/${obligation.eventId}`}>Submit feedback</Link></Button>
                </div>
              ))}
            </CardContent>
          </Card>
        </section>

        <div className="flex items-center gap-2 text-sm text-slate-400"><ClipboardCheck className="size-4" /> Project applications remain with the Project when the original submitter leaves.</div>
      </div>
    </main>
  );
}
