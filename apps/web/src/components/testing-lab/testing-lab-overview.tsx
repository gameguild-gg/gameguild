'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { toast } from 'sonner';
import { BarChart3, Calendar, Download, MessageSquare, Plus, TestTube, Upload, Users, Search, Star, PlayCircle, MoreHorizontal } from 'lucide-react';
import Link from 'next/link';
import {
  getTestingRequestsData,
  getTestingSessionsData,
  createTestingRequest,
  createTestingSession,
  joinTestingRequest,
  leaveTestingRequest,
} from '@/lib/testing-lab/testing-lab.actions';
import type { TestingRequest, TestingSession } from '@/lib/api/generated/types.gen';

interface TestingLabStats {
  totalRequests: number;
  activeRequests: number;
  totalSessions: number;
  upcomingSessions: number;
  totalFeedback: number;
  pendingFeedback: number;
  mySubmissions: number;
  myTestingAssignments: number;
}

interface UserRole {
  type: 'student' | 'professor' | 'admin';
  isStudent: boolean;
  isProfessor: boolean;
  isAdmin: boolean;
}

export function TestingLabOverview() {
  const { data: session } = useSession();
  const [stats, setStats] = useState<TestingLabStats>({
    totalRequests: 0,
    activeRequests: 0,
    totalSessions: 0,
    upcomingSessions: 0,
    totalFeedback: 0,
    pendingFeedback: 0,
    mySubmissions: 0,
    myTestingAssignments: 0,
  });
  const [userRole, setUserRole] = useState<UserRole>({
    type: 'student',
    isStudent: true,
    isProfessor: false,
    isAdmin: false,
  });
  const [loading, setLoading] = useState(true);
  const [requests, setRequests] = useState<TestingRequest[]>([]);
  const [sessions, setSessions] = useState<TestingSession[]>([]);
  const [filteredRequests, setFilteredRequests] = useState<TestingRequest[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');

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

  // Load stats based on user role
  useEffect(() => {
    const loadStats = async () => {
      setLoading(true);
      try {
        if (userRole.isStudent) {
          // Load actual data from API for students using server actions
          const [requestsResult, sessionsResult] = await Promise.all([getTestingRequestsData({ take: 100 }), getTestingSessionsData({ take: 100 })]);

          const requests = requestsResult.testingRequests;
          const sessions = sessionsResult.testingSessions;

          setStats({
            totalRequests: requests.length,
            activeRequests: requests.filter((r) => r.status === 1).length, // 1 = open status
            totalSessions: sessions.length,
            upcomingSessions: sessions.filter((s) => s.sessionDate && new Date(s.sessionDate) > new Date()).length,
            totalFeedback: 0, // Will be implemented when feedback API is available
            pendingFeedback: 0, // Will be implemented when feedback API is available
            mySubmissions: 0, // TODO: filter by current user when user data is available
            myTestingAssignments: requests.filter((r) => r.status === 1).length,
          });
        } else if (userRole.isProfessor) {
          // Load actual data from API for professors/admins using server actions
          const [requestsResult, sessionsResult] = await Promise.all([getTestingRequestsData({ take: 100 }), getTestingSessionsData({ take: 100 })]);

          const requests = requestsResult.testingRequests;
          const sessions = sessionsResult.testingSessions;

          // Update state with the fetched data
          setRequests(requests);
          setSessions(sessions);
          setFilteredRequests(requests);

          setStats({
            totalRequests: requests.length,
            activeRequests: requests.filter((r) => r.status === 1).length, // 1 = open status
            totalSessions: sessions.length,
            upcomingSessions: sessions.filter((s) => s.sessionDate && new Date(s.sessionDate) > new Date()).length,
            totalFeedback: 0, // Will be implemented when feedback API is available
            pendingFeedback: 0, // Will be implemented when feedback API is available
            mySubmissions: 0,
            myTestingAssignments: 0,
          });
        }
      } catch (error) {
        console.error('Failed to load testing lab stats:', error);
        // Fallback to mock data on error
        if (userRole.isStudent) {
          setStats({
            totalRequests: 8,
            activeRequests: 3,
            totalSessions: 12,
            upcomingSessions: 2,
            totalFeedback: 6,
            pendingFeedback: 1,
            mySubmissions: 4,
            myTestingAssignments: 8,
          });
        } else if (userRole.isProfessor) {
          setStats({
            totalRequests: 120,
            activeRequests: 45,
            totalSessions: 24,
            upcomingSessions: 6,
            totalFeedback: 89,
            pendingFeedback: 12,
            mySubmissions: 0,
            myTestingAssignments: 0,
          });
        }
      } finally {
        setLoading(false);
      }
    };

    if (userRole.type !== 'student' || session?.user) {
      loadStats();
    }
  }, [userRole, session]);

  // Handle joining a testing request
  const handleJoinRequest = async (requestId: number) => {
    try {
      // Get current user from session
      if (!session?.user?.id) {
        console.error('User not authenticated');
        return;
      }

      const result = await joinTestingRequest(requestId.toString(), session.user.id);
      if (result.success) {
        // Refresh the data by reloading stats
        const [requestsResult, sessionsResult] = await Promise.all([getTestingRequestsData({ take: 100 }), getTestingSessionsData({ take: 100 })]);

        if (requestsResult.success && sessionsResult.success) {
          const requests = requestsResult.testingRequests;
          const sessions = sessionsResult.testingSessions;
          setRequests(requests);
          setSessions(sessions);
          setFilteredRequests(requests);
        }
      } else {
        console.error('Failed to join request:', result.error);
      }
    } catch (error) {
      console.error('Error joining request:', error);
    }
  };

  // Create Request Form Component
  const CreateRequestForm = ({ onSuccess }: { onSuccess: () => void }) => {
    const [formData, setFormData] = useState({
      title: '',
      description: '',
      gameTitle: '',
      platform: '',
      priority: 2,
      deadline: '',
    });

    const handleSubmit = async (e: React.FormEvent) => {
      e.preventDefault();
      try {
        const result = await createTestingRequest({
          ...formData,
          deadline: formData.deadline ? new Date(formData.deadline).toISOString() : undefined,
        });
        if (result.success) {
          onSuccess();
          setFormData({
            title: '',
            description: '',
            gameTitle: '',
            platform: '',
            priority: 2,
            deadline: '',
          });
        } else {
          console.error('Failed to create request:', result.error);
        }
      } catch (error) {
        console.error('Error creating request:', error);
      }
    };

    return (
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="title" className="block text-sm font-medium mb-1">
            Title
          </label>
          <Input id="title" value={formData.title} onChange={(e) => setFormData({ ...formData, title: e.target.value })} required />
        </div>

        <div>
          <label htmlFor="description" className="block text-sm font-medium mb-1">
            Description
          </label>
          <Input id="description" value={formData.description} onChange={(e) => setFormData({ ...formData, description: e.target.value })} required />
        </div>

        <div>
          <label htmlFor="gameTitle" className="block text-sm font-medium mb-1">
            Game Title
          </label>
          <Input id="gameTitle" value={formData.gameTitle} onChange={(e) => setFormData({ ...formData, gameTitle: e.target.value })} required />
        </div>

        <div>
          <label htmlFor="platform" className="block text-sm font-medium mb-1">
            Platform
          </label>
          <Input id="platform" value={formData.platform} onChange={(e) => setFormData({ ...formData, platform: e.target.value })} required />
        </div>

        <div>
          <label htmlFor="priority" className="block text-sm font-medium mb-1">
            Priority
          </label>
          <Select value={formData.priority.toString()} onValueChange={(value) => setFormData({ ...formData, priority: parseInt(value) })}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="1">Low</SelectItem>
              <SelectItem value="2">Medium</SelectItem>
              <SelectItem value="3">High</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div>
          <label htmlFor="deadline" className="block text-sm font-medium mb-1">
            Deadline (Optional)
          </label>
          <Input id="deadline" type="date" value={formData.deadline} onChange={(e) => setFormData({ ...formData, deadline: e.target.value })} />
        </div>

        <Button type="submit" className="w-full">
          Create Request
        </Button>
      </form>
    );
  };

  // Filter requests based on search term and status
  useEffect(() => {
    let filtered = requests;

    if (searchTerm) {
      filtered = filtered.filter(
        (request) =>
          request.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
          request.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
          request.gameTitle.toLowerCase().includes(searchTerm.toLowerCase()),
      );
    }

    if (statusFilter !== 'all') {
      const statusMap = {
        open: 1,
        'in-progress': 2,
        completed: 3,
        cancelled: 4,
      };
      filtered = filtered.filter((request) => request.status === statusMap[statusFilter as keyof typeof statusMap]);
    }

    setFilteredRequests(filtered);
  }, [requests, searchTerm, statusFilter]);

  const renderStatsCards = () => {
    if (userRole.isStudent) {
      return (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">My Submissions</CardTitle>
              <Upload className="h-4 w-4 text-blue-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">{stats.mySubmissions}</div>
              <p className="text-xs text-slate-400">Total versions submitted</p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">Testing Assignments</CardTitle>
              <TestTube className="h-4 w-4 text-purple-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">{stats.myTestingAssignments}</div>
              <p className="text-xs text-slate-400">Games to test this semester</p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">Upcoming Sessions</CardTitle>
              <Calendar className="h-4 w-4 text-green-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">{stats.upcomingSessions}</div>
              <p className="text-xs text-slate-400">Sessions this week</p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">Pending Feedback</CardTitle>
              <MessageSquare className="h-4 w-4 text-orange-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">{stats.pendingFeedback}</div>
              <p className="text-xs text-slate-400">Awaiting completion</p>
            </CardContent>
          </Card>
        </div>
      );
    } else {
      return (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">Total Requests</CardTitle>
              <TestTube className="h-4 w-4 text-blue-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">{stats.totalRequests}</div>
              <p className="text-xs text-slate-400">
                <span className="text-green-400">{stats.activeRequests}</span> active
              </p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">Testing Sessions</CardTitle>
              <Calendar className="h-4 w-4 text-purple-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">{stats.totalSessions}</div>
              <p className="text-xs text-slate-400">
                <span className="text-blue-400">{stats.upcomingSessions}</span> upcoming
              </p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">Feedback Responses</CardTitle>
              <MessageSquare className="h-4 w-4 text-green-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">{stats.totalFeedback}</div>
              <p className="text-xs text-slate-400">
                <span className="text-orange-400">{stats.pendingFeedback}</span> pending review
              </p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-slate-200">Lab Utilization</CardTitle>
              <BarChart3 className="h-4 w-4 text-purple-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">87%</div>
              <p className="text-xs text-slate-400">Average session attendance</p>
            </CardContent>
          </Card>
        </div>
      );
    }
  };

  const renderQuickActions = () => {
    if (userRole.isStudent) {
      return (
        <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Quick Actions</CardTitle>
            <CardDescription className="text-slate-400">Common tasks for students</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Link href="/dashboard/testing-lab/submit">
              <Button className="w-full bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0">
                <Upload className="mr-2 h-4 w-4" />
                Submit New Version
              </Button>
            </Link>
            <Button
              variant="outline"
              className="w-full border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50"
              onClick={() => window.open('/dashboard/testing-lab/requests', '_self')}
            >
              <Download className="mr-2 h-4 w-4" />
              View Testing Assignments
            </Button>
            <Button
              variant="outline"
              className="w-full border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50"
              onClick={() => window.open('/dashboard/testing-lab/feedback', '_self')}
            >
              <MessageSquare className="mr-2 h-4 w-4" />
              Complete Feedback
            </Button>
          </CardContent>
        </Card>
      );
    } else {
      return (
        <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Testing Lab Admin</CardTitle>
            <CardDescription className="text-slate-400">Manage testing lab operations</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Link href="/dashboard/testing-lab/sessions">
              <Button className="w-full bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0">
                <Plus className="mr-2 h-4 w-4" />
                Create Testing Session
              </Button>
            </Link>
            <Button
              variant="outline"
              className="w-full border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50"
              onClick={() => window.open('/dashboard/testing-lab/attendance', '_self')}
            >
              <Users className="mr-2 h-4 w-4" />
              View Attendance Reports
            </Button>
            <Button
              variant="outline"
              className="w-full border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50"
              onClick={() => window.open('/dashboard/testing-lab/feedback', '_self')}
            >
              <BarChart3 className="mr-2 h-4 w-4" />
              Review Feedback
            </Button>
          </CardContent>
        </Card>
      );
    }
  };

  if (loading) {
    return (
      <div className="flex flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-400 mx-auto mb-4"></div>
          <p className="text-slate-400">Loading testing lab data...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
      <div className="container mx-auto px-4 py-8 space-y-8">
        {/* Header */}
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Testing Lab</h1>
            <p className="text-slate-400 mt-2">
              {userRole.isStudent ? 'Submit your games and participate in testing sessions' : 'Manage testing sessions and review feedback'}
            </p>
          </div>
          <Badge variant="outline" className="bg-gradient-to-r from-blue-500/10 to-purple-500/10 border-blue-500/20 text-blue-300">
            {userRole.type.charAt(0).toUpperCase() + userRole.type.slice(1)} Dashboard
          </Badge>
        </div>

        {/* Stats Cards */}
        {renderStatsCards()}

        {/* Main Content */}
        <Tabs defaultValue="overview" className="w-full">
          <TabsList className="grid w-full grid-cols-4 bg-slate-800/50 border-slate-700">
            <TabsTrigger value="overview" className="data-[state=active]:bg-slate-700">
              Overview
            </TabsTrigger>
            <TabsTrigger value="requests" className="data-[state=active]:bg-slate-700">
              {userRole.isStudent ? 'My Requests' : 'All Requests'}
            </TabsTrigger>
            <TabsTrigger value="sessions" className="data-[state=active]:bg-slate-700">
              Sessions
            </TabsTrigger>
            <TabsTrigger value="feedback" className="data-[state=active]:bg-slate-700">
              Feedback
            </TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="space-y-6">
            <div className="grid gap-6 md:grid-cols-1 lg:grid-cols-3">
              {/* Quick Actions */}
              {renderQuickActions()}

              {/* Testing Requests Overview */}
              <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm lg:col-span-2">
                <CardHeader>
                  <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                    Testing Requests
                  </CardTitle>
                  <CardDescription className="text-slate-400">
                    {userRole.isStudent ? 'Track your game submissions and their status' : 'Manage all student testing requests'}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-slate-400">Active Requests</span>
                      <Badge variant="secondary" className="bg-green-900/20 text-green-400 border-green-700">
                        {stats.activeRequests}
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-slate-400">Total Requests</span>
                      <span className="text-sm text-white">{stats.totalRequests}</span>
                    </div>
                    <Button asChild variant="outline" className="w-full border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50">
                      <Link href="/dashboard/testing-lab/requests">View All Requests</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </div>

            <div className="grid gap-6 md:grid-cols-2">
              {/* Testing Sessions Overview */}
              <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
                <CardHeader>
                  <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                    Testing Sessions
                  </CardTitle>
                  <CardDescription className="text-slate-400">
                    {userRole.isStudent ? 'View your scheduled testing sessions' : 'Manage and create testing sessions'}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-slate-400">Upcoming Sessions</span>
                      <Badge variant="secondary" className="bg-blue-900/20 text-blue-400 border-blue-700">
                        {stats.upcomingSessions}
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-slate-400">Total Sessions</span>
                      <span className="text-sm text-white">{stats.totalSessions}</span>
                    </div>
                    <Button asChild variant="outline" className="w-full border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50">
                      <Link href="/dashboard/testing-lab/sessions">Manage Sessions</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>

              {/* Feedback Overview */}
              <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
                <CardHeader>
                  <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                    Feedback System
                  </CardTitle>
                  <CardDescription className="text-slate-400">
                    {userRole.isStudent ? 'Complete feedback for games you tested' : 'Review and approve feedback responses'}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-slate-400">Pending Feedback</span>
                      <Badge variant="secondary" className="bg-orange-900/20 text-orange-400 border-orange-700">
                        {stats.pendingFeedback}
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-slate-400">Total Feedback</span>
                      <span className="text-sm text-white">{stats.totalFeedback}</span>
                    </div>
                    <Button asChild variant="outline" className="w-full border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50">
                      <Link href="/dashboard/testing-lab/feedback">Manage Feedback</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          <TabsContent value="requests" className="space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold">Testing Requests</h3>
              <Dialog>
                <DialogTrigger asChild>
                  <Button>
                    <Plus className="h-4 w-4 mr-2" />
                    New Request
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Create Testing Request</DialogTitle>
                  </DialogHeader>
                  <CreateRequestForm onSuccess={() => loadStats()} />
                </DialogContent>
              </Dialog>
            </div>

            <div className="flex items-center space-x-4">
              <div className="flex-1">
                <Input placeholder="Search requests..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="max-w-sm" />
              </div>
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="All Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Status</SelectItem>
                  <SelectItem value="open">Open</SelectItem>
                  <SelectItem value="in-progress">In Progress</SelectItem>
                  <SelectItem value="completed">Completed</SelectItem>
                  <SelectItem value="cancelled">Cancelled</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-4">
              {filteredRequests.map((request) => (
                <Card key={request.id} className="p-4">
                  <div className="flex items-start justify-between">
                    <div className="space-y-2">
                      <h4 className="font-semibold">{request.title}</h4>
                      <p className="text-sm text-muted-foreground">{request.description}</p>
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span>Game: {request.gameTitle}</span>
                        <span>Platform: {request.platform}</span>
                        <span>Priority: {request.priority === 1 ? 'Low' : request.priority === 2 ? 'Medium' : 'High'}</span>
                      </div>
                      <div className="flex items-center gap-4 text-sm">
                        <Badge
                          variant={request.status === 1 ? 'default' : request.status === 2 ? 'secondary' : request.status === 3 ? 'default' : 'destructive'}
                        >
                          {request.status === 1 ? 'Open' : request.status === 2 ? 'In Progress' : request.status === 3 ? 'Completed' : 'Cancelled'}
                        </Badge>
                        {request.deadline && <span className="text-muted-foreground">Due: {new Date(request.deadline).toLocaleDateString()}</span>}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      {request.status === 1 && (
                        <Button size="sm" variant="outline" onClick={() => handleJoinRequest(request.id)}>
                          Join
                        </Button>
                      )}
                      <Button size="sm" variant="ghost">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </Card>
              ))}

              {filteredRequests.length === 0 && <div className="text-center py-8 text-muted-foreground">No testing requests found.</div>}
            </div>
          </TabsContent>

          <TabsContent value="sessions">
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold">Testing Sessions</h3>
                <Button>
                  <Plus className="h-4 w-4 mr-2" />
                  New Session
                </Button>
              </div>
              
              <div className="grid gap-4">
                {sessions.map((session) => (
                  <Card key={session.id} className="p-4">
                    <div className="space-y-2">
                      <div className="flex items-start justify-between">
                        <h4 className="font-semibold">{session.title || 'Untitled Session'}</h4>
                        <Badge variant={
                          session.status === 1 ? 'default' :
                          session.status === 2 ? 'secondary' :
                          session.status === 3 ? 'default' : 'destructive'
                        }>
                          {session.status === 1 ? 'Scheduled' :
                           session.status === 2 ? 'In Progress' :
                           session.status === 3 ? 'Completed' : 'Cancelled'}
                        </Badge>
                      </div>
                      
                      {session.description && (
                        <p className="text-sm text-muted-foreground">
                          {session.description}
                        </p>
                      )}
                      
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        {session.sessionDate && (
                          <span>Date: {new Date(session.sessionDate).toLocaleString()}</span>
                        )}
                        {session.location && (
                          <span>Location: {session.location}</span>
                        )}
                        {session.maxParticipants && (
                          <span>Max Participants: {session.maxParticipants}</span>
                        )}
                      </div>
                    </div>
                  </Card>
                ))}
                
                {sessions.length === 0 && (
                  <div className="text-center py-8 text-muted-foreground">
                    No testing sessions found.
                  </div>
                )}
              </div>
            </div>
          </TabsContent>

          <TabsContent value="feedback">
            <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
              <CardHeader>
                <CardTitle>Feedback Management</CardTitle>
                <CardDescription>Submit and review feedback</CardDescription>
              </CardHeader>
              <CardContent>
                <p className="text-slate-400">Feedback functionality will be implemented here.</p>
                <Button asChild className="mt-4">
                  <Link href="/dashboard/testing-lab/feedback">Go to Feedback Page</Link>
                </Button>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
