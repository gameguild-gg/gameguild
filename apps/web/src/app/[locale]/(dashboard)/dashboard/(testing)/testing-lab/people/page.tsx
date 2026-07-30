import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import { Link } from '@/i18n/navigation';
import { getTestingLabDashboard, getTestingRequestDetail, getTestingSessionDetail } from '@/lib/testing-lab';
import { Badge } from '@game-guild/ui/components/badge';
import { Input } from '@game-guild/ui/components/input';
import { Search, Users } from 'lucide-react';

export default async function TestingLabPeoplePage({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const params = await searchParams;
  const q = params.q?.trim().toLowerCase() ?? '';
  const directory = await getTestingLabDashboard();
  const [requestDetails, sessionDetails] = await Promise.all([
    Promise.all(directory.requests.slice(0, 50).map((request) => getTestingRequestDetail(request.id))),
    Promise.all(directory.sessions.slice(0, 50).map((session) => getTestingSessionDetail(session.id))),
  ]);
  const issues = [
    ...directory.accessIssues,
    ...requestDetails.flatMap((detail) => detail.accessIssues),
    ...sessionDetails.flatMap((detail) => detail.accessIssues),
  ];
  const participants = requestDetails.flatMap((detail) =>
    detail.participants.map((participant) => ({
      participant,
      request: detail.request,
    })),
  );
  const registrations = sessionDetails.flatMap((detail) =>
    detail.registrations.map((registration) => ({
      registration,
      session: detail.session,
    })),
  );
  const waitlist = sessionDetails.flatMap((detail) =>
    detail.waitlist.map((entry) => ({
      entry,
      session: detail.session,
    })),
  );
  const matches = (value: string) => !q || value.toLowerCase().includes(q);

  return (
    <main className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={Users}
        title="Testing Lab people"
        description="Review request participants, session registrations, attendance status, and waitlists across the lab."
      />
      <TestingLabAccessIssues issues={[...new Set(issues)]} />
      <form method="get" className="relative max-w-xl">
        <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input name="q" defaultValue={params.q} className="pl-9" placeholder="Search member, request, or session" />
      </form>
      <section>
        <h2 className="mb-3 text-lg font-semibold">Request participants</h2>
        {participants.length === 0 ? (
          <TestingLabEmptyState title="No request participants" description="Participants appear after a member joins or an operator adds them to a request." />
        ) : (
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full min-w-[760px] text-sm">
              <thead className="bg-muted/35 text-left">
                <tr>
                  <th className="px-4 py-3">Member</th>
                  <th className="px-4 py-3">Request</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Tracked time</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {participants
                  .filter(({ participant, request }) =>
                    matches(`${participant.user?.name ?? ''} ${participant.user?.email ?? participant.userId} ${request?.title ?? ''}`),
                  )
                  .map(({ participant, request }) => (
                    <tr key={`${request?.id}-${participant.id ?? participant.userId}`}>
                      <td className="px-4 py-3 font-medium">{participant.user?.name ?? participant.user?.email ?? participant.userId}</td>
                      <td className="px-4 py-3">
                        {request ? (
                          <Link href={`/dashboard/testing-lab/requests/${request.id}`} className="hover:underline">
                            {request.title}
                          </Link>
                        ) : (
                          '-'
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant="outline">{participant.status ?? 'Registered'}</Badge>
                      </td>
                      <td className="px-4 py-3">{participant.timeSpentMinutes ?? 0} min</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      <section>
        <h2 className="mb-3 text-lg font-semibold">Session registrations</h2>
        {registrations.length === 0 ? (
          <TestingLabEmptyState title="No session registrations" description="Registrations appear after members reserve a seat." />
        ) : (
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full min-w-[760px] text-sm">
              <thead className="bg-muted/35 text-left">
                <tr>
                  <th className="px-4 py-3">Member</th>
                  <th className="px-4 py-3">Session</th>
                  <th className="px-4 py-3">Registration</th>
                  <th className="px-4 py-3">Attendance</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {registrations
                  .filter(({ registration, session }) =>
                    matches(`${registration.user?.name ?? ''} ${registration.user?.email ?? registration.userId} ${session?.sessionName ?? ''}`),
                  )
                  .map(({ registration, session }) => (
                    <tr key={`${session?.id}-${registration.id ?? registration.userId}`}>
                      <td className="px-4 py-3 font-medium">{registration.user?.name ?? registration.user?.email ?? registration.userId}</td>
                      <td className="px-4 py-3">
                        {session ? (
                          <Link href={`/dashboard/testing-lab/sessions/${session.id}`} className="hover:underline">
                            {session.sessionName}
                          </Link>
                        ) : (
                          '-'
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant="outline">{registration.status ?? 'Registered'}</Badge>
                      </td>
                      <td className="px-4 py-3">{registration.attendanceStatus ?? 'Registered'}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      <section>
        <h2 className="mb-3 text-lg font-semibold">Waitlist</h2>
        {waitlist.length === 0 ? (
          <p className="rounded-md border border-dashed p-5 text-sm text-muted-foreground">No members are currently waiting for a testing seat.</p>
        ) : (
          <div className="divide-y rounded-md border">
            {waitlist
              .filter(({ entry, session }) => matches(`${entry.user?.name ?? ''} ${entry.user?.email ?? entry.userId} ${session?.sessionName ?? ''}`))
              .map(({ entry, session }) => (
                <div key={`${session?.id}-${entry.id ?? entry.userId}`} className="flex items-center justify-between p-3 text-sm">
                  <span>{entry.user?.name ?? entry.user?.email ?? entry.userId}</span>
                  <span className="text-muted-foreground">
                    {session?.sessionName} · position {entry.position}
                  </span>
                </div>
              ))}
          </div>
        )}
      </section>
    </main>
  );
}
