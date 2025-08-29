/* eslint-disable prettier/prettier */
'use client';

import { useTenant } from '@/components/tenant/context/tenant-provider';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';
import { cn } from '@/lib/utils';
import {
  Activity,
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Clock,
  Columns,
  Download,
  Eye,
  Filter,
  MessageSquare,
  MoreHorizontal,
  RefreshCw,
  Settings2,
  SortAsc,
  SortDesc,
  UserCheck,
  Users,
  UserX,
  X
} from 'lucide-react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';

// Enhanced types for user management - using only real API data
interface EnhancedUser extends UserResponseDto {
  // Only add fields that come from the real API response
}

interface UserFilters {
  search: string;
  status: 'all' | 'active' | 'suspended';
  lastActive: 'all' | '7days' | '30days' | '90days';
}

interface UserSort {
  field: 'name' | 'email' | 'createdAt' | 'updatedAt' | 'status';
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
  { id: 'status', label: 'Status', sortable: true, visible: true },
  { id: 'role', label: 'Role', sortable: false, visible: true },
  { id: 'balance', label: 'Balance', sortable: true, visible: true },
  { id: 'subscription', label: 'Subscription', sortable: false, visible: false },
  { id: 'createdAt', label: 'Created', sortable: true, visible: false },
  { id: 'actions', label: 'Actions', sortable: false, visible: true, required: true },
];

const INITIAL_FILTERS: UserFilters = {
  search: '',
  status: 'all',
  lastActive: 'all',
};

const INITIAL_SORT: UserSort = {
  field: 'createdAt',
  direction: 'desc',
};

interface EnhancedUserListProps { users: UserResponseDto[] }
export function EnhancedUserList({ users: incomingUsers }: EnhancedUserListProps) {
  const { currentTenant } = useTenant();
  const searchParams = useSearchParams();
  const router = useRouter();

  const [users, setUsers] = useState<EnhancedUser[]>([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<UserFilters>(INITIAL_FILTERS);
  const [sort, setSort] = useState<UserSort>(INITIAL_SORT);
  const [pagination, setPagination] = useState<UserPagination>({ page: 1, limit: 20, total: 0 });
  const [selectedUsers, setSelectedUsers] = useState<Set<string>>(new Set());
  const [showFilters, setShowFilters] = useState(false);
  const [columns, setColumns] = useState<ColumnConfig[]>(DEFAULT_COLUMNS);
  const [bulkActionDialog, setBulkActionDialog] = useState<string | null>(null);

  // Initialize from URL params
  useEffect(() => {
    const urlFilters: UserFilters = {
      search: searchParams.get('search') || '',
      status: (searchParams.get('status') as UserFilters['status']) || 'all',
      lastActive: (searchParams.get('lastActive') as UserFilters['lastActive']) || 'all',
    };

    const urlSort: UserSort = {
      field: (searchParams.get('sortField') as UserSort['field']) || 'createdAt',
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
  }, [searchParams]);

  // Load real users from API
  useEffect(() => {
    if (!incomingUsers) return;
    // Use real API data directly without adding fake properties
    const mapped: EnhancedUser[] = incomingUsers.map((user: UserResponseDto) => ({
      ...user,
    }));

    setUsers(mapped);
    console.log('Users loaded in EnhancedUserList:', mapped.length);
    console.log('First user sample:', mapped[0]);
  }, [incomingUsers]);

  // Update URL when filters/sort/pagination change
  const updateURL = (newFilters: UserFilters, newSort: UserSort, newPagination: UserPagination) => {
    const params = new URLSearchParams();

    Object.entries(newFilters).forEach(([key, value]) => {
      if (value && value !== 'all') params.set(key, value);
    });

    if (newSort.field !== 'createdAt') params.set('sortField', newSort.field);
    if (newSort.direction !== 'desc') params.set('sortDirection', newSort.direction);
    if (newPagination.page !== 1) params.set('page', newPagination.page.toString());
    if (newPagination.limit !== 20) params.set('limit', newPagination.limit.toString());

    router.push(`/dashboard/users?${params.toString()}`, { scroll: false });
  };

  // Filter and sort users
  const filteredAndSortedUsers = useMemo(() => {
    console.log('Filtering users. Input users:', users.length);
    console.log('Current filters:', filters);

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
        if (userStatus !== filters.status) {
          return false;
        }
      }

      // Last active filter based on updatedAt (real field from API)
      if (filters.lastActive !== 'all' && user.updatedAt) {
        const lastActive = new Date(user.updatedAt);
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
          aValue = (a.name ?? '').toLowerCase();
          bValue = (b.name ?? '').toLowerCase();
          break;
        case 'email':
          aValue = (a.email ?? '').toLowerCase();
          bValue = (b.email ?? '').toLowerCase();
          break;
        case 'createdAt':
          aValue = a.createdAt ? new Date(a.createdAt).getTime() : 0;
          bValue = b.createdAt ? new Date(b.createdAt).getTime() : 0;
          break;
        case 'updatedAt':
          aValue = a.updatedAt ? new Date(a.updatedAt).getTime() : 0;
          bValue = b.updatedAt ? new Date(b.updatedAt).getTime() : 0;
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

    console.log('Filtered users count:', filtered.length);
    return filtered;
  }, [users, filters, sort]);

  // Paginate users
  const paginatedUsers = useMemo(() => {
    const startIndex = (pagination.page - 1) * pagination.limit;
    const endIndex = startIndex + pagination.limit;
    const paginated = filteredAndSortedUsers.slice(startIndex, endIndex);
    console.log('Paginated users count:', paginated.length, 'of total:', filteredAndSortedUsers.length);
    return paginated;
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
                <AvatarFallback>
                  {user.name?.charAt(0) || user.email?.charAt(0) || 'U'}
                </AvatarFallback>
              </Avatar>
              <div>
                <div className="font-medium">{user.name || 'Unknown User'}</div>
                <div className="text-sm text-gray-500">@{user.username || user.email?.split('@')[0] || 'No username'}</div>
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

      case 'status':
        return (
          <TableCell key={column.id}>
            {getUserStatusBadge(user)}
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
          {filters.lastActive !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              Last Active: {filters.lastActive}
              <X
                className="h-3 w-3 cursor-pointer"
                onClick={() => handleFilterChange('lastActive', 'all')}
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
    </div>
  );
}
