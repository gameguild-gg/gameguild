'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { TestingRequest } from '@/lib/api/testing-types';
import {
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    Search,
    SortAsc,
    SortDesc,
    TestTube
} from 'lucide-react';
import { useMemo, useState } from 'react';

interface TestingRequestsListProps {
    testingRequests: TestingRequest[];
}

interface TestingRequestFilters {
    search: string;
}

interface TestingRequestSort {
    field: keyof TestingRequest;
    direction: 'asc' | 'desc';
}

const INITIAL_FILTERS: TestingRequestFilters = {
    search: ''
};

const INITIAL_SORT: TestingRequestSort = {
    field: 'title',
    direction: 'asc'
};

export function TestingRequestsList({ testingRequests: incomingTestingRequests }: TestingRequestsListProps) {
    const [filters, setFilters] = useState<TestingRequestFilters>(INITIAL_FILTERS);
    const [sort, setSort] = useState<TestingRequestSort>(INITIAL_SORT);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // Filter testing requests based on search
    const filteredTestingRequests = useMemo(() => {
        let result = [...incomingTestingRequests];

        // Apply search filter
        if (filters.search) {
            const searchLower = filters.search.toLowerCase();
            result = result.filter(request =>
                request.title?.toLowerCase().includes(searchLower)
            );
        }

        return result;
    }, [incomingTestingRequests, filters]);

    // Sort testing requests
    const sortedTestingRequests = useMemo(() => {
        return [...filteredTestingRequests].sort((a, b) => {
            const aValue = a[sort.field];
            const bValue = b[sort.field];

            if (aValue == null && bValue == null) return 0;
            if (aValue == null) return 1;
            if (bValue == null) return -1;

            const comparison = aValue < bValue ? -1 : 1;
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }, [filteredTestingRequests, sort]);

    // Paginate testing requests
    const paginatedTestingRequests = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return sortedTestingRequests.slice(startIndex, startIndex + itemsPerPage);
    }, [sortedTestingRequests, currentPage]);

    const totalPages = Math.ceil(sortedTestingRequests.length / itemsPerPage);

    const handleSort = (field: keyof TestingRequest) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
    };

    const getStatusBadge = (request: TestingRequest) => {
        // Since the TestingRequest interface is minimal, we'll show a default status
        return <Badge variant="outline">Active</Badge>;
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
                        <TestTube className="h-5 w-5" />
                        Testing Requests ({sortedTestingRequests.length})
                    </CardTitle>
                </div>
            </CardHeader>

            <CardContent className="space-y-4">
                {/* Search Filter */}
                <div className="flex items-center space-x-2">
                    <div className="relative flex-1">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search testing requests by title..."
                            value={filters.search}
                            onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                            className="pl-9"
                        />
                    </div>
                </div>

                {/* Testing Requests Table */}
                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('title')}
                                >
                                    <div className="flex items-center gap-1">
                                        Title
                                        {sort.field === 'title' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() => handleSort('id')}
                                >
                                    <div className="flex items-center gap-1">
                                        ID
                                        {sort.field === 'id' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {paginatedTestingRequests.length === 0 ? (
                                <TableRow>
                                    <TableCell colSpan={3} className="text-center text-muted-foreground py-8">
                                        {filters.search ? 'No testing requests found matching your search.' : 'No testing requests found.'}
                                    </TableCell>
                                </TableRow>
                            ) : (
                                paginatedTestingRequests.map((request) => (
                                    <TableRow key={request.id}>
                                        <TableCell className="font-medium">
                                            {request.title}
                                        </TableCell>
                                        <TableCell>
                                            {getStatusBadge(request)}
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm text-muted-foreground">
                                                {request.id}
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
                            Showing {((currentPage - 1) * itemsPerPage) + 1} to {Math.min(currentPage * itemsPerPage, sortedTestingRequests.length)} of {sortedTestingRequests.length} testing requests
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
