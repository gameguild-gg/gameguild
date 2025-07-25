'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { BarChart3, Calendar, CheckCircle, Clock, Download, MessageSquare, Plus, TestTube, Upload, Users } from 'lucide-react';
import Link from 'next/link';
import { testingLabApi } from '@/lib/api/testing-lab/testing-lab-api';

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
          // Load actual data from API for students
          const [myRequests, availableRequests, sessions] = await Promise.all([
            testingLabApi.getMyTestingRequests(),
            testingLabApi.getAvailableTestingRequests(),
            testingLabApi.getTestingSessions(),
          ]);

          setStats({
            totalRequests: availableRequests.length,
            activeRequests: availableRequests.filter((r) => r.status === 'open').length,
            totalSessions: sessions.length,
            upcomingSessions: sessions.filter((s) => new Date(s.sessionDate) > new Date() && s.status === 'scheduled').length,
            totalFeedback: 0, // Will be implemented when feedback API is available
            pendingFeedback: 0, // Will be implemented when feedback API is available
            mySubmissions: myRequests.length,
            myTestingAssignments: availableRequests.filter((r) => r.status === 'open').length,
          });
        } else if (userRole.isProfessor) {
          // Load actual data from API for professors/admins
          const sessions = await testingLabApi.getTestingSessions();

          // Mock some additional data that would come from other endpoints
          setStats({
            totalRequests: 120,
            activeRequests: 45,
            totalSessions: sessions.length,
            upcomingSessions: sessions.filter((s) => new Date(s.sessionDate) > new Date() && s.status === 'scheduled').length,
            totalFeedback: 89,
            pendingFeedback: 12,
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
      <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-400 mx-auto mb-4"></div>
          <p className="text-slate-400">Loading testing lab data...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
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

          <TabsContent value="requests">
            <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Recent Activity</CardTitle>
                <CardDescription className="text-slate-400">Latest updates and notifications</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex items-center gap-4 p-4 bg-slate-800/30 rounded-lg border border-slate-700">
                    <CheckCircle className="h-5 w-5 text-green-400 flex-shrink-0" />
                    <div className="flex-1">
                      <p className="text-sm font-medium text-white">Version 1.2 submitted successfully</p>
                      <p className="text-xs text-slate-400">Testing request created for Capstone Project Alpha</p>
                    </div>
                    <Badge variant="outline" className="text-xs border-green-600 text-green-400">
                      <Clock className="mr-1 h-3 w-3" />
                      2h ago
                    </Badge>
                  </div>

                  <div className="flex items-center gap-4 p-4 bg-slate-800/30 rounded-lg border border-slate-700">
                    <MessageSquare className="h-5 w-5 text-blue-400 flex-shrink-0" />
                    <div className="flex-1">
                      <p className="text-sm font-medium text-white">New feedback received</p>
                      <p className="text-xs text-slate-400">Team Beta submitted feedback for your game</p>
                    </div>
                    <Badge variant="outline" className="text-xs border-blue-600 text-blue-400">
                      <Clock className="mr-1 h-3 w-3" />
                      4h ago
                    </Badge>
                  </div>

                  <div className="flex items-center gap-4 p-4 bg-slate-800/30 rounded-lg border border-slate-700">
                    <Calendar className="h-5 w-5 text-purple-400 flex-shrink-0" />
                    <div className="flex-1">
                      <p className="text-sm font-medium text-white">Testing session scheduled</p>
                      <p className="text-xs text-slate-400">Session for your game on Friday, 3:00 PM</p>
                    </div>
                    <Badge variant="outline" className="text-xs border-purple-600 text-purple-400">
                      <Clock className="mr-1 h-3 w-3" />
                      1d ago
                    </Badge>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="sessions">
            <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
              <CardHeader>
                <CardTitle>Testing Sessions</CardTitle>
                <CardDescription>View and manage testing sessions</CardDescription>
              </CardHeader>
              <CardContent>
                <p className="text-slate-400">Sessions functionality will be implemented here.</p>
                <Button asChild className="mt-4">
                  <Link href="/dashboard/testing-lab/sessions">Go to Sessions Page</Link>
                </Button>
              </CardContent>
            </Card>
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
