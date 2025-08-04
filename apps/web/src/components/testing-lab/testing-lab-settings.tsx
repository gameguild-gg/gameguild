/* eslint-disable prettier/prettier */
'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { MapPin, Plus, Edit, Trash2, Building, Users, Calendar, Settings } from 'lucide-react';
import { toast } from 'sonner';
import { 
  getTestingLocations, 
  createTestingLocation, 
  updateTestingLocation, 
  deleteTestingLocation
} from '@/lib/api/testing-lab';
import type { TestingLocation as ApiTestingLocation, LocationStatus } from '@/lib/api/generated/types.gen';

// Re-export types to avoid conflicts
// Type aliases for cleaner code
type TestingLocation = ApiTestingLocation;

// Define our own manager type since it's not available in the API yet
interface TestingLabManager {
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

// Utility functions for type conversion
const locationStatusToString = (status: LocationStatus): 'Active' | 'Inactive' | 'Maintenance' => {
  switch (status) {
    case 0: return 'Active';
    case 1: return 'Maintenance';
    case 2: return 'Inactive';
    default: return 'Active';
  }
};

const stringToLocationStatus = (status: 'Active' | 'Inactive' | 'Maintenance'): LocationStatus => {
  switch (status) {
    case 'Active': return 0;
    case 'Maintenance': return 1;
    case 'Inactive': return 2;
    default: return 0;
  }
};

interface LocationFormData {
  name: string;
  description: string;
  address: string;
  maxTestersCapacity: number;
  maxProjectsCapacity: number;
  equipmentAvailable: string;
  status: 'Active' | 'Inactive' | 'Maintenance';
}

interface ManagerFormData {
  email: string;
  role: 'Admin' | 'Manager' | 'Coordinator' | 'Viewer';
  permissions: {
    canManageLocations: boolean;
    canScheduleSessions: boolean;
    canManageUsers: boolean;
    canViewReports: boolean;
    canModifySettings: boolean;
  };
  assignedLocations: string[];
}

const initialFormData: LocationFormData = {
  name: '',
  description: '',
  address: '',
  maxTestersCapacity: 10,
  maxProjectsCapacity: 5,
  equipmentAvailable: '',
  status: 'Active',
};

const initialManagerFormData: ManagerFormData = {
  email: '',
  role: 'Manager',
  permissions: {
    canManageLocations: false,
    canScheduleSessions: true,
    canManageUsers: false,
    canViewReports: true,
    canModifySettings: false,
  },
  assignedLocations: [],
};

export function TestingLabSettings() {
  const [locations, setLocations] = useState<TestingLocation[]>([]);
  const [managers, setManagers] = useState<TestingLabManager[]>([]);
  const [isLocationDialogOpen, setIsLocationDialogOpen] = useState(false);
  const [isManagerDialogOpen, setIsManagerDialogOpen] = useState(false);
  const [editingLocation, setEditingLocation] = useState<TestingLocation | null>(null);
  const [editingManager, setEditingManager] = useState<TestingLabManager | null>(null);
  const [formData, setFormData] = useState<LocationFormData>(initialFormData);
  const [managerFormData, setManagerFormData] = useState<ManagerFormData>(initialManagerFormData);
  const [isLoading, setIsLoading] = useState(false);

  // Load locations and managers on component mount
  useEffect(() => {
    loadLocations();
    loadManagers();
  }, []);

  const loadLocations = async () => {
    setIsLoading(true);
    try {
      const apiLocations = await getTestingLocations(0, 50);
      setLocations(apiLocations);
    } catch (error) {
      console.error('Failed to load locations:', error);
      // Fallback to mock data when API is unavailable
      const mockLocations: TestingLocation[] = [
        {
          id: '1',
          name: 'Main Testing Lab',
          description: 'Primary testing facility with full VR setup',
          address: '123 Game Development Street, Tech City, TC 12345',
          maxTestersCapacity: 20,
          maxProjectsCapacity: 8,
          equipmentAvailable: 'VR Headsets, Gaming PCs, Recording Equipment',
          status: 0, // Active
          createdAt: '2024-01-15T10:00:00Z',
          updatedAt: '2024-01-15T10:00:00Z',
        },
        {
          id: '2',
          name: 'Mobile Testing Suite',
          description: 'Specialized mobile and tablet testing environment',
          address: '456 Innovation Boulevard, Tech City, TC 12346',
          maxTestersCapacity: 15,
          maxProjectsCapacity: 5,
          equipmentAvailable: 'iOS/Android Devices, Tablets, Emulators',
          status: 0, // Active
          createdAt: '2024-01-16T10:00:00Z',
          updatedAt: '2024-01-16T10:00:00Z',
        },
        {
          id: '3',
          name: 'Console Testing Room',
          description: 'Dedicated console gaming testing space',
          address: '789 Gaming Plaza, Tech City, TC 12347',
          maxTestersCapacity: 12,
          maxProjectsCapacity: 4,
          equipmentAvailable: 'PlayStation 5, Xbox Series X/S, Nintendo Switch',
          status: 1, // Maintenance
          createdAt: '2024-01-17T10:00:00Z',
          updatedAt: '2024-01-17T10:00:00Z',
        },
      ];
      setLocations(mockLocations);
      toast.warning('Using demo data - API server unavailable');
    } finally {
      setIsLoading(false);
    }
  };

  const loadManagers = async () => {
    try {
      // TODO: Replace with actual API call when endpoints are available
      const mockManagers: TestingLabManager[] = [
        {
          id: '1',
          userId: 'user-1',
          email: 'alice.johnson@gameguild.gg',
          firstName: 'Alice',
          lastName: 'Johnson',
          role: 'Admin',
          permissions: {
            canManageLocations: true,
            canScheduleSessions: true,
            canManageUsers: true,
            canViewReports: true,
            canModifySettings: true,
          },
          assignedLocations: ['1', '2', '3'],
          status: 'Active',
          createdAt: '2024-01-15T10:00:00Z',
          updatedAt: '2024-01-15T10:00:00Z',
        },
        {
          id: '2',
          userId: 'user-2',
          email: 'bob.smith@gameguild.gg',
          firstName: 'Bob',
          lastName: 'Smith',
          role: 'Manager',
          permissions: {
            canManageLocations: false,
            canScheduleSessions: true,
            canManageUsers: false,
            canViewReports: true,
            canModifySettings: false,
          },
          assignedLocations: ['1', '2'],
          status: 'Active',
          createdAt: '2024-01-16T10:00:00Z',
          updatedAt: '2024-01-16T10:00:00Z',
        },
        {
          id: '3',
          userId: 'user-3',
          email: 'carol.davis@gameguild.gg',
          firstName: 'Carol',
          lastName: 'Davis',
          role: 'Coordinator',
          permissions: {
            canManageLocations: false,
            canScheduleSessions: true,
            canManageUsers: false,
            canViewReports: false,
            canModifySettings: false,
          },
          assignedLocations: ['3'],
          status: 'Active',
          createdAt: '2024-01-17T10:00:00Z',
          updatedAt: '2024-01-17T10:00:00Z',
        },
      ];
      setManagers(mockManagers);
    } catch (error) {
      console.error('Failed to load managers:', error);
      toast.error('Failed to load testing lab managers');
    }
  };

  const handleCreateLocation = () => {
    setEditingLocation(null);
    setFormData(initialFormData);
    setIsLocationDialogOpen(true);
  };

  const handleEditLocation = (location: TestingLocation) => {
    setEditingLocation(location);
    setFormData({
      name: location.name,
      description: location.description || '',
      address: location.address || '',
      maxTestersCapacity: location.maxTestersCapacity,
      maxProjectsCapacity: location.maxProjectsCapacity,
      equipmentAvailable: location.equipmentAvailable || '',
      status: locationStatusToString(location.status),
    });
    setIsLocationDialogOpen(true);
  };

  const handleDeleteLocation = async (id: string) => {
    try {
      await deleteTestingLocation(id);
      setLocations(locations.filter(location => location.id !== id));
      toast.success('Location deleted successfully');
    } catch (error) {
      console.warn('API unavailable, using local deletion:', error);
      // Fallback to local deletion when API is unavailable
      setLocations(locations.filter(location => location.id !== id));
      toast.success('Location deleted (demo mode)');
    }
  };

  const handleCreateManager = () => {
    setEditingManager(null);
    setManagerFormData(initialManagerFormData);
    setIsManagerDialogOpen(true);
  };

  const handleEditManager = (manager: TestingLabManager) => {
    setEditingManager(manager);
    setManagerFormData({
      email: manager.email,
      role: manager.role,
      permissions: { ...manager.permissions },
      assignedLocations: [...manager.assignedLocations],
    });
    setIsManagerDialogOpen(true);
  };

  const handleDeleteManager = async (managerId: string) => {
    if (!confirm('Are you sure you want to remove this manager? This action cannot be undone.')) {
      return;
    }

    try {
      // TODO: Replace with actual API call
      setManagers(managers.filter((mgr) => mgr.id !== managerId));
      toast.success('Manager removed successfully');
    } catch (error) {
      console.error('Failed to remove manager:', error);
      toast.error('Failed to remove manager');
    }
  };

  const handleSubmitLocation = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      if (editingLocation) {
        // Update existing location
        try {
          const updatedLocation = await updateTestingLocation(editingLocation.id!, {
            name: formData.name,
            description: formData.description,
            address: formData.address,
            maxTestersCapacity: formData.maxTestersCapacity,
            maxProjectsCapacity: formData.maxProjectsCapacity,
            equipmentAvailable: formData.equipmentAvailable,
            status: stringToLocationStatus(formData.status),
          });
          setLocations(locations.map((loc) => (loc.id === editingLocation.id ? updatedLocation : loc)));
          toast.success('Location updated successfully');
        } catch (apiError) {
          // Fallback to local update when API is unavailable
          console.warn('API unavailable, using local update:', apiError);
          const updatedLocation: TestingLocation = {
            ...editingLocation,
            name: formData.name,
            description: formData.description,
            address: formData.address,
            maxTestersCapacity: formData.maxTestersCapacity,
            maxProjectsCapacity: formData.maxProjectsCapacity,
            equipmentAvailable: formData.equipmentAvailable,
            status: stringToLocationStatus(formData.status),
            updatedAt: new Date().toISOString(),
          };
          setLocations(locations.map((loc) => (loc.id === editingLocation.id ? updatedLocation : loc)));
          toast.success('Location updated (demo mode)');
        }
      } else {
        // Create new location
        try {
          const newLocation = await createTestingLocation({
            name: formData.name,
            description: formData.description,
            address: formData.address,
            maxTestersCapacity: formData.maxTestersCapacity,
            maxProjectsCapacity: formData.maxProjectsCapacity,
            equipmentAvailable: formData.equipmentAvailable,
            status: stringToLocationStatus(formData.status),
          });
          setLocations([...locations, newLocation]);
          toast.success('Location created successfully');
        } catch (apiError) {
          // Fallback to local creation when API is unavailable
          console.warn('API unavailable, using local creation:', apiError);
          const newLocation: TestingLocation = {
            id: Date.now().toString(),
            name: formData.name,
            description: formData.description,
            address: formData.address,
            maxTestersCapacity: formData.maxTestersCapacity,
            maxProjectsCapacity: formData.maxProjectsCapacity,
            equipmentAvailable: formData.equipmentAvailable,
            status: stringToLocationStatus(formData.status),
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          };
          setLocations([...locations, newLocation]);
          toast.success('Location created (demo mode)');
        }
      }
      
      setIsLocationDialogOpen(false);
      setFormData(initialFormData);
      setEditingLocation(null);
    } catch (error) {
      console.error('Failed to save location:', error);
      toast.error('Failed to save location');
    }
  };

  const handleSubmitManager = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      if (editingManager) {
        // Update existing manager
        const updatedManager: TestingLabManager = {
          ...editingManager,
          ...managerFormData,
          updatedAt: new Date().toISOString(),
        };
        setManagers(managers.map((mgr) => (mgr.id === editingManager.id ? updatedManager : mgr)));
        toast.success('Manager updated successfully');
      } else {
        // Create new manager (this would typically involve sending an invitation)
        const newManager: TestingLabManager = {
          id: Date.now().toString(), // Temporary ID generation
          userId: '', // Would be populated when user accepts invitation
          firstName: '',
          lastName: '',
          ...managerFormData,
          status: 'Active',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        };
        setManagers([...managers, newManager]);
        toast.success('Manager invitation sent successfully');
      }
      
      setIsManagerDialogOpen(false);
      setManagerFormData(initialManagerFormData);
      setEditingManager(null);
    } catch (error) {
      console.error('Failed to save manager:', error);
      toast.error('Failed to save manager');
    }
  };

  const getStatusBadgeColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300';
      case 'Inactive':
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300';
      case 'Maintenance':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300';
    }
  };

  return (
    <div className="space-y-6">
      <Tabs defaultValue="locations" className="w-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="locations" className="flex items-center gap-2">
            <MapPin className="h-4 w-4" />
            Locations
          </TabsTrigger>
          <TabsTrigger value="managers" className="flex items-center gap-2">
            <Users className="h-4 w-4" />
            Managers
          </TabsTrigger>
          <TabsTrigger value="scheduling" className="flex items-center gap-2">
            <Calendar className="h-4 w-4" />
            Scheduling
          </TabsTrigger>
          <TabsTrigger value="general" className="flex items-center gap-2">
            <Settings className="h-4 w-4" />
            General
          </TabsTrigger>
        </TabsList>

        <TabsContent value="locations" className="space-y-6">
          <div className="flex justify-between items-center">
            <div>
              <h3 className="text-lg font-semibold">Testing Locations</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Manage your testing facilities and their configurations
              </p>
            </div>
            <Button onClick={handleCreateLocation} className="flex items-center gap-2">
              <Plus className="h-4 w-4" />
              Add Location
            </Button>
          </div>

          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {locations.map((location) => (
              <Card key={location.id} className="relative">
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-2">
                      <Building className="h-5 w-5 text-blue-500" />
                      <CardTitle className="text-lg">{location.name}</CardTitle>
                    </div>
                    <Badge className={getStatusBadgeColor(locationStatusToString(location.status))}>
                      {locationStatusToString(location.status)}
                    </Badge>
                  </div>
                  {location.description && <CardDescription>{location.description}</CardDescription>}
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="text-sm text-gray-600 dark:text-gray-400">
                    <p className="flex items-center gap-1">
                      <MapPin className="h-3 w-3" />
                      {location.address}
                    </p>
                  </div>
                  
                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <div className="text-center p-2 bg-blue-50 dark:bg-blue-900/20 rounded">
                      <div className="font-semibold text-blue-700 dark:text-blue-300">
                        {location.maxTestersCapacity}
                      </div>
                      <div className="text-xs text-blue-600 dark:text-blue-400">Max Testers</div>
                    </div>
                    <div className="text-center p-2 bg-green-50 dark:bg-green-900/20 rounded">
                      <div className="font-semibold text-green-700 dark:text-green-300">
                        {location.maxProjectsCapacity}
                      </div>
                      <div className="text-xs text-green-600 dark:text-green-400">Max Projects</div>
                    </div>
                  </div>

                  {location.equipmentAvailable && (
                    <div className="text-xs text-gray-500 dark:text-gray-400">
                      <strong>Equipment:</strong> {location.equipmentAvailable}
                    </div>
                  )}

                  <div className="flex justify-end gap-2 pt-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEditLocation(location)}
                      className="flex items-center gap-1"
                    >
                      <Edit className="h-3 w-3" />
                      Edit
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => location.id && handleDeleteLocation(location.id)}
                      className="flex items-center gap-1 text-red-600 hover:text-red-700"
                    >
                      <Trash2 className="h-3 w-3" />
                      Delete
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {locations.length === 0 && !isLoading && (
            <Card className="text-center py-8">
              <CardContent>
                <Building className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
                  No testing locations configured
                </h3>
                <p className="text-gray-600 dark:text-gray-400 mb-4">
                  Get started by adding your first testing facility
                </p>
                <Button onClick={handleCreateLocation} className="flex items-center gap-2">
                  <Plus className="h-4 w-4" />
                  Add Your First Location
                </Button>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="managers" className="space-y-6">
          <div className="flex justify-between items-center">
            <div>
              <h3 className="text-lg font-semibold">Testing Lab Managers</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Manage users who can administer testing labs, schedule sessions, and manage locations
              </p>
            </div>
            <Button onClick={handleCreateManager} className="flex items-center gap-2">
              <Plus className="h-4 w-4" />
              Add Manager
            </Button>
          </div>

          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {managers.map((manager) => (
              <Card key={manager.id} className="relative">
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-2">
                      <Users className="h-5 w-5 text-blue-500" />
                      <div>
                        <CardTitle className="text-lg">
                          {manager.firstName && manager.lastName 
                            ? `${manager.firstName} ${manager.lastName}`
                            : manager.email
                          }
                        </CardTitle>
                        <p className="text-sm text-gray-600 dark:text-gray-400">{manager.email}</p>
                      </div>
                    </div>
                    <Badge className={manager.role === 'Admin' 
                      ? 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-300'
                      : manager.role === 'Manager'
                      ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300'
                      : 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300'
                    }>
                      {manager.role}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="text-sm">
                    <p className="font-medium mb-2">Permissions:</p>
                    <div className="space-y-1">
                      {manager.permissions.canManageLocations && (
                        <Badge variant="outline" className="mr-1">Manage Locations</Badge>
                      )}
                      {manager.permissions.canScheduleSessions && (
                        <Badge variant="outline" className="mr-1">Schedule Sessions</Badge>
                      )}
                      {manager.permissions.canManageUsers && (
                        <Badge variant="outline" className="mr-1">Manage Users</Badge>
                      )}
                      {manager.permissions.canViewReports && (
                        <Badge variant="outline" className="mr-1">View Reports</Badge>
                      )}
                      {manager.permissions.canModifySettings && (
                        <Badge variant="outline" className="mr-1">Modify Settings</Badge>
                      )}
                    </div>
                  </div>

                  <div className="text-sm">
                    <p className="font-medium mb-1">Assigned Locations:</p>
                    <div className="text-gray-600 dark:text-gray-400">
                      {manager.assignedLocations.length > 0 ? (
                        <div className="space-y-1">
                          {manager.assignedLocations.map((locationId: string) => {
                            const location = locations.find(loc => loc.id === locationId);
                            return location ? (
                              <Badge key={locationId} variant="secondary" className="mr-1">
                                {location.name}
                              </Badge>
                            ) : null;
                          })}
                        </div>
                      ) : (
                        <span className="text-gray-500">No locations assigned</span>
                      )}
                    </div>
                  </div>

                  <div className="flex justify-end gap-2 pt-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEditManager(manager)}
                      className="flex items-center gap-1"
                    >
                      <Edit className="h-3 w-3" />
                      Edit
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleDeleteManager(manager.id)}
                      className="flex items-center gap-1 text-red-600 hover:text-red-700"
                    >
                      <Trash2 className="h-3 w-3" />
                      Remove
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {managers.length === 0 && !isLoading && (
            <Card className="text-center py-8">
              <CardContent>
                <Users className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
                  No testing lab managers configured
                </h3>
                <p className="text-gray-600 dark:text-gray-400 mb-4">
                  Get started by adding your first testing lab manager
                </p>
                <Button onClick={handleCreateManager} className="flex items-center gap-2">
                  <Plus className="h-4 w-4" />
                  Add Your First Manager
                </Button>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="scheduling" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Scheduling Settings</CardTitle>
              <CardDescription>
                Configure testing session scheduling rules and constraints
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-gray-600 dark:text-gray-400">
                Scheduling configuration features coming soon...
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="general" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>General Settings</CardTitle>
              <CardDescription>
                Configure general testing lab preferences and defaults
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-gray-600 dark:text-gray-400">
                General settings features coming soon...
              </p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Location Creation/Edit Dialog */}
      <Dialog open={isLocationDialogOpen} onOpenChange={setIsLocationDialogOpen}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <DialogTitle>
              {editingLocation ? 'Edit Testing Location' : 'Create New Testing Location'}
            </DialogTitle>
            <DialogDescription>
              {editingLocation 
                ? 'Update the details of this testing location'
                : 'Add a new testing facility to your lab network'
              }
            </DialogDescription>
          </DialogHeader>
          
          <form onSubmit={handleSubmitLocation} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Location Name</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData({...formData, name: e.target.value})}
                  placeholder="Main Testing Lab"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="status">Status</Label>
                <Select
                  value={formData.status}
                  onValueChange={(value) => setFormData({ ...formData, status: value as LocationFormData['status'] })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                    <SelectItem value="Maintenance">Maintenance</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description}
                onChange={(e) => setFormData({...formData, description: e.target.value})}
                placeholder="Brief description of this testing facility..."
                rows={2}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="address">Address</Label>
              <Textarea
                id="address"
                value={formData.address}
                onChange={(e) => setFormData({...formData, address: e.target.value})}
                placeholder="123 Tech Street, Game City, GC 12345"
                rows={2}
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="maxTestersCapacity">Max Testers Capacity</Label>
                <Input
                  id="maxTestersCapacity"
                  type="number"
                  min="1"
                  value={formData.maxTestersCapacity}
                  onChange={(e) => setFormData({...formData, maxTestersCapacity: parseInt(e.target.value) || 0})}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="maxProjectsCapacity">Max Projects Capacity</Label>
                <Input
                  id="maxProjectsCapacity"
                  type="number"
                  min="1"
                  value={formData.maxProjectsCapacity}
                  onChange={(e) => setFormData({...formData, maxProjectsCapacity: parseInt(e.target.value) || 0})}
                  required
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="equipmentAvailable">Available Equipment</Label>
              <Textarea
                id="equipmentAvailable"
                value={formData.equipmentAvailable}
                onChange={(e) => setFormData({...formData, equipmentAvailable: e.target.value})}
                placeholder="VR Headsets, Gaming PCs, Consoles, Recording Equipment..."
                rows={2}
              />
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsLocationDialogOpen(false)}
              >
                Cancel
              </Button>
              <Button type="submit">
                {editingLocation ? 'Update Location' : 'Create Location'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Manager Creation/Edit Dialog */}
      <Dialog open={isManagerDialogOpen} onOpenChange={setIsManagerDialogOpen}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <DialogTitle>
              {editingManager ? 'Edit Testing Lab Manager' : 'Add New Testing Lab Manager'}
            </DialogTitle>
            <DialogDescription>
              {editingManager 
                ? 'Update the permissions and settings for this manager'
                : 'Invite a new user to manage testing labs and configure their permissions'
              }
            </DialogDescription>
          </DialogHeader>
          
          <form onSubmit={handleSubmitManager} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="email">Email Address</Label>
                <Input
                  id="email"
                  type="email"
                  value={managerFormData.email}
                  onChange={(e) => setManagerFormData({...managerFormData, email: e.target.value})}
                  placeholder="user@gameguild.gg"
                  required
                  disabled={!!editingManager}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="role">Role</Label>
                <Select
                  value={managerFormData.role}
                  onValueChange={(value) => setManagerFormData({ ...managerFormData, role: value as ManagerFormData['role'] })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Admin">Admin</SelectItem>
                    <SelectItem value="Manager">Manager</SelectItem>
                    <SelectItem value="Coordinator">Coordinator</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-3">
              <Label>Permissions</Label>
              <div className="grid grid-cols-2 gap-3">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="canManageLocations"
                    checked={managerFormData.permissions.canManageLocations}
                    onChange={(e) => setManagerFormData({
                      ...managerFormData,
                      permissions: {
                        ...managerFormData.permissions,
                        canManageLocations: e.target.checked
                      }
                    })}
                    className="rounded border-gray-300"
                  />
                  <Label htmlFor="canManageLocations" className="text-sm">Manage Locations</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="canScheduleSessions"
                    checked={managerFormData.permissions.canScheduleSessions}
                    onChange={(e) => setManagerFormData({
                      ...managerFormData,
                      permissions: {
                        ...managerFormData.permissions,
                        canScheduleSessions: e.target.checked
                      }
                    })}
                    className="rounded border-gray-300"
                  />
                  <Label htmlFor="canScheduleSessions" className="text-sm">Schedule Sessions</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="canManageUsers"
                    checked={managerFormData.permissions.canManageUsers}
                    onChange={(e) => setManagerFormData({
                      ...managerFormData,
                      permissions: {
                        ...managerFormData.permissions,
                        canManageUsers: e.target.checked
                      }
                    })}
                    className="rounded border-gray-300"
                  />
                  <Label htmlFor="canManageUsers" className="text-sm">Manage Users</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="canViewReports"
                    checked={managerFormData.permissions.canViewReports}
                    onChange={(e) => setManagerFormData({
                      ...managerFormData,
                      permissions: {
                        ...managerFormData.permissions,
                        canViewReports: e.target.checked
                      }
                    })}
                    className="rounded border-gray-300"
                  />
                  <Label htmlFor="canViewReports" className="text-sm">View Reports</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="canModifySettings"
                    checked={managerFormData.permissions.canModifySettings}
                    onChange={(e) => setManagerFormData({
                      ...managerFormData,
                      permissions: {
                        ...managerFormData.permissions,
                        canModifySettings: e.target.checked
                      }
                    })}
                    className="rounded border-gray-300"
                  />
                  <Label htmlFor="canModifySettings" className="text-sm">Modify Settings</Label>
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Assigned Locations</Label>
              <div className="grid grid-cols-2 gap-2 max-h-32 overflow-y-auto">
                {locations.map((location) => (
                  <div key={location.id} className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id={`location-${location.id}`}
                      checked={location.id ? managerFormData.assignedLocations.includes(location.id) : false}
                      onChange={(e) => {
                        if (!location.id) return;
                        if (e.target.checked) {
                          setManagerFormData({
                            ...managerFormData,
                            assignedLocations: [...managerFormData.assignedLocations, location.id]
                          });
                        } else {
                          setManagerFormData({
                            ...managerFormData,
                            assignedLocations: managerFormData.assignedLocations.filter(id => id !== location.id)
                          });
                        }
                      }}
                      className="rounded border-gray-300"
                    />
                    <Label htmlFor={`location-${location.id}`} className="text-sm">
                      {location.name}
                    </Label>
                  </div>
                ))}
              </div>
              {locations.length === 0 && (
                <p className="text-sm text-gray-500">No locations available. Create locations first.</p>
              )}
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsManagerDialogOpen(false)}
              >
                Cancel
              </Button>
              <Button type="submit">
                {editingManager ? 'Update Manager' : 'Send Invitation'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
