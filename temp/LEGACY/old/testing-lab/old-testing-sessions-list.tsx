'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { TestingSession } from '@/lib/api/testing-types';
import {
    Calendar,
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    Clock,
    MapPin,
    Search,
    SortAsc,
    SortDesc,
    Users
} from 'lucide-react';
import Link from 'next/link';
import { useMemo, useState } from 'react';

interface TestingSessionsListProps {
    testingSessions: TestingSession[];
}

interface TestingSessionFilters {
    search: string;
}

interface TestingSessionSort {
    field: keyof TestingSession;
    direction: 'asc' | 'desc';
}

const INITIAL_FILTERS: TestingSessionFilters = {
    search: ''
};

const INITIAL_SORT: TestingSessionSort = {
    field: 'startDate',
    direction: 'desc'
};

function getStatusColor(status: string): string {
    switch (status) {
        case 'scheduled':
            return 'bg-blue-100 text-blue-800 border-blue-200';
        case 'active':
            return 'bg-green-100 text-green-800 border-green-200';
        case 'completed':
            return 'bg-gray-100 text-gray-800 border-gray-200';
        case 'cancelled':
            return 'bg-red-100 text-red-800 border-red-200';
        default:
            return 'bg-gray-100 text-gray-800 border-gray-200';
    }
}

function getLocationTypeIcon(locationType: string) {
    switch (locationType) {
        case 'remote':
            return '💻';
        case 'in-person':
            return '🏢';
        case 'hybrid':
            return '🔄';
        default:
            return '📍';
    }
}

export function TestingSessionsList({ testingSessions: incomingTestingSessions }: TestingSessionsListProps) {
    const [filters, setFilters] = useState<TestingSessionFilters>(INITIAL_FILTERS);
    const [sort, setSort] = useState<TestingSessionSort>(INITIAL_SORT);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // Filter testing sessions based on search
    const filteredTestingSessions = useMemo(() => {
        let result = [...incomingTestingSessions];

        // Apply search filter
        if (filters.search) {
            result = result.filter(session =>
                session.sessionName.toLowerCase().includes(filters.search.toLowerCase()) ||
                session.location.name.toLowerCase().includes(filters.search.toLowerCase()) ||
                session.testingRequest?.title?.toLowerCase().includes(filters.search.toLowerCase()) ||
                session.createdBy.name.toLowerCase().includes(filters.search.toLowerCase())
            );
        }

        return result;
    }, [incomingTestingSessions, filters]);

    // Sort testing sessions
    const sortedTestingSessions = useMemo(() => {
        return [...filteredTestingSessions].sort((a, b) => {
            const aVal = a[sort.field];
            const bVal = b[sort.field];

            if (aVal === bVal) return 0;

            const comparison = aVal < bVal ? -1 : 1;
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }, [filteredTestingSessions, sort]);

    // Paginate testing sessions
    const paginatedTestingSessions = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return sortedTestingSessions.slice(startIndex, startIndex + itemsPerPage);
    }, [sortedTestingSessions, currentPage]);

    const totalPages = Math.ceil(sortedTestingSessions.length / itemsPerPage);

    const handleSearch = (value: string) => {
        setFilters(prev => ({ ...prev, search: value }));
        setCurrentPage(1);
    };

    const handleSort = (field: keyof TestingSession) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
        setCurrentPage(1);
    };

    const handlePageChange = (page: number) => {
        setCurrentPage(page);
    };

    const getSortIcon = (field: keyof TestingSession) => {
        if (sort.field !== field) return null;
        return sort.direction === 'asc' ?
            <SortAsc className="h-4 w-4" /> :
            <SortDesc className="h-4 w-4" />;
    };

    return (
        <Card>
            <CardHeader>
                <CardTitle className="flex items-center gap-2">
                    <Calendar className="h-5 w-5" />
                    Testing Sessions
                    <Badge variant="secondary" className="ml-auto">
                        {sortedTestingSessions.length}
                    </Badge>
                </CardTitle>
                <div className="flex items-center space-x-2">
                    <div className="relative flex-1">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search sessions, locations, or testers..."
                            value={filters.search}
                            onChange={(e) => handleSearch(e.target.value)}
                            className="pl-10"
                        />
                    </div>
                </div>
            </CardHeader>
            <CardContent>
                {paginatedTestingSessions.length === 0 ? (
                    <div className="text-center py-12">
                        <Calendar className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                        <h3 className="text-lg font-medium mb-2">No Testing Sessions Found</h3>
                        <p className="text-muted-foreground mb-4">
                            {filters.search ? 'Try adjusting your search criteria' : 'There are no testing sessions available'}
                        </p>
                    </div>
                ) : (
                    <>
                        <div className="rounded-md border">
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead
                                            className="cursor-pointer hover:bg-muted/50"
                                            onClick={() => handleSort('sessionName')}
                                        >
                                            <div className="flex items-center space-x-1">
                                                <span>Session Name</span>
                                                {getSortIcon('sessionName')}
                                            </div>
                                        </TableHead>
                                        <TableHead
                                            className="cursor-pointer hover:bg-muted/50"
                                            onClick={() => handleSort('startDate')}
                                        >
                                            <div className="flex items-center space-x-1">
                                                <span>Start Date</span>
                                                {getSortIcon('startDate')}
                                            </div>
                                        </TableHead>
                                        <TableHead>Location</TableHead>
                                        <TableHead>Capacity</TableHead>
                                        <TableHead>Status</TableHead>
                                        <TableHead>Created By</TableHead>
                                        <TableHead className="text-right">Actions</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {paginatedTestingSessions.map((session) => (
                                        <TableRow key={session.id}>
                                            <TableCell>
                                                <div>
                                                    <div className="font-medium">
                                                        {session.sessionName}
                                                    </div>
                                                    {session.testingRequest?.title && (
                                                        <div className="text-sm text-muted-foreground">
                                                            {session.testingRequest.title}
                                                        </div>
                                                    )}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="flex items-center space-x-1 text-sm">
                                                    <Clock className="h-4 w-4 text-muted-foreground" />
                                                    <span>
                                                        {new Date(session.startDate).toLocaleDateString()} at{' '}
                                                        {new Date(session.startDate).toLocaleTimeString([], {
                                                            hour: '2-digit',
                                                            minute: '2-digit'
                                                        })}
                                                    </span>
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="flex items-center space-x-2">
                                                    <span>{getLocationTypeIcon(session.location.type)}</span>
                                                    <span className="text-sm">{session.location.name}</span>
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="flex items-center space-x-1 text-sm">
                                                    <Users className="h-4 w-4 text-muted-foreground" />
                                                    <span>
                                                        {session.registeredTesterCount}/{session.maxTesters || '∞'}
                                                    </span>
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <Badge
                                                    variant="outline"
                                                    className={getStatusColor(session.status)}
                                                >
                                                    {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant="secondary">
                                                    {session.createdBy.name}
                                                </Badge>
                                            </TableCell>
                                            <TableCell className="text-right">
                                                <Button variant="outline" size="sm" asChild>
                                                    <Link href={`/dashboard/testing-lab/sessions/${session.id}`}>
                                                        <MapPin className="h-4 w-4 mr-1" />
                                                        View
                                                    </Link>
                                                </Button>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </div>

                        {/* Pagination */}
                        {totalPages > 1 && (
                            <div className="flex items-center justify-between mt-4">
                                <div className="text-sm text-muted-foreground">
                                    Showing {Math.min((currentPage - 1) * itemsPerPage + 1, sortedTestingSessions.length)} to{' '}
                                    {Math.min(currentPage * itemsPerPage, sortedTestingSessions.length)} of{' '}
                                    {sortedTestingSessions.length} sessions
                                </div>
                                <div className="flex items-center space-x-2">
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handlePageChange(1)}
                                        disabled={currentPage === 1}
                                    >
                                        <ChevronsLeft className="h-4 w-4" />
                                    </Button>
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handlePageChange(currentPage - 1)}
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
                                        onClick={() => handlePageChange(currentPage + 1)}
                                        disabled={currentPage === totalPages}
                                    >
                                        <ChevronRight className="h-4 w-4" />
                                    </Button>
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handlePageChange(totalPages)}
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
    );
}
