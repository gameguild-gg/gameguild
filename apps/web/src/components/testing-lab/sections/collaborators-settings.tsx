"use client";
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { RoleTemplate } from '@/lib/actions/testing-lab-roles';
import { assignUserRoleAction } from '@/lib/actions/testing-lab-user-roles';
import { searchTestingLabUsersAction, type SearchUserResult } from '@/lib/actions/testing-lab-user-search';
import type { TestingLocation as ApiTestingLocation, UserRoleAssignment as GeneratedUserRoleAssignment } from '@/lib/api/generated/types.gen';
import { ModuleType } from '@/lib/api/generated/types.gen';
import { Edit, Search, Trash2, UserCheck, UserMinus, UserPlus, Users } from 'lucide-react';
import { useCallback, useState } from 'react';
import { toast } from 'sonner';
import { DualColorChip } from '../testing-lab-settings';

export interface TestingLabManager {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Admin' | 'Manager' | 'Coordinator' | 'Viewer';
  permissions: {
    canManageLocations: boolean;
    canScheduleSessions: boolean;
    canManageUsers: boolean;
    canViewReports: boolean;
    canModifySettings: boolean;
  };
  assignedLocations: string[];
  status: 'Active' | 'Inactive';
  createdAt?: string;
  updatedAt?: string;
}

export type TestingLocation = ApiTestingLocation;

export interface UserRoleAssignment extends GeneratedUserRoleAssignment {
  id: string;
  userEmail?: string;
  userName?: string;
  roleTemplateId?: string;
  assignedAt?: string;
  isActive: boolean;
}

export interface CollaboratorsSettingsProps {
  managers: TestingLabManager[];
  roles: RoleTemplate[];
  userRoles: UserRoleAssignment[];
  locations: TestingLocation[];
  onCreateManager: () => void;
  onEditManager: (manager: TestingLabManager) => void;
  onDeleteManager: (managerId: string) => void;
  onEditRole: (role: RoleTemplate) => void; // kept for future edit dialog usage
  onAssignRole: () => void; // legacy open assign dialog
  onRemoveRole: (userId: string, roleName: string) => void;
  onRoleAssigned?: (assignment: UserRoleAssignment) => void; // new direct assign callback
}

interface CollaboratorWithLocationRoles {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  locationRoles: Array<{
    locationId: string;
    locationName: string;
    roleName: string;
    isActive: boolean;
    assignedAt?: string;
  }>;
  status: 'Active' | 'Inactive';
  createdAt?: string;
}

export function CollaboratorsSettings({
  managers,
  roles,
  userRoles,
  locations,
  onCreateManager,
  onEditManager,
  onDeleteManager,
  onEditRole, // eslint-disable-line @typescript-eslint/no-unused-vars
  onAssignRole, // legacy dialog trigger (still shown in table header)
  onRemoveRole,
  onRoleAssigned,
}: CollaboratorsSettingsProps) {
  // Quick assign state
  const [searchTerm, setSearchTerm] = useState('');
  const [isSearching, setIsSearching] = useState(false);
  const [searchResults, setSearchResults] = useState<SearchUserResult[]>([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selectedRoleId, setSelectedRoleId] = useState('');
  const [assigning, setAssigning] = useState(false);

  const runSearch = useCallback(async (q: string) => {
    setIsSearching(true);
    try {
      const results = await searchTestingLabUsersAction(q);
      setSearchResults(results);
    } catch (err) {
      console.error(err);
      toast.error('Search failed');
    } finally {
      setIsSearching(false);
    }
  }, []);

  const handleAssign = useCallback(async () => {
    const role = roles.find(r => r.id === selectedRoleId);
    const user = searchResults.find(u => u.id === selectedUserId);
    if (!role || !user) {
      toast.error('Select user and role');
      return;
    }
    setAssigning(true);
    try {
      const created = await assignUserRoleAction({
        userId: user.id,
        roleName: role.name,
        userEmail: user.email,
      });
      const assignment: UserRoleAssignment = {
        id: created.id || `${user.id}-${role.id}`,
        userId: user.id,
        userEmail: user.email,
        userName: user.name,
        roleTemplateId: role.id,
        roleName: role.name,
        assignedAt: created.assignedAt || new Date().toISOString(),
        isActive: true,
        module: ModuleType.TESTING_LAB,
      };
      onRoleAssigned?.(assignment);
      toast.success('Role assigned');
      setSelectedUserId('');
      setSelectedRoleId('');
    } catch (err) {
      console.error('Assignment failed', err);
      toast.error(err instanceof Error ? err.message : 'Assignment failed');
    } finally {
      setAssigning(false);
    }
  }, [roles, searchResults, selectedRoleId, selectedUserId, onRoleAssigned]);

  const getCollaborators = (): CollaboratorWithLocationRoles[] => {
    const collaboratorMap = new Map<string, CollaboratorWithLocationRoles>();
    // Seed with managers
    managers.forEach(manager => {
      collaboratorMap.set(manager.userId, {
        id: manager.id,
        userId: manager.userId,
        email: manager.email,
        firstName: manager.firstName,
        lastName: manager.lastName,
        locationRoles: [],
        status: manager.status,
        createdAt: manager.createdAt,
      });
      manager.assignedLocations.forEach(locationId => {
        const location = locations.find(l => l.id === locationId);
        if (location) {
          collaboratorMap.get(manager.userId)?.locationRoles.push({
            locationId,
            locationName: location.name || `Location ${locationId}`,
            roleName: manager.role,
            isActive: manager.status === 'Active',
            assignedAt: manager.createdAt,
          });
        }
      });
    });

    // Add role assignments (global across locations until backend provides specific location binding)
    userRoles.forEach(userRole => {
      if (!userRole.userId) return;
      if (!collaboratorMap.has(userRole.userId)) {
        collaboratorMap.set(userRole.userId, {
          id: userRole.id || userRole.userId,
          userId: userRole.userId,
          email: userRole.userEmail || `user-${userRole.userId}@example.com`,
          firstName: userRole.userName?.split(' ')[0] || 'Unknown',
          lastName: userRole.userName?.split(' ').slice(1).join(' ') || 'User',
          locationRoles: [],
          status: userRole.isActive ? 'Active' : 'Inactive',
          createdAt: userRole.assignedAt,
        });
      }
      const collaborator = collaboratorMap.get(userRole.userId);
      if (collaborator && userRole.roleName) {
        // Represent role across all locations for now
        if (locations.length > 0) {
          locations.forEach(location => {
            collaborator.locationRoles.push({
              locationId: location.id || '',
              locationName: location.name || 'Unknown Location',
              roleName: userRole.roleName || '',
              isActive: userRole.isActive,
              assignedAt: userRole.assignedAt,
            });
          });
        } else {
          collaborator.locationRoles.push({
            locationId: 'global',
            locationName: 'All Locations',
            roleName: userRole.roleName,
            isActive: userRole.isActive,
            assignedAt: userRole.assignedAt,
          });
        }
      }
    });
    return Array.from(collaboratorMap.values());
  };

  const collaborators = getCollaborators();

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-2xl font-bold mb-2">Collaborators</h3>
        <p className="text-muted-foreground">Manage collaborators and their location-specific roles in the Testing Lab.</p>
      </div>
      <Separator />
      <div className="space-y-6">
        {/* Quick Assign Panel */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm font-medium">
              <Search className="h-4 w-4" /> Quick Assign
            </CardTitle>
            <CardDescription>Search a user and directly assign a role.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 md:grid-cols-3">
              <div className="space-y-1 col-span-1">
                <div className="text-xs font-medium">Search User</div>
                <div className="flex gap-2">
                  <Input
                    placeholder="email or name"
                    value={searchTerm}
                    onChange={(e) => {
                      const v = e.target.value;
                      setSearchTerm(v);
                      if (v.length >= 2) {
                        runSearch(v);
                      } else {
                        setSearchResults([]);
                      }
                    }}
                  />
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={!searchTerm.trim() || isSearching}
                    onClick={() => runSearch(searchTerm)}
                  >
                    {isSearching ? '...' : 'Go'}
                  </Button>
                </div>
                {searchResults.length > 0 && (
                  <div className="border rounded-md max-h-40 overflow-y-auto text-sm divide-y">
                    {searchResults.map(u => (
                      <button
                        key={u.id}
                        type="button"
                        onClick={() => setSelectedUserId(u.id)}
                        className={`w-full text-left px-2 py-1 hover:bg-muted ${selectedUserId === u.id ? 'bg-muted' : ''}`}
                      >
                        <div className="font-medium truncate">{u.name}</div>
                        <div className="text-xs text-muted-foreground truncate">{u.email}</div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
              <div className="space-y-1 col-span-1">
                <div className="text-xs font-medium">Role</div>
                <Select value={selectedRoleId} onValueChange={setSelectedRoleId}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select role" />
                  </SelectTrigger>
                  <SelectContent>
                    {roles.map(r => (
                      <SelectItem key={r.id} value={r.id}>{r.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-end col-span-1">
                <Button
                  type="button"
                  className="w-full"
                  disabled={!selectedUserId || !selectedRoleId || assigning}
                  onClick={handleAssign}
                >
                  {assigning ? 'Assigning...' : 'Assign'}
                </Button>
              </div>
            </div>
            <p className="text-xs text-muted-foreground">Direct server assignment. Results limited to first 10 matches.</p>
          </CardContent>
        </Card>

        {/* Collaborators Table */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              Collaborators & Role Assignments
              <div className="flex gap-2">
                <Button onClick={onAssignRole} variant="outline" size="sm" className="flex items-center gap-2">
                  <UserCheck className="h-4 w-4" />
                  Assign Role
                </Button>
                <Button onClick={onCreateManager} className="flex items-center gap-2">
                  <UserPlus className="h-4 w-4" />
                  Add Collaborator
                </Button>
              </div>
            </CardTitle>
            <CardDescription>View and manage collaborators with their location-specific role assignments</CardDescription>
          </CardHeader>
          <CardContent>
            {collaborators.length > 0 ? (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Collaborator</TableHead>
                    <TableHead>Location Assignments</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {collaborators.map(collaborator => (
                    <TableRow key={collaborator.id}>
                      <TableCell>
                        <div>
                          <div className="font-medium">{collaborator.firstName} {collaborator.lastName}</div>
                          <div className="text-sm text-muted-foreground">{collaborator.email}</div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          {collaborator.locationRoles.length > 0 ? (
                            collaborator.locationRoles.map(locationRole => {
                              let leftColor: 'blue' | 'purple' | 'orange' | 'teal' = 'blue';
                              let rightColor: 'green' | 'indigo' | 'pink' | 'yellow' = 'green';
                              const locationHash = locationRole.locationId.length;
                              if (locationHash % 4 === 0) leftColor = 'blue';
                              else if (locationHash % 4 === 1) leftColor = 'teal';
                              else if (locationHash % 4 === 2) leftColor = 'orange';
                              else leftColor = 'purple';
                              if (locationRole.roleName.includes('Admin')) rightColor = 'pink';
                              else if (locationRole.roleName.includes('Manager')) rightColor = 'indigo';
                              else if (locationRole.roleName.includes('Coordinator')) rightColor = 'yellow';
                              else rightColor = 'green';
                              return (
                                <DualColorChip
                                  key={`${collaborator.id}-${locationRole.locationId}-${locationRole.roleName}`}
                                  leftText={locationRole.locationName}
                                  rightText={locationRole.roleName}
                                  isActive={locationRole.isActive}
                                  leftColor={leftColor}
                                  rightColor={rightColor}
                                  size="sm"
                                  tooltip={`${locationRole.locationName} → ${locationRole.roleName}${locationRole.assignedAt ? ` | Assigned: ${new Date(locationRole.assignedAt).toLocaleDateString()}` : ''}${!locationRole.isActive ? ' | Status: Inactive' : ''}`}
                                />
                              );
                            })
                          ) : (
                            <span className="text-sm text-muted-foreground">No location assignments</span>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={collaborator.status === 'Active' ? 'default' : 'secondary'}>
                          {collaborator.status}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              const manager = managers.find(m => m.userId === collaborator.userId);
                              if (manager) onEditManager(manager);
                            }}
                            title="Edit collaborator"
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => onDeleteManager(collaborator.userId || collaborator.id)}
                            title="Remove collaborator"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              collaborator.locationRoles.forEach(locationRole => {
                                onRemoveRole(collaborator.userId, locationRole.roleName);
                              });
                            }}
                            title="Remove all roles"
                          >
                            <UserMinus className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            ) : (
              <div className="text-center py-8">
                <Users className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <p className="text-muted-foreground mb-4">No collaborators found</p>
                <Button onClick={onCreateManager} className="flex items-center gap-2">
                  <UserPlus className="h-4 w-4" />
                  Add First Collaborator
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

export default CollaboratorsSettings;
