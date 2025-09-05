'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { getTestingSessionsAction } from '@/lib/admin/testing-lab/sessions/sessions.actions';
import { TestingSession } from '@/lib/admin/testing-lab/types';
import { SessionStatus } from '@/lib/api/generated/types.gen';
import { Calendar, Clock, Loader2, Search, Users } from 'lucide-react';
import React, { useEffect, useMemo, useState } from 'react';

interface SessionFilters {
    search: string;
    status: string;
}

const ITEMS_PER_PAGE = 10;

const INITIAL_FILTERS: SessionFilters = {
    search: '',
    status: 'all',
};

function getStatusColor(status: SessionStatus): string {
    switch (status) {
        case SessionStatus.SCHEDULED:
            return 'bg-blue-100 text-blue-800 border-blue-200';
        case SessionStatus.ACTIVE:
            return 'bg-green-100 text-green-800 border-green-200';
        case SessionStatus.COMPLETED:
            return 'bg-gray-100 text-gray-800 border-gray-200';
        case SessionStatus.CANCELLED:
            return 'bg-red-100 text-red-800 border-red-200';
        default:
            return 'bg-gray-100 text-gray-800 border-gray-200';
    }
}

function getStatusText(status: SessionStatus): string {
    switch (status) {
        case SessionStatus.SCHEDULED:
            return 'Scheduled';
        case SessionStatus.ACTIVE:
            return 'Active';
        case SessionStatus.COMPLETED:
            return 'Completed';
        case SessionStatus.CANCELLED:
            return 'Cancelled';
        default:
            return 'Unknown';
    }
}

export function TestingSessionsList(): React.JSX.Element {
    const [sessions, setSessions] = useState<TestingSession[]>([]);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState<SessionFilters>(INITIAL_FILTERS);
    const [currentPage, setCurrentPage] = useState(1);

    // Load sessions data
    useEffect(() => {
        async function loadSessions() {
            setLoading(true);
            try {
                const data = await getTestingSessionsAction();
                setSessions(data);
            } catch (error) {
                console.error('Failed to load testing sessions:', error);
            } finally {
                setLoading(false);
            }
        }

        loadSessions();
    }, []);

    // Filter sessions based on current filters
    const filteredSessions = useMemo(() => {
        return sessions.filter(session => {
            // Search filter
            if (filters.search) {
                const searchLower = filters.search.toLowerCase();
                if (!session.sessionName.toLowerCase().includes(searchLower) &&
                    !(session.id || '').toLowerCase().includes(searchLower)) {
                    return false;
                }
            }

            // Status filter
            if (filters.status !== 'all' && filters.status !== session.status.toString()) {
                return false;
            }

            return true;
        });
    }, [sessions, filters]);

    // Pagination
    const totalPages = Math.ceil(filteredSessions.length / ITEMS_PER_PAGE);
    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
    const paginatedSessions = filteredSessions.slice(startIndex, startIndex + ITEMS_PER_PAGE);

    const handleSearch = (searchTerm: string) => {
        setFilters(prev => ({ ...prev, search: searchTerm }));
        setCurrentPage(1);
    };

    if (loading) {
        return (
            <Card>
                <CardContent className="flex items-center justify-center py-8">
                    <Loader2 className="w-8 h-8 animate-spin" />
                    <span className="ml-2">Loading sessions...</span>
                </CardContent>
            </Card>
        );
    }

    return (
        <div className="space-y-4">
            {/* Search and Filters */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Calendar className="w-5 h-5" />
                        Testing Sessions ({filteredSessions.length})
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="flex flex-col md:flex-row gap-4">
                        <div className="flex-1">
                            <div className="relative">
                                <Search className="absolute left-3 top-3 w-4 h-4 text-muted-foreground" />
                                <Input
                                    placeholder="Search sessions..."
                                    value={filters.search}
                                    onChange={(e) => handleSearch(e.target.value)}
                                    className="pl-10"
                                />
                            </div>
                        </div>
                        <select
                            value={filters.status}
                            onChange={(e) => setFilters(prev => ({ ...prev, status: e.target.value }))}
                            className="px-3 py-2 border rounded-md"
                        >
                            <option value="all">All Status</option>
                            <option value="0">Scheduled</option>
                            <option value="1">Active</option>
                            <option value="2">Completed</option>
                            <option value="3">Cancelled</option>
                        </select>
                    </div>
                </CardContent>
            </Card>

            {/* Sessions List */}
            <Card>
                <CardContent className="p-0">
                    <div className="overflow-x-auto">
                        <table className="w-full">
                            <thead className="bg-muted/50">
                                <tr>
                                    <th className="text-left p-4 font-semibold">Session Name</th>
                                    <th className="text-left p-4 font-semibold">Date & Time</th>
                                    <th className="text-left p-4 font-semibold">Capacity</th>
                                    <th className="text-left p-4 font-semibold">Status</th>
                                    <th className="text-left p-4 font-semibold">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {paginatedSessions.map((session, index) => (
                                    <tr key={session.id} className={index % 2 === 0 ? 'bg-background' : 'bg-muted/20'}>
                                        <td className="p-4">
                                            <div className="space-y-1">
                                                <div className="font-medium">{session.sessionName}</div>
                                                <div className="text-sm text-muted-foreground">ID: {session.id}</div>
                                            </div>
                                        </td>
                                        <td className="p-4">
                                            <div className="space-y-1">
                                                <div className="flex items-center gap-2">
                                                    <Calendar className="w-4 h-4 text-muted-foreground" />
                                                    <span>{session.sessionDate}</span>
                                                </div>
                                                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                                    <Clock className="w-4 h-4" />
                                                    <span>{session.startTime} - {session.endTime}</span>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="p-4">
                                            <div className="flex items-center gap-2">
                                                <Users className="w-4 h-4 text-muted-foreground" />
                                                <span>
                                                    {session.registeredTesterCount || 0}/{session.maxTesters}
                                                </span>
                                            </div>
                                        </td>
                                        <td className="p-4">
                                            <Badge className={getStatusColor(session.status)}>
                                                {getStatusText(session.status)}
                                            </Badge>
                                        </td>
                                        <td className="p-4">
                                            <Button variant="outline" size="sm">
                                                View Details
                                            </Button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>

                        {paginatedSessions.length === 0 && (
                            <div className="text-center py-8 text-muted-foreground">
                                {filters.search || filters.status !== 'all' ?
                                    'No sessions match your filters.' :
                                    'No testing sessions found.'
                                }
                            </div>
                        )}
                    </div>
                </CardContent>
            </Card>

            {/* Pagination */}
            {totalPages > 1 && (
                <Card>
                    <CardContent className="flex items-center justify-between p-4">
                        <div className="text-sm text-muted-foreground">
                            Showing {startIndex + 1} to {Math.min(startIndex + ITEMS_PER_PAGE, filteredSessions.length)} of {filteredSessions.length} sessions
                        </div>
                        <div className="flex items-center space-x-2">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                                disabled={currentPage === 1}
                            >
                                Previous
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
                                Next
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}
