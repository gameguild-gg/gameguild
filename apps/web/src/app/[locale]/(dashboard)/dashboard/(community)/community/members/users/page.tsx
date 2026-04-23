import { Link } from '@/i18n/navigation';
import { getMembers } from '@/lib/community';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { UserPlus } from 'lucide-react';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const { members, total } = await getMembers({ limit: 50 });

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Users</h1>
          <p className="text-muted-foreground">Manage registered users and their roles.</p>
        </div>
        <Button>
          <UserPlus className="mr-2 size-4" />
          Invite User
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Users</CardTitle>
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
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Joined</TableHead>
                  <TableHead>Last Active</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {members.map((member) => (
                  <TableRow key={member.id} className="cursor-pointer hover:bg-muted/50">
                    <TableCell>
                      <Link href={`/dashboard/community/members/users/${member.id}`} className="flex flex-col">
                        <span className="font-medium">{member.displayName}</span>
                        <span className="text-xs text-muted-foreground">@{member.username}</span>
                      </Link>
                    </TableCell>
                    <TableCell className="text-sm">{member.email}</TableCell>
                    <TableCell>
                      <Badge variant={member.role === 'admin' ? 'default' : member.role === 'moderator' ? 'secondary' : 'outline'}>{member.role}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={member.status === 'active' ? 'default' : member.status === 'banned' ? 'destructive' : 'secondary'}>{member.status}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(member.joinedAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(member.lastActiveAt).toLocaleDateString()}</TableCell>
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
