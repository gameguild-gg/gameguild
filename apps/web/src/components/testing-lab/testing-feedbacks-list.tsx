'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type { TestingFeedback } from '@/lib/api/testing-types';
import {
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
    MessageSquare,
    Search,
    SortAsc,
    SortDesc,
    Star
} from 'lucide-react';
import { useMemo, useState } from 'react';

interface TestingFeedbacksListProps {
    testingFeedbacks: TestingFeedback[];
}

interface TestingFeedbackFilters {
    search: string;
}

interface TestingFeedbackSort {
    field: keyof TestingFeedback;
    direction: 'asc' | 'desc';
}

const INITIAL_FILTERS: TestingFeedbackFilters = {
    search: ''
};

const INITIAL_SORT: TestingFeedbackSort = {
    field: 'submittedAt',
    direction: 'desc'
};

export function TestingFeedbacksList({ testingFeedbacks: incomingTestingFeedbacks }: TestingFeedbacksListProps) {
    const [filters, setFilters] = useState<TestingFeedbackFilters>(INITIAL_FILTERS);
    const [sort, setSort] = useState<TestingFeedbackSort>(INITIAL_SORT);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // Filter testing feedbacks based on search
    const filteredTestingFeedbacks = useMemo(() => {
        let result = [...incomingTestingFeedbacks];

        // Apply search filter
        if (filters.search) {
            result = result.filter(feedback =>
                feedback.content.toLowerCase().includes(filters.search.toLowerCase()) ||
                feedback.sessionTitle.toLowerCase().includes(filters.search.toLowerCase()) ||
                feedback.submittedBy.name.toLowerCase().includes(filters.search.toLowerCase())
            );
        }

        return result;
    }, [incomingTestingFeedbacks, filters]);

    // Sort testing feedbacks
    const sortedTestingFeedbacks = useMemo(() => {
        return [...filteredTestingFeedbacks].sort((a, b) => {
            const aVal = a[sort.field];
            const bVal = b[sort.field];

            if (aVal === bVal) return 0;

            const comparison = aVal < bVal ? -1 : 1;
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }, [filteredTestingFeedbacks, sort]);

    // Paginate testing feedbacks
    const paginatedTestingFeedbacks = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return sortedTestingFeedbacks.slice(startIndex, startIndex + itemsPerPage);
    }, [sortedTestingFeedbacks, currentPage]);

    const totalPages = Math.ceil(sortedTestingFeedbacks.length / itemsPerPage);

    const handleSearch = (value: string) => {
        setFilters(prev => ({ ...prev, search: value }));
        setCurrentPage(1);
    };

    const handleSort = (field: keyof TestingFeedback) => {
        setSort(prev => ({
            field,
            direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc'
        }));
        setCurrentPage(1);
    };

    const handlePageChange = (page: number) => {
        setCurrentPage(Math.max(1, Math.min(page, totalPages)));
    };

    const getSortIcon = (field: keyof TestingFeedback) => {
        if (sort.field !== field) return null;
        return sort.direction === 'asc' ? <SortAsc className="h-4 w-4" /> : <SortDesc className="h-4 w-4" />;
    };

    const getRatingStars = (rating: number) => {
        return Array.from({ length: 5 }, (_, i) => (
            <Star
                key={i}
                className={`h-4 w-4 ${i < rating ? 'fill-yellow-400 text-yellow-400' : 'text-gray-300'
                    }`}
            />
        ));
    };

    return (
        <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
                <div className="flex items-center space-x-2">
                    <MessageSquare className="h-5 w-5 text-muted-foreground" />
                    <CardTitle>Testing Feedbacks</CardTitle>
                </div>
                <div className="flex items-center space-x-2">
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search feedbacks..."
                            value={filters.search}
                            onChange={(e) => handleSearch(e.target.value)}
                            className="pl-10 w-[250px]"
                        />
                    </div>
                </div>
            </CardHeader>
            <CardContent>
                {filteredTestingFeedbacks.length === 0 ? (
                    <div className="text-center py-8">
                        <MessageSquare className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                        <h3 className="text-lg font-semibold text-muted-foreground mb-2">No feedbacks found</h3>
                        <p className="text-sm text-muted-foreground">
                            {filters.search ? 'Try adjusting your search criteria.' : 'No testing feedbacks have been submitted yet.'}
                        </p>
                    </div>
                ) : (
                    <>
                        <div className="rounded-md border">
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead className="w-[200px]">
                                            <Button
                                                variant="ghost"
                                                onClick={() => handleSort('sessionTitle')}
                                                className="h-auto p-0 font-medium hover:bg-transparent"
                                            >
                                                Session
                                                {getSortIcon('sessionTitle')}
                                            </Button>
                                        </TableHead>
                                        <TableHead>
                                            <Button
                                                variant="ghost"
                                                onClick={() => handleSort('content')}
                                                className="h-auto p-0 font-medium hover:bg-transparent"
                                            >
                                                Feedback
                                                {getSortIcon('content')}
                                            </Button>
                                        </TableHead>
                                        <TableHead className="w-[120px]">Rating</TableHead>
                                        <TableHead className="w-[150px]">
                                            <Button
                                                variant="ghost"
                                                onClick={() => handleSort('submittedBy')}
                                                className="h-auto p-0 font-medium hover:bg-transparent"
                                            >
                                                Submitted By
                                                {getSortIcon('submittedBy')}
                                            </Button>
                                        </TableHead>
                                        <TableHead className="w-[120px]">
                                            <Button
                                                variant="ghost"
                                                onClick={() => handleSort('submittedAt')}
                                                className="h-auto p-0 font-medium hover:bg-transparent"
                                            >
                                                Date
                                                {getSortIcon('submittedAt')}
                                            </Button>
                                        </TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {paginatedTestingFeedbacks.map((feedback) => (
                                        <TableRow key={feedback.id}>
                                            <TableCell className="font-medium">
                                                {feedback.sessionTitle}
                                            </TableCell>
                                            <TableCell>
                                                <div className="max-w-[300px] truncate">
                                                    {feedback.content}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="flex items-center space-x-1">
                                                    {getRatingStars(feedback.rating)}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant="secondary">
                                                    {feedback.submittedBy.name}
                                                </Badge>
                                            </TableCell>
                                            <TableCell className="text-sm text-muted-foreground">
                                                {new Date(feedback.submittedAt).toLocaleDateString()}
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
                                    Showing {Math.min((currentPage - 1) * itemsPerPage + 1, sortedTestingFeedbacks.length)} to{' '}
                                    {Math.min(currentPage * itemsPerPage, sortedTestingFeedbacks.length)} of{' '}
                                    {sortedTestingFeedbacks.length} feedbacks
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
