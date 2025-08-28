'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import type { TestingLocation as ApiTestingLocation, UserRoleAssignment as GeneratedUserRoleAssignment, LocationStatus } from '@/lib/api/generated/types.gen';
import { CollaboratorsSettings } from './sections/collaborators-settings';
// Legacy permission conversion utilities imported previously have been removed to avoid dual models.
// We now rely exclusively on aggregated boolean permissions returned by server actions in actions/testing-lab-roles.
import {
  createTestingLocationAction,
  deleteTestingLocationAction,
  getTestingLocationsAction,
  updateTestingLocationAction
} from '@/lib/actions/testing-lab-locations';
import { createRoleTemplateAction, deleteRoleTemplateAction, getTestingLabRoleTemplatesAction, RoleTemplate, updateRoleTemplateAction } from '@/lib/actions/testing-lab-roles';
import { getTestingLabSettings, updateTestingLabSettings } from '@/lib/actions/testing-lab-settings';
import {
  assignUserRoleAction,
  getTestingLabUserRoleAssignmentsAction,
  removeUserRoleAction
} from '@/lib/actions/testing-lab-user-roles';
import { ChevronRight, Edit, MapPin, Plus, RefreshCw, Settings, Shield, Trash2, Users } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Helper to map aggregated API permissions (role.permissions) to form permission shape
function mapAggregatedPermissionsToForm(agg: any) {
  if (!agg) return initialRoleFormData.permissions;
  return {
    // Sessions
    createSession: !!(agg.canCreateSessions ?? agg.CanCreateSessions),
    readSession: !!(agg.canViewSessions ?? agg.CanViewSessions),
    editSession: !!(agg.canEditSessions ?? agg.CanEditSessions),
    deleteSession: !!(agg.canDeleteSessions ?? agg.CanDeleteSessions),
    // Locations
    createLocation: !!(agg.canCreateLocations ?? agg.CanCreateLocations),
    readLocation: !!(agg.canViewLocations ?? agg.CanViewLocations),
    editLocation: !!(agg.canEditLocations ?? agg.CanEditLocations),
    deleteLocation: !!(agg.canDeleteLocations ?? agg.CanDeleteLocations),
    // Feedback
    createFeedback: !!(agg.canCreateFeedback ?? agg.CanCreateFeedback),
    readFeedback: !!(agg.canViewFeedback ?? agg.CanViewFeedback),
    editFeedback: !!(agg.canEditFeedback ?? agg.CanEditFeedback),
    deleteFeedback: !!(agg.canDeleteFeedback ?? agg.CanDeleteFeedback),
    moderateFeedback: !!(agg.canModerateFeedback ?? agg.CanModerateFeedback),
    // Requests
    createRequest: !!(agg.canCreateRequests ?? agg.CanCreateRequests),
    readRequest: !!(agg.canViewRequests ?? agg.CanViewRequests),
    editRequest: !!(agg.canEditRequests ?? agg.CanEditRequests),
    deleteRequest: !!(agg.canDeleteRequests ?? agg.CanDeleteRequests),
    approveRequest: !!(agg.canApproveRequests ?? agg.CanApproveRequests),
    // Participants
    manageParticipant: !!(agg.canManageParticipants ?? agg.CanManageParticipants),
    readParticipant: !!(agg.canViewParticipants ?? agg.CanViewParticipants),
  };
}

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
      className={`inline-flex items-center rounded-md font-medium transition-colors border ${borderColor} hover:shadow-sm cursor-default`}
      title={tooltip || `${leftText} → ${rightText}${!isActive ? ' (Inactive)' : ''}`}
    >
      {/* Left part (Location) */}
      <span className={`${sizeClasses} rounded-l-md ${colorClasses[leftColor]}`}>
        {leftText}
      </span>
      {/* Separator */}
      <div className={`w-px h-4 ${separatorColor}`} />
      {/* Right part (Role) */}
      <span className={`${sizeClasses} rounded-r-md ${colorClasses[rightColor]}`}>
        {rightText}
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
// Use the proper RoleTemplate from actions which has both permissions and permissionTemplates

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
    case 0: return 'Active';      // ACTIVE = 0
    case 1: return 'Maintenance'; // MAINTENANCE = 1  
    case 2: return 'Inactive';    // INACTIVE = 2
    default: return 'Active';
  }
};

const stringToLocationStatus = (status: 'Active' | 'Inactive' | 'Maintenance'): LocationStatus => {
  switch (status) {
    case 'Active': return 0;      // ACTIVE = 0
    case 'Maintenance': return 1; // MAINTENANCE = 1
    case 'Inactive': return 2;    // INACTIVE = 2
    default: return 0; // Default to Active (0)
  }
};

// Reusable GUID validator (RFC 4122 variants 1-5)
const isGuid = (val: string) => /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/.test(val);

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

import { usePathname, useRouter } from 'next/navigation';
import { GeneralSettings, GeneralSettingsState } from './sections/general-settings';

export function TestingLabSettings({ initialSection = 'general', sectionOnly = false }: { initialSection?: string; sectionOnly?: boolean }) {
  const router = useRouter();
  const pathname = usePathname();
  // Navigation state
  const [currentSection, setCurrentSection] = useState(initialSection);

  // Data states
  const [locations, setLocations] = useState<TestingLocation[]>([]);
  const [managers, setManagers] = useState<TestingLabManager[]>([]);
  const [roles, setRoles] = useState<RoleTemplate[]>([]);
  const [userRoles, setUserRoles] = useState<UserRoleAssignment[]>([]);

  // Dialog states
  const [isLocationDialogOpen, setIsLocationDialogOpen] = useState(false);
  const [isManagerDialogOpen, setIsManagerDialogOpen] = useState(false);
  const [isRoleDialogOpen, setIsRoleDialogOpen] = useState(false);
  const [isUserRoleDialogOpen, setIsUserRoleDialogOpen] = useState(false);

  // Editing states
  const [editingLocation, setEditingLocation] = useState<TestingLocation | null>(null);
  const [editingManager, setEditingManager] = useState<TestingLabManager | null>(null);
  const [editingRole, setEditingRole] = useState<RoleTemplate | null>(null);
  const [originalRoleId, setOriginalRoleId] = useState<string | null>(null); // Store original ID for API calls
  const [originalRoleName, setOriginalRoleName] = useState<string | null>(null); // Store original name for comparison
  const [selectedUserEmail, setSelectedUserEmail] = useState<string>('');

  // Form states
  const [formData, setFormData] = useState<LocationFormData>(initialFormData);
  const [managerFormData, setManagerFormData] = useState<ManagerFormData>(initialManagerFormData);
  const [roleFormData, setRoleFormData] = useState<RoleFormData>(initialRoleFormData);
  const [isLoading, setIsLoading] = useState(false);

  // General settings state
  const [generalSettings, setGeneralSettings] = useState<GeneralSettingsState>({
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

  const handleNavigate = (sectionId: string) => {
    setCurrentSection(sectionId);
    if (!pathname) return;
    // Ensure base always ends with /settings
    const settingsIndex = pathname.indexOf('/settings');
    if (settingsIndex === -1) return; // unexpected, abort
    const base = pathname.substring(0, settingsIndex + '/settings'.length);
    try { router.push(`${base}/${sectionId}`); } catch { }
  };

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
            equipmentAvailable: 'Mobile Devices, Tablets, Network Simulation Tools',
            status: 0,
            createdAt: '2024-01-20T10:00:00Z',
            updatedAt: '2024-01-20T10:00:00Z',
          },
        ];
        setLocations(mockLocations);
      }
    } finally {
      setIsLoading(false);
    }
  };

  // Placeholder manager loader (API not yet implemented)
  const loadManagers = async () => {
    try {
      // If an API becomes available, replace this placeholder
      return; // keep existing state (demo/manually managed)
    } catch (e) {
      console.error('Failed to load managers (placeholder):', e);
    }
  };

  // GeneralSettings extracted to sections/general-settings.tsx

  const loadRoles = async () => {
    try {
      // Use server action to get role templates
      const apiRoles = await getTestingLabRoleTemplatesAction();
      console.log('=== ROLES LOADED FROM API ===');
      console.log('Raw API roles:', apiRoles);
      setRoles(apiRoles as any[]);
    } catch (error) {
      console.error('Failed to load roles:', error);
      if (error instanceof Error && error.message.includes('permission')) {
        toast.error('You do not have permission to view role templates');
      } else {
        toast.error('Failed to load testing lab roles');
      }
    }
  };

  const loadUserRoles = async () => {
    try {
      // Use server action to get user role assignments
      const apiUserRoles = await getTestingLabUserRoleAssignmentsAction();
      setUserRoles(apiUserRoles as any[]);
    } catch (error) {
      console.error('Failed to load user roles:', error);
      if (error instanceof Error && error.message.includes('permission')) {
        toast.error('You do not have permission to view user role assignments');
      } else {
        toast.error('Failed to load user role assignments');
      }
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

  const saveGeneralSettings = async (settings: GeneralSettingsState) => {
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

  // Generic confirm dialog state
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmAction, setConfirmAction] = useState<null | (() => Promise<void>)>(null);
  const [confirmTitle, setConfirmTitle] = useState('Confirm Action');
  const [confirmDescription, setConfirmDescription] = useState('Are you sure?');
  const [confirmBusy, setConfirmBusy] = useState(false);

  function openConfirm(opts: { title: string; description: string; onConfirm: () => Promise<void> }) {
    setConfirmTitle(opts.title);
    setConfirmDescription(opts.description);
    setConfirmAction(() => opts.onConfirm);
    setConfirmOpen(true);
  }

  const handleDeleteLocation = async (id: string) => {
    const locationToDelete = locations.find(loc => loc.id === id);
    const locationName = locationToDelete?.name || 'this location';
    openConfirm({
      title: 'Delete Location',
      description: `Delete "${locationName}"? This cannot be undone and may impact ongoing sessions.`,
      onConfirm: async () => {
        try {
          await deleteTestingLocationAction(id);
          setLocations(prev => prev.filter(location => location.id !== id));
          toast.success('Location deleted successfully');
        } catch (error) {
          console.warn('Failed to delete location:', error);
          if (error instanceof Error) toast.error(error.message); else toast.error('Unexpected error deleting location');
        }
      }
    });
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

  const handleDeleteManager = async (managerIdOrUserId: string) => {
    const targetManager = managers.find(m => m.id === managerIdOrUserId) || managers.find(m => m.userId === managerIdOrUserId);
    const name = targetManager ? `${targetManager.firstName || ''} ${targetManager.lastName || ''}`.trim() || targetManager.email : 'this collaborator';
    openConfirm({
      title: 'Remove Collaborator',
      description: `Remove ${name}? This removes their Testing Lab roles.`,
      onConfirm: async () => {
        try {
          const userId = targetManager?.userId;
          if (userId) {
            const assignmentsForUser = userRoles.filter(a => a.userId === userId);
            for (const a of assignmentsForUser) {
              if (a.roleName) {
                try { await removeUserRoleAction(userId, a.roleName); } catch (e) { console.warn('Revoke failed', e); }
              }
            }
            if (targetManager?.role && !assignmentsForUser.some(a => a.roleName === targetManager.role)) {
              try { await removeUserRoleAction(userId, targetManager.role); } catch (e) { console.warn('Manager role revoke failed', e); }
            }
          }
          setManagers(prev => prev.filter(m => m.id !== targetManager?.id));
          if (targetManager?.userId) {
            const removedAssignments = userRoles.filter(ur => ur.userId === targetManager.userId);
            if (removedAssignments.length) {
              setRoles(prev => prev.map(r => {
                const removedCount = removedAssignments.filter(a => a.roleTemplateId === r.id).length;
                return removedCount ? { ...r, userCount: Math.max((r.userCount || 0) - removedCount, 0) } : r;
              }));
            }
            setUserRoles(prev => prev.filter(ur => ur.userId !== targetManager.userId));
          }
          toast.success('Collaborator removed');
        } catch (error) {
          console.error('Failed to remove collaborator:', error);
          toast.error('Failed to remove collaborator');
        }
      }
    });
  };

  const handleSubmitLocation = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (editingLocation) {
        // If the existing location has a non-GUID id (e.g., locally generated fallback like a numeric or timestamp),
        // we should treat this as an unsynced local record and perform a create instead of update.
        const targetId = editingLocation.id!;
        const locationPayload = {
          name: formData.name,
          description: formData.description,
          address: formData.address,
          maxTestersCapacity: formData.maxTestersCapacity,
          maxProjectsCapacity: formData.maxProjectsCapacity,
          equipmentAvailable: formData.equipmentAvailable,
          status: stringToLocationStatus(formData.status),
        };

        if (!isGuid(targetId)) {
          console.warn('Non-GUID location id detected, creating new location instead of updating:', targetId);
          try {
            const created = await createTestingLocationAction(locationPayload as any);
            // Replace the local placeholder entry
            setLocations(locations.map((loc) => (loc.id === targetId ? (created as TestingLocation) : loc)));
            toast.success('Location synced successfully');
          } catch (createErr) {
            console.error('Failed to sync placeholder location, using local update fallback:', createErr);
            const updatedLocal: TestingLocation = {
              ...editingLocation,
              ...locationPayload,
              updatedAt: new Date().toISOString(),
            };
            setLocations(locations.map((loc) => (loc.id === targetId ? updatedLocal : loc)));
            toast.success('Location updated locally (unsynced)');
          }
          setIsLocationDialogOpen(false);
          setFormData(initialFormData);
          setEditingLocation(null);
          return;
        }
        // Update existing location
        try {
          const updatedLocation = await updateTestingLocationAction(targetId, locationPayload as any);
          setLocations(locations.map((loc) => (loc.id === editingLocation.id ? updatedLocation as TestingLocation : loc)));
          toast.success('Location updated successfully');
        } catch (apiError) {
          // Fallback to local update when API is unavailable
          console.warn('API unavailable, using local update:', apiError);
          const updatedLocation: TestingLocation = {
            ...editingLocation,
            ...locationPayload,
            updatedAt: new Date().toISOString(),
          };
          setLocations(locations.map((loc) => (loc.id === editingLocation.id ? updatedLocation : loc)));
          toast.success('Location updated (demo mode)');
        }
      } else {
        // Create new location
        try {
          const newLocation = await createTestingLocationAction({
            name: formData.name,
            description: formData.description,
            address: formData.address,
            maxTestersCapacity: formData.maxTestersCapacity,
            maxProjectsCapacity: formData.maxProjectsCapacity,
            equipmentAvailable: formData.equipmentAvailable,
            status: stringToLocationStatus(formData.status),
          });
          setLocations([...locations, newLocation as TestingLocation]);
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
    console.log('=== handleEditRole DEBUG ===');
    console.log('Full role object:', role);
    console.log('role.permissions:', role.permissions);
    console.log('role.permissionTemplates:', role.permissionTemplates);

    // Map aggregated permissions object to form shape (single source of truth)
    const finalPerms = mapAggregatedPermissionsToForm(role.permissions);
    console.log('Mapped permissions from aggregate (single model):', finalPerms);

    setEditingRole(role);
    setOriginalRoleId(role.id);
    setOriginalRoleName(role.name);
    setRoleFormData({
      name: role.name,
      description: role.description,
      permissions: finalPerms,
    });
    setIsRoleDialogOpen(true);
  };

  const handleDeleteRole = async (roleId: string) => {
    console.log('handleDeleteRole called with roleId:', roleId);
    const role = roles.find(r => r.id === roleId);
    console.log('Found role:', role);
    // Allow deletion of all roles, including system roles

    if (!role) {
      toast.error('Role not found');
      return;
    }

    try {
      console.log('Calling deleteRoleTemplateAction with role.id:', role.id);
      await deleteRoleTemplateAction(role.id);
      setRoles(roles.filter(r => r.id !== roleId));
      toast.success('Role deleted successfully');
    } catch (error) {
      console.error('Failed to delete role:', error);
      if (error instanceof Error) {
        toast.error(error.message);
      } else {
        toast.error('Failed to delete role');
      }
    }
  };

  const handleSubmitRole = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      console.log('=== SAVE OPERATION START ===');
      console.log('Raw form permissions object (expected by server action):', roleFormData.permissions);

      if (editingRole) {
        // Update existing role
        console.log('Updating role with ID:', originalRoleId);
        const updatedRole = await updateRoleTemplateAction(originalRoleId!, {
          name: roleFormData.name !== originalRoleName ? roleFormData.name : undefined,
          description: roleFormData.description,
          // Pass raw form permissions; server action converts to backend DTO
          permissions: roleFormData.permissions,
        });
        console.log('Update API response:', updatedRole);

        await loadRoles();
        toast.success('Role updated successfully');
      } else {
        // Create new role
        const newRole = await createRoleTemplateAction({
          name: roleFormData.name,
          description: roleFormData.description,
          permissions: roleFormData.permissions,
        });
        console.log('Create API response:', newRole);

        await loadRoles();
        toast.success('Role created successfully');
      }

      setIsRoleDialogOpen(false);
      setRoleFormData(initialRoleFormData);
      setEditingRole(null);
      setOriginalRoleId(null);
      setOriginalRoleName(null);
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
      const newAssignment = await assignUserRoleAction({
        userId: selectedUserEmail,
        roleName: selectedRole.name,
        userEmail: selectedUserEmail
      });

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
        // Default module context for Testing Lab (cast to satisfy generated type expectations)
        module: 'TestingLab' as any,
      };

      setUserRoles([...userRoles, compatibleAssignment]);

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
        await removeUserRoleAction(assignment.userId || '', assignment.roleName || '');

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

  const sectionContent = (
    <>
      {currentSection === 'general' && <GeneralSettings generalSettings={generalSettings} setGeneralSettings={setGeneralSettings} saveGeneralSettings={saveGeneralSettings} />}
      {currentSection === 'collaborators' && (
        <CollaboratorsSettings
          managers={managers}
          roles={roles}
          userRoles={userRoles}
          locations={locations}
          onCreateManager={handleCreateManager}
          onEditManager={handleEditManager}
          onDeleteManager={handleDeleteManager}
          onEditRole={handleEditRole}
          onAssignRole={handleAssignRole}
          onRemoveRole={handleRemoveUserRole}
          onRoleAssigned={(assignment) => {
            if (userRoles.some(ur => ur.userId === assignment.userId && ur.roleName === assignment.roleName)) return;
            setUserRoles(prev => [...prev, assignment]);
            setRoles(prev => prev.map(r => r.id === assignment.roleTemplateId ? { ...r, userCount: (r.userCount || 0) + 1 } : r));
          }}
        />
      )}
      {currentSection === 'locations' && (
        <LocationsSettings
          locations={locations}
          onCreateLocation={handleCreateLocation}
          onEditLocation={handleEditLocation}
          onDeleteLocation={handleDeleteLocation}
          onRefreshLocations={loadLocations}
          isLoading={isLoading}
        />
      )}
      {currentSection === 'roles' && (
        <RolesSettings
          roles={roles}
          onCreateRole={handleCreateRole}
          onEditRole={handleEditRole}
          onDeleteRole={handleDeleteRole}
        />
      )}
    </>
  );

  if (sectionOnly) {
    return <div className="flex flex-col flex-1">{sectionContent}</div>;
  }

  return (
    <div className="flex flex-row flex-1">
      {/* Sidebar Navigation */}
      <div className="w-64 border-r border-border bg-muted/30">
        <div className="p-6">
          <nav className="space-y-1">
            {navigationItems.map((item) => {
              const Icon = item.icon;
              const isActive = currentSection === item.id;
              return (
                <button
                  key={item.id}
                  onClick={() => handleNavigate(item.id)}
                  className={`w-full flex items-center gap-3 px-3 py-2 text-sm rounded-lg transition-colors ${isActive
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:text-foreground hover:bg-muted'
                    }`}
                >
                  <Icon className="h-4 w-4" />
                  {item.label}
                  {isActive && <ChevronRight className="h-4 w-4 ml-auto" />}
                </button>
              );
            })}
          </nav>
        </div>
      </div>
      {/* Main Content */}
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="flex flex-1 container">
          <div className="flex flex-col flex-1 ">
            {sectionContent}
          </div>
        </div>
      </div>
      {/* Dialogs */}
      <Dialog open={isLocationDialogOpen} onOpenChange={setIsLocationDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{editingLocation ? 'Edit Location' : 'Create New Location'}</DialogTitle>
            <DialogDescription>
              {editingLocation ? 'Update the location details' : 'Add a new testing location to your lab'}
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="name">Location Name *</Label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., Main Testing Lab, VR Suite, Mobile Lab"
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                placeholder="Describe the testing environment, specializations, or unique features"
                className="min-h-[60px]"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="address">Address</Label>
              <Textarea
                id="address"
                value={formData.address}
                onChange={(e) => setFormData({ ...formData, address: e.target.value })}
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
                  onChange={(e) => setFormData({ ...formData, maxTestersCapacity: parseInt(e.target.value) || 0 })}
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
                  onChange={(e) => setFormData({ ...formData, maxProjectsCapacity: parseInt(e.target.value) || 0 })}
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
                onChange={(e) => setFormData({ ...formData, equipmentAvailable: e.target.value })}
                placeholder="VR Headsets (10x), Gaming PCs (15x), Recording Equipment, Tablets, etc."
                className="min-h-[60px]"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <Select
                value={formData.status}
                onValueChange={(value: 'Active' | 'Inactive' | 'Maintenance') =>
                  setFormData({ ...formData, status: value })
                }
              >
                <SelectTrigger>
                  <SelectValue />
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
            <Button variant="outline" onClick={() => setIsLocationDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={async () => {
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

                  console.log('Form data before sending:', {
                    formData,
                    locationData,
                    statusMapping: {
                      originalStatus: formData.status,
                      mappedStatus: locationData.status
                    }
                  });

                  if (editingLocation) {
                    const targetId = editingLocation.id!;
                    if (!isGuid(targetId)) {
                      console.warn('Non-GUID location id detected (dialog footer), creating new location instead of updating:', targetId);
                      try {
                        const created = await createTestingLocationAction(locationData as any);
                        setLocations(locations.map((loc) => (loc.id === targetId ? (created as TestingLocation) : loc)));
                        toast.success('Location synced successfully');
                        setIsLocationDialogOpen(false);
                        return;
                      } catch (createErr) {
                        console.error('Failed to sync placeholder location from dialog footer, using local update fallback:', createErr);
                        const updatedLocal: TestingLocation = {
                          ...editingLocation,
                          ...locationData,
                          updatedAt: new Date().toISOString(),
                        } as TestingLocation;
                        setLocations(locations.map((loc) => (loc.id === targetId ? updatedLocal : loc)));
                        toast.success('Location updated locally (unsynced)');
                        setIsLocationDialogOpen(false);
                        return;
                      }
                    }
                    const updatedLocation = await updateTestingLocationAction(targetId, locationData as any);
                    setLocations(locations.map((loc) => (loc.id === targetId ? (updatedLocation as TestingLocation) : loc)));
                    toast.success('Location updated successfully');
                  } else {
                    const newLocation = await createTestingLocationAction(locationData);
                    setLocations([...locations, newLocation as TestingLocation]);
                    toast.success('Location created successfully');
                  }
                  setIsLocationDialogOpen(false);
                } catch (error) {
                  console.error('Failed to save location:', error);

                  if (error instanceof Error) {
                    toast.error(error.message);
                  } else {
                    toast.error('An unexpected error occurred. Please try again.');
                  }
                }
              }}
              disabled={!formData.name.trim() || !formData.description.trim() || !formData.address.trim() || formData.maxTestersCapacity <= 0 || formData.maxProjectsCapacity <= 0}
            >
              {editingLocation ? 'Update Location' : 'Create Location'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      {/* Confirmation Dialog */}
      <Dialog open={confirmOpen} onOpenChange={(o) => { if (!o && !confirmBusy) setConfirmOpen(false); }}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>{confirmTitle}</DialogTitle>
            <DialogDescription>{confirmDescription}</DialogDescription>
          </DialogHeader>
          <DialogFooter className="flex gap-2 justify-end">
            <Button variant="outline" disabled={confirmBusy} onClick={() => setConfirmOpen(false)}>Cancel</Button>
            <Button
              variant="destructive"
              disabled={confirmBusy || !confirmAction}
              onClick={async () => {
                if (!confirmAction) return;
                setConfirmBusy(true);
                await confirmAction();
                setConfirmBusy(false);
                setConfirmOpen(false);
              }}
            >{confirmBusy ? 'Working...' : 'Confirm'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Role Dialog */}
      <Dialog open={isRoleDialogOpen} onOpenChange={setIsRoleDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{editingRole ? 'Edit Role Template' : 'Create New Role Template'}</DialogTitle>
            <DialogDescription>
              {editingRole ? 'Update the role template details and permissions' : 'Create a new role template with specific permissions'}
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleSubmitRole}>
            <div className="grid gap-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="role-name">Role Name *</Label>
                <Input
                  id="role-name"
                  value={roleFormData.name}
                  onChange={(e) => setRoleFormData({ ...roleFormData, name: e.target.value })}
                  placeholder="e.g., TestingLabAdmin, GameTester, QALead"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="role-description">Description *</Label>
                <Textarea
                  id="role-description"
                  value={roleFormData.description}
                  onChange={(e) => setRoleFormData({ ...roleFormData, description: e.target.value })}
                  placeholder="Describe the purpose and responsibilities of this role"
                  className="min-h-[60px]"
                  required
                />
              </div>

              <div className="space-y-4">
                <h4 className="font-medium text-sm">Permissions</h4>

                {/* Session Permissions */}
                <div className="space-y-3">
                  <h5 className="font-medium text-sm text-muted-foreground">Testing Sessions</h5>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="create-session"
                        checked={roleFormData.permissions.createSession}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, createSession: checked }
                        })}
                      />
                      <Label htmlFor="create-session">Create Sessions</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="edit-session"
                        checked={roleFormData.permissions.editSession}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, editSession: checked }
                        })}
                      />
                      <Label htmlFor="edit-session">Edit Sessions</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="delete-session"
                        checked={roleFormData.permissions.deleteSession}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, deleteSession: checked }
                        })}
                      />
                      <Label htmlFor="delete-session">Delete Sessions</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="read-session"
                        checked={roleFormData.permissions.readSession}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, readSession: checked }
                        })}
                      />
                      <Label htmlFor="read-session">View Sessions</Label>
                    </div>
                  </div>
                </div>

                {/* Location Permissions */}
                <div className="space-y-3">
                  <h5 className="font-medium text-sm text-muted-foreground">Testing Locations</h5>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="create-location"
                        checked={roleFormData.permissions.createLocation}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, createLocation: checked }
                        })}
                      />
                      <Label htmlFor="create-location">Create Locations</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="edit-location"
                        checked={roleFormData.permissions.editLocation}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, editLocation: checked }
                        })}
                      />
                      <Label htmlFor="edit-location">Edit Locations</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="delete-location"
                        checked={roleFormData.permissions.deleteLocation}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, deleteLocation: checked }
                        })}
                      />
                      <Label htmlFor="delete-location">Delete Locations</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="read-location"
                        checked={roleFormData.permissions.readLocation}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, readLocation: checked }
                        })}
                      />
                      <Label htmlFor="read-location">View Locations</Label>
                    </div>
                  </div>
                </div>

                {/* Feedback Permissions */}
                <div className="space-y-3">
                  <h5 className="font-medium text-sm text-muted-foreground">Testing Feedback</h5>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="create-feedback"
                        checked={roleFormData.permissions.createFeedback}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, createFeedback: checked }
                        })}
                      />
                      <Label htmlFor="create-feedback">Create Feedback</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="edit-feedback"
                        checked={roleFormData.permissions.editFeedback}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, editFeedback: checked }
                        })}
                      />
                      <Label htmlFor="edit-feedback">Edit Feedback</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="delete-feedback"
                        checked={roleFormData.permissions.deleteFeedback}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, deleteFeedback: checked }
                        })}
                      />
                      <Label htmlFor="delete-feedback">Delete Feedback</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="read-feedback"
                        checked={roleFormData.permissions.readFeedback}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, readFeedback: checked }
                        })}
                      />
                      <Label htmlFor="read-feedback">View Feedback</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="moderate-feedback"
                        checked={roleFormData.permissions.moderateFeedback}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, moderateFeedback: checked }
                        })}
                      />
                      <Label htmlFor="moderate-feedback">Moderate Feedback</Label>
                    </div>
                  </div>
                </div>

                {/* Request Permissions */}
                <div className="space-y-3">
                  <h5 className="font-medium text-sm text-muted-foreground">Testing Requests</h5>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="create-request"
                        checked={roleFormData.permissions.createRequest}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, createRequest: checked }
                        })}
                      />
                      <Label htmlFor="create-request">Create Requests</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="edit-request"
                        checked={roleFormData.permissions.editRequest}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, editRequest: checked }
                        })}
                      />
                      <Label htmlFor="edit-request">Edit Requests</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="delete-request"
                        checked={roleFormData.permissions.deleteRequest}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, deleteRequest: checked }
                        })}
                      />
                      <Label htmlFor="delete-request">Delete Requests</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="read-request"
                        checked={roleFormData.permissions.readRequest}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, readRequest: checked }
                        })}
                      />
                      <Label htmlFor="read-request">View Requests</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="approve-request"
                        checked={roleFormData.permissions.approveRequest}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, approveRequest: checked }
                        })}
                      />
                      <Label htmlFor="approve-request">Approve Requests</Label>
                    </div>
                  </div>
                </div>

                {/* Participant Permissions */}
                <div className="space-y-3">
                  <h5 className="font-medium text-sm text-muted-foreground">Testing Participants</h5>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="manage-participant"
                        checked={roleFormData.permissions.manageParticipant}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, manageParticipant: checked }
                        })}
                      />
                      <Label htmlFor="manage-participant">Manage Participants</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="read-participant"
                        checked={roleFormData.permissions.readParticipant}
                        onCheckedChange={(checked) => setRoleFormData({
                          ...roleFormData,
                          permissions: { ...roleFormData.permissions, readParticipant: checked }
                        })}
                      />
                      <Label htmlFor="read-participant">View Participants</Label>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => {
                setIsRoleDialogOpen(false);
                setEditingRole(null);
                setOriginalRoleId(null);
                setOriginalRoleName(null);
                setRoleFormData(initialRoleFormData);
              }}>
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={!roleFormData.name.trim() || !roleFormData.description.trim()}
              >
                {editingRole ? 'Update Role' : 'Create Role'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// Individual Settings Components

// General Settings Component
// General settings component removed (now imported)


// Locations Settings Component
interface LocationsSettingsProps {
  locations: TestingLocation[];

  onCreateLocation: () => void;

  onEditLocation: (location: TestingLocation) => void;

  onDeleteLocation: (locationId: string) => void;

  onRefreshLocations: () => void;

  isLoading: boolean;
}

function LocationsSettings({ locations, onCreateLocation, onEditLocation, onDeleteLocation, onRefreshLocations, isLoading }: LocationsSettingsProps) {
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

      <Separator />

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
                onClick={() => {
                  onRefreshLocations();
                }}
                className="flex items-center gap-1"
              >
                <RefreshCw className="h-3 w-3" />
                Refresh
              </Button>
              <Button onClick={onCreateLocation} className="flex items-center gap-2">
                <Plus className="h-4 w-4" />
                Add Location
              </Button>
            </div>
          </CardTitle>
          <CardDescription>
            Manage your testing facilities and their configurations. Each location can host multiple testing sessions simultaneously.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">
              <p className="text-muted-foreground">Loading locations...</p>
            </div>
          ) : locations.length > 0 ? (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {locations.map((location) => (
                <Card key={location.id} className="relative">
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <MapPin className="h-5 w-5 text-blue-500" />
                        <CardTitle className="text-base">{location.name}</CardTitle>
                      </div>
                      <Badge
                        variant="secondary"
                        className={`text-xs ${getStatusBadgeColor(location.status!)}`}
                      >
                        {locationStatusToString(location.status!)}
                      </Badge>
                    </div>
                    <CardDescription className="text-sm">{location.description}</CardDescription>
                  </CardHeader>
                  <CardContent className="pt-0">
                    <div className="space-y-2 text-sm text-muted-foreground">
                      {location.address && (
                        <p className="flex items-start gap-2">
                          <span>📍</span>
                          <span className="flex-1">{location.address}</span>
                        </p>
                      )}
                      <div className="grid grid-cols-2 gap-2">
                        <div className="flex items-center gap-1">
                          <Users className="h-3 w-3" />
                          <span>{location.maxTestersCapacity} testers</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <MapPin className="h-3 w-3" />
                          <span>{location.maxProjectsCapacity} projects</span>
                        </div>
                      </div>
                      {location.equipmentAvailable && (
                        <p className="text-xs bg-gray-50 dark:bg-gray-800 rounded p-2 mt-2">
                          <strong>Equipment:</strong> {location.equipmentAvailable}
                        </p>
                      )}
                    </div>
                    <div className="flex gap-2 mt-4">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => onEditLocation(location)}
                        className="flex-1"
                      >
                        <Edit className="h-4 w-4 mr-1" />
                        Edit
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => onDeleteLocation(location.id!)}
                        className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950"
                        title="Delete location"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <div className="text-center py-8">
              <MapPin className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No testing locations</h3>
              <p className="text-muted-foreground mb-4">
                Get started by creating your first testing location. You can set up multiple locations
                to organize different types of testing environments.
              </p>
              <Button onClick={onCreateLocation} className="flex items-center gap-2">
                <Plus className="h-4 w-4" />
                Create First Location
              </Button>
            </div>
          )}
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

  onDeleteRole: (roleId: string) => void; // expects GUID id
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

      <Separator />

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            Role Templates
            <Button onClick={onCreateRole} className="flex items-center gap-2">
              <Plus className="h-4 w-4" />
              Create Role
            </Button>
          </CardTitle>
          <CardDescription>
            Define role templates with specific permissions for different team members
          </CardDescription>
        </CardHeader>
        <CardContent>
          {roles.length > 0 ? (
            <div className="grid gap-4 md:grid-cols-2">
              {roles.map((role) => (
                <Card key={role.id} className="relative">
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <Shield className="h-5 w-5 text-blue-500" />
                        <CardTitle className="text-base">{role.name}</CardTitle>
                      </div>
                      {role.isSystemRole && (
                        <Badge variant="secondary" className="text-xs">
                          System Role
                        </Badge>
                      )}
                    </div>
                    <CardDescription className="text-sm">{role.description}</CardDescription>
                  </CardHeader>
                  <CardContent className="pt-0">
                    <div className="space-y-2">
                      <p className="text-sm font-medium">Permissions:</p>
                      <div className="flex flex-wrap gap-1">
                        {(role.permissionTemplates || []).slice(0, 4).map((permission, index) => (
                          <Badge key={`${role.id}-${permission.action}-${permission.resourceType}-${index}`} variant="outline" className="text-xs">
                            {permission.action} {permission.resourceType}
                          </Badge>
                        ))}
                        {(role.permissionTemplates || []).length > 4 && (
                          <Badge variant="outline" className="text-xs">
                            +{(role.permissionTemplates || []).length - 4} more
                          </Badge>
                        )}
                      </div>
                      {role.userCount !== undefined && (
                        <p className="text-xs text-muted-foreground mt-2">
                          {role.userCount} users assigned
                        </p>
                      )}
                    </div>
                    <div className="flex gap-2 mt-4">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => onEditRole(role)}
                        className="flex-1"
                      >
                        <Edit className="h-4 w-4 mr-1" />
                        Edit
                      </Button>
                      {!role.isSystemRole && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => onDeleteRole(role.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <div className="text-center py-8">
              <Shield className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <p className="text-muted-foreground mb-4">No roles found</p>
              <Button onClick={onCreateRole} className="flex items-center gap-2">
                <Plus className="h-4 w-4" />
                Create First Role
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// Export the DualColorChip for reuse in other components
export { DualColorChip };

