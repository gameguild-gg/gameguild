import React from 'react';
import { getGroups } from '@/lib/community';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Button } from '@game-guild/ui/components/button';
import { Plus, Users } from 'lucide-react';

export default async function Page(): Promise<React.JSX.Element> {
  const { groups, total } = await getGroups({ limit: 50 });

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Groups</h1>
          <p className="text-muted-foreground">Manage community groups and teams.</p>
        </div>
        <Button>
          <Plus className="mr-2 size-4" />
          Create Group
        </Button>
      </div>

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
                  <TableHead>Description</TableHead>
                  <TableHead>Members</TableHead>
                  <TableHead>Visibility</TableHead>
                  <TableHead>Created</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {groups.map((group) => (
                  <TableRow key={group.id}>
                    <TableCell className="font-medium">{group.name}</TableCell>
                    <TableCell className="max-w-xs truncate text-sm text-muted-foreground">{group.description}</TableCell>
                    <TableCell>{group.memberCount}</TableCell>
                    <TableCell>
                      <Badge variant={group.isPublic ? 'default' : 'secondary'}>{group.isPublic ? 'Public' : 'Private'}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(group.createdAt).toLocaleDateString()}</TableCell>
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
