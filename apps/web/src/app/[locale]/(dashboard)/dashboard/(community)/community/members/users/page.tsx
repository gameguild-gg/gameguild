import { Link } from '@/i18n/navigation';
import { COMMUNITY_ACCESS_ROLES, getMemberAccessDirectory } from '@/lib/community';
import { updateMemberAccessRole } from '@/lib/community/actions/member-access';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Crown, ShieldCheck, UserPlus, Users } from 'lucide-react';
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

export default async function Page({ searchParams }: Props): Promise<React.JSX.Element> {
  const query = await searchParams;
  const directory = await getMemberAccessDirectory({ limit: 50 });
  const members = directory.members;
  const total = directory.total;
  const superAdmins = members.filter((row) => row.isSuperAdmin).length;
  const tenantAdmins = members.filter((row) => row.role === 'TenantAdmin' || row.role === 'Owner').length;

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Users and roles</h1>
          <p className="text-muted-foreground">Manage registered members, promote admins, and demote access from one dashboard.</p>
        </div>
        <Button>
          <UserPlus className="mr-2 size-4" />
          Invite User
        </Button>
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
            <CardTitle className="text-sm font-medium">Platform admins</CardTitle>
            <ShieldCheck className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{tenantAdmins}</div>
            <CardDescription>Tenant owner/admin access</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Super admins</CardTitle>
            <Crown className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{superAdmins}</div>
            <CardDescription>Full platform authority</CardDescription>
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
                  <TableHead>Status</TableHead>
                  <TableHead>Joined</TableHead>
                  <TableHead>Last Active</TableHead>
                  <TableHead className="min-w-60 text-right">Promote / demote</TableHead>
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
                      <Badge variant={row.member.status === 'active' ? 'default' : row.member.status === 'banned' ? 'destructive' : 'secondary'}>{row.member.status}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(row.member.joinedAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(row.member.lastActiveAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-right">
                      {row.primaryMembership?.tenantId ? (
                        <form action={updateMemberAccessRole} className="ml-auto flex items-center justify-end gap-2">
                          <input type="hidden" name="userId" value={row.member.id} />
                          <input type="hidden" name="tenantId" value={row.primaryMembership.tenantId} />
                          <Select name="role" defaultValue={row.role}>
                            <SelectTrigger className="w-40">
                              <SelectValue placeholder="Select role" />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectGroup>
                                <SelectLabel>Access role</SelectLabel>
                                {COMMUNITY_ACCESS_ROLES.map((role) => (
                                  <SelectItem key={role.value} value={role.value}>
                                    {role.label}
                                  </SelectItem>
                                ))}
                              </SelectGroup>
                            </SelectContent>
                          </Select>
                          <Button type="submit" variant="outline" size="sm">
                            Save
                          </Button>
                        </form>
                      ) : (
                        <span className="text-sm text-muted-foreground">No membership</span>
                      )}
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
