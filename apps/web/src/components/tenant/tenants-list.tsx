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
    Search,
    SortAsc,
    SortDesc
} from 'lucide-react';
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
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    };

    const SortIcon = ({ field }: { field: keyof Tenant }) => {
        if (sort.field !== field) return null;
        return sort.direction === 'asc' ? <SortAsc className="h-4 w-4" /> : <SortDesc className="h-4 w-4" />;
    };

    return (
        <div className="space-y-6">
            {/* Search */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Search className="h-5 w-5" />
                        Search Tenants
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="flex gap-4">
                        <div className="flex-1">
                            <Input
                                placeholder="Search by name, description, or slug..."
                                value={filters.search}
                                onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                            />
                        </div>
                        {filters.search && (
                            <Button
                                variant="outline"
                                onClick={() => setFilters(INITIAL_FILTERS)}
                            >
                                Clear
                            </Button>
                        )}
                    </div>
                </CardContent>
            </Card>

            {/* Tenants Table */}
            <Card>
                <CardHeader>
                    <CardTitle>Tenant List</CardTitle>
                </CardHeader>
                <CardContent>
                    {filteredTenants.length === 0 ? (
                        <div className="text-center py-8">
                            <Building className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">No tenants found</h3>
                            <p className="text-gray-500 mb-4">
                                {filters.search ? 'No tenants match your search criteria.' : 'No tenants have been created yet.'}
                            </p>
                        </div>
                    ) : (
                        <>
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead
                                            className="cursor-pointer hover:bg-gray-50"
                                            onClick={() => handleSort('name')}
                                        >
                                            <div className="flex items-center gap-1">
                                                Name
                                                <SortIcon field="name" />
                                            </div>
                                        </TableHead>
                                        <TableHead>Description</TableHead>
                                        <TableHead
                                            className="cursor-pointer hover:bg-gray-50"
                                            onClick={() => handleSort('isActive')}
                                        >
                                            <div className="flex items-center gap-1">
                                                Status
                                                <SortIcon field="isActive" />
                                            </div>
                                        </TableHead>
                                        <TableHead
                                            className="cursor-pointer hover:bg-gray-50"
                                            onClick={() => handleSort('createdAt')}
                                        >
                                            <div className="flex items-center gap-1">
                                                Created
                                                <SortIcon field="createdAt" />
                                            </div>
                                        </TableHead>
                                        <TableHead
                                            className="cursor-pointer hover:bg-gray-50"
                                            onClick={() => handleSort('updatedAt')}
                                        >
                                            <div className="flex items-center gap-1">
                                                Updated
                                                <SortIcon field="updatedAt" />
                                            </div>
                                        </TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {paginatedTenants.map((tenant) => (
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
                                            <TableCell>{formatDate(tenant.createdAt)}</TableCell>
                                            <TableCell>{formatDate(tenant.updatedAt)}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>

                            {/* Pagination */}
                            {totalPages > 1 && (
                                <div className="flex items-center justify-between px-2 py-4">
                                    <div className="text-sm text-gray-600">
                                        Showing {((currentPage - 1) * itemsPerPage) + 1} to {Math.min(currentPage * itemsPerPage, sortedTenants.length)} of {sortedTenants.length} results
                                    </div>
                                    <div className="flex items-center space-x-2">
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
                                        <span className="text-sm font-medium">
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
                        </>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}
