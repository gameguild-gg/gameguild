import { Link } from '@/i18n/navigation';
import { cancelLaunchPadRegistrationForm, updateLaunchPadApplicationForm, withdrawLaunchPadApplicationForm } from '@/lib/launch-pad/actions';
import { getMyLaunchPadApplications, getMyLaunchPadRegistrations } from '@/lib/launch-pad/queries';
import { getTestingProjectVersionOptions } from '@/lib/testing-lab/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Textarea } from '@game-guild/ui/components/textarea';
import { ArrowLeft, ClipboardList, Users } from 'lucide-react';

export default async function LaunchPadParticipationPage() {
  const [applications, registrations, versions] = await Promise.all([
    getMyLaunchPadApplications(),
    getMyLaunchPadRegistrations(),
    getTestingProjectVersionOptions(),
  ]);
  const projectTitleById = new Map(versions.map((version) => [version.projectId, version.projectTitle]));

  return (
    <main className="mx-auto w-full max-w-[1280px] space-y-7 px-4 py-8 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <Link href="/launch-pad" className="mb-4 inline-flex min-h-9 items-center gap-2 text-sm text-muted-foreground hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <ArrowLeft className="size-4" aria-hidden="true" />
            Launch Pad
          </Link>
          <p className="text-sm font-medium text-muted-foreground">Your activity</p>
          <h1 className="mt-1 text-3xl font-semibold tracking-tight">Your Launch Pad</h1>
          <p className="mt-2 text-sm text-muted-foreground">Manage project applications and individual event registrations.</p>
        </div>
        <Button nativeButton={false} variant="outline" render={<Link href="/launch-pad/events" />}>Browse events</Button>
      </header>

      <div className="grid items-start gap-5 lg:grid-cols-2">
        <Card className="border-border bg-card text-card-foreground">
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><ClipboardList className="size-5 text-primary" aria-hidden="true" />Project applications</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {applications.length === 0 ? (
              <p className="rounded-lg bg-muted/50 p-4 text-sm text-muted-foreground">You haven’t submitted a project to a Launch Pad event yet.</p>
            ) : applications.map((application) => (
              <article key={application.id} className="rounded-lg border border-border p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <Link href={`/launch-pad/events/${application.eventId}`} className="font-medium hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
                      {projectTitleById.get(application.projectId) || 'Your project'}
                    </Link>
                    <p className="mt-1 text-xs text-muted-foreground">Project application</p>
                  </div>
                  <Badge variant="outline">{String(application.status).replace(/([a-z])([A-Z])/g, '$1 $2')}</Badge>
                </div>
                {String(application.status) === 'Submitted' && (
                  <form action={updateLaunchPadApplicationForm} className="mt-4 space-y-3 border-t border-border pt-4">
                    <input type="hidden" name="applicationId" value={application.id} />
                    <input type="hidden" name="eventId" value={application.eventId} />
                    <label className="block space-y-2 text-sm font-medium">
                      <span>Project version</span>
                      <select name="projectVersionId" defaultValue={application.projectVersionId} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm font-normal focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
                        {versions.filter((version) => version.projectId === application.projectId).map((version) => (
                          <option key={version.id} value={version.id}>{version.versionNumber}</option>
                        ))}
                      </select>
                    </label>
                    <label className="block space-y-2 text-sm font-medium">
                      <span>Project pitch</span>
                      <Textarea name="pitch" defaultValue={application.pitch ?? ''} placeholder="What should reviewers know about this release?" />
                    </label>
                    <Button size="sm">Save application</Button>
                  </form>
                )}
                {['Submitted', 'UnderReview', 'Waitlisted'].includes(String(application.status)) ? (
                  <form action={withdrawLaunchPadApplicationForm} className="mt-3">
                    <input type="hidden" name="applicationId" value={application.id} />
                    <input type="hidden" name="eventId" value={application.eventId} />
                    <Button size="sm" variant="outline">Withdraw application</Button>
                  </form>
                ) : null}
              </article>
            ))}
          </CardContent>
        </Card>

        <Card className="border-border bg-card text-card-foreground">
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><Users className="size-5 text-primary" aria-hidden="true" />Participant registrations</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {registrations.length === 0 ? (
              <p className="rounded-lg bg-muted/50 p-4 text-sm text-muted-foreground">You haven’t registered for a participant role yet.</p>
            ) : registrations.map((registration) => (
              <article key={registration.id} className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-border p-4">
                <div>
                  <p className="font-medium">Participant role</p>
                  <p className="mt-1 text-xs text-muted-foreground">Registration {registration.id.slice(0, 8)}</p>
                </div>
                <div className="flex items-center gap-3">
                  <Badge variant="outline">{String(registration.status).replace(/([a-z])([A-Z])/g, '$1 $2')}</Badge>
                  {['Registered', 'Waitlisted', '0', '1'].includes(String(registration.status)) ? (
                    <form action={cancelLaunchPadRegistrationForm}>
                      <input type="hidden" name="registrationId" value={registration.id} />
                      <Button size="sm" variant="outline">Cancel</Button>
                    </form>
                  ) : null}
                </div>
              </article>
            ))}
          </CardContent>
        </Card>
      </div>
    </main>
  );
}
