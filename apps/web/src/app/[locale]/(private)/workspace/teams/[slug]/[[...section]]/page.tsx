import { ContextWorkspaceNav } from '@/components/workspaces/context-workspace-nav';
import { WorkspaceLibraryPanel } from '@/components/workspaces/workspace-library-panel';
import { Link } from '@/i18n/navigation';
import {
  addTeamMemberForm,
  archiveTeamForm,
  changeTeamMemberForm,
  createTeamInvitationForm,
  removeTeamMemberForm,
  restoreTeamForm,
  revokeTeamInvitationForm,
  updateTeamForm,
} from '@/lib/workspace-actions';
import {
  getWorkspaceLibrary,
  getWorkspaceProjectOwnership,
  getWorkspaceTeam,
  getWorkspaceTeamInvitations,
  getWorkspaceTeamProjects,
} from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { DateTimePicker } from '@/components/ui/date-time-picker';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { FolderKanban, HardDrive, Users } from 'lucide-react';
import { notFound } from 'next/navigation';

const sections = ['Overview', 'Members', 'Projects', 'Workload', 'Files', 'Invitations', 'Agreements', 'Settings'];
const sectionSlugs = new Set(sections.map((section) => section.toLowerCase().replaceAll(' ', '-')));

export async function TeamWorkspacePage({
  params,
  team: suppliedTeam,
  surface = 'member',
}: {
  params: Promise<{ slug: string; section?: string[] }>;
  team?: Awaited<ReturnType<typeof getWorkspaceTeam>>;
  surface?: 'member' | 'admin';
}) {
  const { slug, section = [] } = await params;
  const active = section[0] ?? 'overview';
  if (section.length > 1 || !sectionSlugs.has(active)) notFound();
  const team = suppliedTeam ?? await getWorkspaceTeam(slug);
  if (!team) notFound();
  const projects = await getWorkspaceTeamProjects(team.id);
  const ownerships = active === 'workload' || active === 'agreements'
    ? await Promise.all(projects.map((project) => getWorkspaceProjectOwnership(project.id)))
    : [];
  const library = active === 'files' ? await getWorkspaceLibrary('Team', team.id) : null;
  const invitations = active === 'invitations' ? await getWorkspaceTeamInvitations(team.id) : [];
  const base = surface === 'admin' ? `/dashboard/community/teams/${team.id}` : `/teams/${team.slug}`;
  const projectRoot = surface === 'admin' ? '/dashboard/community/projects' : '/projects';
  const isArchived = String(team.status).toLowerCase() === 'archived' || Number(team.status) === 1;

  return (
    <div className="space-y-6 p-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-2"><Users className="size-5" /><Badge variant="outline">Team</Badge>{team.isPersonal && <Badge>Personal</Badge>}</div>
          <h1 className="mt-2 text-3xl font-semibold">{team.name}</h1>
          <p className="mt-1 max-w-3xl text-muted-foreground">{team.description || 'Coordinate people, projects, files and agreements from one team workspace.'}</p>
        </div>
        <Button asChild><Link href={`${projectRoot}/new`}>Create project</Link></Button>
      </header>
      <ContextWorkspaceNav base={base} active={active} items={sections} />

      {active === 'overview' && <div className="grid gap-4 md:grid-cols-3">
        <Metric title="Active members" value={team.members.filter((member) => member.isActive).length} icon={<Users className="size-4" />} />
        <Metric title="Projects" value={projects.length} icon={<FolderKanban className="size-4" />} />
        <Metric title="Library" value="Team files" icon={<HardDrive className="size-4" />} />
      </div>}

      {active === 'members' && <div className="grid gap-4 xl:grid-cols-[1fr_22rem]">
        <Card><CardHeader><CardTitle>Members</CardTitle><CardDescription>Authority controls management; professional title remains descriptive.</CardDescription></CardHeader><CardContent className="space-y-3">{team.members.map((member) => <form key={member.userId} action={changeTeamMemberForm} className="grid gap-3 rounded-lg border p-4 md:grid-cols-[1fr_10rem_1fr_auto_auto] md:items-end"><input type="hidden" name="teamId" value={team.id} /><input type="hidden" name="userId" value={member.userId} /><input type="hidden" name="returnPath" value={`${base}/members`} /><div><Label>User</Label><p className="mt-2 break-all text-sm">{member.userId}</p></div><div><Label htmlFor={`authority-${member.userId}`}>Authority</Label><select id={`authority-${member.userId}`} name="authority" defaultValue={String(member.authority)} className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option>Owner</option><option>Manager</option><option>Member</option><option>Viewer</option></select></div><div><Label htmlFor={`title-${member.userId}`}>Professional title</Label><Input id={`title-${member.userId}`} name="professionalTitle" defaultValue={member.professionalTitle ?? ''} /></div><Button type="submit" variant="secondary">Save</Button><Button formAction={removeTeamMemberForm} type="submit" variant="destructive">Remove</Button></form>)}</CardContent></Card>
        <Card><CardHeader><CardTitle>Add tenant member</CardTitle><CardDescription>Only an active member of this tenant can be added.</CardDescription></CardHeader><CardContent><form action={addTeamMemberForm} className="space-y-3"><input type="hidden" name="teamId" value={team.id} /><input type="hidden" name="returnPath" value={`${base}/members`} /><div><Label htmlFor="new-member-id">User ID</Label><Input id="new-member-id" name="userId" required /></div><div><Label htmlFor="new-member-authority">Authority</Label><select id="new-member-authority" name="authority" defaultValue="Member" className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option>Owner</option><option>Manager</option><option>Member</option><option>Viewer</option></select></div><div><Label htmlFor="new-member-title">Professional title</Label><Input id="new-member-title" name="professionalTitle" /></div><Button type="submit">Add member</Button></form></CardContent></Card>
      </div>}

      {active === 'projects' && <Card><CardHeader><CardTitle>Team projects</CardTitle><CardDescription>Ownership and participation are explicit for every project.</CardDescription></CardHeader><CardContent className="grid gap-3 md:grid-cols-2">{projects.length ? projects.map((project) => <Link key={project.id} href={surface === 'admin' ? `/dashboard/community/projects/${project.id}` : `/projects/${project.slug}`} className="rounded-lg border p-4 transition-colors hover:bg-muted/50"><div className="flex items-center justify-between gap-2"><h2 className="font-medium">{project.title}</h2><Badge>{String(project.teamRole)}</Badge></div><p className="mt-2 text-sm text-muted-foreground">{String(project.status)} · {String(project.participationMode)}</p></Link>) : <Empty message="This Team does not participate in a Project yet." />}</CardContent></Card>}

      {active === 'workload' && <Card><CardHeader><CardTitle>Workload</CardTitle><CardDescription>Only active, allocated members can receive new project tasks.</CardDescription></CardHeader><CardContent className="space-y-3">{ownerships.flatMap((ownership) => ownership?.allocations ?? []).filter((allocation) => allocation.isActive).map((allocation) => <div key={allocation.id} className="grid gap-2 rounded-lg border p-4 md:grid-cols-[1fr_auto_auto]"><div><p className="font-medium">{allocation.function}</p><p className="text-sm text-muted-foreground">{allocation.userId}</p></div><span>{allocation.capacityPercentage}% capacity</span><Badge variant="outline">Active</Badge></div>)}{!ownerships.some((ownership) => ownership?.allocations.some((allocation) => allocation.isActive)) && <Empty message="No active allocations for this Team." />}</CardContent></Card>}

      {active === 'files' && <WorkspaceLibraryPanel title="Team library" library={library} resourceType="Team" resourceId={team.id} returnPath={`${base}/files`} />}

      {active === 'invitations' && <div className="grid gap-4 xl:grid-cols-[1fr_22rem]">
        <Card><CardHeader><CardTitle>Invitations</CardTitle><CardDescription>Tokens are hashed, invitations expire, can be revoked and are single-use.</CardDescription></CardHeader><CardContent className="space-y-3">{invitations.map((invitation) => <div key={invitation.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-4"><div><p className="font-medium">{invitation.invitedEmail || invitation.invitedUserId || 'Invitation'}</p><p className="text-sm text-muted-foreground">{String(invitation.authority)} · expires {new Date(invitation.expiresAt).toLocaleString()}</p></div><div className="flex items-center gap-2">{invitation.usedAt ? <Badge>Used</Badge> : invitation.revokedAt ? <Badge variant="secondary">Revoked</Badge> : <><Badge variant="outline">Pending</Badge><form action={revokeTeamInvitationForm}><input type="hidden" name="teamId" value={team.id} /><input type="hidden" name="invitationId" value={invitation.id} /><input type="hidden" name="returnPath" value={`${base}/invitations`} /><Button type="submit" size="sm" variant="destructive">Revoke</Button></form></>}</div></div>)}{!invitations.length && <Empty message="No Team invitations." />}</CardContent></Card>
        <Card><CardHeader><CardTitle>Invite member</CardTitle><CardDescription>Provide an email or a user ID.</CardDescription></CardHeader><CardContent><form action={createTeamInvitationForm} className="space-y-3"><input type="hidden" name="teamId" value={team.id} /><input type="hidden" name="returnPath" value={`${base}/invitations`} /><div><Label htmlFor="invite-email">Email</Label><Input id="invite-email" name="email" type="email" /></div><div><Label htmlFor="invite-user">User ID</Label><Input id="invite-user" name="userId" /></div><div><Label htmlFor="invite-authority">Authority</Label><select id="invite-authority" name="authority" defaultValue="Member" className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option>Owner</option><option>Manager</option><option>Member</option><option>Viewer</option></select></div><div><Label htmlFor="invite-expiry">Expires</Label><DateTimePicker id="invite-expiry" name="expiresAt" required /></div><Button type="submit">Create invitation</Button></form></CardContent></Card>
      </div>}

      {active === 'agreements' && <Card><CardHeader><CardTitle>Project agreements</CardTitle><CardDescription>Proposals and counterproposals remain attached to the Project and require two distinct actors to accept.</CardDescription></CardHeader><CardContent className="space-y-3">{ownerships.flatMap((ownership) => ownership?.agreements ?? []).map((agreement) => <div key={agreement.id} className="rounded-lg border p-4"><div className="flex justify-between gap-2"><p className="font-medium">{agreement.scope}</p><Badge variant="outline">{String(agreement.status)}</Badge></div><p className="mt-2 text-sm text-muted-foreground">{agreement.deliverables}</p><p className="mt-2 text-xs text-muted-foreground">Revision {agreement.revision}</p></div>)}{!ownerships.some((ownership) => ownership?.agreements.length) && <Empty message="No agreement has been proposed for this Team's Projects." />}</CardContent></Card>}

      {active === 'settings' && <div className="space-y-4"><Card><CardHeader><CardTitle>Team settings</CardTitle><CardDescription>Manage the Team identity and visibility. Personal Team ownership remains protected.</CardDescription></CardHeader><CardContent><form action={updateTeamForm} className="grid max-w-3xl gap-4 sm:grid-cols-2"><input type="hidden" name="teamId" value={team.id} /><input type="hidden" name="returnPath" value={`${base}/settings`} /><div><Label htmlFor="team-name">Name</Label><Input id="team-name" name="name" required defaultValue={team.name} /></div><div><Label htmlFor="team-slug">Slug</Label><Input id="team-slug" name="slug" required defaultValue={team.slug} /></div><div><Label htmlFor="team-visibility">Visibility</Label><select id="team-visibility" name="visibility" defaultValue={String(team.visibility)} className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option>Private</option><option>Tenant</option><option>Public</option></select></div><div><Label>Type</Label><p className="mt-2 text-sm">{team.isPersonal ? 'Personal Team' : 'Team'}</p></div><div className="sm:col-span-2"><Label htmlFor="team-description">Description</Label><Textarea id="team-description" name="description" defaultValue={team.description ?? ''} /></div><Button type="submit" className="sm:col-span-2 sm:w-fit">Save settings</Button></form></CardContent></Card>{!team.isPersonal && <Card className={isArchived ? 'border-emerald-500/40' : 'border-destructive/40'}><CardHeader><CardTitle>{isArchived ? 'Restore Team' : 'Archive Team'}</CardTitle><CardDescription>{isArchived ? 'Restoring returns the Team to active workspaces without changing its ownership.' : 'Archiving removes the Team from active workspaces without transferring Project ownership automatically.'}</CardDescription></CardHeader><CardContent>{isArchived ? <form action={restoreTeamForm}><input type="hidden" name="teamId" value={team.id} /><input type="hidden" name="returnPath" value={`${base}/settings`} /><Button type="submit">Restore Team</Button></form> : <form action={archiveTeamForm}><input type="hidden" name="teamId" value={team.id} /><input type="hidden" name="returnPath" value={`${base}/settings`} /><Button type="submit" variant="destructive">Archive Team</Button></form>}</CardContent></Card>}</div>}
    </div>
  );
}

export default TeamWorkspacePage;

function Metric({ title, value, icon }: { title: string; value: string | number; icon: React.ReactNode }) { return <Card><CardHeader className="flex-row items-center justify-between space-y-0 pb-2"><CardTitle className="text-sm font-medium">{title}</CardTitle>{icon}</CardHeader><CardContent><p className="text-2xl font-semibold">{value}</p></CardContent></Card>; }
function Empty({ message }: { message: string }) { return <p className="py-8 text-center text-sm text-muted-foreground">{message}</p>; }
