'use client';

import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Calendar, Filter, Gamepad2, RefreshCw, Search, TrendingUp, Users } from 'lucide-react';
import { EnhancedTestingSessionsList } from './enhanced-testing-sessions-list';

interface TestingSession {
  id: string;
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  location?: {
    id: string;
    name: string;
    capacity: number;
  };
  maxTesters: number;
  registeredTesterCount: number;
  registeredProjectMemberCount: number;
  registeredProjectCount: number;
  status?: 'scheduled' | 'active' | 'completed' | 'cancelled';
  manager?: {
    id: string;
    name: string;
    email: string;
  };
  testingRequests?: {
    id: string;
    title: string;
    projectVersion: {
      versionNumber: string;
      project: {
        title: string;
      };
    };
  }[];
  description?: string;
  notes?: string;
  attendanceRate?: number;
  averageRating?: number;
  feedbackCount?: number;
}

interface TestingSessionsManagementProps {
  initialSessions?: TestingSession[];
}

export function TestingSessionsManagement({ initialSessions = [] }: TestingSessionsManagementProps) {
  const { data: session } = useSession();
  const [sessions, setSessions] = useState<TestingSession[]>(Array.isArray(initialSessions) ? initialSessions : []);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [viewMode, setViewMode] = useState<'grid' | 'list' | 'table'>('grid');
  const [loading, setLoading] = useState(false);

  // Filter sessions based on search and status
  const filteredSessions = sessions.filter((session) => {
    const matchesSearch =
      searchTerm === '' ||
      session.sessionName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      session.location?.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      session.manager?.name.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus = statusFilter === 'all' || session.status === statusFilter;

    return matchesSearch && matchesStatus;
  });

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const formatTime = (timeString: string) => {
    const [hours, minutes] = timeString.split(':');
    const date = new Date();
    date.setHours(parseInt(hours), parseInt(minutes));
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  const getStatusColor = (status: string | null | undefined) => {
    switch (status) {
      case 'scheduled':
        return 'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-500/30';
      case 'active':
        return 'bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-300 dark:border-green-500/30';
      case 'completed':
        return 'bg-gray-100 text-gray-800 border-gray-200 dark:bg-gray-900/30 dark:text-gray-300 dark:border-gray-500/30';
      case 'cancelled':
        return 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-300 dark:border-red-500/30';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200 dark:bg-gray-900/30 dark:text-gray-300 dark:border-gray-500/30';
    }
  };

  const refreshSessions = async () => {
    setLoading(true);
    // TODO: Implement refresh logic
    setLoading(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-400">Loading testing sessions...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card className="bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-900/20 dark:to-blue-800/10 border-blue-200 dark:border-blue-500/20">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-blue-600 dark:text-blue-300 text-sm font-medium">Total Sessions</p>
                <p className="text-3xl font-bold text-gray-900 dark:text-white">{sessions.length}</p>
              </div>
              <Calendar className="h-8 w-8 text-blue-500 dark:text-blue-400" />
            </div>
            <p className="text-blue-600/60 dark:text-blue-300/60 text-xs mt-2">Scheduled sessions</p>
          </CardContent>
        </Card>

        <Card className="bg-gradient-to-br from-green-50 to-green-100 dark:from-green-900/20 dark:to-green-800/10 border-green-200 dark:border-green-500/20">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-green-600 dark:text-green-300 text-sm font-medium">Active Testers</p>
                <p className="text-3xl font-bold text-gray-900 dark:text-white">{sessions.reduce((acc, s) => acc + s.registeredTesterCount, 0)}</p>
              </div>
              <Users className="h-8 w-8 text-green-500 dark:text-green-400" />
            </div>
            <p className="text-green-600/60 dark:text-green-300/60 text-xs mt-2">Registered participants</p>
          </CardContent>
        </Card>

        <Card className="bg-gradient-to-br from-purple-50 to-purple-100 dark:from-purple-900/20 dark:to-purple-800/10 border-purple-200 dark:border-purple-500/20">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-purple-600 dark:text-purple-300 text-sm font-medium">Projects</p>
                <p className="text-3xl font-bold text-gray-900 dark:text-white">{sessions.reduce((acc, s) => acc + (s.registeredProjectCount || 0), 0)}</p>
              </div>
              <Gamepad2 className="h-8 w-8 text-purple-500 dark:text-purple-400" />
            </div>
            <p className="text-purple-600/60 dark:text-purple-300/60 text-xs mt-2">Games being tested</p>
          </CardContent>
        </Card>

        <Card className="bg-gradient-to-br from-orange-50 to-orange-100 dark:from-orange-900/20 dark:to-orange-800/10 border-orange-200 dark:border-orange-500/20">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-orange-600 dark:text-orange-300 text-sm font-medium">Avg Attendance</p>
                <p className="text-3xl font-bold text-gray-900 dark:text-white">
                  {sessions.length > 0 ? Math.round(sessions.reduce((acc, s) => acc + (s.attendanceRate || 0), 0) / sessions.length) : 0}%
                </p>
              </div>
              <TrendingUp className="h-8 w-8 text-orange-500 dark:text-orange-400" />
            </div>
            <p className="text-orange-600/60 dark:text-orange-300/60 text-xs mt-2">Session participation</p>
          </CardContent>
        </Card>
      </div>

      {/* Header with action buttons */}
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
        <div className="flex items-center gap-2">
          <Calendar className="h-6 w-6 text-blue-600" />
          <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Testing Sessions ({filteredSessions.length})</h2>
            <p className="text-sm text-gray-600 dark:text-gray-400">Manage testing sessions and track participation</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={refreshSessions}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
        </div>
      </div>

      {/* Enhanced Filters and Search */}
      <Card className="bg-white dark:bg-slate-800/50 border-gray-200 dark:border-slate-600/70">
        <CardContent className="p-6">
          <div className="flex flex-col lg:flex-row gap-4 items-center justify-between">
            <div className="flex flex-col sm:flex-row gap-4 flex-1">
              <div className="relative flex-1 max-w-md">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 dark:text-slate-400 h-4 w-4" />
                <Input
                  placeholder="Search sessions, locations, or managers..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>

              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger className="w-full sm:w-48">
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue placeholder="Filter by status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Statuses</SelectItem>
                  <SelectItem value="scheduled">Scheduled</SelectItem>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="completed">Completed</SelectItem>
                  <SelectItem value="cancelled">Cancelled</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex gap-2">
              <Button variant={viewMode === 'grid' ? 'default' : 'outline'} size="sm" onClick={() => setViewMode('grid')}>
                Grid
              </Button>
              <Button variant={viewMode === 'list' ? 'default' : 'outline'} size="sm" onClick={() => setViewMode('list')}>
                List
              </Button>
              <Button variant={viewMode === 'table' ? 'default' : 'outline'} size="sm" onClick={() => setViewMode('table')}>
                Table
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Sessions Display */}
      {filteredSessions.length === 0 ? (
        <Card className="bg-white dark:bg-slate-800/50 border-gray-200 dark:border-slate-600/70">
          <CardContent className="p-12 text-center">
            <Calendar className="h-12 w-12 text-gray-400 dark:text-slate-500 mx-auto mb-4" />
            <h3 className="text-xl font-semibold text-gray-700 dark:text-slate-300 mb-2">No sessions found</h3>
            <p className="text-gray-500 dark:text-slate-500">
              {searchTerm || statusFilter !== 'all' ? 'Try adjusting your search or filters' : 'No testing sessions have been scheduled yet'}
            </p>
          </CardContent>
        </Card>
      ) : (
        <EnhancedTestingSessionsList initialSessions={filteredSessions} />
      )}
    </div>
  );
}
