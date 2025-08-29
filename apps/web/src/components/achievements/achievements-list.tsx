'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { AchievementDto } from '@/lib/core/api/generated/types.gen';
import {
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    Search,
    SortAsc,
    SortDesc,
    Trophy
} from 'lucide-react';
import { useMemo, useState } from 'react';

interface AchievementsListProps {
    achievements: AchievementDto[];
}

interface AchievementFilters {
    search: string;
}

interface AchievementSort {
    field: keyof AchievementDto;
    direction: 'asc' | 'desc';
}

const INITIAL_FILTERS: AchievementFilters = {
    search: ''
};

const INITIAL_SORT: AchievementSort = {
    field: 'updatedAt',
    direction: 'desc'
};

export function AchievementsList({ achievements: incomingAchievements }: AchievementsListProps) {
    const [filters, setFilters] = useState<AchievementFilters>(INITIAL_FILTERS);
    const [sort, setSort] = useState<AchievementSort>(INITIAL_SORT);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // Filter achievements based on search
    const filteredAchievements = useMemo(() => {
        let result = [...incomingAchievements];

        // Apply search filter
        if (filters.search) {
            const searchLower = filters.search.toLowerCase();
            result = result.filter(achievement =>
                achievement.name?.toLowerCase().includes(searchLower) ||
                achievement.description?.toLowerCase().includes(searchLower) ||
                achievement.category?.toLowerCase().includes(searchLower)
            );
        }

        return result;
    }, [incomingAchievements, filters]);

    // Sort achievements
    const sortedAchievements = useMemo(() => {
        return [...filteredAchievements].sort((a, b) => {
            const aValue = a[sort.field];
            const bValue = b[sort.field];

            if (aValue == null && bValue == null) return 0;
            if (aValue == null) return 1;
            if (bValue == null) return -1;

            const comparison = aValue < bValue ? -1 : 1;
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }, [filteredAchievements, sort]);

    // Paginate achievements
    const paginatedAchievements = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return sortedAchievements.slice(startIndex, startIndex + itemsPerPage);
    }, [sortedAchievements, currentPage]);

    const totalPages = Math.ceil(sortedAchievements.length / itemsPerPage);

    const handleSort = (field: keyof AchievementDto) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
    };

    const getAchievementStatusBadge = (achievement: AchievementDto) => {
        if (achievement.isActive) {
            return <Badge variant="default" className="bg-green-100 text-green-800">Active</Badge>;
        } else {
            return <Badge variant="secondary" className="bg-gray-100 text-gray-800">Inactive</Badge>;
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
                        <Trophy className="h-5 w-5" />
                        Achievements ({sortedAchievements.length})
                    </CardTitle>
                </div>
            </CardHeader>

            <CardContent className="space-y-4">
                {/* Search Filter */}
                <div className="flex items-center space-x-2">
                    <div className="relative flex-1">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search achievements by name, description, or category..."
                            value={filters.search}
                            onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                            className="pl-9"
                        />
                    </div>
                </div>

                {/* Achievements Table */}
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
                                    onClick={() => handleSort('category')}
                                >
                                    <div className="flex items-center gap-1">
                                        Category
                                        {sort.field === 'category' && (
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
                            {paginatedAchievements.length === 0 ? (
                                <TableRow>
                                    <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                                        {filters.search ? 'No achievements found matching your search.' : 'No achievements found.'}
                                    </TableCell>
                                </TableRow>
                            ) : (
                                paginatedAchievements.map((achievement) => (
                                    <TableRow key={achievement.id}>
                                        <TableCell className="font-medium">
                                            {achievement.name}
                                        </TableCell>
                                        <TableCell className="max-w-xs truncate">
                                            {achievement.description || <span className="text-gray-400">No description</span>}
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant="outline">
                                                {achievement.category || 'General'}
                                            </Badge>
                                        </TableCell>
                                        <TableCell>
                                            {getAchievementStatusBadge(achievement)}
                                        </TableCell>
                                        <TableCell>
                                            <div className="text-sm text-muted-foreground">
                                                {formatDate(achievement.updatedAt)}
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
                            Showing {((currentPage - 1) * itemsPerPage) + 1} to {Math.min(currentPage * itemsPerPage, sortedAchievements.length)} of {sortedAchievements.length} achievements
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
