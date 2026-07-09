import { COMMUNITY_ACCESS_ROLES, getMemberAccessDirectory } from '@/lib/community';
import { updateMemberAccessRole } from '@/lib/community/actions/member-access';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Crown, ShieldCheck, Users } from 'lucide-react';
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

function getRoleLabel(value: string) {
  return COMMUNITY_ACCESS_ROLES.find((role) => role.value === value)?.label ?? value;
}

export default async function Page({ searchParams }: Props): Promise<React.JSX.Element> {
  const query = await searchParams;
  const directory = await getMemberAccessDirectory({ limit: 100 });
  const members = directory.members;
  const total = directory.total;
  const superAdmins = members.filter((row) => row.isSuperAdmin).length;
  const platformAdmins = members.filter((row) => row.role === 'TenantAdmin' || row.role === 'Owner').length;
  const assignedMembers = members.filter((row) => row.primaryMembership?.tenantId).length;

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight">Roles</h1>
        <p className="text-muted-foreground">Manage platform access levels, super admins, and tenant operator permissions.</p>
      </div>

      {query?.message ? (
        <Alert>
          <ShieldCheck className="size-4" />
          <AlertTitle>Role updated</AlertTitle>
          <AlertDescription>{query.message}</AlertDescription>
        </Alert>
      ) : null}

      {query?.error || directory.error ? (
        <Alert variant="destructive">
          <ShieldCheck className="size-4" />
          <AlertTitle>Role warning</AlertTitle>
          <AlertDescription>{query?.error ?? directory.error}</AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Assigned users</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{assignedMembers}</div>
            <CardDescription>{total} users loaded from identity</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Platform admins</CardTitle>
            <ShieldCheck className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{platformAdmins}</div>
            <CardDescription>Tenant admin or owner access</CardDescription>
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

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_360px]">
        <Card>
          <CardHeader>
            <CardTitle>Role assignments</CardTitle>
            <CardDescription>Update access from a dedicated platform-management view.</CardDescription>
          </CardHeader>
          <CardContent>
            {members.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 text-center">
                <ShieldCheck className="mb-4 size-12 text-muted-foreground" />
                <h3 className="text-lg font-semibold">No users available</h3>
                <p className="text-sm text-muted-foreground">Users will appear here once the identity directory is available.</p>
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Workspace</TableHead>
                    <TableHead>Current role</TableHead>
                    <TableHead className="min-w-64 text-right">Assigned role</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {members.map((row) => (
                    <TableRow key={row.member.id}>
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="font-medium">{row.member.displayName}</span>
                          <span className="text-xs text-muted-foreground">{row.member.email}</span>
                        </div>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {row.primaryMembership?.tenantName ?? row.primaryMembership?.tenantSlug ?? 'No active workspace'}
                        {row.membershipLoadError ? <span className="block text-xs text-destructive">{row.membershipLoadError}</span> : null}
                      </TableCell>
                      <TableCell>
                        <Badge variant={getRoleBadgeVariant(row.role)}>{getRoleLabel(row.role)}</Badge>
                        {row.isCurrentUser ? <Badge variant="outline" className="ml-2">You</Badge> : null}
                      </TableCell>
                      <TableCell className="text-right">
                        {row.primaryMembership?.tenantId ? (
                          <form action={updateMemberAccessRole} className="ml-auto flex items-center justify-end gap-2">
                            <input type="hidden" name="userId" value={row.member.id} />
                            <input type="hidden" name="tenantId" value={row.primaryMembership.tenantId} />
                            <Select name="role" defaultValue={row.role}>
                              <SelectTrigger className="w-44">
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

        <Card>
          <CardHeader>
            <CardTitle>Role catalog</CardTitle>
            <CardDescription>Available platform access presets.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {COMMUNITY_ACCESS_ROLES.map((role) => (
              <div key={role.value} className="rounded-lg border p-3">
                <div className="flex items-center justify-between gap-3">
                  <span className="font-medium">{role.label}</span>
                  <Badge variant={getRoleBadgeVariant(role.value)}>{role.value}</Badge>
                </div>
                <p className="mt-2 text-sm text-muted-foreground">{role.description}</p>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
