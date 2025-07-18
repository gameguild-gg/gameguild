'use client';

import { useState, useEffect } from 'react';
import { useSession } from 'next-auth/react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  TestTube,
  Upload,
  Calendar,
  Users,
  BarChart3,
  Clock,
  CheckCircle,
  Plus,
  Download,
  MessageSquare
} from 'lucide-react';
import Link from 'next/link';

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
        // Mock data for now - replace with actual API calls
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
      } catch (error) {
        console.error('Failed to load testing lab stats:', error);
      } finally {
        setLoading(false);
      }
    };

    loadStats();
  }, [userRole]);

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
              <CardTitle className="text-sm font-medium text-slate-200">System Health</CardTitle>
              <BarChart3 className="h-4 w-4 text-emerald-400" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-white">98%</div>
              <p className="text-xs text-slate-400">Uptime this month</p>
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
            <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              Quick Actions
            </CardTitle>
            <CardDescription className="text-slate-400">
              Common tasks for students
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <Link href="/dashboard/testing-lab/submit">
              <Button className="w-full justify-start bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0">
                <Upload className="mr-2 h-4 w-4" />
                Submit New Version
              </Button>
            </Link>
            <Link href="/dashboard/testing-lab/sessions">
              <Button variant="outline" className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-800">
                <Calendar className="mr-2 h-4 w-4" />
                View Testing Sessions
              </Button>
            </Link>
            <Link href="/dashboard/testing-lab/feedback">
              <Button variant="outline" className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-800">
                <MessageSquare className="mr-2 h-4 w-4" />
                Complete Feedback
              </Button>
            </Link>
          </CardContent>
        </Card>
      );
    } else {
      return (
        <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              Professor Tools
            </CardTitle>
            <CardDescription className="text-slate-400">
              Manage testing lab operations
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <Link href="/dashboard/testing-lab/sessions">
              <Button className="w-full justify-start bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0">
                <Plus className="mr-2 h-4 w-4" />
                Create Testing Session
              </Button>
            </Link>
            <Link href="/dashboard/testing-lab/feedback">
              <Button variant="outline" className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-800">
                <MessageSquare className="mr-2 h-4 w-4" />
                Review Feedback
              </Button>
            </Link>
            <Link href="/dashboard/testing-lab/attendance">
              <Button variant="outline" className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-800">
                <Users className="mr-2 h-4 w-4" />
                Track Attendance
              </Button>
            </Link>
            <Button variant="outline" className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-800">
              <Download className="mr-2 h-4 w-4" />
              Export Reports
            </Button>
          </CardContent>
        </Card>
      );
    }
  };

  const renderRecentActivity = () => {
    const studentActivity = [
      {
        type: 'submission',
        title: 'Game Version 2.1 Submitted',
        description: 'Successfully uploaded to fa23-capstone-2023-24-t07',
        time: '2 hours ago',
        status: 'completed',
      },
      {
        type: 'session',
        title: 'Testing Session Scheduled',
        description: 'Tuesday 2:00 PM - Room TEC 125',
        time: '1 day ago',
        status: 'upcoming',
      },
      {
        type: 'feedback',
        title: 'Feedback Required',
        description: 'Team 03 - Action RPG Game',
        time: '2 days ago',
        status: 'pending',
      },
    ];

    const professorActivity = [
      {
        type: 'session',
        title: 'Testing Session Created',
        description: 'Block 3 - 15 teams scheduled',
        time: '30 minutes ago',
        status: 'completed',
      },
      {
        type: 'feedback',
        title: 'Feedback Reviews Completed',
        description: '8 responses approved, 3 pending',
        time: '2 hours ago',
        status: 'completed',
      },
      {
        type: 'submission',
        title: 'New Submissions Received',
        description: '5 teams submitted updated versions',
        time: '4 hours ago',
        status: 'new',
      },
    ];

    const activities = userRole.isStudent ? studentActivity : professorActivity;

    return (
      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
        <CardHeader>
          <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
            Recent Activity
          </CardTitle>
          <CardDescription className="text-slate-400">
            Latest updates and notifications
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {activities.map((activity, index) => (
            <div key={index} className="flex items-start space-x-3 p-3 rounded-lg bg-slate-800/30 border border-slate-700/50">
              <div className="flex-shrink-0">
                {activity.type === 'submission' && <Upload className="h-5 w-5 text-blue-400" />}
                {activity.type === 'session' && <Calendar className="h-5 w-5 text-purple-400" />}
                {activity.type === 'feedback' && <MessageSquare className="h-5 w-5 text-green-400" />}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-white">{activity.title}</p>
                <p className="text-xs text-slate-400">{activity.description}</p>
                <div className="flex items-center justify-between mt-2">
                  <span className="text-xs text-slate-500">{activity.time}</span>
                  <Badge
                    variant={activity.status === 'completed' ? 'default' : activity.status === 'pending' ? 'destructive' : 'secondary'}
                    className="text-xs"
                  >
                    {activity.status}
                  </Badge>
                </div>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>
    );
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-4">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-400 mx-auto"></div>
          <p className="text-slate-400">Loading testing lab overview...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
            Testing Lab
          </h1>
          <p className="text-slate-400 mt-1">
            {userRole.isStudent
              ? 'Submit your games and participate in testing sessions'
              : 'Manage testing sessions and review feedback'
            }
          </p>
        </div>
        <Badge
          variant="outline"
          className="bg-gradient-to-r from-blue-500/10 to-purple-500/10 border-blue-500/20 text-blue-300"
        >
          <TestTube className="w-4 h-4 mr-1" />
          {userRole.type.charAt(0).toUpperCase() + userRole.type.slice(1)}
        </Badge>
      </div>

      {/* Stats Cards */}
      {renderStatsCards()}

      {/* Tabs for different views */}
      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList className="grid w-full grid-cols-4 bg-slate-800/50 border-slate-700">
          <TabsTrigger value="overview" className="data-[state=active]:bg-slate-700">
            Overview
          </TabsTrigger>
          <TabsTrigger value="requests" className="data-[state=active]:bg-slate-700">
            {userRole.isStudent ? 'My Submissions' : 'All Requests'}
          </TabsTrigger>
          <TabsTrigger value="sessions" className="data-[state=active]:bg-slate-700">
            Sessions
          </TabsTrigger>
          <TabsTrigger value="feedback" className="data-[state=active]:bg-slate-700">
            Feedback
          </TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid gap-6 md:grid-cols-2">
            {renderQuickActions()}
            {renderRecentActivity()}
          </div>
        </TabsContent>

        <TabsContent value="requests" className="space-y-6">
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                {userRole.isStudent ? 'My Submissions' : 'Testing Requests'}
              </CardTitle>
              <CardDescription className="text-slate-400">
                {userRole.isStudent
                  ? 'Track your game submissions and their status'
                  : 'Manage all student testing requests'
                }
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-slate-400">
                <TestTube className="h-12 w-12 mx-auto mb-4 opacity-50" />
                <p>No requests found. Check back later or submit a new version.</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="sessions" className="space-y-6">
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                Testing Sessions
              </CardTitle>
              <CardDescription className="text-slate-400">
                {userRole.isStudent
                  ? 'View your scheduled testing sessions'
                  : 'Manage and create testing sessions'
                }
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-slate-400">
                <Calendar className="h-12 w-12 mx-auto mb-4 opacity-50" />
                <p>No sessions scheduled. Check back later.</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="feedback" className="space-y-6">
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                Feedback Management
              </CardTitle>
              <CardDescription className="text-slate-400">
                {userRole.isStudent
                  ? 'Complete feedback for games you tested'
                  : 'Review and approve feedback responses'
                }
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-slate-400">
                <MessageSquare className="h-12 w-12 mx-auto mb-4 opacity-50" />
                <p>No feedback items found.</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
