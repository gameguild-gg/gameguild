'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Trash2, Plus, Shield, Users, Settings, Eye } from 'lucide-react';
import { useModulePermissions } from '@/hooks/useModulePermissions';
import { ModuleRole, UserRoleAssignment, ModuleType, RoleLevel } from '@/types/modulePermissions';

interface UserRoleManagerProps {
  userId: string;
  tenantId?: string;
  onRoleChanged?: () => void;
}

export function UserRoleManager({ userId, tenantId, onRoleChanged }: UserRoleManagerProps) {
  const { userRoles, availableRoles, testingLabRoles, assignRole, revokeRole, loading, rolesLoading } = useModulePermissions(userId, tenantId);

  const [selectedRoleId, setSelectedRoleId] = useState<string>('');
  const [isAssigning, setIsAssigning] = useState(false);
  const [isRevoking, setIsRevoking] = useState<string | null>(null);
  const [showAssignDialog, setShowAssignDialog] = useState(false);

  const handleAssignRole = async () => {
    if (!selectedRoleId) return;

    setIsAssigning(true);
    try {
      await assignRole(selectedRoleId, tenantId);
      setShowAssignDialog(false);
      setSelectedRoleId('');
      onRoleChanged?.();
    } catch (error) {
      console.error('Failed to assign role:', error);
    } finally {
      setIsAssigning(false);
    }
  };

  const handleRevokeRole = async (roleAssignmentId: string) => {
    setIsRevoking(roleAssignmentId);
    try {
      await revokeRole(roleAssignmentId);
      onRoleChanged?.();
    } catch (error) {
      console.error('Failed to revoke role:', error);
    } finally {
      setIsRevoking(null);
    }
  };

  const getRoleDisplayInfo = (roleId: string) => {
    const role = availableRoles.find((r) => r.id === roleId);
    return {
      name: role?.displayName || roleId,
      description: role?.description || '',
      level: role?.level || RoleLevel.Global,
      module: role?.module || ModuleType.Users,
    };
  };

  const getRoleLevelColor = (level: RoleLevel) => {
    switch (level) {
      case RoleLevel.Global:
        return 'bg-red-100 text-red-800';
      case RoleLevel.Tenant:
        return 'bg-blue-100 text-blue-800';
      case RoleLevel.Module:
        return 'bg-green-100 text-green-800';
      case RoleLevel.Resource:
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getModuleIcon = (module: ModuleType) => {
    switch (module) {
      case ModuleType.TestingLab:
        return <Shield className="h-4 w-4" />;
      case ModuleType.UserManagement:
        return <Users className="h-4 w-4" />;
      case ModuleType.Configuration:
        return <Settings className="h-4 w-4" />;
      default:
        return <Eye className="h-4 w-4" />;
    }
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Loading...</CardTitle>
        </CardHeader>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Current Roles */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            User Roles
          </CardTitle>
          <CardDescription>Manage the roles assigned to this user</CardDescription>
        </CardHeader>
        <CardContent>
          {userRoles.length === 0 ? (
            <Alert>
              <AlertDescription>No roles assigned to this user. Assign roles to grant permissions.</AlertDescription>
            </Alert>
          ) : (
            <div className="space-y-3">
              {userRoles.map((assignment) => {
                const roleInfo = getRoleDisplayInfo(assignment.roleId);
                return (
                  <div key={assignment.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center gap-3">
                      {getModuleIcon(roleInfo.module)}
                      <div>
                        <div className="font-medium">{roleInfo.name}</div>
                        <div className="text-sm text-muted-foreground">{roleInfo.description}</div>
                        <div className="flex items-center gap-2 mt-1">
                          <Badge className={getRoleLevelColor(roleInfo.level)}>{roleInfo.level}</Badge>
                          <Badge variant="outline">{roleInfo.module}</Badge>
                          {assignment.expiresAt && <Badge variant="secondary">Expires: {new Date(assignment.expiresAt).toLocaleDateString()}</Badge>}
                        </div>
                      </div>
                    </div>
                    <Button variant="outline" size="sm" onClick={() => handleRevokeRole(assignment.id)} disabled={isRevoking === assignment.id} className="text-red-600 hover:text-red-700">
                      {isRevoking === assignment.id ? (
                        'Removing...'
                      ) : (
                        <>
                          <Trash2 className="h-4 w-4 mr-2" />
                          Remove
                        </>
                      )}
                    </Button>
                  </div>
                );
              })}
            </div>
          )}

          <div className="mt-4">
            <Dialog open={showAssignDialog} onOpenChange={setShowAssignDialog}>
              <DialogTrigger asChild>
                <Button>
                  <Plus className="h-4 w-4 mr-2" />
                  Assign Role
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Assign Role</DialogTitle>
                  <DialogDescription>Select a role to assign to this user</DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <Select value={selectedRoleId} onValueChange={setSelectedRoleId}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select a role" />
                    </SelectTrigger>
                    <SelectContent>
                      {availableRoles.map((role) => (
                        <SelectItem key={role.id} value={role.id}>
                          <div className="flex items-center gap-2">
                            {getModuleIcon(role.module)}
                            <div>
                              <div className="font-medium">{role.displayName}</div>
                              <div className="text-xs text-muted-foreground">
                                {role.level} • {role.module}
                              </div>
                            </div>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <DialogFooter>
                  <Button variant="outline" onClick={() => setShowAssignDialog(false)}>
                    Cancel
                  </Button>
                  <Button onClick={handleAssignRole} disabled={!selectedRoleId || isAssigning}>
                    {isAssigning ? 'Assigning...' : 'Assign Role'}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          </div>
        </CardContent>
      </Card>

      {/* Testing Lab Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Testing Lab Roles
          </CardTitle>
          <CardDescription>Quick role assignment for Testing Lab module</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3">
            {testingLabRoles.map((role) => (
              <Button
                key={role.id}
                variant="outline"
                className="h-auto p-4 flex flex-col items-start"
                onClick={() => {
                  setSelectedRoleId(role.id);
                  setShowAssignDialog(true);
                }}
                disabled={userRoles.some((ur) => ur.roleId === role.id)}
              >
                <div className="font-medium">{role.displayName}</div>
                <div className="text-sm text-muted-foreground text-left">{role.description}</div>
                {userRoles.some((ur) => ur.roleId === role.id) && (
                  <Badge variant="secondary" className="mt-2">
                    Already Assigned
                  </Badge>
                )}
              </Button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Role Reference */}
      <Card>
        <CardHeader>
          <CardTitle>Role Reference</CardTitle>
          <CardDescription>Understanding permission levels and modules</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <h4 className="font-medium mb-2">Permission Levels</h4>
              <div className="grid grid-cols-2 gap-2 text-sm">
                <div className="flex items-center gap-2">
                  <Badge className="bg-red-100 text-red-800">Global</Badge>
                  <span>System-wide access</span>
                </div>
                <div className="flex items-center gap-2">
                  <Badge className="bg-blue-100 text-blue-800">Tenant</Badge>
                  <span>Organization-wide access</span>
                </div>
                <div className="flex items-center gap-2">
                  <Badge className="bg-green-100 text-green-800">Module</Badge>
                  <span>Feature-specific access</span>
                </div>
                <div className="flex items-center gap-2">
                  <Badge className="bg-purple-100 text-purple-800">Resource</Badge>
                  <span>Individual item access</span>
                </div>
              </div>
            </div>

            <div>
              <h4 className="font-medium mb-2">Testing Lab Roles Summary</h4>
              <div className="space-y-2 text-sm">
                <div>
                  <strong>Admin:</strong> Full control - create, edit, delete, configure
                </div>
                <div>
                  <strong>Manager:</strong> Create and edit sessions, manage requests, no delete
                </div>
                <div>
                  <strong>Coordinator:</strong> Limited session editing, handle submissions
                </div>
                <div>
                  <strong>Tester:</strong> Participate in sessions, provide feedback
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

export default UserRoleManager;
