'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { toast } from 'sonner';
import { Calendar, Clock, Users, TestTube, Plus, Download, Search, PlayCircle } from 'lucide-react';
import Link from 'next/link';
import type { TestingRequest, TestingSession } from '@/lib/api/generated/types.gen';
// import { joinTestingRequest, searchTestingRequests } from '@/lib/testing-lab/testing-lab.actions';

interface TestingLabDashboardProps {
  initialRequests: TestingRequest[];
  initialSessions: TestingSession[];
}

interface UserRole {
  type: 'student' | 'professor' | 'admin';
  isStudent: boolean;
  isProfessor: boolean;
  isAdmin: boolean;
}

export function TestingLabDashboard({ initialRequests, initialSessions }: TestingLabDashboardProps) {
  const { data: session } = useSession();
  const [requests] = useState<TestingRequest[]>(initialRequests);
  const [sessions] = useState<TestingSession[]>(initialSessions);
  const [filteredRequests, setFilteredRequests] = useState<TestingRequest[]>(initialRequests);
  const [filteredSessions, setFilteredSessions] = useState<TestingSession[]>(initialSessions);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [loading, setLoading] = useState(false);
  const [showCreateRequestDialog, setShowCreateRequestDialog] = useState(false);
  const [showCreateSessionDialog, setShowCreateSessionDialog] = useState(false);
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
      const isAdmin = isProfessor; // For now, professors are also admins

      setUserRole({
        type: isAdmin ? 'admin' : isProfessor ? 'professor' : 'student',
        isStudent,
        isProfessor,
        isAdmin,
      });
    }
  }, [session]);

  // Filter requests and sessions based on search and status
  useEffect(() => {
    let filtered = requests;

    if (searchTerm) {
      filtered = filtered.filter((request) => (request.title && request.title.toLowerCase().includes(searchTerm.toLowerCase())) || (request.description && request.description.toLowerCase().includes(searchTerm.toLowerCase())));
    }

    if (statusFilter !== 'all') {
      filtered = filtered.filter((request) => request.status.toString() === statusFilter);
    }

    setFilteredRequests(filtered);
  }, [requests, searchTerm, statusFilter]);

  useEffect(() => {
    let filtered = sessions;

    if (searchTerm) {
      filtered = filtered.filter((session) => session.sessionName && session.sessionName.toLowerCase().includes(searchTerm.toLowerCase()));
    }

    setFilteredSessions(filtered);
  }, [sessions, searchTerm]);

  const handleSearch = async () => {
    if (!searchTerm.trim()) return;

    setLoading(true);
    try {
      // const searchResults = await searchTestingRequests(searchTerm);
      // setFilteredRequests(searchResults.testingRequests || []);
    } catch {
      toast.error('Failed to search testing requests');
    } finally {
      setLoading(false);
    }
  };

  const handleJoinRequest = async (_requestId: string) => {
    if (!session?.user?.id) return;

    setLoading(true);
    try {
      // await joinTestingRequest(requestId, session.user.id);
      toast.success('Successfully joined testing request');
      // Refresh data
      window.location.reload();
    } catch {
      toast.error('Failed to join testing request');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status: number) => {
    switch (status) {
      case 0:
        return <Badge variant="outline">Draft</Badge>;
      case 1:
        return <Badge className="bg-green-500">Open</Badge>;
      case 2:
        return <Badge className="bg-blue-500">In Progress</Badge>;
      case 3:
        return <Badge className="bg-gray-500">Completed</Badge>;
      case 4:
        return <Badge variant="destructive">Cancelled</Badge>;
      default:
        return <Badge variant="outline">Unknown</Badge>;
    }
  };

  const getSessionStatusBadge = (status: string | number) => {
    const statusStr = typeof status === 'number' ? status.toString() : status.toLowerCase();

    switch (statusStr) {
      case 'scheduled':
      case '0':
        return <Badge className="bg-blue-500">Scheduled</Badge>;
      case 'inprogress':
      case '1':
        return <Badge className="bg-yellow-500">In Progress</Badge>;
      case 'completed':
      case '2':
        return <Badge className="bg-green-500">Completed</Badge>;
      case 'cancelled':
      case '3':
        return <Badge variant="destructive">Cancelled</Badge>;
      default:
        return <Badge variant="outline">Unknown</Badge>;
    }
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Testing Lab Dashboard</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage game testing requests and sessions</p>
        </div>
        <div className="flex gap-2">
          {(userRole.isProfessor || userRole.isAdmin) && (
            <>
              <Dialog open={showCreateRequestDialog} onOpenChange={setShowCreateRequestDialog}>
                <DialogTrigger asChild>
                  <Button>
                    <Plus className="h-4 w-4 mr-2" />
                    New Request
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-2xl">
                  <DialogHeader>
                    <DialogTitle>Create Testing Request</DialogTitle>
                    <DialogDescription>Create a new testing request for your game project</DialogDescription>
                  </DialogHeader>
                  {/* Create request form will go here */}
                </DialogContent>
              </Dialog>
              <Dialog open={showCreateSessionDialog} onOpenChange={setShowCreateSessionDialog}>
                <DialogTrigger asChild>
                  <Button variant="outline">
                    <Calendar className="h-4 w-4 mr-2" />
                    New Session
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-2xl">
                  <DialogHeader>
                    <DialogTitle>Create Testing Session</DialogTitle>
                    <DialogDescription>Schedule a new testing session</DialogDescription>
                  </DialogHeader>
                  {/* Create session form will go here */}
                </DialogContent>
              </Dialog>
            </>
          )}
        </div>
      </div>

      {/* Search and Filters */}
      <Card>
        <CardContent className="p-4">
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1">
              <div className="flex gap-2">
                <Input placeholder="Search testing requests..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} onKeyPress={(e) => e.key === 'Enter' && handleSearch()} />
                <Button onClick={handleSearch} disabled={loading}>
                  <Search className="h-4 w-4" />
                </Button>
              </div>
            </div>
            <div className="flex gap-2">
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Status</SelectItem>
                  <SelectItem value="0">Draft</SelectItem>
                  <SelectItem value="1">Open</SelectItem>
                  <SelectItem value="2">In Progress</SelectItem>
                  <SelectItem value="3">Completed</SelectItem>
                  <SelectItem value="4">Cancelled</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Main Content */}
      <Tabs defaultValue="requests" className="space-y-4">
        <TabsList>
          <TabsTrigger value="requests">Testing Requests</TabsTrigger>
          <TabsTrigger value="sessions">Testing Sessions</TabsTrigger>
          {userRole.isProfessor && <TabsTrigger value="analytics">Analytics</TabsTrigger>}
        </TabsList>

        {/* Testing Requests Tab */}
        <TabsContent value="requests" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {filteredRequests.map((request) => (
              <Card key={request.id} className="hover:shadow-lg transition-shadow">
                <CardHeader className="pb-3">
                  <div className="flex justify-between items-start">
                    <CardTitle className="text-lg">{request.title || 'Untitled Request'}</CardTitle>
                    {getStatusBadge(request.status)}
                  </div>
                  <CardDescription className="line-clamp-2">{request.description || 'No description available'}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  {request.projectVersion && request.projectVersion.project && (
                    <div className="text-sm text-gray-600 dark:text-gray-400">
                      <strong>Project:</strong> {request.projectVersion.project.title} v{request.projectVersion.versionNumber}
                    </div>
                  )}
                  <div className="flex items-center gap-4 text-sm text-gray-600 dark:text-gray-400">
                    <div className="flex items-center gap-1">
                      <Users className="h-4 w-4" />
                      {request.currentTesterCount || 0}/{request.maxTesters || 'Unlimited'}
                    </div>
                    <div className="flex items-center gap-1">
                      <Calendar className="h-4 w-4" />
                      {new Date(request.endDate).toLocaleDateString()}
                    </div>
                  </div>
                  <div className="flex gap-2">
                    {userRole.isStudent && request.status === 1 && request.id && (
                      <Button size="sm" onClick={() => handleJoinRequest(request.id!)} disabled={loading}>
                        <TestTube className="h-4 w-4 mr-1" />
                        Join
                      </Button>
                    )}
                    {request.downloadUrl && (
                      <Button size="sm" variant="outline" asChild>
                        <Link href={request.downloadUrl} target="_blank">
                          <Download className="h-4 w-4 mr-1" />
                          Download
                        </Link>
                      </Button>
                    )}
                    {request.id && (
                      <Button size="sm" variant="outline" asChild>
                        <Link href={`/dashboard/testing-lab/requests/${request.id}`}>View Details</Link>
                      </Button>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Testing Sessions Tab */}
        <TabsContent value="sessions" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {filteredSessions.map((session) => (
              <Card key={session.id} className="hover:shadow-lg transition-shadow">
                <CardHeader className="pb-3">
                  <div className="flex justify-between items-start">
                    <CardTitle className="text-lg">{session.sessionName}</CardTitle>
                    {getSessionStatusBadge(session.status)}
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2 text-sm text-gray-600 dark:text-gray-400">
                    <div className="flex items-center gap-2">
                      <Calendar className="h-4 w-4" />
                      {new Date(session.sessionDate).toLocaleDateString()}
                    </div>
                    <div className="flex items-center gap-2">
                      <Clock className="h-4 w-4" />
                      {session.startTime} - {session.endTime}
                    </div>
                    {session.location && (
                      <div className="flex items-center gap-2">
                        <div className="h-4 w-4 rounded-full bg-blue-500"></div>
                        {session.location?.name || 'Location TBD'}
                      </div>
                    )}
                    <div className="flex items-center gap-2">
                      <Users className="h-4 w-4" />
                      {session.registeredTesterCount || 0}/{session.maxTesters} registered
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline" asChild>
                      <Link href={`/dashboard/testing-lab/sessions/${session.id}`}>View Details</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Analytics Tab */}
        {userRole.isProfessor && (
          <TabsContent value="analytics" className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Total Requests</CardTitle>
                  <TestTube className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{requests.length}</div>
                </CardContent>
              </Card>
              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Active Requests</CardTitle>
                  <PlayCircle className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{requests.filter((r) => r.status === 1 || r.status === 2).length}</div>
                </CardContent>
              </Card>
              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Total Sessions</CardTitle>
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{sessions.length}</div>
                </CardContent>
              </Card>
              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Upcoming Sessions</CardTitle>
                  <Clock className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{sessions.filter((s) => new Date(s.sessionDate) > new Date()).length}</div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}
