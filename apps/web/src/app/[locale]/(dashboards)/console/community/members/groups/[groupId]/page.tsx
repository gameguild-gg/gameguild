import { getGroup, getGroupMembers, getMemberAccessDirectory } from '@/lib/community';
import {
  addCommunityGroupMember,
  approveCommunityGroupMember,
  archiveCommunityGroup,
  changeCommunityGroupMemberRole,
  rejectCommunityGroupMember,
  removeCommunityGroupMember,
  updateCommunityGroup,
} from '@/lib/community/actions/groups';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Archive, ShieldCheck, UserPlus, Users } from 'lucide-react';
import React from 'react';

interface Props {
  params: Promise<{ groupId: string }>;
  searchParams?: Promise<{
    message?: string;
    error?: string;
  }>;
}

const GROUP_TYPES = [
  ['InterestCommunity', 'Interest community'],
  ['StudyGroup', 'Study group'],
  ['ProjectTeam', 'Project team'],
  ['CourseCohort', 'Course cohort'],
  ['Institution', 'Institution'],
  ['GameJamTeam', 'Game jam team'],
] as const;

const GROUP_VISIBILITIES = [
  ['Public', 'Public'],
  ['Private', 'Private'],
  ['InviteOnly', 'Invite only'],
] as const;

const GROUP_ROLES = ['Owner', 'Admin', 'Moderator', 'Member'] as const;

function statusVariant(status: string) {
  if (status === 'Active') return 'default';
  if (status === 'Pending') return 'secondary';
  if (status === 'Rejected' || status === 'Removed') return 'destructive';
  return 'outline';
}

export default async function Page({ params, searchParams }: Props): Promise<React.JSX.Element> {
  const [{ groupId }, query] = await Promise.all([params, searchParams ?? Promise.resolve(undefined)]);
  const [groupResult, membersResult, directory] = await Promise.all([
    getGroup(groupId),
    getGroupMembers(groupId, { limit: 200 }),
    getMemberAccessDirectory({ limit: 500 }),
  ]);
  const group = groupResult.group;
  const warning = query?.error ?? groupResult.error ?? membersResult.error ?? directory.error;

  if (!group) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <Alert variant="destructive">
          <ShieldCheck className="size-4" />
          <AlertTitle>Group unavailable</AlertTitle>
          <AlertDescription>{warning ?? 'The selected group could not be loaded.'}</AlertDescription>
        </Alert>
      </div>
    );
  }

  const members = membersResult.members;
  const activeMembers = members.filter((member) => member.status === 'Active').length;
  const pendingMembers = members.filter((member) => member.status === 'Pending').length;
  const availableUsers = directory.members.filter((row) => !members.some((member) => member.userId === row.member.id));
  const approverId = directory.currentUserId ?? '';

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div className="flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-3xl font-bold tracking-tight">{group.name}</h1>
            <Badge variant={group.status === 'Active' ? 'default' : 'secondary'}>{group.status}</Badge>
            <Badge variant="outline">{group.visibility}</Badge>
          </div>
          <p className="text-muted-foreground">{group.description || 'Manage this community group, its membership, and moderation roles.'}</p>
        </div>
        <form action={archiveCommunityGroup}>
          <input type="hidden" name="groupId" value={group.id} />
          <Button type="submit" variant="outline">
            <Archive className="mr-2 size-4" />
            Archive group
          </Button>
        </form>
      </div>

      {query?.message ? (
        <Alert>
          <ShieldCheck className="size-4" />
          <AlertTitle>Group updated</AlertTitle>
          <AlertDescription>{query.message}</AlertDescription>
        </Alert>
      ) : null}

      {warning ? (
        <Alert variant="destructive">
          <ShieldCheck className="size-4" />
          <AlertTitle>Group warning</AlertTitle>
          <AlertDescription>{warning}</AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Members</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{members.length}</div>
            <CardDescription>{activeMembers} active</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Pending</CardTitle>
            <ShieldCheck className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{pendingMembers}</div>
            <CardDescription>Waiting for approval or rejection</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Type</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{group.type}</div>
            <CardDescription>{group.visibility} membership</CardDescription>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
        <Card>
          <CardHeader>
            <CardTitle>Group settings</CardTitle>
            <CardDescription>Edit the group identity and visibility rules.</CardDescription>
          </CardHeader>
          <CardContent>
            <form action={updateCommunityGroup} className="space-y-4">
              <input type="hidden" name="groupId" value={group.id} />
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="group-name">Name</Label>
                  <Input id="group-name" name="name" defaultValue={group.name} required />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="group-type">Type</Label>
                  <Select name="type" defaultValue={group.type}>
                    <SelectTrigger id="group-type">
                      <SelectValue placeholder="Select type" />
                    </SelectTrigger>
                    <SelectContent>
                      {GROUP_TYPES.map(([value, label]) => (
                        <SelectItem key={value} value={value}>
                          {label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_220px]">
                <div className="space-y-2">
                  <Label htmlFor="group-description">Description</Label>
                  <Textarea id="group-description" name="description" defaultValue={group.description} rows={3} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="group-visibility">Visibility</Label>
                  <Select name="visibility" defaultValue={group.visibility}>
                    <SelectTrigger id="group-visibility">
                      <SelectValue placeholder="Select visibility" />
                    </SelectTrigger>
                    <SelectContent>
                      {GROUP_VISIBILITIES.map(([value, label]) => (
                        <SelectItem key={value} value={value}>
                          {label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <Button type="submit">Save group</Button>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Add group member</CardTitle>
            <CardDescription>Add an existing platform user to this group.</CardDescription>
          </CardHeader>
          <CardContent>
            <form action={addCommunityGroupMember} className="space-y-4">
              <input type="hidden" name="groupId" value={group.id} />
              <div className="space-y-2">
                <Label htmlFor="member-user">User</Label>
                {availableUsers.length > 0 ? (
                  <Select name="userId" defaultValue={availableUsers[0]?.member.id}>
                    <SelectTrigger id="member-user">
                      <SelectValue placeholder="Select user" />
                    </SelectTrigger>
                    <SelectContent>
                      {availableUsers.map((row) => (
                        <SelectItem key={row.member.id} value={row.member.id}>
                          {row.member.displayName} · {row.member.email}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <Input id="member-user" name="userId" placeholder="User ID" required />
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="member-role">Group role</Label>
                <Select name="role" defaultValue="Member">
                  <SelectTrigger id="member-role">
                    <SelectValue placeholder="Select role" />
                  </SelectTrigger>
                  <SelectContent>
                    {GROUP_ROLES.map((role) => (
                      <SelectItem key={role} value={role}>
                        {role}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button type="submit" className="w-full">
                <UserPlus className="mr-2 size-4" />
                Add member
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Group members</CardTitle>
          <CardDescription>Manage owners, admins, moderators, pending requests, and removals.</CardDescription>
        </CardHeader>
        <CardContent>
          {members.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No members are attached to this group yet.</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Member</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Requested</TableHead>
                  <TableHead className="min-w-72 text-right">Role and actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {members.map((member) => (
                  <TableRow key={member.id}>
                    <TableCell>
                      <div className="flex flex-col">
                        <span className="font-medium">{member.displayName}</span>
                        <span className="text-xs text-muted-foreground">{member.email || member.userId}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={statusVariant(member.status)}>{member.status}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(member.requestedAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <div className="flex flex-wrap items-center justify-end gap-2">
                        {member.status === 'Pending' ? (
                          <>
                            <form action={approveCommunityGroupMember}>
                              <input type="hidden" name="groupId" value={group.id} />
                              <input type="hidden" name="userId" value={member.userId} />
                              <input type="hidden" name="approvedByUserId" value={approverId} />
                              <Button type="submit" size="sm">
                                Approve
                              </Button>
                            </form>
                            <form action={rejectCommunityGroupMember}>
                              <input type="hidden" name="groupId" value={group.id} />
                              <input type="hidden" name="userId" value={member.userId} />
                              <Button type="submit" size="sm" variant="outline">
                                Reject
                              </Button>
                            </form>
                          </>
                        ) : null}
                        <form action={changeCommunityGroupMemberRole} className="flex items-center justify-end gap-2">
                          <input type="hidden" name="groupId" value={group.id} />
                          <input type="hidden" name="userId" value={member.userId} />
                          <Select name="role" defaultValue={member.role}>
                            <SelectTrigger className="w-36">
                              <SelectValue placeholder="Role" />
                            </SelectTrigger>
                            <SelectContent>
                              {GROUP_ROLES.map((role) => (
                                <SelectItem key={role} value={role}>
                                  {role}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                          <Button type="submit" size="sm" variant="outline">
                            Update role
                          </Button>
                        </form>
                        <form action={removeCommunityGroupMember}>
                          <input type="hidden" name="groupId" value={group.id} />
                          <input type="hidden" name="userId" value={member.userId} />
                          <Button type="submit" size="sm" variant="outline">
                            Remove
                          </Button>
                        </form>
                      </div>
                    </TableCell>
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
