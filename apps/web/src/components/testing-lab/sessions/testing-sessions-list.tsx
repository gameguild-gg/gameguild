'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { HoverCard, HoverCardContent, HoverCardTrigger } from '@/components/ui/hover-card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { TestingSession } from '@/lib/api/generated/types.gen';
import {
    Calendar,
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    Clock,
    Eye,
    MapPin,
    Search,
    SortAsc,
    SortDesc,
    TestTube,
    Users
} from 'lucide-react';
import Link from 'next/link';
import { useMemo, useState } from 'react';

interface TestingSessionsListProps {
    sessions: TestingSession[];
}

interface SessionFilters {
    search: string;
}

interface SessionSort {
    field: keyof TestingSession;
    direction: 'asc' | 'desc';
}

const INITIAL_FILTERS: SessionFilters = {
    search: ''
};

const INITIAL_SORT: SessionSort = {
    field: 'sessionDate',
    direction: 'desc'
};

export function TestingSessionsList({ sessions: incomingSessions }: TestingSessionsListProps) {
    const [filters, setFilters] = useState<SessionFilters>(INITIAL_FILTERS);
    const [sort, setSort] = useState<SessionSort>(INITIAL_SORT);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // Filter sessions based on search
    const filteredSessions = useMemo(() => {
        let result = [...incomingSessions];

        // Apply search filter
        if (filters.search) {
            const searchLower = filters.search.toLowerCase();
            result = result.filter(session =>
                session.sessionName?.toLowerCase().includes(searchLower) ||
                session.location?.name?.toLowerCase().includes(searchLower) ||
                session.manager?.name?.toLowerCase().includes(searchLower) ||
                session.manager?.username?.toLowerCase().includes(searchLower)
            );
        }

        return result;
    }, [incomingSessions, filters]);

    // Sort sessions
    const sortedSessions = useMemo(() => {
        return [...filteredSessions].sort((a, b) => {
            const aValue = a[sort.field];
            const bValue = b[sort.field];

            if (aValue == null && bValue == null) return 0;
            if (aValue == null) return 1;
            if (bValue == null) return -1;

            const comparison = aValue < bValue ? -1 : 1;
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }, [filteredSessions, sort]);

    // Paginate sessions
    const paginatedSessions = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return sortedSessions.slice(startIndex, startIndex + itemsPerPage);
    }, [sortedSessions, currentPage]);

    const totalPages = Math.ceil(sortedSessions.length / itemsPerPage);

    const handleSort = (field: keyof TestingSession) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
    };

    const getStatusColor = (status: number): 'default' | 'secondary' | 'destructive' | 'outline' => {
        switch (status) {
            case 0: return 'secondary';   // Scheduled
            case 1: return 'default';     // Active  
            case 2: return 'outline';     // Completed
            case 3: return 'destructive'; // Cancelled
            default: return 'outline';
        }
    };

    const getStatusText = (status: number): string => {
        switch (status) {
            case 0: return 'Scheduled';
            case 1: return 'Active';
            case 2: return 'Completed';
            case 3: return 'Cancelled';
            default: return 'Unknown';
        }
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    const formatTime = (timeString?: string) => {
        if (!timeString) return 'N/A';
        try {
            // Check if it's an ISO datetime string
            if (timeString.includes('T')) {
                const date = new Date(timeString);
                return date.toLocaleTimeString('pt-BR', {
                    hour: '2-digit',
                    minute: '2-digit',
                    hour12: false
                });
            }
            // Parse regular time string (format: HH:MM:SS or HH:MM)
            const [hours, minutes] = timeString.split(':');
            return `${hours}:${minutes}`;
        } catch {
            return timeString;
        }
    };

    return (
        <Card>
            <CardHeader>
                <div className="flex items-center justify-between">
                    <CardTitle className="flex items-center gap-2">
                        <TestTube className="h-5 w-5" />
                        Testing Sessions ({sortedSessions.length})
                    </CardTitle>
                </div>
            </CardHeader>

            <CardContent className="space-y-4">
                {/* Search Filter */}
                <div className="flex items-center space-x-2">
                    <div className="relative flex-1">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search sessions by name, location, or manager..."
                            value={filters.search}
                            onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                            className="pl-9"
                        />
                    </div>
                </div>

                {/* Sessions Table - Desktop and Large Tablets */}
                <div className="hidden lg:block rounded-md border overflow-x-auto">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead
                                    className="cursor-pointer hover:bg-muted/50 whitespace-nowrap"
                                    onClick={() => handleSort('sessionName')}
                                >
                                    <div className="flex items-center gap-1">
                                        <TestTube className="h-4 w-4" />
                                        Session Name
                                        {sort.field === 'sessionName' && (
                                            sort.direction === 'asc' ?
                                                <SortAsc className="h-4 w-4" /> :
                                                <SortDesc className="h-4 w-4" />
                                        )}
                                    </div>
                                </TableHead>
                                <TableHead className="whitespace-nowrap">
                                    <div className="flex items-center gap-1">
                                        <MapPin className="h-4 w-4" />
                                        Location & Date
                                    </div>
                                </TableHead>
                                <TableHead className="whitespace-nowrap">
                                    <div className="flex items-center gap-1">
                                        <Users className="h-4 w-4" />
                                        Capacity
                                    </div>
                                </TableHead>
                                <TableHead className="whitespace-nowrap">Manager</TableHead>
                                <TableHead className="whitespace-nowrap">Status</TableHead>
                                <TableHead className="text-right whitespace-nowrap">Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {paginatedSessions.length === 0 ? (
                                <TableRow>
                                    <TableCell colSpan={6} className="text-center text-muted-foreground py-8">
                                        {filters.search ? 'No sessions found matching your search.' : 'No testing sessions found.'}
                                    </TableCell>
                                </TableRow>
                            ) : (
                                paginatedSessions.map((session) => (
                                    <TableRow key={session.id}>
                                        <TableCell className="min-w-[200px]">
                                            <div className="space-y-1">
                                                <div className="font-medium">
                                                    {session.sessionName}
                                                </div>
                                                <div className="text-xs text-muted-foreground">
                                                    ID: {session.id}
                                                </div>
                                            </div>
                                        </TableCell>
                                        <TableCell className="min-w-[280px]">
                                            <HoverCard>
                                                <HoverCardTrigger asChild>
                                                    <div className="cursor-pointer">
                                                        {/* Layout responsivo: lado a lado em desktop, empilhado em mobile */}
                                                        <div className="flex flex-col xl:flex-row xl:items-center xl:justify-between gap-2 xl:gap-4">
                                                            {/* Location */}
                                                            <div className="flex items-center gap-1">
                                                                <MapPin className="h-3 w-3 text-muted-foreground" />
                                                                <span className="text-sm">
                                                                    {session.location?.name || 'No location'}
                                                                </span>
                                                            </div>

                                                            {/* Date & Time */}
                                                            <div className="lg:text-right">
                                                                <div className="flex items-center gap-2 text-sm lg:justify-end flex-wrap">
                                                                    <div className="flex items-center gap-1">
                                                                        <Calendar className="h-3 w-3" />
                                                                        {formatDate(session.sessionDate)}
                                                                    </div>
                                                                    <div className="flex items-center gap-1 text-muted-foreground">
                                                                        <Clock className="h-3 w-3" />
                                                                        {formatTime(session.startTime)} - {formatTime(session.endTime)}
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </HoverCardTrigger>
                                                <HoverCardContent className="w-80">
                                                    <div className="space-y-3">
                                                        <div className="flex items-start gap-3">
                                                            <MapPin className="h-5 w-5 text-primary mt-0.5" />
                                                            <div className="space-y-1">
                                                                <h4 className="font-semibold">
                                                                    {session.location?.name || 'No Location Set'}
                                                                </h4>
                                                                {session.location?.address && (
                                                                    <p className="text-sm text-muted-foreground">
                                                                        {session.location.address}
                                                                    </p>
                                                                )}
                                                            </div>
                                                        </div>
                                                        {session.location?.description && (
                                                            <p className="text-sm text-muted-foreground">
                                                                {session.location.description}
                                                            </p>
                                                        )}
                                                        <div className="flex items-center gap-4 text-xs text-muted-foreground">
                                                            {session.location?.createdAt && (
                                                                <div>Added: {formatDate(session.location.createdAt)}</div>
                                                            )}
                                                        </div>
                                                    </div>
                                                </HoverCardContent>
                                            </HoverCard>
                                        </TableCell>
                                        <TableCell className="min-w-[140px]">
                                            <HoverCard>
                                                <HoverCardTrigger asChild>
                                                    <div className="space-y-1 cursor-pointer">
                                                        <div className="text-sm flex items-center gap-1">
                                                            <Users className="h-3 w-3 text-muted-foreground" />
                                                            {session.registeredTesterCount || 0}/{session.maxTesters} testers
                                                        </div>
                                                        <div className="text-xs text-muted-foreground">
                                                            {session.registeredProjectCount || 0}/{session.maxProjects} projects
                                                        </div>
                                                    </div>
                                                </HoverCardTrigger>
                                                <HoverCardContent className="w-80">
                                                    <div className="space-y-4">
                                                        <div className="flex items-start gap-3">
                                                            <Users className="h-5 w-5 text-primary mt-0.5" />
                                                            <div className="space-y-1">
                                                                <h4 className="font-semibold">Session Capacity</h4>
                                                                <p className="text-sm text-muted-foreground">
                                                                    Registration details for this session
                                                                </p>
                                                            </div>
                                                        </div>

                                                        {/* Testers Section */}
                                                        <div className="space-y-2">
                                                            <h5 className="font-medium text-sm">Registered Testers ({session.registeredTesterCount || 0}/{session.maxTesters})</h5>
                                                            {session.registeredTesterCount ? (
                                                                <div className="text-sm text-muted-foreground">
                                                                    <p>• {session.registeredTesterCount} testers confirmed</p>
                                                                    <p>• {(session.maxTesters || 0) - (session.registeredTesterCount || 0)} slots available</p>
                                                                </div>
                                                            ) : (
                                                                <p className="text-sm text-muted-foreground">No testers registered yet</p>
                                                            )}
                                                        </div>

                                                        {/* Projects Section */}
                                                        <div className="space-y-2">
                                                            <h5 className="font-medium text-sm">Registered Projects ({session.registeredProjectCount || 0}/{session.maxProjects})</h5>
                                                            {session.registeredProjectCount ? (
                                                                <div className="text-sm text-muted-foreground">
                                                                    <p>• {session.registeredProjectCount} games registered</p>
                                                                    <p>• {session.registeredProjectMemberCount || 0} project members</p>
                                                                    <p>• {(session.maxProjects || 0) - (session.registeredProjectCount || 0)} slots available</p>
                                                                </div>
                                                            ) : (
                                                                <p className="text-sm text-muted-foreground">No games registered yet</p>
                                                            )}
                                                        </div>

                                                        <div className="text-xs text-muted-foreground pt-2 border-t">
                                                            Click on the session to see detailed participant lists
                                                        </div>
                                                    </div>
                                                </HoverCardContent>
                                            </HoverCard>
                                        </TableCell>
                                        <TableCell className="min-w-[140px]">
                                            <div className="text-sm">
                                                {session.manager ? (
                                                    <div>
                                                        <div className="font-medium">
                                                            {session.manager.name || session.manager.username}
                                                        </div>
                                                        {session.manager.email && (
                                                            <div className="text-xs text-muted-foreground">
                                                                {session.manager.email}
                                                            </div>
                                                        )}
                                                    </div>
                                                ) : (
                                                    <span className="text-muted-foreground">No manager</span>
                                                )}
                                            </div>
                                        </TableCell>
                                        <TableCell className="min-w-[100px]">
                                            <Badge variant={getStatusColor(session.status)}>
                                                {getStatusText(session.status)}
                                            </Badge>
                                        </TableCell>
                                        <TableCell className="text-right min-w-[80px]">
                                            <div className="flex justify-end gap-1">
                                                <Link href={`/dashboard/testing-lab/sessions/${session.id}`}>
                                                    <Button variant="ghost" size="sm" title="View Details">
                                                        <Eye className="h-4 w-4" />
                                                    </Button>
                                                </Link>
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))
                            )}
                        </TableBody>
                    </Table>
                </div>

                {/* Sessions Cards - Mobile and Tablets */}
                <div className="lg:hidden space-y-4">
                    {paginatedSessions.length === 0 ? (
                        <Card>
                            <CardContent className="pt-6">
                                <div className="text-center text-muted-foreground py-8">
                                    {filters.search ? 'No sessions found matching your search.' : 'No testing sessions found.'}
                                </div>
                            </CardContent>
                        </Card>
                    ) : (
                        paginatedSessions.map((session) => (
                            <Card key={session.id} className="p-4">
                                <div className="space-y-3">
                                    {/* Header */}
                                    <div className="flex items-start justify-between">
                                        <div className="space-y-1 flex-1">
                                            <h3 className="font-semibold text-base">{session.sessionName}</h3>
                                            <p className="text-xs text-muted-foreground">ID: {session.id}</p>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <Badge variant={getStatusColor(session.status)}>
                                                {getStatusText(session.status)}
                                            </Badge>
                                            <Link href={`/dashboard/testing-lab/sessions/${session.id}`}>
                                                <Button variant="ghost" size="sm" title="View Details">
                                                    <Eye className="h-4 w-4" />
                                                </Button>
                                            </Link>
                                        </div>
                                    </div>

                                    {/* Location & Date */}
                                    <HoverCard>
                                        <HoverCardTrigger asChild>
                                            <div className="space-y-2 cursor-pointer">
                                                <div className="flex items-center gap-2">
                                                    <MapPin className="h-4 w-4 text-muted-foreground" />
                                                    <span className="text-sm font-medium">
                                                        {session.location?.name || 'No location'}
                                                    </span>
                                                </div>
                                                <div className="flex items-center gap-4 text-sm text-muted-foreground pl-6">
                                                    <div className="flex items-center gap-1">
                                                        <Calendar className="h-3 w-3" />
                                                        {formatDate(session.sessionDate)}
                                                    </div>
                                                    <div className="flex items-center gap-1">
                                                        <Clock className="h-3 w-3" />
                                                        {formatTime(session.startTime)} - {formatTime(session.endTime)}
                                                    </div>
                                                </div>
                                            </div>
                                        </HoverCardTrigger>
                                        <HoverCardContent className="w-80">
                                            <div className="space-y-3">
                                                <div className="flex items-start gap-3">
                                                    <MapPin className="h-5 w-5 text-primary mt-0.5" />
                                                    <div className="space-y-1">
                                                        <h4 className="font-semibold">
                                                            {session.location?.name || 'No Location Set'}
                                                        </h4>
                                                        {session.location?.address && (
                                                            <p className="text-sm text-muted-foreground">
                                                                {session.location.address}
                                                            </p>
                                                        )}
                                                    </div>
                                                </div>
                                                {session.location?.description && (
                                                    <p className="text-sm text-muted-foreground">
                                                        {session.location.description}
                                                    </p>
                                                )}
                                            </div>
                                        </HoverCardContent>
                                    </HoverCard>

                                    {/* Capacity & Manager */}
                                    <div className="grid grid-cols-2 gap-4 pt-2 border-t">
                                        <div>
                                            <HoverCard>
                                                <HoverCardTrigger asChild>
                                                    <div className="cursor-pointer">
                                                        <h4 className="text-sm font-medium mb-1">Capacity</h4>
                                                        <div className="space-y-1 text-sm text-muted-foreground">
                                                            <div className="flex items-center gap-1">
                                                                <Users className="h-3 w-3" />
                                                                {session.registeredTesterCount || 0}/{session.maxTesters} testers
                                                            </div>
                                                            <div className="text-xs">
                                                                {session.registeredProjectCount || 0}/{session.maxProjects} projects
                                                            </div>
                                                        </div>
                                                    </div>
                                                </HoverCardTrigger>
                                                <HoverCardContent className="w-80">
                                                    <div className="space-y-4">
                                                        <div className="flex items-start gap-3">
                                                            <Users className="h-5 w-5 text-primary mt-0.5" />
                                                            <div className="space-y-1">
                                                                <h4 className="font-semibold">Session Capacity</h4>
                                                                <p className="text-sm text-muted-foreground">
                                                                    Registration details for this session
                                                                </p>
                                                            </div>
                                                        </div>

                                                        {/* Testers Section */}
                                                        <div className="space-y-2">
                                                            <h5 className="font-medium text-sm">Registered Testers ({session.registeredTesterCount || 0}/{session.maxTesters})</h5>
                                                            {session.registeredTesterCount ? (
                                                                <div className="text-sm text-muted-foreground">
                                                                    <p>• {session.registeredTesterCount} testers confirmed</p>
                                                                    <p>• {(session.maxTesters || 0) - (session.registeredTesterCount || 0)} slots available</p>
                                                                </div>
                                                            ) : (
                                                                <p className="text-sm text-muted-foreground">No testers registered yet</p>
                                                            )}
                                                        </div>

                                                        {/* Projects Section */}
                                                        <div className="space-y-2">
                                                            <h5 className="font-medium text-sm">Registered Projects ({session.registeredProjectCount || 0}/{session.maxProjects})</h5>
                                                            {session.registeredProjectCount ? (
                                                                <div className="text-sm text-muted-foreground">
                                                                    <p>• {session.registeredProjectCount} games registered</p>
                                                                    <p>• {session.registeredProjectMemberCount || 0} project members</p>
                                                                    <p>• {(session.maxProjects || 0) - (session.registeredProjectCount || 0)} slots available</p>
                                                                </div>
                                                            ) : (
                                                                <p className="text-sm text-muted-foreground">No games registered yet</p>
                                                            )}
                                                        </div>
                                                    </div>
                                                </HoverCardContent>
                                            </HoverCard>
                                        </div>
                                        <div>
                                            <h4 className="text-sm font-medium mb-1">Manager</h4>
                                            <div className="text-sm text-muted-foreground">
                                                {session.manager ? (
                                                    <div>
                                                        <div className="font-medium text-foreground">
                                                            {session.manager.name || session.manager.username}
                                                        </div>
                                                        {session.manager.email && (
                                                            <div className="text-xs">
                                                                {session.manager.email}
                                                            </div>
                                                        )}
                                                    </div>
                                                ) : (
                                                    <span>No manager</span>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </Card>
                        ))
                    )}
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                    <div className="flex items-center justify-between">
                        <p className="text-sm text-muted-foreground">
                            Showing {Math.min((currentPage - 1) * itemsPerPage + 1, sortedSessions.length)} to{' '}
                            {Math.min(currentPage * itemsPerPage, sortedSessions.length)} of {sortedSessions.length} sessions
                        </p>
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
                                onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
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
                                onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
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
