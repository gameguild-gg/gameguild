import React from 'react';
import { Link } from '@/i18n/navigation';
import { submitTestingBuild } from '@/lib/testing-lab/actions';
import {
  countAvailableTesterSlots,
  getTestingLabDashboard,
  getTestingProjectOptions,
  normalizeTestingLocationStatus,
  normalizeTestingRequestStatus,
  normalizeTestingSessionStatus,
  type TestingRequestSummary,
  type TestingSessionSummary,
} from '@/lib/testing-lab';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { ArrowRight, CalendarDays, ClipboardCheck, FlaskConical, MapPin, ShieldAlert, Users } from 'lucide-react';

function formatDate(value?: string | null): string {
  if (!value) return 'Not scheduled';
  return new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function RequestCard({ request }: { request: TestingRequestSummary }) {
  const status = normalizeTestingRequestStatus(request.status);
  const maxTesters = request.maxTesters ?? null;
  const currentTesters = request.currentTesterCount ?? 0;
  const projectTitle = request.projectVersion?.project?.title ?? request.projectVersion?.project?.name ?? request.projectVersion?.project?.slug ?? null;
  const versionNumber = request.projectVersion?.versionNumber ?? null;

  return (
    <Card className="min-w-0">
      <CardHeader className="space-y-2">
        <div className="flex min-w-0 items-start justify-between gap-3">
          <div className="min-w-0">
            <CardTitle className="break-words">{request.title}</CardTitle>
            <CardDescription className="break-words">{request.description ?? 'No description provided.'}</CardDescription>
            {projectTitle ? (
              <p className="mt-2 break-words text-xs font-medium text-muted-foreground">
                {projectTitle}{versionNumber ? ` · ${versionNumber}` : ''}
              </p>
            ) : null}
          </div>
          <Badge variant={status === 'Active' || status === 'Open' ? 'default' : 'outline'}>{status}</Badge>
        </div>
      </CardHeader>
      <CardContent className="grid gap-3 text-sm md:grid-cols-3">
        <div>
          <p className="text-muted-foreground">Window</p>
          <p className="font-medium">{formatDate(request.startDate)} to {formatDate(request.endDate)}</p>
        </div>
        <div>
          <p className="text-muted-foreground">Tester capacity</p>
          <p className="font-medium">{maxTesters === null ? 'Unlimited' : `${currentTesters}/${maxTesters}`}</p>
        </div>
        <div>
          <p className="text-muted-foreground">Build</p>
          {request.downloadUrl ? (
            <a className="font-medium underline underline-offset-4" href={request.downloadUrl}>
              Download link
            </a>
          ) : (
            <p className="font-medium">Not attached</p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function SessionRow({ session }: { session: TestingSessionSummary }) {
  const status = normalizeTestingSessionStatus(session.status);
  const registered = session.registeredTesterCount ?? 0;
  const max = session.maxTesters ?? 0;

  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border p-3">
      <div className="min-w-0">
        <p className="truncate text-sm font-medium">{session.sessionName}</p>
        <p className="text-xs text-muted-foreground">
          {formatDate(session.sessionDate)} · {session.location?.name ?? 'No location'}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <Badge variant="outline">{registered}/{max}</Badge>
        <Badge>{status}</Badge>
      </div>
    </div>
  );
}

export default async function TestingLabPage(): Promise<React.JSX.Element> {
  const [data, projects] = await Promise.all([getTestingLabDashboard(), getTestingProjectOptions()]);
  const openRequests = data.requests.filter((request) => ['Open', 'Active', 'InProgress'].includes(normalizeTestingRequestStatus(request.status)));
  const scheduledSessions = data.sessions.filter((session) => normalizeTestingSessionStatus(session.status) === 'Scheduled');
  const availableSlots = countAvailableTesterSlots(data.requests);

  return (
    <div className="flex min-w-0 max-w-full flex-col gap-6 p-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Testing Lab</h1>
          <p className="text-muted-foreground">Manage build submissions, moderated testing sessions, locations, and tester capacity.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button asChild variant="outline">
            <Link href="/testing-lab">
              Public lab
              <ArrowRight className="ml-2 size-4" />
            </Link>
          </Button>
          <Button asChild variant="outline">
            <Link href="/projects">
              Project showcase
              <ArrowRight className="ml-2 size-4" />
            </Link>
          </Button>
        </div>
      </div>

      {data.accessIssues.length > 0 ? (
        <Alert>
          <ShieldAlert className="size-4" />
          <AlertTitle>Some Testing Lab data could not be loaded</AlertTitle>
          <AlertDescription>{data.accessIssues.join(', ')}</AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Open requests</CardTitle>
            <FlaskConical className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{openRequests.length}</p>
            <CardDescription>{data.requests.length} total submissions</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Scheduled sessions</CardTitle>
            <CalendarDays className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{scheduledSessions.length}</p>
            <CardDescription>{data.publicSessions.length} public sessions visible</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Tester slots</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{availableSlots}</p>
            <CardDescription>Available across capped requests</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Locations</CardTitle>
            <MapPin className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{data.locations.length}</p>
            <CardDescription>Lab, remote, and hybrid spaces</CardDescription>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>
            <h2>Operational workflow</h2>
          </CardTitle>
          <CardDescription>Testing Lab work moves from build intake to tester scheduling, then into feedback summaries.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3 md:grid-cols-3">
          {[
            ['1', 'Intake', 'Submit the build, target audience, download link, and tester instructions.'],
            ['2', 'Schedule', 'Assign capped tester slots, session windows, and remote or physical locations.'],
            ['3', 'Report', 'Collect comparable notes and turn the patterns into next actions for the team.'],
          ].map(([step, title, body]) => (
            <div key={step} className="rounded-lg border bg-muted/25 p-4">
              <div className="mb-4 flex items-center gap-3">
                <span className="flex size-8 items-center justify-center rounded-full bg-primary text-sm font-semibold text-primary-foreground">
                  {step}
                </span>
                <h2 className="font-semibold">{title}</h2>
              </div>
              <p className="text-sm leading-6 text-muted-foreground">{body}</p>
            </div>
          ))}
        </CardContent>
      </Card>

      <div className="grid min-w-0 max-w-full gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(300px,420px)]">
        <section className="min-w-0 space-y-4">
          {data.requests.length === 0 ? (
            <Card>
              <CardHeader>
                <CardTitle>No testing requests yet</CardTitle>
                <CardDescription>Submit a build to create the first Testing Lab request.</CardDescription>
              </CardHeader>
            </Card>
          ) : (
            data.requests.map((request) => <RequestCard key={request.id} request={request} />)
          )}

          <Card>
            <CardHeader>
              <CardTitle>Sessions</CardTitle>
              <CardDescription>Scheduled and public testing windows.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-2">
              {data.sessions.length === 0 ? (
                <p className="text-sm text-muted-foreground">No sessions are scheduled.</p>
              ) : (
                data.sessions.slice(0, 6).map((session) => <SessionRow key={session.id} session={session} />)
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Locations</CardTitle>
              <CardDescription>Capacity and availability for moderated testing.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-3 md:grid-cols-2">
              {data.locations.length === 0 ? (
                <p className="text-sm text-muted-foreground">No testing locations configured.</p>
              ) : (
                data.locations.slice(0, 6).map((location) => (
                  <div key={location.id} className="rounded-lg border p-3">
                    <div className="flex items-center justify-between gap-2">
                      <p className="font-medium">{location.name}</p>
                      <Badge variant="outline">{normalizeTestingLocationStatus(location.status)}</Badge>
                    </div>
                    <p className="mt-1 text-sm text-muted-foreground">{location.isVirtual ? 'Virtual' : [location.city, location.country].filter(Boolean).join(', ') || 'Physical location'}</p>
                    <p className="mt-2 text-xs text-muted-foreground">
                      Capacity: {location.capacity ?? location.maxTestersCapacity ?? 0} testers · {location.maxProjectsCapacity ?? 0} projects
                    </p>
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </section>

        <Card className="h-fit min-w-0">
          <CardHeader className="min-w-0">
            <div className="flex min-w-0 items-center gap-3">
              <div className="flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                <ClipboardCheck className="size-5" />
              </div>
              <div className="min-w-0">
                <CardTitle>Submit build</CardTitle>
                <CardDescription className="break-words">Create a lightweight Testing Lab request from a team build.</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="min-w-0">
            <form action={submitTestingBuild} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="testing-title">Title</Label>
                <Input id="testing-title" name="title" required placeholder="Vertical slice feedback pass" />
              </div>
              <div className="space-y-2">
                <Label>Project</Label>
                <div className="grid gap-2">
                  {projects.length === 0 ? (
                    <p className="rounded-lg border p-3 text-sm text-muted-foreground">Create a project first, then submit a build for testing.</p>
                  ) : (
                    projects.slice(0, 6).map((project, index) => (
                      <label key={project.id} className="flex cursor-pointer items-center gap-3 rounded-lg border p-3">
                        <input type="radio" name="projectId" value={project.id} defaultChecked={index === 0} required className="size-4" />
                        <span className="min-w-0">
                          <span className="block truncate text-sm font-medium">{project.title}</span>
                          <span className="block truncate text-xs text-muted-foreground">{project.slug ?? project.id}</span>
                        </span>
                      </label>
                    ))
                  )}
                </div>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="testing-version">Version</Label>
                  <Input id="testing-version" name="versionNumber" required placeholder="0.3.0" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="testing-max">Max testers</Label>
                  <Input id="testing-max" name="maxTesters" type="number" min="1" placeholder="12" />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="testing-download">Download URL</Label>
                <Input id="testing-download" name="downloadUrl" type="url" placeholder="https://example.com/build.zip" />
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="testing-start">Start</Label>
                  <Input id="testing-start" name="startDate" type="datetime-local" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="testing-end">End</Label>
                  <Input id="testing-end" name="endDate" type="datetime-local" />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="testing-description">Description</Label>
                <Textarea id="testing-description" name="description" rows={3} placeholder="What changed in this build and what should testers focus on?" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="testing-instructions">Tester instructions</Label>
                <Textarea id="testing-instructions" name="instructionsContent" rows={4} placeholder="Install steps, target tasks, known issues, and success criteria." />
              </div>
              <div className="space-y-2">
                <Label htmlFor="testing-feedback">Feedback questions</Label>
                <Textarea id="testing-feedback" name="feedbackFormContent" rows={4} placeholder="What confused you? What felt polished? Where did you stop?" />
              </div>
              <Button type="submit" className="w-full" disabled={projects.length === 0}>
                Submit to Testing Lab
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
