"use client";
import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Edit, UserCheck, UserMinus, UserPlus, Users, Trash2 } from 'lucide-react';
import { DualColorChip } from '../testing-lab-settings';
import type { RoleTemplate } from '@/actions/testing-lab-roles';
import type { TestingLocation as ApiTestingLocation, UserRoleAssignment as GeneratedUserRoleAssignment } from '@/lib/api/generated/types.gen';

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
  onEditRole: (role: RoleTemplate) => void;
  onAssignRole: () => void;
  onRemoveRole: (userId: string, roleName: string) => void;
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
  onEditRole,
  onAssignRole,
  onRemoveRole,
}: CollaboratorsSettingsProps) {
  const getCollaborators = (): CollaboratorWithLocationRoles[] => {
    const collaboratorMap = new Map<string, CollaboratorWithLocationRoles>();
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
    userRoles.forEach(userRole => {
      if (!collaboratorMap.has(userRole.userId || '')) {
        collaboratorMap.set(userRole.userId || '', {
          id: userRole.id || '',
          userId: userRole.userId || '',
          email: userRole.userEmail || `user-${userRole.userId}@example.com`,
          firstName: userRole.userName?.split(' ')[0] || 'Unknown',
          lastName: userRole.userName?.split(' ').slice(1).join(' ') || 'User',
          locationRoles: [],
          status: userRole.isActive ? 'Active' : 'Inactive',
          createdAt: userRole.createdAt,
        });
      }
      const collaborator = collaboratorMap.get(userRole.userId || '');
      if (collaborator && userRole.roleName) {
        if (locations.length > 0) {
          locations.forEach(location => {
            collaborator.locationRoles.push({
              locationId: location.id || '',
              locationName: location.name || 'Unknown Location',
              roleName: userRole.roleName || '',
              isActive: userRole.isActive,
              assignedAt: userRole.createdAt,
            });
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
                            onClick={() => onDeleteManager(collaborator.id)}
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
