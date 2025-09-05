'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { HoverCard, HoverCardContent, HoverCardTrigger } from '@/components/ui/hover-card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { EnhancedTestingRequest } from '@/lib/admin/testing-lab/requests/testing-requests.actions';
import {
    Calendar,
    Check,
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    Clock,
    ExternalLink,
    Eye,
    Gamepad2,
    MapPin,
    Search,
    TestTube,
    Users,
    X
} from 'lucide-react';
import Link from 'next/link';
import { useMemo, useState } from 'react';

interface TestingRequestsListProps {
    testingRequests: EnhancedTestingRequest[];
}

interface TestingRequestFilters {
    search: string;
}

interface TestingRequestSort {
    field: keyof EnhancedTestingRequest;
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
                request.title?.toLowerCase().includes(searchLower) ||
                request.gameName?.toLowerCase().includes(searchLower) ||
                request.gameVersion?.toLowerCase().includes(searchLower) ||
                request.assignedSession?.sessionName?.toLowerCase().includes(searchLower)
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

    const handleSort = (field: keyof EnhancedTestingRequest) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
    };

    const getStatusBadge = (request: EnhancedTestingRequest) => {
        switch (request.status) {
            case 0: // Draft
                return <Badge variant="outline">Waiting</Badge>;
            case 1: // Open
                return <Badge variant="secondary">Under Review</Badge>;
            case 2: // InProgress
                return <Badge className="bg-green-500">Approved</Badge>;
            case 3: // Completed
                return <Badge className="bg-green-600">Approved</Badge>;
            case 4: // Cancelled
                return <Badge variant="destructive">Rejected</Badge>;
            default:
                return <Badge variant="outline">Unknown</Badge>;
        }
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
                            placeholder="Search by title, game name, or session..."
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
                                <TableHead>
                                    <div className="flex items-center gap-1">
                                        <Gamepad2 className="h-4 w-4" />
                                        Game
                                    </div>
                                </TableHead>
                                <TableHead>
                                    <div className="flex items-center gap-1">
                                        <Calendar className="h-4 w-4" />
                                        Session
                                    </div>
                                </TableHead>
                                <TableHead>
                                    <div className="flex items-center gap-1">
                                        <Clock className="h-4 w-4" />
                                        Submitted
                                    </div>
                                </TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead className="text-right">Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {paginatedTestingRequests.length === 0 ? (
                                <TableRow>
                                    <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                                        {filters.search ? 'No testing requests found matching your search.' : 'No testing requests found.'}
                                    </TableCell>
                                </TableRow>
                            ) : (
                                paginatedTestingRequests.map((request) => (
                                    <TableRow key={request.id}>
                                        <TableCell>
                                            <HoverCard>
                                                <HoverCardTrigger asChild>
                                                    <div className="cursor-pointer">
                                                        <div className="font-medium text-sm hover:text-primary transition-colors">
                                                            {request.gameName}
                                                        </div>
                                                        <div className="text-xs text-muted-foreground">
                                                            v{request.gameVersion}
                                                        </div>
                                                    </div>
                                                </HoverCardTrigger>
                                                <HoverCardContent className="w-80">
                                                    <div className="space-y-3">
                                                        <div className="flex items-start gap-3">
                                                            <Gamepad2 className="h-5 w-5 text-primary mt-0.5" />
                                                            <div className="space-y-1">
                                                                <h4 className="font-semibold">{request.gameName}</h4>
                                                                <p className="text-sm text-muted-foreground">
                                                                    Version {request.gameVersion}
                                                                </p>
                                                            </div>
                                                        </div>
                                                        {request.projectVersion?.project?.description && (
                                                            <p className="text-sm text-muted-foreground">
                                                                {request.projectVersion.project.description}
                                                            </p>
                                                        )}
                                                        <div className="flex items-center gap-4 text-xs text-muted-foreground">
                                                            {request.projectVersion?.createdAt && (
                                                                <div className="flex items-center gap-1">
                                                                    <Clock className="h-3 w-3" />
                                                                    Created {formatDate(request.projectVersion.createdAt)}
                                                                </div>
                                                            )}
                                                        </div>
                                                    </div>
                                                </HoverCardContent>
                                            </HoverCard>
                                        </TableCell>
                                        <TableCell>
                                            {request.assignedSession ? (
                                                <HoverCard>
                                                    <HoverCardTrigger asChild>
                                                        <Link
                                                            href={`/dashboard/testing-lab/sessions/${request.assignedSession.id}`}
                                                            className="block cursor-pointer"
                                                        >
                                                            <div className="font-medium text-sm flex items-center gap-1 hover:text-primary transition-colors">
                                                                {request.assignedSession.sessionName}
                                                                <ExternalLink className="h-3 w-3" />
                                                            </div>
                                                            <div className="text-xs text-muted-foreground">
                                                                {formatDate(request.assignedSession.sessionDate)}
                                                            </div>
                                                        </Link>
                                                    </HoverCardTrigger>
                                                    <HoverCardContent className="w-80">
                                                        <div className="space-y-3">
                                                            <div className="flex items-start gap-3">
                                                                <Calendar className="h-5 w-5 text-primary mt-0.5" />
                                                                <div className="space-y-1">
                                                                    <h4 className="font-semibold">{request.assignedSession.sessionName}</h4>
                                                                    <p className="text-sm text-muted-foreground">
                                                                        {formatDate(request.assignedSession.sessionDate)}
                                                                    </p>
                                                                </div>
                                                            </div>
                                                            <div className="flex items-center gap-4 text-xs text-muted-foreground">
                                                                <div className="flex items-center gap-1">
                                                                    <Users className="h-3 w-3" />
                                                                    Max {request.assignedSession.maxTesters} testers
                                                                </div>
                                                                {request.assignedSession.location?.name && (
                                                                    <div className="flex items-center gap-1">
                                                                        <MapPin className="h-3 w-3" />
                                                                        {request.assignedSession.location.name}
                                                                    </div>
                                                                )}
                                                            </div>
                                                            <div className="text-xs text-muted-foreground">
                                                                <div>Start: {request.assignedSession.startTime}</div>
                                                                <div>End: {request.assignedSession.endTime}</div>
                                                            </div>
                                                        </div>
                                                    </HoverCardContent>
                                                </HoverCard>
                                            ) : (
                                                <Badge variant="outline" className="text-xs">
                                                    Not Assigned
                                                </Badge>
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm text-muted-foreground">
                                                {formatDate(request.createdAt)}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            {getStatusBadge(request)}
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <div className="flex items-center justify-end gap-1">
                                                <Button
                                                    size="sm"
                                                    variant="outline"
                                                    asChild
                                                    className="h-8 w-8 p-0"
                                                >
                                                    <Link href={`/dashboard/testing-lab/requests/${request.id}`}>
                                                        <Eye className="h-4 w-4" />
                                                        <span className="sr-only">View details</span>
                                                    </Link>
                                                </Button>
                                                {request.status === 1 && ( // Only show approve/reject for requests under review
                                                    <>
                                                        <Button
                                                            size="sm"
                                                            variant="default"
                                                            className="h-8 w-8 p-0"
                                                            onClick={() => {
                                                                // TODO: Implement approve action
                                                                console.log('Approve request', request.id);
                                                            }}
                                                        >
                                                            <Check className="h-4 w-4" />
                                                            <span className="sr-only">Approve</span>
                                                        </Button>
                                                        <Button
                                                            size="sm"
                                                            variant="destructive"
                                                            className="h-8 w-8 p-0"
                                                            onClick={() => {
                                                                // TODO: Implement reject action
                                                                console.log('Reject request', request.id);
                                                            }}
                                                        >
                                                            <X className="h-4 w-4" />
                                                            <span className="sr-only">Reject</span>
                                                        </Button>
                                                    </>
                                                )}
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
