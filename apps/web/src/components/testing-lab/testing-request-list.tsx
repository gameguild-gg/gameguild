'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Download, FileText, MessageSquare, Play, Users } from 'lucide-react';
import Link from 'next/link';
import type { TestingRequest } from '@/lib/api/generated/types.gen';
import { RequestFilterControls } from './request-filter-controls';

interface UserRole {
  type: 'student' | 'professor' | 'admin';
  isStudent: boolean;
  isProfessor: boolean;
  isAdmin: boolean;
}

interface TestingRequestListProps {
  data: {
    testingRequests: TestingRequest[];
    total: number;
  };
}

export function TestingRequestList({ data }: TestingRequestListProps) {
  const { data: session } = useSession();
  const [requests, setRequests] = useState<TestingRequest[]>(data.testingRequests);
  const [filteredRequests, setFilteredRequests] = useState<TestingRequest[]>(data.testingRequests);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string[]>([]);
  const [viewMode, setViewMode] = useState<'grid' | 'list' | 'table'>('grid');
  const [userRole, setUserRole] = useState<UserRole>({
    type: 'student',
    isStudent: true,
    isProfessor: false,
    isAdmin: false,
  });

  // Determine user role based on email domain
  useEffect(() => {
    if (session?.user?.email) {
      const email = session.user.email.toLowerCase();
      const isStudent = email.endsWith('@mymail.champlain.edu');
      const isProfessor = email.endsWith('@champlain.edu') && !email.endsWith('@mymail.champlain.edu');
      const isAdmin = isProfessor;

      setUserRole({
        type: isAdmin ? 'admin' : isProfessor ? 'professor' : 'student',
        isStudent,
        isProfessor,
        isAdmin,
      });
    }
  }, [session]);

  // Update requests when data prop changes
  useEffect(() => {
    setRequests(data.testingRequests);
    setFilteredRequests(data.testingRequests);
  }, [data]);

  // Filter requests based on search and status
  useEffect(() => {
    let filtered = requests;

    if (searchTerm) {
      filtered = filtered.filter(
        (request) =>
          request.title?.toLowerCase().includes(searchTerm.toLowerCase()) ||
          request.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
          request.createdByUserId?.toLowerCase().includes(searchTerm.toLowerCase()),
      );
    }

    if (statusFilter.length > 0) {
      filtered = filtered.filter((request) => statusFilter.includes(request.status));
    }

    setFilteredRequests(filtered);
  }, [requests, searchTerm, statusFilter]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'open':
        return 'bg-green-900/20 text-green-400 border-green-700';
      case 'inProgress':
        return 'bg-blue-900/20 text-blue-400 border-blue-700';
      case 'completed':
        return 'bg-gray-900/20 text-gray-400 border-gray-700';
      case 'cancelled':
        return 'bg-red-900/20 text-red-400 border-red-700';
      default:
        return 'bg-slate-900/20 text-slate-400 border-slate-700';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-400 mx-auto mb-4"></div>
          <p className="text-slate-400">Loading testing requests...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-8">
        {/* Header */}
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold">{userRole.isStudent ? 'Available Testing Requests' : 'My Testing Requests'}</h1>
            <p className="text-muted-foreground mt-2">
              {userRole.isStudent ? 'Find games to test and provide valuable feedback' : 'Manage your submitted testing requests'}
            </p>
          </div>
          {!userRole.isStudent && (
            <Button asChild>
              <Link href="/dashboard/testing-lab/submit">Submit New Version</Link>
            </Button>
          )}
        </div>

        {/* Filter Controls */}
        <RequestFilterControls
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          statusFilter={statusFilter}
          onStatusFilterChange={setStatusFilter}
          viewMode={viewMode}
          onViewModeChange={setViewMode}
          onRefresh={() => window.location.reload()}
        />

        {/* Requests Grid */}
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {filteredRequests.map((request) => (
            <Card
              key={request.id}
              className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm hover:border-slate-600 transition-colors"
            >
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <CardTitle className="text-lg text-white line-clamp-2">{request.title}</CardTitle>
                    <CardDescription className="text-slate-400 mt-1">by {request.createdBy.name}</CardDescription>
                  </div>
                  <Badge variant="outline" className={getStatusColor(request.status)}>
                    {request.status}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {request.description && <p className="text-sm text-slate-300 line-clamp-3">{request.description}</p>}

                <div className="space-y-2">
                  <div className="flex items-center gap-2 text-sm text-slate-400">
                    <Calendar className="h-4 w-4" />
                    <span>
                      {formatDate(request.startDate)} - {formatDate(request.endDate)}
                    </span>
                  </div>
                  <div className="flex items-center gap-2 text-sm text-slate-400">
                    <Users className="h-4 w-4" />
                    <span>
                      {request.currentTesterCount} / {request.maxTesters || '∞'} testers
                    </span>
                  </div>
                  {request.projectVersion && (
                    <div className="flex items-center gap-2 text-sm text-slate-400">
                      <FileText className="h-4 w-4" />
                      <span>Version {request.projectVersion.versionNumber}</span>
                    </div>
                  )}
                </div>

                <div className="flex gap-2 pt-2">
                  {userRole.isStudent && request.status === 'open' && (
                    <>
                      {request.downloadUrl && (
                        <Button size="sm" className="bg-green-600 hover:bg-green-700 text-white" onClick={() => window.open(request.downloadUrl, '_blank')}>
                          <Download className="h-4 w-4 mr-1" />
                          Download
                        </Button>
                      )}
                      <Button size="sm" variant="outline" className="border-blue-600 text-blue-400 hover:bg-blue-600 hover:text-white" asChild>
                        <Link href={`/dashboard/testing-lab/requests/${request.id}`}>
                          <Play className="h-4 w-4 mr-1" />
                          Start Testing
                        </Link>
                      </Button>
                    </>
                  )}
                  {!userRole.isStudent && (
                    <Button size="sm" variant="outline" className="border-slate-600 text-slate-300 hover:bg-slate-700" asChild>
                      <Link href={`/dashboard/testing-lab/requests/${request.id}/feedback`}>
                        <MessageSquare className="h-4 w-4 mr-1" />
                        View Feedback
                      </Link>
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {filteredRequests.length === 0 && (
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardContent className="text-center py-12">
              <FileText className="h-12 w-12 text-slate-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-white mb-2">No testing requests found</h3>
              <p className="text-slate-400 mb-4">
                {searchTerm || statusFilter.length > 0
                  ? 'Try adjusting your search or filter criteria'
                  : userRole.isStudent
                    ? 'There are no testing requests available at the moment'
                    : 'You have not submitted any testing requests yet'}
              </p>
              {!userRole.isStudent && (
                <Button asChild className="bg-blue-600 hover:bg-blue-700">
                  <Link href="/dashboard/testing-lab/submit">Submit Your First Version</Link>
                </Button>
              )}
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
