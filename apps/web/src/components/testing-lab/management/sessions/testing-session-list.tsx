'use client';

import { useEffect, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Clock, MapPin, Users, Edit, Eye, Play } from 'lucide-react';
import Link from 'next/link';
import type { TestingSession } from '@/lib/api/generated/types.gen';
import { SessionFilterControls } from './session-filter-controls';

interface TestingSessionListProps {
  data: {
    testingSessions: TestingSession[];
    total: number;
  };
}

export function TestingSessionList({ data }: TestingSessionListProps) {
  const [sessions, setSessions] = useState<TestingSession[]>(data.testingSessions);
  const [filteredSessions, setFilteredSessions] = useState<TestingSession[]>(data.testingSessions);
  const [loading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string[]>([]);
  const [viewMode, setViewMode] = useState<'grid' | 'list' | 'table'>('grid');

  // Update sessions when data prop changes
  useEffect(() => {
    setSessions(data.testingSessions);
    setFilteredSessions(data.testingSessions);
  }, [data]);

  // Filter sessions based on search and status
  useEffect(() => {
    let filtered = sessions;

    if (searchTerm) {
      filtered = filtered.filter(
        (session) =>
          session.sessionName?.toLowerCase().includes(searchTerm.toLowerCase()) || session.location?.name?.toLowerCase().includes(searchTerm.toLowerCase()) || session.testingRequest?.title?.toLowerCase().includes(searchTerm.toLowerCase()),
      );
    }

    if (statusFilter.length > 0) {
      filtered = filtered.filter((session) => {
        const statusString = getStatusString(session.status);
        return statusFilter.includes(statusString);
      });
    }

    setFilteredSessions(filtered);
  }, [sessions, searchTerm, statusFilter]);

  const getStatusString = (status: number) => {
    switch (status) {
      case 0:
        return 'scheduled';
      case 1:
        return 'active';
      case 2:
        return 'completed';
      case 3:
        return 'cancelled';
      default:
        return 'scheduled';
    }
  };

  const getStatusColor = (status: number) => {
    switch (status) {
      case 0: // Scheduled
        return 'bg-blue-900/20 text-blue-400 border-blue-700';
      case 1: // Active
        return 'bg-green-900/20 text-green-400 border-green-700';
      case 2: // Completed
        return 'bg-gray-900/20 text-gray-400 border-gray-700';
      case 3: // Cancelled
        return 'bg-red-900/20 text-red-400 border-red-700';
      default:
        return 'bg-slate-900/20 text-slate-400 border-slate-700';
    }
  };

  const getStatusLabel = (status: number) => {
    switch (status) {
      case 0:
        return 'Scheduled';
      case 1:
        return 'Active';
      case 2:
        return 'Completed';
      case 3:
        return 'Cancelled';
      default:
        return 'Unknown';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const formatTime = (timeString: string) => {
    return new Date(timeString).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-400 mx-auto mb-4"></div>
          <p className="text-slate-400">Loading testing sessions...</p>
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
            <h1 className="text-3xl font-bold">Testing Sessions</h1>
            <p className="text-muted-foreground mt-2">Manage and join testing sessions</p>
          </div>
          <Button asChild>
            <Link href="/dashboard/testing-lab/sessions/create">Create New Session</Link>
          </Button>
        </div>

        {/* Filter Controls */}
        <SessionFilterControls
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          statusFilter={statusFilter}
          onStatusFilterChange={setStatusFilter}
          viewMode={viewMode}
          onViewModeChange={setViewMode}
          onRefresh={() => window.location.reload()}
        />

        {/* Sessions Grid */}
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {filteredSessions.map((session) => (
            <Card key={session.id} className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm hover:border-slate-600 transition-colors">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <CardTitle className="text-lg text-white line-clamp-2">{session.sessionName}</CardTitle>
                    <CardDescription className="text-slate-400 mt-1">{session.testingRequest?.title && `Testing: ${session.testingRequest.title}`}</CardDescription>
                  </div>
                  <Badge variant="outline" className={getStatusColor(session.status)}>
                    {getStatusLabel(session.status)}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <div className="flex items-center gap-2 text-sm text-slate-400">
                    <Calendar className="h-4 w-4" />
                    <span>{formatDate(session.sessionDate)}</span>
                  </div>
                  <div className="flex items-center gap-2 text-sm text-slate-400">
                    <Clock className="h-4 w-4" />
                    <span>
                      {formatTime(session.startTime)} - {formatTime(session.endTime)}
                    </span>
                  </div>
                  {session.location && (
                    <div className="flex items-center gap-2 text-sm text-slate-400">
                      <MapPin className="h-4 w-4" />
                      <span>{session.location.name}</span>
                    </div>
                  )}
                  <div className="flex items-center gap-2 text-sm text-slate-400">
                    <Users className="h-4 w-4" />
                    <span>
                      {session.registeredTesterCount || 0} / {session.maxTesters || '∞'} testers
                    </span>
                  </div>
                </div>

                <div className="flex gap-2 pt-2">
                  {session.status === 0 && (
                    <Button size="sm" className="bg-blue-600 hover:bg-blue-700 text-white" asChild>
                      <Link href={`/dashboard/testing-lab/sessions/${session.id}/register`}>
                        <Play className="h-4 w-4 mr-1" />
                        Join Session
                      </Link>
                    </Button>
                  )}
                  <Button size="sm" variant="outline" className="border-slate-600 text-slate-300 hover:bg-slate-700" asChild>
                    <Link href={`/dashboard/testing-lab/sessions/${session.id}`}>
                      <Eye className="h-4 w-4 mr-1" />
                      View Details
                    </Link>
                  </Button>
                  {session.status === 0 && (
                    <Button size="sm" variant="outline" className="border-blue-600 text-blue-400 hover:bg-blue-600 hover:text-white" asChild>
                      <Link href={`/dashboard/testing-lab/sessions/${session.id}/edit`}>
                        <Edit className="h-4 w-4 mr-1" />
                        Edit
                      </Link>
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {filteredSessions.length === 0 && (
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardContent className="text-center py-12">
              <Calendar className="h-12 w-12 text-slate-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-white mb-2">No testing sessions found</h3>
              <p className="text-slate-400 mb-4">{searchTerm || statusFilter.length > 0 ? 'Try adjusting your search or filter criteria' : 'There are no testing sessions available at the moment'}</p>
              <Button asChild className="bg-blue-600 hover:bg-blue-700">
                <Link href="/dashboard/testing-lab/sessions/create">Create Your First Session</Link>
              </Button>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
