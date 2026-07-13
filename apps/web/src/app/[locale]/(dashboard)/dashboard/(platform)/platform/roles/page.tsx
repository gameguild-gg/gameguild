import {
  COMMUNITY_ACCESS_ROLES,
  PLATFORM_PERMISSION_MATRIX,
  getMemberAccessDirectory,
  getPermissionTemplates,
  getPlatformRoles,
  getUserPlatformRoles,
  type PlatformRole,
} from '@/lib/community';
import { updateMemberAccessRole } from '@/lib/community/actions/member-access';
import { assignPlatformRole, createPlatformRole, deletePlatformRole, removePlatformRole, updatePlatformRole } from '@/lib/community/actions/roles';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Check, Crown, KeyRound, Plus, ShieldCheck, Trash2, Users } from 'lucide-react';
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

function PermissionCheckboxes({ role }: { role?: PlatformRole }) {
  const selected = new Set(role?.permissions ?? []);

  return (
    <div className="grid gap-4 md:grid-cols-2">
      {PLATFORM_PERMISSION_MATRIX.map((group) => (
        <div key={group.area} className="rounded-lg border p-3">
          <div className="mb-3">
            <p className="font-medium">{group.area}</p>
            <p className="text-xs text-muted-foreground">{group.description}</p>
          </div>
          <div className="space-y-2">
            {group.permissions.map((permission) => (
              <label key={permission.value} className="flex items-start gap-2 text-sm">
                <input
                  type="checkbox"
                  name="permissions"
                  value={permission.value}
                  defaultChecked={selected.has(permission.value)}
                  className="mt-1 size-4 rounded border-border"
                />
                <span>
                  <span className="block font-medium">{permission.label}</span>
                  <span className="block text-xs text-muted-foreground">{permission.value}</span>
                </span>
              </label>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function CreateRoleDialog() {
  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button>
          <Plus className="mr-2 size-4" />
          Create role
        </Button>
      </DialogTrigger>
      <DialogContent className="max-h-[88vh] max-w-4xl overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create role</DialogTitle>
          <DialogDescription>Create a global platform role and assign its permission matrix.</DialogDescription>
        </DialogHeader>
        <form action={createPlatformRole} className="space-y-4">
          <input type="hidden" name="tenantId" value="" />
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="role-name">Name</Label>
              <Input id="role-name" name="name" required placeholder="Course Operator" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="role-description">Description</Label>
              <Input id="role-description" name="description" placeholder="Can run learning operations" />
            </div>
          </div>
          <PermissionCheckboxes />
          <DialogFooter>
            <Button type="submit">Create role</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function CustomRoleCard({ role }: { role: PlatformRole }) {
  return (
    <div className="rounded-lg border p-4">
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-semibold">{role.name}</h3>
            <Badge variant={role.isActive ? 'default' : 'secondary'}>{role.isActive ? 'Active' : 'Inactive'}</Badge>
            <Badge variant="outline">{role.tenantId ? 'Workspace scoped' : 'Global'}</Badge>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">{role.description || 'No description provided.'}</p>
        </div>
        <form action={deletePlatformRole}>
          <input type="hidden" name="roleId" value={role.id} />
          <input type="hidden" name="name" value={role.name} />
          <Button type="submit" variant="outline" size="sm">
            <Trash2 className="mr-2 size-4" />
            Delete
          </Button>
        </form>
      </div>
      <form action={updatePlatformRole} className="space-y-4">
        <input type="hidden" name="roleId" value={role.id} />
        <div className="grid gap-4 md:grid-cols-[240px_minmax(0,1fr)_120px]">
          <div className="space-y-2">
            <Label htmlFor={`role-${role.id}-name`}>Name</Label>
            <Input id={`role-${role.id}-name`} name="name" defaultValue={role.name} required />
          </div>
          <div className="space-y-2">
            <Label htmlFor={`role-${role.id}-description`}>Description</Label>
            <Textarea id={`role-${role.id}-description`} name="description" defaultValue={role.description} rows={2} />
          </div>
          <label className="flex items-end gap-2 pb-2 text-sm">
            <input type="checkbox" name="isActive" defaultChecked={role.isActive} className="size-4 rounded border-border" />
            Active
          </label>
        </div>
        <PermissionCheckboxes role={role} />
        <Button type="submit" variant="outline">
          Save role
        </Button>
      </form>
    </div>
  );
}

export default async function Page({ searchParams }: Props): Promise<React.JSX.Element> {
  const query = await searchParams;
  const [directory, platformRolesResult, permissionTemplatesResult] = await Promise.all([
    getMemberAccessDirectory({ limit: 100 }),
    getPlatformRoles({ includeInactive: true }),
    getPermissionTemplates(),
  ]);
  const members = directory.members;
  const platformRoles = platformRolesResult.roles;
  const templates = permissionTemplatesResult.templates;
  const userRoleResults = await Promise.all(
    members.map(async (row) => [row.member.id, await getUserPlatformRoles(row.member.id)] as const),
  );
  const userRoles = new Map(userRoleResults.map(([userId, result]) => [userId, result.roles]));
  const userRoleError = userRoleResults.find(([, result]) => result.error)?.[1].error;
  const total = directory.total;
  const superAdmins = members.filter((row) => row.isSuperAdmin).length;
  const platformAdmins = members.filter((row) => row.role === 'TenantAdmin' || row.role === 'Owner').length;
  const assignedMembers = members.filter((row) => row.primaryMembership?.tenantId).length;
  const warning = query?.error ?? directory.error ?? platformRolesResult.error ?? permissionTemplatesResult.error ?? userRoleError;

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div className="flex flex-col gap-2">
          <h1 className="text-3xl font-bold tracking-tight">Roles</h1>
          <p className="text-muted-foreground">Manage workspace access, custom platform roles, and permission grants.</p>
        </div>
        <CreateRoleDialog />
      </div>

      {query?.message ? (
        <Alert>
          <ShieldCheck className="size-4" />
          <AlertTitle>Role updated</AlertTitle>
          <AlertDescription>{query.message}</AlertDescription>
        </Alert>
      ) : null}

      {warning ? (
        <Alert variant="destructive">
          <ShieldCheck className="size-4" />
          <AlertTitle>Role warning</AlertTitle>
          <AlertDescription>{warning}</AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-4 md:grid-cols-4">
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
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Role catalog</CardTitle>
            <KeyRound className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{platformRoles.length}</div>
            <CardDescription>Loaded from /v1/roles</CardDescription>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_380px]">
        <Card>
          <CardHeader>
            <CardTitle>Role assignments</CardTitle>
            <CardDescription>Update workspace access roles for each member.</CardDescription>
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
                        {row.isSuperAdmin && superAdmins <= 1 ? (
                          <span className="text-sm text-muted-foreground">Transfer super admin before changing this account.</span>
                        ) : row.primaryMembership?.tenantId ? (
                          <form action={updateMemberAccessRole} className="ml-auto flex items-center justify-end gap-2">
                            <input type="hidden" name="userId" value={row.member.id} />
                            <input type="hidden" name="tenantId" value={row.primaryMembership.tenantId} />
                            <Select name="role" defaultValue={row.role}>
                              <SelectTrigger className="w-44">
                                <SelectValue placeholder="Select role" />
                              </SelectTrigger>
                              <SelectContent>
                                <SelectGroup>
                                  <SelectLabel>Workspace access roles</SelectLabel>
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
            <CardTitle>Workspace access roles</CardTitle>
            <CardDescription>Built-in membership presets still used by tenants and workspaces.</CardDescription>
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

      <Card>
        <CardHeader>
          <CardTitle>Custom role assignments</CardTitle>
          <CardDescription>Grant or remove permission-based roles independently from workspace access.</CardDescription>
        </CardHeader>
        <CardContent>
          {members.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No users are available for role assignment.</div>
          ) : platformRoles.filter((role) => role.isActive).length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">Create an active custom role before assigning permissions to users.</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User</TableHead>
                  <TableHead>Assigned custom roles</TableHead>
                  <TableHead className="min-w-80 text-right">Grant role</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {members.map((row) => {
                  const assignedRoles = userRoles.get(row.member.id) ?? [];
                  const assignedIds = new Set(assignedRoles.map((role) => role.id));
                  const availableRoles = platformRoles.filter((role) => role.isActive && !assignedIds.has(role.id));

                  return (
                    <TableRow key={`custom-${row.member.id}`}>
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="font-medium">{row.member.displayName}</span>
                          <span className="text-xs text-muted-foreground">{row.member.email}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        {assignedRoles.length === 0 ? (
                          <span className="text-sm text-muted-foreground">No custom roles</span>
                        ) : (
                          <div className="flex flex-wrap gap-2">
                            {assignedRoles.map((role) => (
                              <form key={role.id} action={removePlatformRole}>
                                <input type="hidden" name="userId" value={row.member.id} />
                                <input type="hidden" name="roleId" value={role.id} />
                                <input type="hidden" name="roleName" value={role.name} />
                                <Button
                                  type="submit"
                                  variant="outline"
                                  size="sm"
                                  aria-label={`Remove ${role.name} from ${row.member.displayName}`}
                                  title={`Remove ${role.name}`}
                                >
                                  {role.name}
                                  <Trash2 className="ml-2 size-3" />
                                </Button>
                              </form>
                            ))}
                          </div>
                        )}
                      </TableCell>
                      <TableCell>
                        {availableRoles.length > 0 ? (
                          <form action={assignPlatformRole} className="ml-auto flex items-center justify-end gap-2">
                            <input type="hidden" name="userId" value={row.member.id} />
                            <Select name="roleId" defaultValue={availableRoles[0]?.id}>
                              <SelectTrigger className="w-52" aria-label={`Custom role for ${row.member.displayName}`}>
                                <SelectValue placeholder="Select custom role" />
                              </SelectTrigger>
                              <SelectContent>
                                {availableRoles.map((role) => (
                                  <SelectItem key={role.id} value={role.id}>
                                    {role.name}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                            <Button type="submit" variant="outline" size="sm">
                              Assign custom role
                            </Button>
                          </form>
                        ) : (
                          <span className="block text-right text-sm text-muted-foreground">All active roles assigned</span>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Custom roles</CardTitle>
          <CardDescription>Create and edit the permission sets used for platform administration.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {platformRoles.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No custom roles have been created yet.</div>
          ) : (
            platformRoles.map((role) => <CustomRoleCard key={role.id} role={role} />)
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Permission matrix</CardTitle>
          <CardDescription>Visual audit of permissions granted to each custom role.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {platformRoles.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">Create a role to populate the permission matrix.</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Permission</TableHead>
                    {platformRoles.map((role) => (
                      <TableHead key={role.id} className="min-w-36 text-center">
                        {role.name}
                      </TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {PLATFORM_PERMISSION_MATRIX.flatMap((group) =>
                    group.permissions.map((permission) => (
                      <TableRow key={`${group.area}-${permission.value}`}>
                        <TableCell>
                          <div className="flex flex-col">
                            <span className="font-medium">{permission.label}</span>
                            <span className="text-xs text-muted-foreground">{group.area} · {permission.value}</span>
                          </div>
                        </TableCell>
                        {platformRoles.map((role) => (
                          <TableCell key={`${role.id}-${permission.value}`} className="text-center">
                            {role.permissions.includes(permission.value) ? (
                              <Badge variant="default" className="mx-auto">
                                <Check className="mr-1 size-3" />
                                Granted
                              </Badge>
                            ) : (
                              <span className="text-sm text-muted-foreground">—</span>
                            )}
                          </TableCell>
                        ))}
                      </TableRow>
                    )),
                  )}
                </TableBody>
              </Table>
            </div>
          )}

          {templates.length > 0 ? (
            <div className="space-y-3">
              <h3 className="font-semibold">Permission templates</h3>
              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                {templates.map((template) => (
                  <div key={template.id} className="rounded-lg border p-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-medium">{template.name}</p>
                        <p className="text-xs text-muted-foreground">{template.category}</p>
                      </div>
                      <Badge variant={template.isActive ? 'default' : 'secondary'}>{template.permissions.length} permissions</Badge>
                    </div>
                    <p className="mt-2 text-sm text-muted-foreground">{template.description}</p>
                  </div>
                ))}
              </div>
            </div>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
