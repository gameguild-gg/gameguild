'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tenant } from '@/lib/api/generated/types.gen';
import {
    Building,
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    Eye,
    Search,
    SortAsc,
    SortDesc
} from 'lucide-react';
import Link from 'next/link';
import { useMemo, useState } from 'react';

interface TenantsListProps {
    tenants: Tenant[];
}

interface TenantFilters {
    search: string;
}

interface TenantSort {
    field: keyof Tenant;
    direction: 'asc' | 'desc';
}

const INITIAL_FILTERS: TenantFilters = {
    search: ''
};

const INITIAL_SORT: TenantSort = {
    field: 'updatedAt',
    direction: 'desc'
};

export function TenantsList({ tenants: incomingTenants }: TenantsListProps) {
    const [filters, setFilters] = useState<TenantFilters>(INITIAL_FILTERS);
    const [sort, setSort] = useState<TenantSort>(INITIAL_SORT);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // Filter tenants based on search
    const filteredTenants = useMemo(() => {
        let result = [...incomingTenants];

        // Apply search filter
        if (filters.search) {
            const searchLower = filters.search.toLowerCase();
            result = result.filter(tenant =>
                tenant.name?.toLowerCase().includes(searchLower) ||
                tenant.description?.toLowerCase().includes(searchLower) ||
                tenant.slug?.toLowerCase().includes(searchLower)
            );
        }

        return result;
    }, [incomingTenants, filters]);

    // Sort tenants
    const sortedTenants = useMemo(() => {
        return [...filteredTenants].sort((a, b) => {
            const aValue = a[sort.field];
            const bValue = b[sort.field];

            if (aValue == null && bValue == null) return 0;
            if (aValue == null) return 1;
            if (bValue == null) return -1;

            const comparison = aValue < bValue ? -1 : 1;
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }, [filteredTenants, sort]);

    // Paginate tenants
    const paginatedTenants = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return sortedTenants.slice(startIndex, startIndex + itemsPerPage);
    }, [sortedTenants, currentPage]);

    const totalPages = Math.ceil(sortedTenants.length / itemsPerPage);

    const handleSort = (field: keyof Tenant) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
    };

    const getTenantStatusBadge = (tenant: Tenant) => {
        if (tenant.isActive) {
            return <Badge variant="default" className="bg-green-100 text-green-800">Active</Badge>;
        } else {
            return <Badge variant="secondary" className="bg-gray-100 text-gray-800">Inactive</Badge>;
        }
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return '-';
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
                        <Building className="h-5 w-5" />
                        Tenants ({sortedTenants.length})
                    </CardTitle>
                </div>
            </CardHeader>

            <CardContent className="space-y-4">
                {/* Search Filter */}
                <div className="flex items-center space-x-2">
                    <div className="relative flex-1">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search tenants by name, description, or slug..."
                            value={filters.search}
                            onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                            className="pl-9"
                        />
                    </div>
                </div>

                {/* Tenants Table */}
                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('name')}
                                >
                                    <div className="flex items-center gap-1">
                                        Name
                                        {sort.field === 'name' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                                <TableHead>Description</TableHead>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('isActive')}
                                >
                                    <div className="flex items-center gap-1">
                                        Status
                                        {sort.field === 'isActive' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('createdAt')}
                                >
                                    <div className="flex items-center gap-1">
                                        Created
                                        {sort.field === 'createdAt' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
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
                                <TableHead>Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {paginatedTenants.length === 0 ? (
                                <TableRow>
                                    <TableCell colSpan={6} className="text-center text-muted-foreground py-8">
                                        {filters.search ? 'No tenants found matching your search.' : 'No tenants found.'}
                                    </TableCell>
                                </TableRow>
                            ) : (
                                paginatedTenants.map((tenant) => (
                                    <TableRow key={tenant.id}>
                                        <TableCell className="font-medium">
                                            {tenant.name}
                                        </TableCell>
                                        <TableCell className="max-w-xs truncate">
                                            {tenant.description || <span className="text-gray-400">No description</span>}
                                        </TableCell>
                                        <TableCell>
                                            {getTenantStatusBadge(tenant)}
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm text-muted-foreground">
                                                {formatDate(tenant.createdAt)}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm text-muted-foreground">
                                                {formatDate(tenant.updatedAt)}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <Link href={`/dashboard/tenant/${tenant.id}`}>
                                                <Button variant="outline" size="sm" className="flex items-center gap-2">
                                                    <Eye className="h-4 w-4" />
                                                    View Details
                                                </Button>
                                            </Link>
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
                            Showing {((currentPage - 1) * itemsPerPage) + 1} to {Math.min(currentPage * itemsPerPage, sortedTenants.length)} of {sortedTenants.length} tenants
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
