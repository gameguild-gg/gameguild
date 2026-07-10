import { Link } from '@/i18n/navigation';
import { COMMUNITY_ACCESS_ROLES, getMemberAccessDirectory } from '@/lib/community';
import { invitePlatformUser } from '@/lib/community/actions/member-access';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { ShieldCheck, UserPlus, Users } from 'lucide-react';
import React from 'react';

interface Props {
  searchParams?: Promise<{
    message?: string;
    error?: string;
  }>;
}

function getRoleBadgeVariant(role: string) {
  if (role === 'SystemAdmin' || role === 'Admin') return 'default';
  if (role === 'TenantAdmin' || role === 'Owner') return 'secondary';
  return 'outline';
}

function getWorkspaceOptions(members: Awaited<ReturnType<typeof getMemberAccessDirectory>>['members']) {
  const workspaces = new Map<string, { tenantId: string; label: string }>();

  for (const row of members) {
    for (const membership of row.memberships) {
      if (!membership.tenantId || workspaces.has(membership.tenantId)) continue;

      workspaces.set(membership.tenantId, {
        tenantId: membership.tenantId,
        label: membership.tenantName ?? membership.tenantSlug ?? membership.tenantId,
      });
    }
  }

  return [...workspaces.values()].sort((left, right) => left.label.localeCompare(right.label));
}

function getAccessStatus(row: Awaited<ReturnType<typeof getMemberAccessDirectory>>['members'][number]) {
  if (!row.primaryMembership) return 'No workspace';
  if (row.primaryMembership.isActive === false) return 'Inactive';
  return 'Accepted';
}

function getAccessStatusVariant(status: string) {
  if (status === 'Accepted') return 'default';
  if (status === 'Inactive') return 'secondary';
  return 'outline';
}

export default async function Page({ searchParams }: Props): Promise<React.JSX.Element> {
  const query = await searchParams;
  const directory = await getMemberAccessDirectory({ limit: 50 });
  const members = directory.members;
  const total = directory.total;
  const activeMembers = members.filter((row) => row.member.status === 'active').length;
  const workspaceMembers = members.filter((row) => row.primaryMembership?.tenantId).length;
  const workspaceOptions = getWorkspaceOptions(members);
  const inviteSenderEmail = members.find((row) => row.isCurrentUser)?.member.email ?? '';

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Users</h1>
          <p className="text-muted-foreground">Browse registered community members and their workspace access.</p>
        </div>
        <Dialog>
          <DialogTrigger asChild>
            <Button>
              <UserPlus className="mr-2 size-4" />
              Invite User
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Invite user</DialogTitle>
              <DialogDescription>Create a platform user and assign their initial workspace access.</DialogDescription>
            </DialogHeader>
            <form action={invitePlatformUser} className="space-y-4">
              <input type="hidden" name="invitedByEmail" value={inviteSenderEmail} />
              <div className="space-y-2">
                <Label htmlFor="invite-email">Email</Label>
                <Input id="invite-email" name="email" type="email" autoComplete="email" required placeholder="member@example.com" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="invite-name">Name</Label>
                <Input id="invite-name" name="name" autoComplete="name" placeholder="Member name" />
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="invite-workspace">Workspace</Label>
                  {workspaceOptions.length > 0 ? (
                    <Select name="tenantId" defaultValue={workspaceOptions[0]?.tenantId}>
                      <SelectTrigger id="invite-workspace" className="w-full">
                        <SelectValue placeholder="Select workspace" />
                      </SelectTrigger>
                      <SelectContent>
                        {workspaceOptions.map((workspace) => (
                          <SelectItem key={workspace.tenantId} value={workspace.tenantId}>
                            {workspace.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <Input id="invite-workspace" name="tenantId" required placeholder="Workspace ID" />
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="invite-role">Access role</Label>
                  <Select name="role" defaultValue="Member">
                    <SelectTrigger id="invite-role" className="w-full">
                      <SelectValue placeholder="Select role" />
                    </SelectTrigger>
                    <SelectContent>
                      {COMMUNITY_ACCESS_ROLES.map((role) => (
                        <SelectItem key={role.value} value={role.value}>
                          {role.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <DialogFooter>
                <Button type="submit">
                  <UserPlus className="mr-2 size-4" />
                  Send invite
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {query?.message ? (
        <Alert>
          <ShieldCheck className="size-4" />
          <AlertTitle>Access updated</AlertTitle>
          <AlertDescription>{query.message}</AlertDescription>
        </Alert>
      ) : null}

      {query?.error || directory.error ? (
        <Alert variant="destructive">
          <ShieldCheck className="size-4" />
          <AlertTitle>Access warning</AlertTitle>
          <AlertDescription>{query?.error ?? directory.error}</AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Members</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{total}</div>
            <CardDescription>Loaded from the identity API</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Active members</CardTitle>
            <ShieldCheck className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeMembers}</div>
            <CardDescription>Recently active community accounts</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Workspace access</CardTitle>
            <ShieldCheck className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{workspaceMembers}</div>
            <CardDescription>Members linked to an access workspace</CardDescription>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All users</CardTitle>
          <CardDescription>{total > 0 ? `${total} users registered` : 'No users registered yet'}</CardDescription>
        </CardHeader>
        <CardContent>
          {members.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <UserPlus className="mb-4 size-12 text-muted-foreground" />
              <h3 className="text-lg font-semibold">No users yet</h3>
              <p className="text-sm text-muted-foreground">Users will appear here once they register or are invited.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Current role</TableHead>
                  <TableHead>Access workspace</TableHead>
                  <TableHead>Access status</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Joined</TableHead>
                  <TableHead>Last Active</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {members.map((row) => (
                  <TableRow key={row.member.id} className="hover:bg-muted/50">
                    <TableCell>
                      <Link href={`/dashboard/community/members/users/${row.member.id}`} className="flex flex-col">
                        <span className="font-medium">{row.member.displayName}</span>
                        <span className="text-xs text-muted-foreground">@{row.member.username}</span>
                      </Link>
                    </TableCell>
                    <TableCell className="text-sm">{row.member.email}</TableCell>
                    <TableCell>
                      <Badge variant={getRoleBadgeVariant(row.role)}>{row.role}</Badge>
                      {row.isCurrentUser ? <Badge variant="outline" className="ml-2">You</Badge> : null}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {row.primaryMembership?.tenantName ?? row.primaryMembership?.tenantSlug ?? 'No active workspace'}
                      {row.membershipLoadError ? <span className="block text-xs text-destructive">{row.membershipLoadError}</span> : null}
                    </TableCell>
                    <TableCell>
                      <Badge variant={getAccessStatusVariant(getAccessStatus(row))}>{getAccessStatus(row)}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={row.member.status === 'active' ? 'default' : row.member.status === 'banned' ? 'destructive' : 'secondary'}>{row.member.status}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(row.member.joinedAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(row.member.lastActiveAt).toLocaleDateString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
