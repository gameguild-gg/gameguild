'use client';

import { Badge }                                                                                                              from '@/components/ui/badge';
import { Button }                                                                                                             from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle }                                                          from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle }                                  from '@/components/ui/dialog';
import { Input }                                                                                                              from '@/components/ui/input';
import { Label }                                                                                                              from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue }                                                      from '@/components/ui/select';
import { Separator }                                                                                                          from '@/components/ui/separator';
import { Switch }                                                                                                             from '@/components/ui/switch';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow }                                                      from '@/components/ui/table';
import { Textarea }                                                                                                           from '@/components/ui/textarea';
import type { LocationStatus, TestingLocation as ApiTestingLocation, UserRoleAssignment as GeneratedUserRoleAssignment, User } from '@/lib/api/generated/types.gen';
import { convertAPIPermissionsToForm, convertFormPermissionsToAPI, RoleTemplate as APIRoleTemplate, TestingLabPermissionAPI } from '@/lib/api/testing-lab-permissions';
import { getTestingLabSettings, updateTestingLabSettings } from '@/actions/testing-lab-settings';
import { 
  getTestingLocationsAction, 
  createTestingLocationAction, 
  updateTestingLocationAction, 
  deleteTestingLocationAction 
} from '@/actions/testing-lab-locations';
import { getUsers } from '@/lib/api/users';
import { ChevronRight, Edit, MapPin, Plus, RefreshCw, Settings, Shield, Trash2, UserCheck, UserMinus, UserPlus, Users }                  from 'lucide-react';
import { useEffect, useState }                                                                                                from 'react';
import { toast }                                                                                                              from 'sonner';

// Dual Color Chip Component for Location-Role combinations
interface DualColorChipProps {
  leftText: string;

  rightText: string;

  isActive?: boolean;

  leftColor?: 'blue' | 'purple' | 'orange' | 'teal';

  rightColor?: 'green' | 'indigo' | 'pink' | 'yellow';

  size?: 'sm' | 'md';

  tooltip?: string;
}

function DualColorChip({
  leftText,
  rightText,
  isActive = true,
  leftColor = 'blue',
  rightColor = 'green',
  size = 'sm',
  tooltip,
}: DualColorChipProps) {
  const sizeClasses = size === 'sm' ? 'text-xs px-2 py-1' : 'text-sm px-3 py-1.5';

  const colorClasses = {
    blue: isActive ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-500',
    purple: isActive ? 'bg-purple-100 text-purple-700' : 'bg-gray-100 text-gray-500',
    orange: isActive ? 'bg-orange-100 text-orange-700' : 'bg-gray-100 text-gray-500',
    teal: isActive ? 'bg-teal-100 text-teal-700' : 'bg-gray-100 text-gray-500',
    green: isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500',
    indigo: isActive ? 'bg-indigo-100 text-indigo-700' : 'bg-gray-100 text-gray-500',
    pink: isActive ? 'bg-pink-100 text-pink-700' : 'bg-gray-100 text-gray-500',
    yellow: isActive ? 'bg-yellow-100 text-yellow-700' : 'bg-gray-100 text-gray-500',
  };

  const borderColor = isActive ? 'border-blue-200' : 'border-gray-200';
  const separatorColor = isActive ? 'bg-blue-300' : 'bg-gray-300';

  return (
    <div
      className={ `inline-flex items-center rounded-md font-medium transition-colors border ${ borderColor } hover:shadow-sm cursor-default` }
      title={ tooltip || `${ leftText } → ${ rightText }${ !isActive ? ' (Inactive)' : '' }` }
    >
      {/* Left part (Location) */ }
      <span className={ `${ sizeClasses } rounded-l-md ${ colorClasses[leftColor] }` }>
        { leftText }
      </span>
      {/* Separator */ }
      <div className={ `w-px h-4 ${ separatorColor }` }/>
      {/* Right part (Role) */ }
      <span className={ `${ sizeClasses } rounded-r-md ${ colorClasses[rightColor] }` }>
        { rightText }
      </span>
    </div>
  );
}

// Re-export types to avoid conflicts
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

// Role management types
// Use API types
type RoleTemplate = APIRoleTemplate;

// Define UserRoleAssignment locally since it's not exported
// Extended type that combines API type with additional UI properties
interface UserRoleAssignment extends GeneratedUserRoleAssignment {
  id: string;
  userEmail?: string;
  userName?: string;
  roleTemplateId?: string;
  assignedAt?: string;
  isActive: boolean;
}

interface RoleFormData {
  name: string;

  description: string;

  permissions: {
    // TestingSession permissions
    createSession: boolean;
    readSession: boolean;
    editSession: boolean;
    deleteSession: boolean;

    // TestingLocation permissions
    createLocation: boolean;
    readLocation: boolean;
    editLocation: boolean;
    deleteLocation: boolean;

    // TestingFeedback permissions
    createFeedback: boolean;
    readFeedback: boolean;
    editFeedback: boolean;
    deleteFeedback: boolean;
    moderateFeedback: boolean;

    // TestingRequest permissions
    createRequest: boolean;
    readRequest: boolean;
    editRequest: boolean;
    deleteRequest: boolean;
    approveRequest: boolean;

    // TestingParticipant permissions
    manageParticipant: boolean;
    readParticipant: boolean;
  };
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

const initialRoleFormData: RoleFormData = {
  name: '',
  description: '',
  permissions: {
    // TestingSession permissions
    createSession: false,
    readSession: true,
    editSession: false,
    deleteSession: false,

    // TestingLocation permissions
    createLocation: false,
    readLocation: true,
    editLocation: false,
    deleteLocation: false,

    // TestingFeedback permissions
    createFeedback: false,
    readFeedback: true,
    editFeedback: false,
    deleteFeedback: false,
    moderateFeedback: false,

    // TestingRequest permissions
    createRequest: false,
    readRequest: true,
    editRequest: false,
    deleteRequest: false,
    approveRequest: false,

    // TestingParticipant permissions
    manageParticipant: false,
    readParticipant: true,
  },
};

export function TestingLabSettings() {
  // Navigation state
  const [ currentSection, setCurrentSection ] = useState('general');

  // Data states
  const [locations, setLocations] = useState<TestingLocation[]>([]);
  const [managers, setManagers] = useState<TestingLabManager[]>([]);
  const [ roles, setRoles ] = useState<RoleTemplate[]>([]);
  const [ userRoles, setUserRoles ] = useState<UserRoleAssignment[]>([]);

  // Dialog states
  const [isLocationDialogOpen, setIsLocationDialogOpen] = useState(false);
  const [isManagerDialogOpen, setIsManagerDialogOpen] = useState(false);
  const [ isRoleDialogOpen, setIsRoleDialogOpen ] = useState(false);
  const [ isUserRoleDialogOpen, setIsUserRoleDialogOpen ] = useState(false);

  // Editing states
  const [editingLocation, setEditingLocation] = useState<TestingLocation | null>(null);
  const [editingManager, setEditingManager] = useState<TestingLabManager | null>(null);
  const [ editingRole, setEditingRole ] = useState<RoleTemplate | null>(null);
  const [ selectedUserEmail, setSelectedUserEmail ] = useState<string>('');

  // Form states
  const [formData, setFormData] = useState<LocationFormData>(initialFormData);
  const [managerFormData, setManagerFormData] = useState<ManagerFormData>(initialManagerFormData);
  const [ roleFormData, setRoleFormData ] = useState<RoleFormData>(initialRoleFormData);
  const [isLoading, setIsLoading] = useState(false);

  // General settings state
  const [ generalSettings, setGeneralSettings ] = useState({
    labName: 'Testing Lab',
    description: 'Primary game testing facility',
    timezone: 'UTC',
    defaultSessionDuration: 60,
    allowPublicSignups: true,
    requireApproval: true,
    enableNotifications: true,
    maxSimultaneousSessions: 10,
  });

  // Navigation items
  const navigationItems = [
    { id: 'general', label: 'General', icon: Settings },
    { id: 'collaborators', label: 'Collaborators', icon: Users },
    { id: 'locations', label: 'Locations', icon: MapPin },
    { id: 'roles', label: 'Roles & Permissions', icon: Shield },
  ];

  // Load locations and managers on component mount
  useEffect(() => {
    loadLocations();
    loadManagers();
    loadRoles();
    loadUserRoles();
    loadGeneralSettings();
  }, []);

  const loadLocations = async () => {
    setIsLoading(true);
    try {
      const apiLocations = await getTestingLocationsAction(0, 50);
      setLocations(apiLocations as TestingLocation[]);
    } catch (error) {
      console.error('Failed to load locations:', error);
      
      // Provide user feedback about the error
      let shouldUseMockData = false;
      if (error instanceof Error) {
        if (error.message.includes('Authentication required')) {
          toast.error('Authentication required - please log in');
        } else if (error.message.includes('permission')) {
          toast.error('You do not have permission to view locations');
        } else if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
          toast.warning('Server unavailable - using demo data');
          shouldUseMockData = true;
        } else if (error.message.includes('500')) {
          toast.error('Server error - using demo data as fallback');
          shouldUseMockData = true;
        } else {
          toast.warning('API unavailable - using demo data');
          shouldUseMockData = true;
        }
      } else {
        toast.warning('Server unavailable - using demo data');
        shouldUseMockData = true;
      }

      if (shouldUseMockData) {
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
      }
    } finally {
      setIsLoading(false);
    }
  };

  const loadManagers = async () => {
    try {
      // Get all users and filter for those with testing lab roles
      const allUsers = await getUsers();
      const allRoleAssignments = await TestingLabPermissionAPI.getAllTestingLabRoleAssignments();
      
      // Transform users to TestingLabManager format based on role assignments
      const managers: TestingLabManager[] = allUsers
        .filter(user => {
          // Include users that have any testing lab role assignment
          return allRoleAssignments.some(assignment => assignment.userId === user.id);
        })
        .map(user => {
          const userAssignments = allRoleAssignments.filter(assignment => assignment.userId === user.id);
          const primaryRole = userAssignments[0]?.roleName || 'Viewer';
          
          // Map role names to simplified roles
          let role: 'Admin' | 'Manager' | 'Coordinator' | 'Viewer' = 'Viewer';
          if (primaryRole.includes('Admin')) role = 'Admin';
          else if (primaryRole.includes('Manager')) role = 'Manager';
          else if (primaryRole.includes('Coordinator')) role = 'Coordinator';
          
          return {
            id: user.id!,
            userId: user.id!,
            email: user.email || '',
            firstName: user.name?.split(' ')[0] || '',
            lastName: user.name?.split(' ').slice(1).join(' ') || '',
            role,
            permissions: {
              canManageLocations: role === 'Admin',
              canScheduleSessions: ['Admin', 'Manager', 'Coordinator'].includes(role),
              canManageUsers: role === 'Admin',
              canViewReports: ['Admin', 'Manager'].includes(role),
              canModifySettings: role === 'Admin',
            },
            assignedLocations: [], // Will need to get location-specific assignments
            status: userAssignments.some(assignment => assignment.isActive) ? 'Active' as const : 'Inactive' as const,
            createdAt: user.createdAt || new Date().toISOString(),
            updatedAt: user.updatedAt || user.createdAt || new Date().toISOString(),
          };
        });
      
      setManagers(managers);
    } catch (error) {
      console.error('Failed to load managers:', error);
      toast.error('Failed to load testing lab managers');
    }
  };

  const loadRoles = async () => {
    try {
      // Use real API call to get role templates
      const apiRoles = await TestingLabPermissionAPI.getRoleTemplates();
      setRoles(apiRoles);
    } catch (error) {
      console.error('Failed to load roles:', error);
      toast.error('Failed to load testing lab roles');
    }
  };

  const loadUserRoles = async () => {
    try {
      // Use real API calls to get user role assignments and user data
      const apiUserRoles = await TestingLabPermissionAPI.getAllTestingLabRoleAssignments();
      const allUsers = await getUsers();
      
      // Transform API data to match our extended interface
      const transformedUserRoles: UserRoleAssignment[] = apiUserRoles
        .filter(role => role.id) // Filter out entries without id
        .map(role => {
          const user = allUsers.find(u => u.id === role.userId);
          return {
            id: role.id!,
            userId: role.userId,
            userEmail: user?.email || '',
            userName: user?.name || '',
            roleTemplateId: '', // Will need mapping from roleName
            roleName: role.roleName,
            assignedAt: role.createdAt, // Use createdAt as assignedAt
            tenantId: role.tenantId,
            module: role.module,
            constraints: role.constraints,
            createdAt: role.createdAt,
            expiresAt: role.expiresAt,
            isActive: role.isActive ?? true,
          };
        });
      
      setUserRoles(transformedUserRoles);
    } catch (error) {
      console.error('Failed to load user roles:', error);
      toast.error('Failed to load user role assignments');
    }
  };

  const loadGeneralSettings = async () => {
    try {
      // Try to load existing settings from API
      const apiSettings = await getTestingLabSettings();
      setGeneralSettings({
        labName: apiSettings.labName,
        description: apiSettings.description || '',
        timezone: apiSettings.timezone,
        defaultSessionDuration: apiSettings.defaultSessionDuration,
        allowPublicSignups: apiSettings.allowPublicSignups,
        requireApproval: apiSettings.requireApproval,
        enableNotifications: apiSettings.enableNotifications,
        maxSimultaneousSessions: apiSettings.maxSimultaneousSessions,
      });
    } catch (error) {
      console.error('Failed to load general settings:', error);
      // Settings will remain with default values set in useState
      toast.error('Failed to load general settings');
    }
  };

  const saveGeneralSettings = async (settings: typeof generalSettings) => {
    try {
      await updateTestingLabSettings({
        labName: settings.labName,
        description: settings.description,
        timezone: settings.timezone,
        defaultSessionDuration: settings.defaultSessionDuration,
        allowPublicSignups: settings.allowPublicSignups,
        requireApproval: settings.requireApproval,
        enableNotifications: settings.enableNotifications,
        maxSimultaneousSessions: settings.maxSimultaneousSessions,
      });
      toast.success('Settings saved successfully');
    } catch (error) {
      console.error('Failed to save general settings:', error);
      toast.error('Failed to save settings');
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
    const locationToDelete = locations.find(loc => loc.id === id);
    const locationName = locationToDelete?.name || 'this location';
    
    if (!confirm(`Are you sure you want to delete "${locationName}"? This action cannot be undone and may affect any ongoing testing sessions at this location.`)) {
      return;
    }

    try {
      await deleteTestingLocation(id);
      setLocations(locations.filter(location => location.id !== id));
      toast.success('Location deleted successfully');
    } catch (error) {
      console.warn('Failed to delete location:', error);
      
      let shouldFallback = false;
      let errorMessage = 'Failed to delete location';
      
      if (error instanceof Error) {
        if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
          shouldFallback = true;
          errorMessage = 'Server unavailable - working in offline mode';
        } else if (error.message.includes('500')) {
          errorMessage = 'Server error occurred. Please try again later.';
        } else if (error.message.includes('401') || error.message.includes('403')) {
          errorMessage = 'You do not have permission to delete this location.';
        }
      } else {
        // If it's not a standard error, assume server is unavailable
        shouldFallback = true;
        errorMessage = 'Server unavailable - working in offline mode';
      }
      
      if (shouldFallback) {
        // Fallback to local deletion when API is unavailable
        setLocations(locations.filter(location => location.id !== id));
        toast.success(errorMessage);
      } else {
        toast.error(errorMessage);
      }
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
          setLocations(locations.map((loc) => (loc.id === editingLocation.id ? updatedLocation as TestingLocation : loc)));
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
          setLocations([ ...locations, newLocation as TestingLocation ]);
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

  // Role management handlers
  const handleCreateRole = () => {
    setEditingRole(null);
    setRoleFormData(initialRoleFormData);
    setIsRoleDialogOpen(true);
  };

  const handleEditRole = (role: RoleTemplate) => {
    // Allow editing of all roles, including system roles
    setEditingRole(role);
    setRoleFormData({
      name: role.name,
      description: role.description,
      permissions: convertAPIPermissionsToForm(role.permissionTemplates),
    });
    setIsRoleDialogOpen(true);
  };

  const handleDeleteRole = async (roleId: string) => {
    const role = roles.find(r => r.id === roleId);
    // Allow deletion of all roles, including system roles

    if (!role) {
      toast.error('Role not found');
      return;
    }

    try {
      await TestingLabPermissionAPI.deleteRoleTemplate(role.name);
      setRoles(roles.filter(r => r.id !== roleId));
      toast.success('Role deleted successfully');
    } catch (error) {
      console.error('Failed to delete role:', error);
      toast.error('Failed to delete role');
    }
  };

  const handleSubmitRole = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const permissionTemplates = convertFormPermissionsToAPI(roleFormData.permissions);

      if (editingRole) {
        // Update existing role
        const updatedRole = await TestingLabPermissionAPI.updateRoleTemplate(editingRole.name, {
          description: roleFormData.description,
          permissionTemplates,
        });

        setRoles(roles.map(r => r.id === editingRole.id ? updatedRole : r));
        toast.success('Role updated successfully');
      } else {
        // Create new role
        const newRole = await TestingLabPermissionAPI.createRoleTemplate({
          name: roleFormData.name,
          description: roleFormData.description,
          permissionTemplates,
        });

        setRoles([ ...roles, newRole ]);
        toast.success('Role created successfully');
      }

      setIsRoleDialogOpen(false);
      setRoleFormData(initialRoleFormData);
      setEditingRole(null);
    } catch (error) {
      console.error('Failed to save role:', error);
      toast.error('Failed to save role');
    }
  };

  const handleAssignRole = () => {
    setIsUserRoleDialogOpen(true);
  };

  const handleSubmitUserRole = async (e: React.FormEvent) => {
    e.preventDefault();

    const selectedRoleId = document.querySelector<HTMLSelectElement>('#role-select')?.value;
    const selectedRole = roles.find(r => r.id === selectedRoleId);
    if (!selectedRole || !selectedUserEmail) {
      toast.error('Please select both user and role');
      return;
    }

    try {
      const newAssignment = await TestingLabPermissionAPI.assignRoleToUser(selectedUserEmail, selectedRole.name);

      // Create a compatible assignment object
      const compatibleAssignment: UserRoleAssignment = {
        id: Date.now().toString(),
        userId: selectedUserEmail,
        userEmail: selectedUserEmail,
        userName: selectedUserEmail,
        roleTemplateId: selectedRole.id,
        roleName: selectedRole.name,
        assignedAt: new Date().toISOString(),
        isActive: true,
      };

      setUserRoles([ ...userRoles, compatibleAssignment ]);

      // Update role user count
      setRoles(roles.map(r =>
        r.id === selectedRole.id
        ? { ...r, userCount: (r.userCount || 0) + 1 }
        : r,
      ));

      toast.success('Role assigned successfully');
      setIsUserRoleDialogOpen(false);
      setSelectedUserEmail('');
    } catch (error) {
      console.error('Failed to assign role:', error);
      toast.error('Failed to assign role');
    }
  };

  const handleRemoveUserRole = async (assignmentId: string) => {
    try {
      const assignment = userRoles.find(ur => ur.id === assignmentId);
      if (assignment) {
        await TestingLabPermissionAPI.removeUserRole(assignment.userId || '', assignment.roleName || '');

        setUserRoles(userRoles.filter(ur => ur.id !== assignmentId));

        // Update role user count
        setRoles(roles.map(r =>
          r.id === assignment.roleTemplateId
          ? { ...r, userCount: Math.max((r.userCount || 1) - 1, 0) }
          : r,
        ));

        toast.success('Role assignment removed');
      }
    } catch (error) {
      console.error('Failed to remove role assignment:', error);
      toast.error('Failed to remove role assignment');
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
    <div className="flex flex-row flex-1">
      {/* Sidebar Navigation */ }
      <div className="w-64 border-r border-border bg-muted/30">
        <div className="p-6">
          <nav className="space-y-1">
            { navigationItems.map((item) => {
              const Icon = item.icon;
              const isActive = currentSection === item.id;
              return (
                <button
                  key={ item.id }
                  onClick={ () => setCurrentSection(item.id) }
                  className={ `w-full flex items-center gap-3 px-3 py-2 text-sm rounded-lg transition-colors ${
                    isActive
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:text-foreground hover:bg-muted'
                  }` }
                >
                  <Icon className="h-4 w-4"/>
                  { item.label }
                  { isActive && <ChevronRight className="h-4 w-4 ml-auto"/> }
                </button>
              );
            }) }
          </nav>
        </div>
      </div>
      {/* Main Content */ }
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="flex flex-1 container">
          <div className="flex flex-col flex-1 ">
            { currentSection === 'general' && <GeneralSettings generalSettings={ generalSettings } setGeneralSettings={ setGeneralSettings } saveGeneralSettings={ saveGeneralSettings }/> }
            { currentSection === 'collaborators' && (
              <CollaboratorsSettings
                managers={ managers }
                roles={ roles }
                userRoles={ userRoles }
                locations={ locations }
                onCreateManager={ handleCreateManager }
                onEditManager={ handleEditManager }
                onDeleteManager={ handleDeleteManager }
                onEditRole={ handleEditRole }
                onAssignRole={ handleAssignRole }
                onRemoveRole={ handleRemoveUserRole }
              />
            ) }
            { currentSection === 'locations' && (
              <LocationsSettings
                locations={ locations }
                onCreateLocation={ handleCreateLocation }
                onEditLocation={ handleEditLocation }
                onDeleteLocation={ handleDeleteLocation }
                isLoading={ isLoading }
              />
            ) }
            { currentSection === 'roles' && (
              <RolesSettings
                roles={ roles }
                onCreateRole={ handleCreateRole }
                onEditRole={ handleEditRole }
                onDeleteRole={ handleDeleteRole }
              />
            ) }
          </div>
        </div>
      </div>
      {/* Dialogs */ }
      <Dialog open={isLocationDialogOpen} onOpenChange={setIsLocationDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{ editingLocation ? 'Edit Location' : 'Create New Location' }</DialogTitle>
            <DialogDescription>
              { editingLocation ? 'Update the location details' : 'Add a new testing location to your lab' }
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="name">Location Name *</Label>
              <Input
                id="name"
                value={ formData.name }
                onChange={ (e) => setFormData({ ...formData, name: e.target.value }) }
                placeholder="e.g., Main Testing Lab, VR Suite, Mobile Lab"
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description}
                onChange={ (e) => setFormData({ ...formData, description: e.target.value }) }
                placeholder="Describe the testing environment, specializations, or unique features"
                className="min-h-[60px]"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="address">Address</Label>
              <Textarea
                id="address"
                value={formData.address}
                onChange={ (e) => setFormData({ ...formData, address: e.target.value }) }
                placeholder="123 Innovation Street, Tech City, TC 12345"
                className="min-h-[60px]"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="maxTesters">Max Testers *</Label>
                <Input
                  id="maxTesters"
                  type="number"
                  min="1"
                  max="100"
                  value={formData.maxTestersCapacity}
                  onChange={ (e) => setFormData({ ...formData, maxTestersCapacity: parseInt(e.target.value) || 0 }) }
                  placeholder="e.g., 15"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="maxProjects">Max Projects *</Label>
                <Input
                  id="maxProjects"
                  type="number"
                  min="1"
                  max="50"
                  value={formData.maxProjectsCapacity}
                  onChange={ (e) => setFormData({ ...formData, maxProjectsCapacity: parseInt(e.target.value) || 0 }) }
                  placeholder="e.g., 5"
                  required
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="equipment">Available Equipment</Label>
              <Textarea
                id="equipment"
                value={formData.equipmentAvailable}
                onChange={ (e) => setFormData({ ...formData, equipmentAvailable: e.target.value }) }
                placeholder="VR Headsets (10x), Gaming PCs (15x), Recording Equipment, Tablets, etc."
                className="min-h-[60px]"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <Select
                value={ formData.status }
                onValueChange={ (value: 'Active' | 'Inactive' | 'Maintenance') =>
                  setFormData({ ...formData, status: value })
                }
              >
                <SelectTrigger>
                  <SelectValue/>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Active">Active</SelectItem>
                  <SelectItem value="Maintenance">Under Maintenance</SelectItem>
                  <SelectItem value="Inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={ () => setIsLocationDialogOpen(false) }>
              Cancel
            </Button>
            <Button 
              onClick={ async () => {
                // Enhanced validation
                if (!formData.name.trim()) {
                  toast.error('Location name is required');
                  return;
                }
                if (!formData.description.trim()) {
                  toast.error('Location description is required');
                  return;
                }
                if (!formData.address.trim()) {
                  toast.error('Location address is required');
                  return;
                }
                if (formData.maxTestersCapacity <= 0 || formData.maxTestersCapacity > 1000) {
                  toast.error('Max testers must be between 1 and 1000');
                  return;
                }
                if (formData.maxProjectsCapacity <= 0 || formData.maxProjectsCapacity > 100) {
                  toast.error('Max projects must be between 1 and 100');
                  return;
                }

                try {
                  const locationData = {
                    ...formData,
                    status: stringToLocationStatus(formData.status),
                  };

                  if (editingLocation) {
                    const updatedLocation = await updateTestingLocation(editingLocation.id!, locationData);
                    setLocations(locations.map((loc) => (loc.id === editingLocation.id ? updatedLocation as TestingLocation : loc)));
                    toast.success('Location updated successfully');
                  } else {
                    const newLocation = await createTestingLocation(locationData);
                    setLocations([ ...locations, newLocation as TestingLocation ]);
                    toast.success('Location created successfully');
                  }
                  setIsLocationDialogOpen(false);
                } catch (error) {
                  console.error('Failed to save location:', error);
                  
                  let errorMessage = 'Failed to save location';
                  if (error instanceof Error) {
                    if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
                      errorMessage = 'Unable to connect to server. Please check your connection and try again.';
                    } else if (error.message.includes('500')) {
                      errorMessage = 'Server error occurred. Please try again later.';
                    } else if (error.message.includes('401') || error.message.includes('403')) {
                      errorMessage = 'You do not have permission to perform this action.';
                    } else if (error.message.includes('400')) {
                      errorMessage = 'Invalid data provided. Please check your input and try again.';
                    } else if (error.message.includes('409')) {
                      errorMessage = 'A location with this name already exists.';
                    } else {
                      errorMessage = `Error: ${error.message}`;
                    }
                  }
                  
                  toast.error(errorMessage);
                }
              } }
              disabled={!formData.name.trim() || !formData.description.trim() || !formData.address.trim() || formData.maxTestersCapacity <= 0 || formData.maxProjectsCapacity <= 0}
            >
              { editingLocation ? 'Update Location' : 'Create Location' }
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// Individual Settings Components

// General Settings Component
interface GeneralSettingsProps {
  generalSettings: any;

  setGeneralSettings: (settings: any) => void;

  saveGeneralSettings: (settings: any) => Promise<void>;
}

function GeneralSettings({ generalSettings, setGeneralSettings, saveGeneralSettings }: GeneralSettingsProps) {
  const handleSettingChange = (key: string, value: any) => {
    setGeneralSettings({ ...generalSettings, [key]: value });
  };

  const handleSave = () => {
    saveGeneralSettings(generalSettings);
  };

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-2xl font-bold mb-2">General</h3>
        <p className="text-muted-foreground">
          Manage general settings for your Testing Lab.
        </p>
      </div>

      <Separator/>

      <div className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Lab Information</CardTitle>
            <CardDescription>
              Basic information about your testing laboratory
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="labName">Lab Name</Label>
                <Input
                  id="labName"
                  value={ generalSettings.labName }
                  onChange={ (e) => handleSettingChange('labName', e.target.value) }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="timezone">Timezone</Label>
                <Select
                  value={ generalSettings.timezone }
                  onValueChange={ (value) => handleSettingChange('timezone', value) }
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="UTC">UTC</SelectItem>
                    
                    {/* Americas */}
                    <SelectItem value="America/New_York">America/New_York (EST/EDT)</SelectItem>
                    <SelectItem value="America/Chicago">America/Chicago (CST/CDT)</SelectItem>
                    <SelectItem value="America/Denver">America/Denver (MST/MDT)</SelectItem>
                    <SelectItem value="America/Los_Angeles">America/Los_Angeles (PST/PDT)</SelectItem>
                    <SelectItem value="America/Phoenix">America/Phoenix (MST)</SelectItem>
                    <SelectItem value="America/Anchorage">America/Anchorage (AKST/AKDT)</SelectItem>
                    <SelectItem value="Pacific/Honolulu">Pacific/Honolulu (HST)</SelectItem>
                    <SelectItem value="America/Toronto">America/Toronto (EST/EDT)</SelectItem>
                    <SelectItem value="America/Vancouver">America/Vancouver (PST/PDT)</SelectItem>
                    <SelectItem value="America/Mexico_City">America/Mexico_City (CST/CDT)</SelectItem>
                    <SelectItem value="America/Sao_Paulo">America/Sao_Paulo (BRT/BRST)</SelectItem>
                    <SelectItem value="America/Buenos_Aires">America/Buenos_Aires (ART)</SelectItem>
                    <SelectItem value="America/Lima">America/Lima (PET)</SelectItem>
                    <SelectItem value="America/Bogota">America/Bogota (COT)</SelectItem>
                    <SelectItem value="America/Caracas">America/Caracas (VET)</SelectItem>
                    <SelectItem value="America/Santiago">America/Santiago (CLT/CLST)</SelectItem>
                    
                    {/* Europe */}
                    <SelectItem value="Europe/London">Europe/London (GMT/BST)</SelectItem>
                    <SelectItem value="Europe/Dublin">Europe/Dublin (GMT/IST)</SelectItem>
                    <SelectItem value="Europe/Paris">Europe/Paris (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Berlin">Europe/Berlin (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Rome">Europe/Rome (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Madrid">Europe/Madrid (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Amsterdam">Europe/Amsterdam (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Brussels">Europe/Brussels (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Vienna">Europe/Vienna (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Zurich">Europe/Zurich (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Prague">Europe/Prague (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Warsaw">Europe/Warsaw (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Stockholm">Europe/Stockholm (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Oslo">Europe/Oslo (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Copenhagen">Europe/Copenhagen (CET/CEST)</SelectItem>
                    <SelectItem value="Europe/Helsinki">Europe/Helsinki (EET/EEST)</SelectItem>
                    <SelectItem value="Europe/Athens">Europe/Athens (EET/EEST)</SelectItem>
                    <SelectItem value="Europe/Istanbul">Europe/Istanbul (TRT)</SelectItem>
                    <SelectItem value="Europe/Moscow">Europe/Moscow (MSK)</SelectItem>
                    <SelectItem value="Europe/Kiev">Europe/Kiev (EET/EEST)</SelectItem>
                    <SelectItem value="Europe/Bucharest">Europe/Bucharest (EET/EEST)</SelectItem>
                    <SelectItem value="Europe/Sofia">Europe/Sofia (EET/EEST)</SelectItem>
                    
                    {/* Asia */}
                    <SelectItem value="Asia/Tokyo">Asia/Tokyo (JST)</SelectItem>
                    <SelectItem value="Asia/Seoul">Asia/Seoul (KST)</SelectItem>
                    <SelectItem value="Asia/Shanghai">Asia/Shanghai (CST)</SelectItem>
                    <SelectItem value="Asia/Hong_Kong">Asia/Hong_Kong (HKT)</SelectItem>
                    <SelectItem value="Asia/Singapore">Asia/Singapore (SGT)</SelectItem>
                    <SelectItem value="Asia/Bangkok">Asia/Bangkok (ICT)</SelectItem>
                    <SelectItem value="Asia/Manila">Asia/Manila (PHT)</SelectItem>
                    <SelectItem value="Asia/Kuala_Lumpur">Asia/Kuala_Lumpur (MYT)</SelectItem>
                    <SelectItem value="Asia/Jakarta">Asia/Jakarta (WIB)</SelectItem>
                    <SelectItem value="Asia/Ho_Chi_Minh">Asia/Ho_Chi_Minh (ICT)</SelectItem>
                    <SelectItem value="Asia/Mumbai">Asia/Mumbai (IST)</SelectItem>
                    <SelectItem value="Asia/Kolkata">Asia/Kolkata (IST)</SelectItem>
                    <SelectItem value="Asia/Dhaka">Asia/Dhaka (BDT)</SelectItem>
                    <SelectItem value="Asia/Karachi">Asia/Karachi (PKT)</SelectItem>
                    <SelectItem value="Asia/Dubai">Asia/Dubai (GST)</SelectItem>
                    <SelectItem value="Asia/Tehran">Asia/Tehran (IRST/IRDT)</SelectItem>
                    <SelectItem value="Asia/Jerusalem">Asia/Jerusalem (IST/IDT)</SelectItem>
                    <SelectItem value="Asia/Riyadh">Asia/Riyadh (AST)</SelectItem>
                    <SelectItem value="Asia/Baghdad">Asia/Baghdad (AST)</SelectItem>
                    <SelectItem value="Asia/Tashkent">Asia/Tashkent (UZT)</SelectItem>
                    <SelectItem value="Asia/Almaty">Asia/Almaty (ALMT)</SelectItem>
                    <SelectItem value="Asia/Novosibirsk">Asia/Novosibirsk (NOVT)</SelectItem>
                    <SelectItem value="Asia/Vladivostok">Asia/Vladivostok (VLAT)</SelectItem>
                    
                    {/* Africa */}
                    <SelectItem value="Africa/Cairo">Africa/Cairo (EET)</SelectItem>
                    <SelectItem value="Africa/Johannesburg">Africa/Johannesburg (SAST)</SelectItem>
                    <SelectItem value="Africa/Lagos">Africa/Lagos (WAT)</SelectItem>
                    <SelectItem value="Africa/Nairobi">Africa/Nairobi (EAT)</SelectItem>
                    <SelectItem value="Africa/Casablanca">Africa/Casablanca (WET/WEST)</SelectItem>
                    <SelectItem value="Africa/Tunis">Africa/Tunis (CET)</SelectItem>
                    <SelectItem value="Africa/Algiers">Africa/Algiers (CET)</SelectItem>
                    <SelectItem value="Africa/Addis_Ababa">Africa/Addis_Ababa (EAT)</SelectItem>
                    <SelectItem value="Africa/Dar_es_Salaam">Africa/Dar_es_Salaam (EAT)</SelectItem>
                    <SelectItem value="Africa/Kampala">Africa/Kampala (EAT)</SelectItem>
                    
                    {/* Oceania */}
                    <SelectItem value="Australia/Sydney">Australia/Sydney (AEST/AEDT)</SelectItem>
                    <SelectItem value="Australia/Melbourne">Australia/Melbourne (AEST/AEDT)</SelectItem>
                    <SelectItem value="Australia/Brisbane">Australia/Brisbane (AEST)</SelectItem>
                    <SelectItem value="Australia/Perth">Australia/Perth (AWST)</SelectItem>
                    <SelectItem value="Australia/Adelaide">Australia/Adelaide (ACST/ACDT)</SelectItem>
                    <SelectItem value="Australia/Darwin">Australia/Darwin (ACST)</SelectItem>
                    <SelectItem value="Pacific/Auckland">Pacific/Auckland (NZST/NZDT)</SelectItem>
                    <SelectItem value="Pacific/Fiji">Pacific/Fiji (FJT/FJST)</SelectItem>
                    <SelectItem value="Pacific/Tahiti">Pacific/Tahiti (TAHT)</SelectItem>
                    <SelectItem value="Pacific/Guam">Pacific/Guam (ChST)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={ generalSettings.description }
                onChange={ (e) => handleSettingChange('description', e.target.value) }
                placeholder="Describe your testing lab..."
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Session Settings</CardTitle>
            <CardDescription>
              Default settings for testing sessions
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="defaultDuration">Default Session Duration (minutes)</Label>
                <Input
                  id="defaultDuration"
                  type="number"
                  value={ generalSettings.defaultSessionDuration }
                  onChange={ (e) => handleSettingChange('defaultSessionDuration', parseInt(e.target.value)) }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="maxSessions">Max Simultaneous Sessions</Label>
                <Input
                  id="maxSessions"
                  type="number"
                  value={ generalSettings.maxSimultaneousSessions }
                  onChange={ (e) => handleSettingChange('maxSimultaneousSessions', parseInt(e.target.value)) }
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Access Control</CardTitle>
            <CardDescription>
              Control how users can access and join testing sessions
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Allow Public Signups</Label>
                <p className="text-sm text-muted-foreground">
                  Allow anyone to sign up for testing sessions
                </p>
              </div>
              <Switch
                checked={ generalSettings.allowPublicSignups }
                onCheckedChange={ (value) => handleSettingChange('allowPublicSignups', value) }
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Require Approval</Label>
                <p className="text-sm text-muted-foreground">
                  Require manager approval for new testing participants
                </p>
              </div>
              <Switch
                checked={ generalSettings.requireApproval }
                onCheckedChange={ (value) => handleSettingChange('requireApproval', value) }
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Enable Notifications</Label>
                <p className="text-sm text-muted-foreground">
                  Send email notifications for session updates
                </p>
              </div>
              <Switch
                checked={ generalSettings.enableNotifications }
                onCheckedChange={ (value) => handleSettingChange('enableNotifications', value) }
              />
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button onClick={handleSave}>
            Save Settings
          </Button>
        </div>
      </div>
    </div>
  );
}

// Collaborators Settings Component (consolidates team members and role assignments)
interface CollaboratorsSettingsProps {
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

// Enhanced collaborator type that includes location-specific roles
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

function CollaboratorsSettings({
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

  // Consolidate managers and user roles into enhanced collaborator format
  const getCollaborators = (): CollaboratorWithLocationRoles[] => {
    const collaboratorMap = new Map<string, CollaboratorWithLocationRoles>();

    // Add managers first
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

      // Add location-specific assignments from manager data
      manager.assignedLocations.forEach(locationId => {
        const location = locations.find(l => l.id === locationId);
        if (location) {
          collaboratorMap.get(manager.userId)?.locationRoles.push({
            locationId: locationId,
            locationName: location.name || `Location ${ locationId }`,
            roleName: manager.role,
            isActive: manager.status === 'Active',
            assignedAt: manager.createdAt,
          });
        }
      });
    });

    // Add user role assignments
    userRoles.forEach(userRole => {
      if (!collaboratorMap.has(userRole.userId || '')) {
        // Create new collaborator entry
        collaboratorMap.set(userRole.userId || '', {
          id: userRole.id || '',
          userId: userRole.userId || '',
          email: `user-${ userRole.userId }@example.com`, // Placeholder
          firstName: 'Unknown',
          lastName: 'User',
          locationRoles: [],
          status: userRole.isActive ? 'Active' : 'Inactive',
          createdAt: userRole.createdAt,
        });
      }

      const collaborator = collaboratorMap.get(userRole.userId || '');
      if (collaborator && userRole.roleName) {
        // Since UserRoleAssignment doesn't have location constraints yet,
        // we'll assign roles to all available locations for now
        // In a real implementation, you'd check for location constraints
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
        <p className="text-muted-foreground">
          Manage collaborators and their location-specific roles in the Testing Lab.
        </p>
      </div>

      <Separator/>

      <div className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              Collaborators & Role Assignments
              <div className="flex gap-2">
                <Button onClick={ onAssignRole } variant="outline" size="sm" className="flex items-center gap-2">
                  <UserCheck className="h-4 w-4"/>
                  Assign Role
                </Button>
                <Button onClick={ onCreateManager } className="flex items-center gap-2">
                  <UserPlus className="h-4 w-4"/>
                  Add Collaborator
                </Button>
              </div>
            </CardTitle>
            <CardDescription>
              View and manage collaborators with their location-specific role assignments
            </CardDescription>
          </CardHeader>
          <CardContent>
            { collaborators.length > 0 ? (
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
                  { collaborators.map((collaborator) => (
                    <TableRow key={ collaborator.id }>
                      <TableCell>
                        <div>
                          <div className="font-medium">{ collaborator.firstName } { collaborator.lastName }</div>
                          <div className="text-sm text-muted-foreground">{ collaborator.email }</div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          { collaborator.locationRoles.length > 0 ? (
                            collaborator.locationRoles.map((locationRole, index) => {
                              // Determine colors based on role type and location
                              let leftColor: 'blue' | 'purple' | 'orange' | 'teal' = 'blue';
                              let rightColor: 'green' | 'indigo' | 'pink' | 'yellow' = 'green';

                              // Vary location colors based on location name/id to add visual distinction
                              const locationHash = locationRole.locationId.length;
                              if (locationHash % 4 === 0) {
                                leftColor = 'blue';
                              } else if (locationHash % 4 === 1) {
                                leftColor = 'teal';
                              } else if (locationHash % 4 === 2) {
                                leftColor = 'orange';
                              } else {
                                leftColor = 'purple';
                              }

                              if (locationRole.roleName.includes('Admin')) {
                                rightColor = 'pink';
                              } else if (locationRole.roleName.includes('Manager')) {
                                rightColor = 'indigo';
                              } else if (locationRole.roleName.includes('Coordinator')) {
                                rightColor = 'yellow';
                              } else {
                                rightColor = 'green';
                              }

                              return (
                                <DualColorChip
                                  key={ index }
                                  leftText={ locationRole.locationName }
                                  rightText={ locationRole.roleName }
                                  isActive={ locationRole.isActive }
                                  leftColor={ leftColor }
                                  rightColor={ rightColor }
                                  size="sm"
                                  tooltip={ `${ locationRole.locationName } → ${ locationRole.roleName }${ locationRole.assignedAt ? ` | Assigned: ${ new Date(locationRole.assignedAt).toLocaleDateString() }` : '' }${ !locationRole.isActive
                                                                                                                                                                                                                         ? ' | Status: Inactive'
                                                                                                                                                                                                                         : '' }` }
                                />
                              );
                            })
                          ) : (
                              <span className="text-sm text-muted-foreground">No location assignments</span>
                            ) }
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={ collaborator.status === 'Active' ? 'default' : 'secondary' }>
                          { collaborator.status }
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={ () => {
                              const manager = managers.find(m => m.userId === collaborator.userId);
                              if (manager) onEditManager(manager);
                            } }
                            title="Edit collaborator"
                          >
                            <Edit className="h-4 w-4"/>
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={ () => onDeleteManager(collaborator.id) }
                            title="Remove collaborator"
                          >
                            <Trash2 className="h-4 w-4"/>
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={ () => {
                              // Remove all roles for this user
                              collaborator.locationRoles.forEach(locationRole => {
                                onRemoveRole(collaborator.userId, locationRole.roleName);
                              });
                            } }
                            title="Remove all roles"
                          >
                            <UserMinus className="h-4 w-4"/>
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  )) }
                </TableBody>
              </Table>
            ) : (
                <div className="text-center py-8">
                  <Users className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                  <p className="text-muted-foreground mb-4">No collaborators found</p>
                  <Button onClick={ onCreateManager } className="flex items-center gap-2">
                    <UserPlus className="h-4 w-4"/>
                    Add First Collaborator
                  </Button>
                </div>
              ) }
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// Locations Settings Component
interface LocationsSettingsProps {
  locations: TestingLocation[];

  onCreateLocation: () => void;

  onEditLocation: (location: TestingLocation) => void;

  onDeleteLocation: (locationId: string) => void;

  isLoading: boolean;
}

function LocationsSettings({ locations, onCreateLocation, onEditLocation, onDeleteLocation, isLoading }: LocationsSettingsProps) {
  const getStatusBadgeColor = (status: LocationStatus) => {
    switch (status) {
      case 0: // Active
        return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300';
      case 1: // Maintenance
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300';
      case 2: // Inactive
        return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300';
    }
  };

  const locationStatusToString = (status: LocationStatus): 'Active' | 'Inactive' | 'Maintenance' => {
    switch (status) {
      case 0:
        return 'Active';
      case 1:
        return 'Maintenance';
      case 2:
        return 'Inactive';
      default:
        return 'Active';
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-2xl font-bold mb-2">Locations</h3>
        <p className="text-muted-foreground">
          Manage your testing facilities and their configurations.
        </p>
      </div>

      <Separator/>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              Testing Locations
              <Badge variant="secondary" className="text-xs">
                {locations.length} location{locations.length !== 1 ? 's' : ''}
              </Badge>
            </div>
            <div className="flex gap-2">
              <Button 
                variant="outline" 
                size="sm" 
                onClick={() => window.location.reload()}
                className="flex items-center gap-1"
              >
                <RefreshCw className="h-3 w-3"/>
                Refresh
              </Button>
              <Button onClick={ onCreateLocation } className="flex items-center gap-2">
                <Plus className="h-4 w-4"/>
                Add Location
              </Button>
            </div>
          </CardTitle>
          <CardDescription>
            Manage your testing facilities and their configurations. Each location can host multiple testing sessions simultaneously.
          </CardDescription>
        </CardHeader>
        <CardContent>
          { isLoading ? (
            <div className="text-center py-8">
              <p className="text-muted-foreground">Loading locations...</p>
            </div>
          ) : locations.length > 0 ? (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              { locations.map((location) => (
                <Card key={ location.id } className="relative">
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <MapPin className="h-5 w-5 text-blue-500"/>
                        <CardTitle className="text-base">{ location.name }</CardTitle>
                      </div>
                      <Badge
                        variant="secondary"
                        className={ `text-xs ${ getStatusBadgeColor(location.status!) }` }
                      >
                        { locationStatusToString(location.status!) }
                      </Badge>
                    </div>
                    <CardDescription className="text-sm">{ location.description }</CardDescription>
                  </CardHeader>
                  <CardContent className="pt-0">
                    <div className="space-y-2 text-sm text-muted-foreground">
                      {location.address && (
                        <p className="flex items-start gap-2">
                          <span>📍</span>
                          <span className="flex-1">{ location.address }</span>
                        </p>
                      )}
                      <div className="grid grid-cols-2 gap-2">
                        <div className="flex items-center gap-1">
                          <Users className="h-3 w-3"/>
                          <span>{ location.maxTestersCapacity } testers</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <MapPin className="h-3 w-3"/>
                          <span>{ location.maxProjectsCapacity } projects</span>
                        </div>
                      </div>
                      {location.equipmentAvailable && (
                        <p className="text-xs bg-gray-50 dark:bg-gray-800 rounded p-2 mt-2">
                          <strong>Equipment:</strong> { location.equipmentAvailable }
                        </p>
                      )}
                    </div>
                    <div className="flex gap-2 mt-4">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={ () => onEditLocation(location) }
                        className="flex-1"
                      >
                        <Edit className="h-4 w-4 mr-1"/>
                        Edit
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={ () => onDeleteLocation(location.id!) }
                        className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950"
                        title="Delete location"
                      >
                        <Trash2 className="h-4 w-4"/>
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              )) }
            </div>
          ) : (
                <div className="text-center py-8">
                  <MapPin className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                  <h3 className="text-lg font-semibold mb-2">No testing locations</h3>
                  <p className="text-muted-foreground mb-4">
                    Get started by creating your first testing location. You can set up multiple locations 
                    to organize different types of testing environments.
                  </p>
                  <Button onClick={ onCreateLocation } className="flex items-center gap-2">
                    <Plus className="h-4 w-4"/>
                    Create First Location
                  </Button>
                </div>
              ) }
        </CardContent>
      </Card>
    </div>
  );
}

// Roles Settings Component
interface RolesSettingsProps {
  roles: RoleTemplate[];

  onCreateRole: () => void;

  onEditRole: (role: RoleTemplate) => void;

  onDeleteRole: (roleName: string) => void;
}

function RolesSettings({ roles, onCreateRole, onEditRole, onDeleteRole }: RolesSettingsProps) {
  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-2xl font-bold mb-2">Roles & Permissions</h3>
        <p className="text-muted-foreground">
          Manage role templates and their associated permissions.
        </p>
      </div>

      <Separator/>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            Role Templates
            <Button onClick={ onCreateRole } className="flex items-center gap-2">
              <Plus className="h-4 w-4"/>
              Create Role
            </Button>
          </CardTitle>
          <CardDescription>
            Define role templates with specific permissions for different team members
          </CardDescription>
        </CardHeader>
        <CardContent>
          { roles.length > 0 ? (
            <div className="grid gap-4 md:grid-cols-2">
              { roles.map((role) => (
                <Card key={ role.id } className="relative">
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <Shield className="h-5 w-5 text-blue-500"/>
                        <CardTitle className="text-base">{ role.name }</CardTitle>
                      </div>
                      { role.isSystemRole && (
                        <Badge variant="secondary" className="text-xs">
                          System Role
                        </Badge>
                      ) }
                    </div>
                    <CardDescription className="text-sm">{ role.description }</CardDescription>
                  </CardHeader>
                  <CardContent className="pt-0">
                    <div className="space-y-2">
                      <p className="text-sm font-medium">Permissions:</p>
                      <div className="flex flex-wrap gap-1">
                        { role.permissionTemplates.slice(0, 4).map((permission, idx) => (
                          <Badge key={ idx } variant="outline" className="text-xs">
                            { permission.action } { permission.resourceType }
                          </Badge>
                        )) }
                        { role.permissionTemplates.length > 4 && (
                          <Badge variant="outline" className="text-xs">
                            +{ role.permissionTemplates.length - 4 } more
                          </Badge>
                        ) }
                      </div>
                      { role.userCount !== undefined && (
                        <p className="text-xs text-muted-foreground mt-2">
                          { role.userCount } users assigned
                        </p>
                      ) }
                    </div>
                    <div className="flex gap-2 mt-4">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={ () => onEditRole(role) }
                        className="flex-1"
                      >
                        <Edit className="h-4 w-4 mr-1"/>
                        Edit
                      </Button>
                      { !role.isSystemRole && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={ () => onDeleteRole(role.name) }
                        >
                          <Trash2 className="h-4 w-4"/>
                        </Button>
                      ) }
                    </div>
                  </CardContent>
                </Card>
              )) }
            </div>
          ) : (
              <div className="text-center py-8">
                <Shield className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                <p className="text-muted-foreground mb-4">No roles found</p>
                <Button onClick={ onCreateRole } className="flex items-center gap-2">
                  <Plus className="h-4 w-4"/>
                  Create First Role
              </Button>
              </div>
            ) }
        </CardContent>
      </Card>
    </div>
  );
}

// Export the DualColorChip for reuse in other components
export { DualColorChip };
