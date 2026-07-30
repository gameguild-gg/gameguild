import { TestingLabActionForm } from '@/components/testing-lab/testing-lab-action-form';
import { TestingLabConfirmAction } from '@/components/testing-lab/testing-lab-confirm-action';
import { EditTestingSessionDialog } from '@/components/testing-lab/testing-lab-dialogs';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import {
  deleteTestingSession,
  linkTestingSessionProject,
  restoreTestingSession,
  unlinkTestingSessionProject,
  updateTestingAttendance,
} from '@/lib/testing-lab/actions';
import { getTestingLabDashboard, getTestingProjectOptions, getTestingSessionDetail, normalizeTestingSessionStatus } from '@/lib/testing-lab';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { CalendarDays, Link2 } from 'lucide-react';
import { notFound } from 'next/navigation';

export default async function TestingSessionDetailPage({ params }: { params: Promise<{ sessionId: string }> }) {
  const { sessionId } = await params;
  const [detail, projectOptions, directory] = await Promise.all([getTestingSessionDetail(sessionId), getTestingProjectOptions(), getTestingLabDashboard()]);
  if (!detail.session && detail.accessIssues.length === 0) notFound();
  const session = detail.session;
  if (!session)
    return (
      <div className="p-6">
        <TestingLabAccessIssues issues={detail.accessIssues} />
      </div>
    );

  return (
    <div className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={CalendarDays}
        title={session.sessionName}
        description={`${session.sessionDate ? new Date(session.sessionDate).toLocaleDateString() : 'Date pending'} at ${session.location?.name ?? 'an unassigned location'}.`}
        actions={
          <>
            <EditTestingSessionDialog session={session} locations={directory.locations} />
            <TestingLabConfirmAction
              action={session.isDeleted ? restoreTestingSession : deleteTestingSession}
              fields={{ sessionId: session.id }}
              label={session.isDeleted ? 'Restore' : 'Archive'}
              title={session.isDeleted ? 'Restore this testing session?' : 'Archive this testing session?'}
              description={
                session.isDeleted
                  ? 'The session will return to the operational schedule.'
                  : 'Registration will stop and the session will leave active schedules. It can be restored later.'
              }
              confirmLabel={session.isDeleted ? 'Restore session' : 'Archive session'}
              intent={session.isDeleted ? 'restore' : 'archive'}
            />
          </>
        }
      />
      <TestingLabAccessIssues issues={detail.accessIssues} />

      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Status</CardTitle>
          </CardHeader>
          <CardContent>
            <Badge>{normalizeTestingSessionStatus(session.status)}</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Registrations</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">
            {detail.registrations.length}/{session.maxTesters ?? 0}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Waitlist</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">{detail.waitlist.length}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Projects</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">{detail.projects.filter((project) => project.isActive !== false).length}</CardContent>
        </Card>
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="space-y-6">
          <div>
            <h2 className="mb-3 text-lg font-semibold">Registrations and attendance</h2>
            {detail.registrations.length === 0 ? (
              <TestingLabEmptyState title="No registrations" description="Eligible members can register from the public Testing Lab." />
            ) : (
              <div className="divide-y rounded-md border">
                {detail.registrations.map((registration) => (
                  <div key={registration.id ?? registration.userId} className="flex flex-col gap-3 p-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <p className="text-sm font-medium">{registration.user?.name ?? registration.user?.email ?? registration.userId}</p>
                      <p className="text-xs text-muted-foreground">
                        {registration.registrationType ?? 'Tester'} · {registration.status ?? 'Registered'}
                      </p>
                    </div>
                    <TestingLabActionForm action={updateTestingAttendance} submitLabel="Save" pendingLabel="Saving..." className="flex flex-wrap items-center gap-2" actionsClassName="">
                      <input type="hidden" name="sessionId" value={session.id} />
                      <input type="hidden" name="userId" value={registration.userId} />
                      <select
                        name="attendanceStatus"
                        defaultValue={registration.attendanceStatus ?? 'Registered'}
                        className="h-8 rounded-md border bg-background px-2 text-xs"
                      >
                        {['Registered', 'Present', 'Completed', 'NoShow'].map((value) => (
                          <option key={value}>{value}</option>
                        ))}
                      </select>
                    </TestingLabActionForm>
                  </div>
                ))}
              </div>
            )}
          </div>
          <div>
            <h2 className="mb-3 text-lg font-semibold">Waitlist</h2>
            {detail.waitlist.length === 0 ? (
              <p className="rounded-md border border-dashed p-5 text-sm text-muted-foreground">No members are waiting for a seat.</p>
            ) : (
              <div className="divide-y rounded-md border">
                {detail.waitlist.map((entry) => (
                  <div key={entry.id ?? entry.userId} className="flex justify-between p-3 text-sm">
                    <span>{entry.user?.name ?? entry.user?.email ?? entry.userId}</span>
                    <Badge variant="outline">Position {entry.position}</Badge>
                  </div>
                ))}
              </div>
            )}
          </div>
          <div>
            <h2 className="mb-3 text-lg font-semibold">Linked projects</h2>
            {detail.projects.length === 0 ? (
              <p className="rounded-md border border-dashed p-5 text-sm text-muted-foreground">No projects linked to this session.</p>
            ) : (
              <div className="divide-y rounded-md border">
                {detail.projects.map((project) => (
                  <div key={project.linkId ?? project.projectId} className="flex items-center justify-between gap-3 p-3 text-sm">
                    <span>{projectOptions.find((option) => option.id === project.projectId)?.title ?? project.projectId}</span>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline">{project.isActive === false ? 'Inactive' : 'Active'}</Badge>
                      {project.projectId ? (
                        <TestingLabConfirmAction
                          action={unlinkTestingSessionProject}
                          fields={{ sessionId: session.id, projectId: project.projectId }}
                          label="Unlink"
                          title="Unlink this project?"
                          description="The project leaves this testing session. Its project record and previous evidence remain intact."
                          confirmLabel="Unlink project"
                          intent="delete"
                        />
                      ) : null}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
        <aside className="h-fit rounded-md border p-4">
          <div className="mb-4 flex items-center gap-2">
            <Link2 className="size-4" />
            <h2 className="font-semibold">Link a project</h2>
          </div>
          <TestingLabActionForm action={linkTestingSessionProject} submitLabel="Link project" pendingLabel="Linking..." resetOnSuccess className="space-y-3" submitClassName="w-full">
            <input type="hidden" name="sessionId" value={session.id} />
            <div className="space-y-2">
              <Label htmlFor="session-project">Project</Label>
              <select id="session-project" name="projectId" required className="h-9 w-full rounded-md border bg-background px-3 text-sm">
                <option value="">Choose a project</option>
                {projectOptions.map((project) => (
                  <option key={project.id} value={project.id}>
                    {project.title}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="project-notes">Notes</Label>
              <Input id="project-notes" name="notes" placeholder="Build or test focus" />
            </div>
          </TestingLabActionForm>
        </aside>
      </section>
    </div>
  );
}
