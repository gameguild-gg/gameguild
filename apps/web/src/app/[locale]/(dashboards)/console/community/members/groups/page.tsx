import React from 'react';
import { Link } from '@/i18n/navigation';
import { getGroups } from '@/lib/community';
import { createCommunityGroup } from '@/lib/community/actions/groups';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Button } from '@game-guild/ui/components/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Textarea } from '@game-guild/ui/components/textarea';
import { ArrowRight, Plus, ShieldCheck, Users } from 'lucide-react';

interface Props {
  searchParams?: Promise<{
    message?: string;
    error?: string;
  }>;
}

export default async function Page({ searchParams }: Props): Promise<React.JSX.Element> {
  const query = await searchParams;
  const { groups, total } = await getGroups({ limit: 50 });

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Groups</h1>
          <p className="text-muted-foreground">Manage community groups and teams.</p>
        </div>
        <Dialog>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 size-4" />
              Create Group
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Create group</DialogTitle>
              <DialogDescription>Organize members by interest, project team, cohort, institution, or game jam.</DialogDescription>
            </DialogHeader>
            <form action={createCommunityGroup} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="group-name">Name</Label>
                <Input id="group-name" name="name" placeholder="Mentors, Capstone Team, Pixel Art Study Group" required />
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="group-type">Type</Label>
                  <Select name="type" defaultValue="InterestCommunity">
                    <SelectTrigger id="group-type">
                      <SelectValue placeholder="Select type" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="InterestCommunity">Interest community</SelectItem>
                      <SelectItem value="StudyGroup">Study group</SelectItem>
                      <SelectItem value="ProjectTeam">Project team</SelectItem>
                      <SelectItem value="CourseCohort">Course cohort</SelectItem>
                      <SelectItem value="Institution">Institution</SelectItem>
                      <SelectItem value="GameJamTeam">Game jam team</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="group-visibility">Visibility</Label>
                  <Select name="visibility" defaultValue="Public">
                    <SelectTrigger id="group-visibility">
                      <SelectValue placeholder="Select visibility" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Public">Public</SelectItem>
                      <SelectItem value="Private">Private</SelectItem>
                      <SelectItem value="InviteOnly">Invite only</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="group-description">Description</Label>
                <Textarea id="group-description" name="description" rows={4} placeholder="Purpose, membership criteria, or operating notes." />
              </div>
              <DialogFooter>
                <Button type="submit">Create group</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {query?.message ? (
        <Alert>
          <ShieldCheck className="size-4" />
          <AlertTitle>Group created</AlertTitle>
          <AlertDescription>{query.message}</AlertDescription>
        </Alert>
      ) : null}

      {query?.error ? (
        <Alert variant="destructive">
          <ShieldCheck className="size-4" />
          <AlertTitle>Group could not be created</AlertTitle>
          <AlertDescription>{query.error}</AlertDescription>
        </Alert>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>All Groups</CardTitle>
          <CardDescription>{total > 0 ? `${total} groups created` : 'No groups created yet'}</CardDescription>
        </CardHeader>
        <CardContent>
          {groups.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <Users className="mb-4 size-12 text-muted-foreground" />
              <h3 className="text-lg font-semibold">No groups yet</h3>
              <p className="text-sm text-muted-foreground">Create groups to organize your community members by interests, roles, or projects.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Members</TableHead>
                  <TableHead>Visibility</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Manage</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {groups.map((group) => (
                  <TableRow key={group.id}>
                    <TableCell className="font-medium">
                      <Link href={`/console/community/members/groups/${group.id}`} className="hover:underline">
                        {group.name}
                      </Link>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{group.type}</Badge>
                    </TableCell>
                    <TableCell className="max-w-xs truncate text-sm text-muted-foreground">{group.description}</TableCell>
                    <TableCell>
                      <div className="flex flex-col">
                        <span>{group.memberCount}</span>
                        {group.pendingMemberCount > 0 ? <span className="text-xs text-amber-600">{group.pendingMemberCount} pending</span> : null}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={group.isPublic ? 'default' : 'secondary'}>{group.visibility}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={group.status === 'Active' ? 'default' : 'secondary'}>{group.status}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(group.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-right">
                      <Button asChild variant="outline" size="sm">
                        <Link href={`/console/community/members/groups/${group.id}`}>
                          Manage
                          <ArrowRight className="ml-2 size-4" />
                        </Link>
                      </Button>
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
