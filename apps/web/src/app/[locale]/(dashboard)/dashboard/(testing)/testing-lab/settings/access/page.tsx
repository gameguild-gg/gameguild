import { TestingLabAccessManagement } from '@/components/testing-lab/testing-lab-access-management';
import { TestingLabConfirmAction } from '@/components/testing-lab/testing-lab-confirm-action';
import { CreateTestingLabRoleDialog, EditTestingLabRoleDialog } from '@/components/testing-lab/testing-lab-dialogs';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import { deleteTestingLabRole } from '@/lib/testing-lab/actions';
import { getMembers } from '@/lib/community/queries/members';
import { getTestingLabAdministration, getTestingLabDashboard } from '@/lib/testing-lab';
import { Badge } from '@game-guild/ui/components/badge';
import { ShieldCheck } from 'lucide-react';

export default async function TestingLabAccessPage() {
  const [administration, memberDirectory, directory] = await Promise.all([
    getTestingLabAdministration(),
    getMembers({ page: 1, limit: 100 }),
    getTestingLabDashboard(),
  ]);
  const resources = [
    ...directory.requests.map((request) => ({ id: request.id, label: request.title, type: 'TestingRequest' as const })),
    ...directory.sessions.map((session) => ({ id: session.id, label: session.sessionName, type: 'TestingSession' as const })),
    ...directory.locations.map((location) => ({ id: location.id, label: location.name, type: 'TestingLocation' as const })),
  ];

  return (
    <div className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={ShieldCheck}
        title="Testing Lab access"
        description="Create reusable roles, inspect effective access, and manage role or resource-level exceptions for real platform members."
        actions={<CreateTestingLabRoleDialog />}
      />
      <TestingLabAccessIssues issues={[...administration.accessIssues, ...directory.accessIssues]} />
      <section className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
        <div>
          <h2 className="mb-3 text-lg font-semibold">Role templates</h2>
          {administration.roles.length === 0 ? (
            <TestingLabEmptyState
              title="No Testing Lab roles"
              description="Create a role template that matches how facilitators, moderators, and reviewers operate."
              action={<CreateTestingLabRoleDialog />}
            />
          ) : (
            <div className="space-y-3">
              {administration.roles.map((role) => {
                const permissions = Object.entries(role.permissions ?? {}).filter(([, enabled]) => enabled).map(([permission]) => permission);
                return (
                  <article key={role.id ?? role.name} className="rounded-md border p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <h3 className="font-semibold">{role.name}</h3>
                          {role.isSystemRole ? <Badge variant="secondary">System</Badge> : null}
                        </div>
                        <p className="mt-1 text-sm text-muted-foreground">{role.description ?? 'No description.'}</p>
                      </div>
                      {!role.isSystemRole ? (
                        <div className="flex items-center gap-2">
                          <EditTestingLabRoleDialog role={role} />
                          <TestingLabConfirmAction
                            action={deleteTestingLabRole}
                            fields={{ idOrName: role.id ?? role.name ?? '' }}
                            label="Delete"
                            title={`Delete ${role.name}?`}
                            description="This removes the reusable role template. Existing assignments must be reviewed separately."
                            confirmLabel="Delete role"
                            intent="delete"
                          />
                        </div>
                      ) : null}
                    </div>
                    <div className="mt-4 flex flex-wrap gap-1.5">
                      {permissions.length === 0 ? <span className="text-sm text-muted-foreground">No permissions enabled.</span> : permissions.map((permission) => (
                        <Badge key={permission} variant="outline">{permission.replace(/^can/, '').replace(/([A-Z])/g, ' $1').trim()}</Badge>
                      ))}
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </div>
        <aside className="h-fit rounded-md border p-4">
          <h2 className="mb-1 font-semibold">Member access</h2>
          <p className="mb-4 text-sm text-muted-foreground">Inspect and change effective Testing Lab access without entering identifiers manually.</p>
          <TestingLabAccessManagement
            members={memberDirectory.members.map((member) => ({ id: member.id, label: `${member.displayName} · ${member.email}` }))}
            roles={administration.roles}
            resources={resources}
          />
        </aside>
      </section>
    </div>
  );
}
