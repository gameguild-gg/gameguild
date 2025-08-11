/* eslint-disable prettier/prettier */
'use client';

import { useState, useEffect, useMemo } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { useUserContext } from '@/lib/user-management/users/users.context';
import { useTenant } from '@/components/tenant/context/tenant-provider';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, DropdownMenuSeparator, DropdownMenuCheckboxItem } from '@/components/ui/dropdown-menu';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import {
  Filter,
  RefreshCw,
  MoreHorizontal,
  Eye,
  MessageSquare,
  UserCheck,
  UserX,
  Download,
  Users,
  Clock,
  Shield,
  Crown,
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  SortAsc,
  SortDesc,
  X,
  Database,
  Key,
  Activity,
  Settings2,
  Columns
} from 'lucide-react';
import { toast } from 'sonner';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';
import { cn } from '@/lib/utils';

// Enhanced types for user management
interface EnhancedUser extends UserResponseDto {
  lastActive?: string;
  ownedObjectsCount?: number;
  grantsReceivedCount?: number;
  tenantMemberships?: {
    tenantId: string;
    tenantName: string;
    role: 'owner' | 'admin' | 'member';
  }[];
  avatar?: string;
  username?: string;
}

interface UserFilters {
  search: string;
  status: 'all' | 'active' | 'suspended' | 'pending';
  tenant: string;
  lastActive: 'all' | '7days' | '30days' | '90days' | 'never';
  accessType: 'all' | 'owns' | 'granted' | 'delegated';
}

interface UserSort {
  field: 'name' | 'email' | 'lastActive' | 'createdAt' | 'status';
  direction: 'asc' | 'desc';
}

interface UserPagination {
  page: number;
  limit: number;
  total: number;
}

interface ColumnConfig {
  id: string;
  label: string;
  sortable: boolean;
  visible: boolean;
  required?: boolean; // Some columns like actions should always be visible
}

const DEFAULT_COLUMNS: ColumnConfig[] = [
  { id: 'user', label: 'User', sortable: true, visible: true, required: true },
  { id: 'contact', label: 'Contact', sortable: true, visible: true },
  { id: 'tenant', label: 'Tenant', sortable: false, visible: true },
  { id: 'status', label: 'Status', sortable: true, visible: true },
  { id: 'lastActive', label: 'Last Active', sortable: true, visible: true },
  { id: 'ownership', label: 'Ownership', sortable: false, visible: true },
  { id: 'grants', label: 'Grants', sortable: false, visible: true },
  { id: 'role', label: 'Role', sortable: false, visible: false },
  { id: 'balance', label: 'Balance', sortable: true, visible: false },
  { id: 'subscription', label: 'Subscription', sortable: false, visible: false },
  { id: 'createdAt', label: 'Created', sortable: true, visible: false },
  { id: 'actions', label: 'Actions', sortable: false, visible: true, required: true },
];

const AVAILABLE_TENANTS = [
  'GameDev Studio',
  'IndieCorp', 
  'MegaGames',
  'PixelPerfect',
  'CodeCrafters',
  'GameForge'
];

const INITIAL_FILTERS: UserFilters = {
  search: '',
  status: 'all',
  tenant: 'current', // Will be set to current tenant name in effect
  lastActive: 'all',
  accessType: 'all',
};

const INITIAL_SORT: UserSort = {
  field: 'lastActive',
  direction: 'desc',
};

export function EnhancedUserList() {
  const { paginatedUsers: contextUsers } = useUserContext();
  const { currentTenant } = useTenant();
  const searchParams = useSearchParams();
  const router = useRouter();

  // Enhanced state management
  const [users, setUsers] = useState<EnhancedUser[]>([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<UserFilters>(INITIAL_FILTERS);
  const [sort, setSort] = useState<UserSort>(INITIAL_SORT);
  const [pagination, setPagination] = useState<UserPagination>({ page: 1, limit: 20, total: 0 });
  const [selectedUsers, setSelectedUsers] = useState<Set<string>>(new Set());
  const [showFilters, setShowFilters] = useState(false);
  const [columns, setColumns] = useState<ColumnConfig[]>(DEFAULT_COLUMNS);
  const [impersonatingUser, setImpersonatingUser] = useState<string | null>(null);
  const [bulkActionDialog, setBulkActionDialog] = useState<string | null>(null);
  const [editingUserTenants, setEditingUserTenants] = useState<{userId: string, tenants: string[]} | null>(null);

  // Initialize from URL params and set default to current tenant
  useEffect(() => {
    const urlFilters: UserFilters = {
      search: searchParams.get('search') || '',
      status: (searchParams.get('status') as UserFilters['status']) || 'all',
      tenant: searchParams.get('tenant') || (currentTenant?.name || 'all'),
      lastActive: (searchParams.get('lastActive') as UserFilters['lastActive']) || 'all',
      accessType: (searchParams.get('accessType') as UserFilters['accessType']) || 'all',
    };

    const urlSort: UserSort = {
      field: (searchParams.get('sortField') as UserSort['field']) || 'lastActive',
      direction: (searchParams.get('sortDirection') as UserSort['direction']) || 'desc',
    };

    const urlPagination: UserPagination = {
      page: parseInt(searchParams.get('page') || '1'),
      limit: parseInt(searchParams.get('limit') || '20'),
      total: 0,
    };

    setFilters(urlFilters);
    setSort(urlSort);
    setPagination(urlPagination);
  }, [searchParams, currentTenant?.name]);

  // Enhanced users with real data filtered by current tenant
  useEffect(() => {
    if (contextUsers && currentTenant) {
      // Filter users to only show those belonging to the current tenant
      const enhancedUsers: EnhancedUser[] = contextUsers
        .map((user: UserResponseDto, index: number) => ({
          ...user,
          lastActive: user.updatedAt || user.createdAt || new Date().toISOString(),
          ownedObjectsCount: Math.floor(Math.random() * 10) + 1, // This would come from real data
          grantsReceivedCount: Math.floor(Math.random() * 15) + 1, // This would come from real data
          tenantMemberships: [
            {
              tenantId: currentTenant.id,
              tenantName: currentTenant.name,
              role: index % 3 === 0 ? 'owner' : index % 3 === 1 ? 'admin' : 'member'
            }
          ],
          avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${user.email}`,
          username: user.email?.split('@')[0] || `user${index}`, // Derive from email since username not in API
        }));
      setUsers(enhancedUsers);
    } else if (contextUsers && !currentTenant) {
      // Show Game Guild only users when no tenant is selected
      const enhancedUsers: EnhancedUser[] = contextUsers.map((user: UserResponseDto, index: number) => ({
        ...user,
        lastActive: user.updatedAt || user.createdAt || new Date().toISOString(),
        ownedObjectsCount: Math.floor(Math.random() * 10) + 1,
        grantsReceivedCount: Math.floor(Math.random() * 15) + 1,
        tenantMemberships: [], // Game Guild only users have no tenant memberships
        avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${user.email}`,
        username: user.email?.split('@')[0] || `user${index}`, // Derive from email since username not in API
      }));
      setUsers(enhancedUsers);
    }
  }, [contextUsers, currentTenant]);

  // Update URL when filters/sort/pagination change
  const updateURL = (newFilters: UserFilters, newSort: UserSort, newPagination: UserPagination) => {
    const params = new URLSearchParams();
    
    Object.entries(newFilters).forEach(([key, value]) => {
      if (value && value !== 'all') params.set(key, value);
    });
    
    if (newSort.field !== 'lastActive') params.set('sortField', newSort.field);
    if (newSort.direction !== 'desc') params.set('sortDirection', newSort.direction);
    if (newPagination.page !== 1) params.set('page', newPagination.page.toString());
    if (newPagination.limit !== 20) params.set('limit', newPagination.limit.toString());

    router.push(`/dashboard/users?${params.toString()}`, { scroll: false });
  };

  // Filter and sort users
  const filteredAndSortedUsers = useMemo(() => {
    const filtered = users.filter((user) => {
      // Search filter
      if (filters.search) {
        const searchTerm = filters.search.toLowerCase();
        const searchableFields = [
          user.name,
          user.email,
          user.username,
          user.id,
        ].filter(Boolean).map(field => field!.toLowerCase());
        
        if (!searchableFields.some(field => field.includes(searchTerm))) {
          return false;
        }
      }

      // Status filter
      if (filters.status !== 'all') {
        const userStatus = user.isActive ? 'active' : 'suspended';
        if (userStatus !== filters.status && filters.status !== 'pending') {
          return false;
        }
      }

      // Tenant filter
      if (filters.tenant !== 'all') {
        if (filters.tenant === 'game-guild-only') {
          // Show only users with no tenant memberships (Game Guild only)
          if (user.tenantMemberships && user.tenantMemberships.length > 0) {
            return false;
          }
        } else {
          // Show users who belong to the selected tenant
          if (!user.tenantMemberships || !user.tenantMemberships.some(m => m.tenantName === filters.tenant)) {
            return false;
          }
        }
      }

      // Last active filter
      if (filters.lastActive !== 'all' && user.lastActive) {
        const lastActive = new Date(user.lastActive);
        const now = new Date();
        const daysDiff = (now.getTime() - lastActive.getTime()) / (1000 * 60 * 60 * 24);
        
        switch (filters.lastActive) {
          case '7days':
            if (daysDiff > 7) return false;
            break;
          case '30days':
            if (daysDiff > 30) return false;
            break;
          case '90days':
            if (daysDiff > 90) return false;
            break;
          case 'never':
            if (daysDiff < 365) return false; // Assume never if > 1 year
            break;
        }
      }

      return true;
    });

    // Sort users
    filtered.sort((a, b) => {
      let aValue: string | number;
      let bValue: string | number;

      switch (sort.field) {
        case 'name':
          aValue = a.name || '';
          bValue = b.name || '';
          break;
        case 'email':
          aValue = a.email || '';
          bValue = b.email || '';
          break;
        case 'lastActive':
          aValue = a.lastActive ? new Date(a.lastActive).getTime() : 0;
          bValue = b.lastActive ? new Date(b.lastActive).getTime() : 0;
          break;
        case 'createdAt':
          aValue = a.createdAt ? new Date(a.createdAt).getTime() : 0;
          bValue = b.createdAt ? new Date(b.createdAt).getTime() : 0;
          break;
        case 'status':
          aValue = a.isActive ? 'active' : 'suspended';
          bValue = b.isActive ? 'active' : 'suspended';
          break;
        default:
          return 0;
      }

      if (sort.direction === 'desc') {
        return aValue < bValue ? 1 : aValue > bValue ? -1 : 0;
      } else {
        return aValue > bValue ? 1 : aValue < bValue ? -1 : 0;
      }
    });

    return filtered;
  }, [users, filters, sort]);

  // Paginate users
  const paginatedUsers = useMemo(() => {
    const startIndex = (pagination.page - 1) * pagination.limit;
    const endIndex = startIndex + pagination.limit;
    return filteredAndSortedUsers.slice(startIndex, endIndex);
  }, [filteredAndSortedUsers, pagination]);

  // Update pagination total
  useEffect(() => {
    setPagination(prev => ({
      ...prev,
      total: filteredAndSortedUsers.length,
    }));
  }, [filteredAndSortedUsers.length]);

  // Handlers
  const handleFilterChange = (key: keyof UserFilters, value: string) => {
    const newFilters = { ...filters, [key]: value };
    setFilters(newFilters);
    const newPagination = { ...pagination, page: 1 };
    setPagination(newPagination);
    updateURL(newFilters, sort, newPagination);
  };

  const handleSortChange = (field: UserSort['field']) => {
    const newSort: UserSort = {
      field,
      direction: sort.field === field && sort.direction === 'asc' ? 'desc' : 'asc',
    };
    setSort(newSort);
    updateURL(filters, newSort, pagination);
  };

  const handlePageChange = (page: number) => {
    const newPagination = { ...pagination, page };
    setPagination(newPagination);
    updateURL(filters, sort, newPagination);
  };

  const handleUserSelect = (userId: string, selected: boolean) => {
    const newSelected = new Set(selectedUsers);
    if (selected) {
      newSelected.add(userId);
    } else {
      newSelected.delete(userId);
    }
    setSelectedUsers(newSelected);
  };

  const handleSelectAll = (selected: boolean) => {
    if (selected) {
      setSelectedUsers(new Set(paginatedUsers.map(user => user.id!)));
    } else {
      setSelectedUsers(new Set());
    }
  };

  const handleRefresh = async () => {
    setLoading(true);
    try {
      // In a real implementation, this would refetch data from the server
      await new Promise(resolve => setTimeout(resolve, 1000));
      toast.success('User data refreshed');
    } catch {
      toast.error('Failed to refresh user data');
    } finally {
      setLoading(false);
    }
  };

  const handleBulkAction = async (action: string) => {
    if (selectedUsers.size === 0) return;

    try {
      switch (action) {
        case 'activate':
          // Implement bulk activate
          toast.success(`Activated ${selectedUsers.size} users`);
          break;
        case 'deactivate':
          // Implement bulk deactivate
          toast.success(`Deactivated ${selectedUsers.size} users`);
          break;
        case 'message':
          // Implement bulk message
          toast.success(`Sent message to ${selectedUsers.size} users`);
          break;
        case 'export':
          // Implement export
          toast.success(`Exported ${selectedUsers.size} users`);
          break;
        case 'revoke-grants':
          // Implement revoke grants
          toast.success(`Revoked grants for ${selectedUsers.size} users`);
          break;
      }
      setSelectedUsers(new Set());
      setBulkActionDialog(null);
    } catch {
      toast.error(`Failed to ${action} users`);
    }
  };

  const handleImpersonate = (userId: string) => {
    setImpersonatingUser(userId);
    toast.success('Impersonation started', {
      description: 'You are now viewing the platform as this user',
    });
  };

  const stopImpersonation = () => {
    setImpersonatingUser(null);
    toast.success('Impersonation stopped');
  };

  const handleTenantEdit = (userId: string) => {
    const user = users.find(u => u.id === userId);
    if (user) {
      setEditingUserTenants({
        userId,
        tenants: user.tenantMemberships?.map(m => m.tenantName) || []
      });
    }
  };

  const handleTenantSave = async (userId: string, newTenantNames: string[]) => {
    try {
      // Update the user's tenant memberships
      setUsers(prev => prev.map(user => 
        user.id === userId 
          ? { 
              ...user, 
              tenantMemberships: newTenantNames.map(tenantName => ({
                tenantId: `tenant-${tenantName.toLowerCase().replace(/\s+/g, '-')}`,
                tenantName,
                role: 'member' as const
              }))
            }
          : user
      ));
      setEditingUserTenants(null);
      toast.success('User tenant memberships updated successfully');
    } catch {
      toast.error('Failed to update user tenant memberships');
    }
  };

  const handleColumnToggle = (columnId: string) => {
    setColumns(prev => prev.map(col => 
      col.id === columnId ? { ...col, visible: !col.visible } : col
    ));
  };

  const resetColumns = () => {
    setColumns(DEFAULT_COLUMNS);
    toast.success('Column settings reset to default');
  };

  const getVisibleColumns = () => columns.filter(col => col.visible);

  const getSortFieldForColumn = (columnId: string): UserSort['field'] | null => {
    const sortMapping: Record<string, UserSort['field']> = {
      'user': 'name',
      'contact': 'email',
      'status': 'status',
      'lastActive': 'lastActive',
      'createdAt': 'createdAt',
      'balance': 'name', // Using name as fallback since balance isn't in sort options
    };
    return sortMapping[columnId] || null;
  };

  const renderTableHeader = (column: ColumnConfig) => {
    const sortField = getSortFieldForColumn(column.id);
    const isCurrentSort = sort.field === sortField;
    
    if (column.sortable && sortField) {
      return (
        <TableHead 
          key={column.id}
          className="cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800"
          onClick={() => handleSortChange(sortField)}
        >
          <div className="flex items-center gap-1">
            {column.label}
            {isCurrentSort && (
              sort.direction === 'asc' ? <SortAsc className="h-4 w-4" /> : <SortDesc className="h-4 w-4" />
            )}
          </div>
        </TableHead>
      );
    }
    
    return (
      <TableHead 
        key={column.id}
        className={column.id === 'actions' ? 'w-16' : undefined}
      >
        {column.label}
      </TableHead>
    );
  };

  const renderTableCell = (column: ColumnConfig, user: EnhancedUser) => {
    switch (column.id) {
      case 'user':
        return (
          <TableCell key={column.id}>
            <div className="flex items-center gap-3">
              <Avatar className="h-10 w-10">
                <AvatarImage src={user.avatar} />
                <AvatarFallback>
                  {user.name?.charAt(0) || user.email?.charAt(0) || 'U'}
                </AvatarFallback>
              </Avatar>
              <div>
                <div className="font-medium">{user.name || 'Unknown User'}</div>
                <div className="text-sm text-gray-500">@{user.username || 'No username'}</div>
              </div>
            </div>
          </TableCell>
        );
      
      case 'contact':
        return (
          <TableCell key={column.id}>
            <div>
              <div className="font-medium">{user.email}</div>
              <div className="text-xs text-gray-500 font-mono">
                ID: {user.id}
              </div>
            </div>
          </TableCell>
        );
      
      case 'tenant':
        return (
          <TableCell key={column.id}>
            <div 
              className="flex flex-wrap gap-1 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 p-1 rounded"
              onClick={() => handleTenantEdit(user.id!)}
              title="Click to edit tenant memberships"
            >
              {user.tenantMemberships && user.tenantMemberships.length > 0 ? (
                user.tenantMemberships.map((membership, index) => (
                  <Badge key={index} variant="outline" className="text-xs">
                    {membership.tenantName}
                    <span className="ml-1 text-[10px] opacity-75">({membership.role})</span>
                  </Badge>
                ))
              ) : (
                <Badge variant="secondary" className="text-xs">
                  Game Guild Only
                </Badge>
              )}
            </div>
          </TableCell>
        );
      
      case 'status':
        return (
          <TableCell key={column.id}>
            {getUserStatusBadge(user)}
          </TableCell>
        );
      
      case 'lastActive':
        return (
          <TableCell key={column.id}>
            <div className="flex items-center gap-1 text-sm text-gray-600">
              <Clock className="h-3 w-3" />
              {getLastActiveText(user.lastActive)}
            </div>
          </TableCell>
        );
      
      case 'ownership':
        return (
          <TableCell key={column.id}>
            <div className="flex items-center gap-1 text-sm">
              <Database className="h-3 w-3 text-blue-500" />
              {user.ownedObjectsCount} items
            </div>
          </TableCell>
        );
      
      case 'grants':
        return (
          <TableCell key={column.id}>
            <div className="flex items-center gap-1 text-sm">
              <Key className="h-3 w-3 text-green-500" />
              {user.grantsReceivedCount} grants
            </div>
          </TableCell>
        );
      
      case 'role':
        return (
          <TableCell key={column.id}>
            <Badge variant="secondary">{user.role || 'User'}</Badge>
          </TableCell>
        );
      
      case 'balance':
        return (
          <TableCell key={column.id}>
            <div className="text-sm">
              <div className="font-medium">${user.balance || 0}</div>
              <div className="text-gray-500">Available: ${user.availableBalance || 0}</div>
            </div>
          </TableCell>
        );
      
      case 'subscription':
        return (
          <TableCell key={column.id}>
            <Badge variant={user.activeSubscription ? "default" : "secondary"}>
              {user.subscriptionType || 'None'}
            </Badge>
          </TableCell>
        );
      
      case 'createdAt':
        return (
          <TableCell key={column.id}>
            <div className="text-sm text-gray-600">
              {user.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'Unknown'}
            </div>
          </TableCell>
        );
      
      case 'actions':
        return (
          <TableCell key={column.id}>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => router.push(`/dashboard/users/${user.id}`)}>
                  <Eye className="h-4 w-4 mr-2" />
                  View Details
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleImpersonate(user.id!)}>
                  <Shield className="h-4 w-4 mr-2" />
                  Impersonate
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleTenantEdit(user.id!)}>
                  <Users className="h-4 w-4 mr-2" />
                  Edit Tenants
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <MessageSquare className="h-4 w-4 mr-2" />
                  Send Message
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  <Activity className="h-4 w-4 mr-2" />
                  View Activity
                </DropdownMenuItem>
                <DropdownMenuItem className="text-red-600">
                  <AlertTriangle className="h-4 w-4 mr-2" />
                  Suspend User
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </TableCell>
        );
      
      default:
        return <TableCell key={column.id}>-</TableCell>;
    }
  };

  const getUserStatusBadge = (user: EnhancedUser) => {
    if (user.isActive) {
      return <Badge variant="default" className="bg-green-100 text-green-800">Active</Badge>;
    } else {
      return <Badge variant="destructive">Suspended</Badge>;
    }
  };

  const getLastActiveText = (lastActive?: string) => {
    if (!lastActive) return 'Never';
    
    const date = new Date(lastActive);
    const now = new Date();
    const daysDiff = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    
    if (daysDiff === 0) return 'Today';
    if (daysDiff === 1) return 'Yesterday';
    if (daysDiff < 7) return `${daysDiff} days ago`;
    if (daysDiff < 30) return `${Math.floor(daysDiff / 7)} weeks ago`;
    if (daysDiff < 365) return `${Math.floor(daysDiff / 30)} months ago`;
    return `${Math.floor(daysDiff / 365)} years ago`;
  };

  const hasActiveFilters = Object.entries(filters).some(([key, value]) => {
    if (key === 'search') return value.length > 0;
    return value !== 'all';
  });

  const clearAllFilters = () => {
    setFilters(INITIAL_FILTERS);
    const newPagination = { ...pagination, page: 1 };
    setPagination(newPagination);
    updateURL(INITIAL_FILTERS, sort, newPagination);
  };

  const totalPages = Math.ceil(pagination.total / pagination.limit);

  return (
    <div className="space-y-6">
      {/* Header with actions */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">User Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            {currentTenant 
              ? `Manage users for ${currentTenant.name}` 
              : 'Manage Game Guild users, permissions, and access control'
            }
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={loading}
          >
            <RefreshCw className={cn("h-4 w-4", loading && "animate-spin")} />
            Refresh
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
              >
                <Columns className="h-4 w-4" />
                Columns
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <div className="px-2 py-1.5 text-sm font-semibold">Show Columns</div>
              <DropdownMenuSeparator />
              {columns.map((column) => (
                <DropdownMenuCheckboxItem
                  key={column.id}
                  checked={column.visible}
                  onCheckedChange={() => !column.required && handleColumnToggle(column.id)}
                  disabled={column.required}
                >
                  {column.label}
                  {column.required && (
                    <span className="ml-1 text-xs text-gray-500">(Required)</span>
                  )}
                </DropdownMenuCheckboxItem>
              ))}
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={resetColumns}>
                <Settings2 className="h-4 w-4 mr-2" />
                Reset to Default
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
          >
            <Filter className="h-4 w-4" />
            Filters
            {hasActiveFilters && <Badge variant="secondary" className="ml-1 h-5 w-5 p-0 text-xs">!</Badge>}
          </Button>
        </div>
      </div>

      {/* Impersonation Banner */}
      {impersonatingUser && (
        <Card className="border-orange-200 bg-orange-50 dark:bg-orange-950">
          <CardContent className="flex items-center justify-between p-4">
            <div className="flex items-center gap-2">
              <Crown className="h-5 w-5 text-orange-600" />
              <span className="font-medium text-orange-800 dark:text-orange-200">
                Impersonating User: {users.find(u => u.id === impersonatingUser)?.name}
              </span>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={stopImpersonation}
              className="border-orange-300 text-orange-700 hover:bg-orange-100"
            >
              Stop Impersonation
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Filters Panel */}
      {showFilters && (
        <Card>
          <CardHeader className="pb-4">
            <div className="flex items-center justify-between">
              <CardTitle className="text-lg">Filter Users</CardTitle>
              {hasActiveFilters && (
                <Button variant="ghost" size="sm" onClick={clearAllFilters}>
                  <X className="h-4 w-4 mr-1" />
                  Clear All
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
              <div>
                <label className="text-sm font-medium mb-2 block">Search</label>
                <Input
                  placeholder="Name, email, username, ID..."
                  value={filters.search}
                  onChange={(e) => handleFilterChange('search', e.target.value)}
                />
              </div>
              
              <div>
                <label className="text-sm font-medium mb-2 block">Status</label>
                <Select
                  value={filters.status}
                  onValueChange={(value) => handleFilterChange('status', value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Statuses</SelectItem>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="suspended">Suspended</SelectItem>
                    <SelectItem value="pending">Pending</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">Tenant</label>
                <Select
                  value={filters.tenant}
                  onValueChange={(value) => handleFilterChange('tenant', value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Users</SelectItem>
                    <SelectItem value="game-guild-only">Game Guild Only</SelectItem>
                    {currentTenant && (
                      <SelectItem value={currentTenant.name}>
                        {currentTenant.name} (Current)
                      </SelectItem>
                    )}
                    {AVAILABLE_TENANTS.filter(t => t !== currentTenant?.name).map((tenant) => (
                      <SelectItem key={tenant} value={tenant}>
                        {tenant}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">Last Active</label>
                <Select
                  value={filters.lastActive}
                  onValueChange={(value) => handleFilterChange('lastActive', value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Any Time</SelectItem>
                    <SelectItem value="7days">Last 7 days</SelectItem>
                    <SelectItem value="30days">Last 30 days</SelectItem>
                    <SelectItem value="90days">Last 90 days</SelectItem>
                    <SelectItem value="never">Never logged in</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">Access Type</label>
                <Select
                  value={filters.accessType}
                  onValueChange={(value) => handleFilterChange('accessType', value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Access</SelectItem>
                    <SelectItem value="owns">Object Owners</SelectItem>
                    <SelectItem value="granted">Granted Access</SelectItem>
                    <SelectItem value="delegated">Delegated Access</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Active Filters Display */}
      {hasActiveFilters && (
        <div className="flex flex-wrap gap-2">
          {filters.search && (
            <Badge variant="secondary" className="gap-1">
              Search: {filters.search}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => handleFilterChange('search', '')}
              />
            </Badge>
          )}
          {filters.status !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              Status: {filters.status}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => handleFilterChange('status', 'all')}
              />
            </Badge>
          )}
          {filters.tenant !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              Tenant: {filters.tenant}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => handleFilterChange('tenant', 'all')}
              />
            </Badge>
          )}
          {filters.lastActive !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              Last Active: {filters.lastActive}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => handleFilterChange('lastActive', 'all')}
              />
            </Badge>
          )}
          {filters.accessType !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              Access: {filters.accessType}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => handleFilterChange('accessType', 'all')}
              />
            </Badge>
          )}
        </div>
      )}

      {/* Bulk Actions */}
      {selectedUsers.size > 0 && (
        <Card className="border-blue-200 bg-blue-50 dark:bg-blue-950">
          <CardContent className="flex items-center justify-between p-4">
            <div className="flex items-center gap-4">
              <span className="font-medium text-blue-800 dark:text-blue-200">
                {selectedUsers.size} user{selectedUsers.size > 1 ? 's' : ''} selected
              </span>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setBulkActionDialog('activate')}
                >
                  <UserCheck className="h-4 w-4 mr-1" />
                  Activate
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setBulkActionDialog('deactivate')}
                >
                  <UserX className="h-4 w-4 mr-1" />
                  Deactivate
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setBulkActionDialog('message')}
                >
                  <MessageSquare className="h-4 w-4 mr-1" />
                  Message
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setBulkActionDialog('export')}
                >
                  <Download className="h-4 w-4 mr-1" />
                  Export
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setBulkActionDialog('revoke-grants')}
                >
                  <Key className="h-4 w-4 mr-1" />
                  Revoke Grants
                </Button>
              </div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setSelectedUsers(new Set())}
            >
              <X className="h-4 w-4" />
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Users Table */}
      <Card>
        <CardHeader className="pb-4">
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <Users className="h-5 w-5" />
              Users ({pagination.total})
            </CardTitle>
            <div className="flex items-center gap-2">
              <Select
                value={pagination.limit.toString()}
                onValueChange={(value) => {
                  const newPagination = { ...pagination, limit: parseInt(value), page: 1 };
                  setPagination(newPagination);
                  updateURL(filters, sort, newPagination);
                }}
              >
                <SelectTrigger className="w-24">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="10">10</SelectItem>
                  <SelectItem value="20">20</SelectItem>
                  <SelectItem value="50">50</SelectItem>
                  <SelectItem value="100">100</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {paginatedUsers.length === 0 ? (
            <div className="text-center py-12">
              <Users className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
                No users found
              </h3>
              <p className="text-gray-600 dark:text-gray-400 mb-4">
                {hasActiveFilters
                  ? 'Try adjusting your filters to see more results'
                  : 'No users are available in the system'}
              </p>
              {hasActiveFilters && (
                <Button variant="outline" onClick={clearAllFilters}>
                  Clear Filters
                </Button>
              )}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-12">
                      <Checkbox
                        checked={selectedUsers.size === paginatedUsers.length && paginatedUsers.length > 0}
                        onCheckedChange={handleSelectAll}
                      />
                    </TableHead>
                    {getVisibleColumns().map(column => renderTableHeader(column))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {paginatedUsers.map((user) => (
                    <TableRow key={user.id} className="hover:bg-gray-50 dark:hover:bg-gray-800">
                      <TableCell>
                        <Checkbox
                          checked={selectedUsers.has(user.id!)}
                          onCheckedChange={(checked) => handleUserSelect(user.id!, checked as boolean)}
                        />
                      </TableCell>
                      {getVisibleColumns().map(column => renderTableCell(column, user))}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-gray-600 dark:text-gray-400">
            Showing {(pagination.page - 1) * pagination.limit + 1} to{' '}
            {Math.min(pagination.page * pagination.limit, pagination.total)} of {pagination.total} users
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(1)}
              disabled={pagination.page === 1}
            >
              <ChevronsLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(pagination.page - 1)}
              disabled={pagination.page === 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <div className="flex items-center gap-1">
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                const pageNum = Math.max(1, Math.min(totalPages - 4, pagination.page - 2)) + i;
                return (
                  <Button
                    key={pageNum}
                    variant={pageNum === pagination.page ? "default" : "outline"}
                    size="sm"
                    onClick={() => handlePageChange(pageNum)}
                  >
                    {pageNum}
                  </Button>
                );
              })}
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(pagination.page + 1)}
              disabled={pagination.page === totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(totalPages)}
              disabled={pagination.page === totalPages}
            >
              <ChevronsRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Bulk Action Dialogs */}
      <Dialog open={!!bulkActionDialog} onOpenChange={() => setBulkActionDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {bulkActionDialog === 'activate' && 'Activate Users'}
              {bulkActionDialog === 'deactivate' && 'Deactivate Users'}
              {bulkActionDialog === 'message' && 'Send Message'}
              {bulkActionDialog === 'export' && 'Export Users'}
              {bulkActionDialog === 'revoke-grants' && 'Revoke Grants'}
            </DialogTitle>
            <DialogDescription>
              This action will affect {selectedUsers.size} selected user{selectedUsers.size > 1 ? 's' : ''}.
              Are you sure you want to continue?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setBulkActionDialog(null)}>
              Cancel
            </Button>
            <Button onClick={() => handleBulkAction(bulkActionDialog!)}>
              Confirm
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Tenant Editing Dialog */}
      <Dialog open={!!editingUserTenants} onOpenChange={() => setEditingUserTenants(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Edit User Tenants</DialogTitle>
            <DialogDescription>
              Select which tenants this user belongs to. Users with no tenants are Game Guild only.
            </DialogDescription>
          </DialogHeader>
          {editingUserTenants && (
            <div className="space-y-4">
              <div className="space-y-2">
                <p className="text-sm font-medium">Available Tenants:</p>
                <div className="space-y-2">
                  {currentTenant && (
                    <div className="flex items-center space-x-2">
                      <Checkbox
                        id={`tenant-${currentTenant.name}`}
                        checked={editingUserTenants.tenants.includes(currentTenant.name)}
                        onCheckedChange={(checked) => {
                          if (checked) {
                            setEditingUserTenants({
                              ...editingUserTenants,
                              tenants: [...editingUserTenants.tenants, currentTenant.name]
                            });
                          } else {
                            setEditingUserTenants({
                              ...editingUserTenants,
                              tenants: editingUserTenants.tenants.filter(t => t !== currentTenant.name)
                            });
                          }
                        }}
                      />
                      <label htmlFor={`tenant-${currentTenant.name}`} className="text-sm font-medium">
                        {currentTenant.name} (Current)
                      </label>
                    </div>
                  )}
                  {AVAILABLE_TENANTS.filter(t => t !== currentTenant?.name).map((tenant) => (
                    <div key={tenant} className="flex items-center space-x-2">
                      <Checkbox
                        id={`tenant-${tenant}`}
                        checked={editingUserTenants.tenants.includes(tenant)}
                        onCheckedChange={(checked) => {
                          if (checked) {
                            setEditingUserTenants({
                              ...editingUserTenants,
                              tenants: [...editingUserTenants.tenants, tenant]
                            });
                          } else {
                            setEditingUserTenants({
                              ...editingUserTenants,
                              tenants: editingUserTenants.tenants.filter(t => t !== tenant)
                            });
                          }
                        }}
                      />
                      <label htmlFor={`tenant-${tenant}`} className="text-sm">
                        {tenant}
                      </label>
                    </div>
                  ))}
                </div>
              </div>
              <div className="text-xs text-gray-500">
                <strong>Note:</strong> If no tenants are selected, the user will be marked as &ldquo;Game Guild Only&rdquo;.
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditingUserTenants(null)}>
              Cancel
            </Button>
            <Button 
              onClick={() => {
                if (editingUserTenants) {
                  handleTenantSave(editingUserTenants.userId, editingUserTenants.tenants);
                }
              }}
            >
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
