'use client';

import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';
import {
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    Search,
    SortAsc,
    SortDesc,
    Users
} from 'lucide-react';
import { useMemo, useState } from 'react';

interface UserListProps {
    users: UserResponseDto[];
}

interface UserFilters {
    search: string;
}

interface UserSort {
    field: keyof UserResponseDto;
    direction: 'asc' | 'desc';
}

const INITIAL_FILTERS: UserFilters = {
    search: ''
};

const INITIAL_SORT: UserSort = {
    field: 'updatedAt',
    direction: 'desc'
};

export function UserList({ users: incomingUsers }: UserListProps) {
    const [filters, setFilters] = useState<UserFilters>(INITIAL_FILTERS);
    const [sort, setSort] = useState<UserSort>(INITIAL_SORT);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // Filter users based on search
    const filteredUsers = useMemo(() => {
        let result = [...incomingUsers];

        // Apply search filter
        if (filters.search) {
            const searchLower = filters.search.toLowerCase();
            result = result.filter(user =>
                user.email?.toLowerCase().includes(searchLower) ||
                user.username?.toLowerCase().includes(searchLower) ||
                user.name?.toLowerCase().includes(searchLower)
            );
        }

        return result;
    }, [incomingUsers, filters]);

    // Sort users
    const sortedUsers = useMemo(() => {
        return [...filteredUsers].sort((a, b) => {
            const aValue = a[sort.field];
            const bValue = b[sort.field];

            if (aValue == null && bValue == null) return 0;
            if (aValue == null) return 1;
            if (bValue == null) return -1;

            const comparison = aValue < bValue ? -1 : 1;
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }, [filteredUsers, sort]);

    // Paginate users
    const paginatedUsers = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return sortedUsers.slice(startIndex, startIndex + itemsPerPage);
    }, [sortedUsers, currentPage]);

    const totalPages = Math.ceil(sortedUsers.length / itemsPerPage);

    const handleSort = (field: keyof UserResponseDto) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
    };

    const getUserStatusBadge = (user: UserResponseDto) => {
        if (user.isActive) {
            return <Badge variant="default" className="bg-green-100 text-green-800">Active</Badge>;
        } else {
            return <Badge variant="secondary" className="bg-red-100 text-red-800">Inactive</Badge>;
        }
    };

    const getUserInitials = (user: UserResponseDto) => {
        if (user.name) {
            const nameParts = user.name.split(' ');
            const first = nameParts[0]?.[0] || '';
            const last = nameParts[1]?.[0] || '';
            return (first + last).toUpperCase();
        }
        return user.email?.[0]?.toUpperCase() || user.username?.[0]?.toUpperCase() || '?';
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    return (
        <Card>
            <CardHeader>
                <div className="flex items-center justify-between">
                    <CardTitle className="flex items-center gap-2">
                        <Users className="h-5 w-5" />
                        Users ({sortedUsers.length})
                    </CardTitle>
                </div>
            </CardHeader>

            <CardContent className="space-y-4">
                {/* Search Filter */}
                <div className="flex items-center space-x-2">
                    <div className="relative flex-1">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search users by name, email or username..."
                            value={filters.search}
                            onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                            className="pl-9"
                        />
                    </div>
                </div>

                {/* Users Table */}
                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>User</TableHead>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('email')}
                                >
                                    <div className="flex items-center gap-1">
                                        Email
                                        {sort.field === 'email' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('username')}
                                >
                                    <div className="flex items-center gap-1">
                                        Username
                                        {sort.field === 'username' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('updatedAt')}
                                >
                                    <div className="flex items-center gap-1">
                                        Updated
                                        {sort.field === 'updatedAt' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {paginatedUsers.length === 0 ? (
                                <TableRow>
                                    <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                                        {filters.search ? 'No users found matching your search.' : 'No users found.'}
                                    </TableCell>
                                </TableRow>
                            ) : (
                                paginatedUsers.map((user) => (
                                    <TableRow key={user.id}>
                                        <TableCell>
                                            <div className="flex items-center gap-3">
                                                <Avatar className="h-8 w-8">
                                                    <AvatarFallback className="text-xs">
                                                        {getUserInitials(user)}
                                                    </AvatarFallback>
                                                </Avatar>
                                                <div>
                                                    <div className="font-medium">
                                                        {user.name || user.username || 'Unnamed User'}
                                                    </div>
                                                    <div className="text-sm text-muted-foreground">
                                                        ID: {user.id?.slice(0, 8) || 'N/A'}...
                                                    </div>
                                                </div>
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm">{user.email}</div>
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm font-mono">{user.username}</div>
                                        </TableCell>
                                        <TableCell>
                                            {getUserStatusBadge(user)}
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm text-muted-foreground">
                                                {formatDate(user.updatedAt)}
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))
                            )}
                        </TableBody>
                    </Table>
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                    <div className="flex items-center justify-between">
                        <div className="text-sm text-muted-foreground">
                            Showing {((currentPage - 1) * itemsPerPage) + 1} to {Math.min(currentPage * itemsPerPage, sortedUsers.length)} of {sortedUsers.length} users
                        </div>
                        <div className="flex items-center gap-2">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setCurrentPage(1)}
                                disabled={currentPage === 1}
                            >
                                <ChevronsLeft className="h-4 w-4" />
                            </Button>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                                disabled={currentPage === 1}
                            >
                                <ChevronLeft className="h-4 w-4" />
                            </Button>
                            <span className="text-sm">
                                Page {currentPage} of {totalPages}
                            </span>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                                disabled={currentPage === totalPages}
                            >
                                <ChevronRight className="h-4 w-4" />
                            </Button>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setCurrentPage(totalPages)}
                                disabled={currentPage === totalPages}
                            >
                                <ChevronsRight className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>
                )}
            </CardContent>
        </Card>
    );
}
