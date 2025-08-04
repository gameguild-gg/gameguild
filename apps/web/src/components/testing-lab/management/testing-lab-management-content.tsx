'use client';

import { useState } from 'react';
import { Plus, RefreshCw, Search, Eye, Edit, Trash2, Calendar, Users } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { toast } from 'sonner';
import { getTestingRequestsData, createTestingRequest, updateTestingRequest, deleteTestingRequest, getTestingSessionsData, createTestingSession, updateTestingSession, deleteTestingSession } from '@/lib/testing-lab/testing-lab.actions';
import type { TestingRequest, TestingSession } from '@/lib/api/generated/types.gen';

interface TestingLabManagementContentProps {
  initialTestingRequests: TestingRequest[];
  initialTestingSessions: TestingSession[];
}

export function TestingLabManagementContent({ initialTestingRequests, initialTestingSessions }: TestingLabManagementContentProps) {
  const [testingRequests, setTestingRequests] = useState<TestingRequest[]>(initialTestingRequests);
  const [testingSessions, setTestingSessions] = useState<TestingSession[]>(initialTestingSessions);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'requests' | 'sessions'>('requests');
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [isCreateRequestDialogOpen, setIsCreateRequestDialogOpen] = useState(false);
  const [isCreateSessionDialogOpen, setIsCreateSessionDialogOpen] = useState(false);
  const [isEditRequestDialogOpen, setIsEditRequestDialogOpen] = useState(false);
  const [isEditSessionDialogOpen, setIsEditSessionDialogOpen] = useState(false);
  const [selectedRequest, setSelectedRequest] = useState<TestingRequest | null>(null);
  const [selectedSession, setSelectedSession] = useState<TestingSession | null>(null);

  // Form state for testing requests
  const [requestFormData, setRequestFormData] = useState({
    title: '',
    description: '',
    projectVersionId: '',
    downloadUrl: '',
    instructionsType: 'inline' as 'inline' | 'file' | 'url',
    instructionsContent: '',
    instructionsUrl: '',
    feedbackFormContent: '',
    maxTesters: 10,
    startDate: '',
    endDate: '',
  });

  // Form state for testing sessions
  const [sessionFormData, setSessionFormData] = useState({
    testingRequestId: '',
    sessionName: '',
    sessionDate: '',
    startTime: '',
    endTime: '',
    maxTesters: 5,
  });

  const resetRequestForm = () => {
    setRequestFormData({
      title: '',
      description: '',
      projectVersionId: '',
      downloadUrl: '',
      instructionsType: 'inline',
      instructionsContent: '',
      instructionsUrl: '',
      feedbackFormContent: '',
      maxTesters: 10,
      startDate: '',
      endDate: '',
    });
  };

  const resetSessionForm = () => {
    setSessionFormData({
      testingRequestId: '',
      sessionName: '',
      sessionDate: '',
      startTime: '',
      endTime: '',
      maxTesters: 5,
    });
  };

  const refreshData = async () => {
    setLoading(true);
    try {
      const [requestsResult, sessionsResult] = await Promise.all([getTestingRequestsData({ take: 100 }), getTestingSessionsData({ take: 100 })]);
      setTestingRequests(requestsResult.testingRequests);
      setTestingSessions(sessionsResult.testingSessions);
    } catch (error) {
      toast.error('Failed to refresh data');
      console.error('Error refreshing data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateRequest = async () => {
    try {
      setLoading(true);
      await createTestingRequest(requestFormData);
      toast.success('Testing request created successfully');
      setIsCreateRequestDialogOpen(false);
      resetRequestForm();
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to create testing request');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSession = async () => {
    try {
      setLoading(true);
      await createTestingSession(sessionFormData);
      toast.success('Testing session created successfully');
      setIsCreateSessionDialogOpen(false);
      resetSessionForm();
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to create testing session');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteRequest = async (request: TestingRequest) => {
    if (!confirm(`Are you sure you want to delete "${request.title}"? This action cannot be undone.`)) {
      return;
    }

    try {
      setLoading(true);
      await deleteTestingRequest(request.id!);
      toast.success('Testing request deleted successfully');
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to delete testing request');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteSession = async (session: TestingSession) => {
    if (!confirm(`Are you sure you want to delete "${session.sessionName}"? This action cannot be undone.`)) {
      return;
    }

    try {
      setLoading(true);
      await deleteTestingSession(session.id!);
      toast.success('Testing session deleted successfully');
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to delete testing session');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status: any) => {
    const statusMap: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
      '0': { label: 'Draft', variant: 'outline' },
      '1': { label: 'Open', variant: 'default' },
      '2': { label: 'In Progress', variant: 'secondary' },
      '3': { label: 'Completed', variant: 'default' },
      '4': { label: 'Cancelled', variant: 'destructive' },
    };

    const statusInfo = statusMap[status?.toString()] || { label: 'Unknown', variant: 'outline' as const };
    return <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>;
  };

  const filteredRequests = testingRequests.filter((request) => {
    const matchesSearch = !searchTerm || request.title?.toLowerCase().includes(searchTerm.toLowerCase()) || request.description?.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus = statusFilter === 'all' || request.status?.toString() === statusFilter;

    return matchesSearch && matchesStatus;
  });

  const filteredSessions = testingSessions.filter((session) => {
    const matchesSearch = !searchTerm || session.sessionName?.toLowerCase().includes(searchTerm.toLowerCase()) || session.testingRequest?.title?.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus = statusFilter === 'all' || session.status?.toString() === statusFilter;

    return matchesSearch && matchesStatus;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-green-100 rounded-lg">
            <Users className="h-6 w-6 text-green-600" />
          </div>
          <div>
            <h2 className="text-xl font-semibold">Testing Lab ({activeTab === 'requests' ? testingRequests.length : testingSessions.length})</h2>
            <p className="text-sm text-gray-600">Manage testing requests and sessions</p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" size="sm" onClick={refreshData} disabled={loading}>
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          {activeTab === 'requests' ? (
            <Dialog open={isCreateRequestDialogOpen} onOpenChange={setIsCreateRequestDialogOpen}>
              <DialogTrigger asChild>
                <Button size="sm">
                  <Plus className="h-4 w-4 mr-2" />
                  Add Request
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-2xl">
                <DialogHeader>
                  <DialogTitle>Create Testing Request</DialogTitle>
                  <DialogDescription>Create a new testing request for project feedback</DialogDescription>
                </DialogHeader>

                <div className="grid gap-4 py-4">
                  <div className="grid gap-2">
                    <Label htmlFor="title">Title *</Label>
                    <Input id="title" value={requestFormData.title} onChange={(e) => setRequestFormData((prev) => ({ ...prev, title: e.target.value }))} placeholder="Testing request title" />
                  </div>

                  <div className="grid gap-2">
                    <Label htmlFor="description">Description</Label>
                    <Textarea id="description" value={requestFormData.description} onChange={(e) => setRequestFormData((prev) => ({ ...prev, description: e.target.value }))} placeholder="Describe what needs to be tested" rows={3} />
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="grid gap-2">
                      <Label htmlFor="projectVersionId">Project Version ID *</Label>
                      <Input id="projectVersionId" value={requestFormData.projectVersionId} onChange={(e) => setRequestFormData((prev) => ({ ...prev, projectVersionId: e.target.value }))} placeholder="Project version identifier" />
                    </div>

                    <div className="grid gap-2">
                      <Label htmlFor="maxTesters">Max Testers</Label>
                      <Input id="maxTesters" type="number" value={requestFormData.maxTesters} onChange={(e) => setRequestFormData((prev) => ({ ...prev, maxTesters: parseInt(e.target.value) || 10 }))} />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="grid gap-2">
                      <Label htmlFor="startDate">Start Date *</Label>
                      <Input id="startDate" type="datetime-local" value={requestFormData.startDate} onChange={(e) => setRequestFormData((prev) => ({ ...prev, startDate: e.target.value }))} />
                    </div>

                    <div className="grid gap-2">
                      <Label htmlFor="endDate">End Date *</Label>
                      <Input id="endDate" type="datetime-local" value={requestFormData.endDate} onChange={(e) => setRequestFormData((prev) => ({ ...prev, endDate: e.target.value }))} />
                    </div>
                  </div>
                </div>

                <div className="flex justify-end space-x-2">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setIsCreateRequestDialogOpen(false);
                      resetRequestForm();
                    }}
                  >
                    Cancel
                  </Button>
                  <Button onClick={handleCreateRequest} disabled={loading || !requestFormData.title || !requestFormData.projectVersionId}>
                    {loading ? 'Creating...' : 'Create Request'}
                  </Button>
                </div>
              </DialogContent>
            </Dialog>
          ) : (
            <Dialog open={isCreateSessionDialogOpen} onOpenChange={setIsCreateSessionDialogOpen}>
              <DialogTrigger asChild>
                <Button size="sm">
                  <Plus className="h-4 w-4 mr-2" />
                  Add Session
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-2xl">
                <DialogHeader>
                  <DialogTitle>Create Testing Session</DialogTitle>
                  <DialogDescription>Schedule a new testing session</DialogDescription>
                </DialogHeader>

                <div className="grid gap-4 py-4">
                  <div className="grid gap-2">
                    <Label htmlFor="sessionName">Session Name *</Label>
                    <Input id="sessionName" value={sessionFormData.sessionName} onChange={(e) => setSessionFormData((prev) => ({ ...prev, sessionName: e.target.value }))} placeholder="Testing session name" />
                  </div>

                  <div className="grid gap-2">
                    <Label htmlFor="testingRequestId">Testing Request ID *</Label>
                    <Input id="testingRequestId" value={sessionFormData.testingRequestId} onChange={(e) => setSessionFormData((prev) => ({ ...prev, testingRequestId: e.target.value }))} placeholder="Associated testing request ID" />
                  </div>

                  <div className="grid grid-cols-3 gap-4">
                    <div className="grid gap-2">
                      <Label htmlFor="sessionDate">Session Date *</Label>
                      <Input id="sessionDate" type="date" value={sessionFormData.sessionDate} onChange={(e) => setSessionFormData((prev) => ({ ...prev, sessionDate: e.target.value }))} />
                    </div>

                    <div className="grid gap-2">
                      <Label htmlFor="startTime">Start Time *</Label>
                      <Input id="startTime" type="time" value={sessionFormData.startTime} onChange={(e) => setSessionFormData((prev) => ({ ...prev, startTime: e.target.value }))} />
                    </div>

                    <div className="grid gap-2">
                      <Label htmlFor="endTime">End Time *</Label>
                      <Input id="endTime" type="time" value={sessionFormData.endTime} onChange={(e) => setSessionFormData((prev) => ({ ...prev, endTime: e.target.value }))} />
                    </div>
                  </div>

                  <div className="grid gap-2">
                    <Label htmlFor="sessionMaxTesters">Max Testers</Label>
                    <Input id="sessionMaxTesters" type="number" value={sessionFormData.maxTesters} onChange={(e) => setSessionFormData((prev) => ({ ...prev, maxTesters: parseInt(e.target.value) || 5 }))} />
                  </div>
                </div>

                <div className="flex justify-end space-x-2">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setIsCreateSessionDialogOpen(false);
                      resetSessionForm();
                    }}
                  >
                    Cancel
                  </Button>
                  <Button onClick={handleCreateSession} disabled={loading || !sessionFormData.sessionName || !sessionFormData.testingRequestId || !sessionFormData.sessionDate}>
                    {loading ? 'Creating...' : 'Create Session'}
                  </Button>
                </div>
              </DialogContent>
            </Dialog>
          )}
        </div>
      </div>

      {/* Tabs */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex space-x-4 mb-4">
            <Button variant={activeTab === 'requests' ? 'default' : 'outline'} onClick={() => setActiveTab('requests')}>
              Testing Requests ({testingRequests.length})
            </Button>
            <Button variant={activeTab === 'sessions' ? 'default' : 'outline'} onClick={() => setActiveTab('sessions')}>
              Testing Sessions ({testingSessions.length})
            </Button>
          </div>

          {/* Filters */}
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input placeholder={`Search ${activeTab}...`} value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-10" />
            </div>

            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Filter by status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value="0">Draft</SelectItem>
                <SelectItem value="1">Open</SelectItem>
                <SelectItem value="2">In Progress</SelectItem>
                <SelectItem value="3">Completed</SelectItem>
                <SelectItem value="4">Cancelled</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Content */}
      <Card>
        <CardHeader>
          <CardTitle>{activeTab === 'requests' ? 'Testing Requests' : 'Testing Sessions'}</CardTitle>
          <CardDescription>{activeTab === 'requests' ? filteredRequests.length : filteredSessions.length} items found</CardDescription>
        </CardHeader>
        <CardContent>
          {activeTab === 'requests' ? (
            filteredRequests.length === 0 ? (
              <div className="text-center py-8">
                <Users className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No testing requests found</h3>
                <p className="text-gray-600 mb-4">Create your first testing request to get started.</p>
                <Button onClick={() => setIsCreateRequestDialogOpen(true)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Create Request
                </Button>
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Title</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Testers</TableHead>
                    <TableHead>Start Date</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredRequests.map((request) => (
                    <TableRow key={request.id}>
                      <TableCell>
                        <div>
                          <div className="font-medium">{request.title}</div>
                          {request.description && <div className="text-sm text-gray-600 truncate">{request.description}</div>}
                        </div>
                      </TableCell>
                      <TableCell>{getStatusBadge(request.status)}</TableCell>
                      <TableCell>
                        {request.currentTesterCount || 0} / {request.maxTesters || 'N/A'}
                      </TableCell>
                      <TableCell>{request.startDate ? new Date(request.startDate).toLocaleDateString() : 'Not set'}</TableCell>
                      <TableCell>
                        <div className="flex items-center space-x-2">
                          <Button variant="ghost" size="sm">
                            <Eye className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm">
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => handleDeleteRequest(request)}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )
          ) : filteredSessions.length === 0 ? (
            <div className="text-center py-8">
              <Calendar className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No testing sessions found</h3>
              <p className="text-gray-600 mb-4">Schedule your first testing session.</p>
              <Button onClick={() => setIsCreateSessionDialogOpen(true)}>
                <Plus className="h-4 w-4 mr-2" />
                Create Session
              </Button>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Session Name</TableHead>
                  <TableHead>Testing Request</TableHead>
                  <TableHead>Date & Time</TableHead>
                  <TableHead>Testers</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredSessions.map((session) => (
                  <TableRow key={session.id}>
                    <TableCell>
                      <div className="font-medium">{session.sessionName}</div>
                    </TableCell>
                    <TableCell>
                      <div className="text-sm">{session.testingRequest?.title || session.testingRequestId || 'Unknown'}</div>
                    </TableCell>
                    <TableCell>
                      <div className="text-sm">
                        {session.sessionDate ? new Date(session.sessionDate).toLocaleDateString() : 'Not set'}
                        <br />
                        {session.startTime} - {session.endTime}
                      </div>
                    </TableCell>
                    <TableCell>
                      {session.registeredTesterCount || 0} / {session.maxTesters}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        <Button variant="ghost" size="sm">
                          <Eye className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="sm">
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => handleDeleteSession(session)}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
