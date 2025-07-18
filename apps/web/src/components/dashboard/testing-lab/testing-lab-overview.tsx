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
  FileText, 
  BarChart3,
  Clock,
  CheckCircle,
  AlertTriangle,
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

  useEffect(() => {
    // Determine user role based on email domain
    if (session?.user?.email) {
      const email = session.user.email;
      let roleType: 'student' | 'professor' | 'admin' = 'student';
      
      if (email.endsWith('@champlain.edu')) {
        roleType = 'professor';
      } else if (email.endsWith('@mymail.champlain.edu')) {
        roleType = 'student';
      } else {
        roleType = 'admin';
      }

      setUserRole({
        type: roleType,
        isStudent: roleType === 'student',
        isProfessor: roleType === 'professor',
        isAdmin: roleType === 'admin',
      });
    }

    // Mock data for now - replace with actual API calls
    setStats({
      totalRequests: 24,
      activeRequests: 8,
      totalSessions: 12,
      upcomingSessions: 3,
      totalFeedback: 156,
      pendingFeedback: 12,
      mySubmissions: userRole.isStudent ? 3 : 0,
      myTestingAssignments: 8,
    });
    setLoading(false);
  }, [session, userRole.isStudent]);

  const getStatsCards = () => {
    const baseCards = [
      {
        title: 'Total Requests',
        value: stats.totalRequests,
        icon: TestTube,
        description: 'Testing requests in system',
        color: 'text-blue-600',
      },
      {
        title: 'Active Testing',
        value: stats.activeRequests,
        icon: Clock,
        description: 'Currently open for testing',
        color: 'text-green-600',
      },
      {
        title: 'Testing Sessions',
        value: stats.totalSessions,
        icon: Calendar,
        description: 'Scheduled sessions',
        color: 'text-purple-600',
      },
      {
        title: 'Feedback Received',
        value: stats.totalFeedback,
        icon: MessageSquare,
        description: 'Total feedback submissions',
        color: 'text-orange-600',
      },
    ];

    if (userRole.isStudent) {
      return [
        {
          title: 'My Submissions',
          value: stats.mySubmissions,
          icon: Upload,
          description: 'Games I\'ve submitted',
          color: 'text-blue-600',
        },
        {
          title: 'Testing Assignments',
          value: stats.myTestingAssignments,
          icon: TestTube,
          description: 'Games to test',
          color: 'text-green-600',
        },
        ...baseCards.slice(2),
      ];
    }

    return baseCards;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-white">Testing Lab</h1>
          <p className="text-slate-400 mt-1">
            {userRole.isStudent && "Submit your projects and test others' games"}
            {userRole.isProfessor && "Manage testing sessions and track student progress"}
            {userRole.isAdmin && "Oversee the entire testing lab ecosystem"}
          </p>
        </div>
        <div className="flex gap-3">
          {userRole.isStudent && (
            <Button asChild className="bg-blue-600 hover:bg-blue-700">
              <Link href="/dashboard/testing-lab/submit">
                <Upload className="h-4 w-4 mr-2" />
                Submit Version
              </Link>
            </Button>
          )}
          {(userRole.isProfessor || userRole.isAdmin) && (
            <Button asChild className="bg-purple-600 hover:bg-purple-700">
              <Link href="/dashboard/testing-lab/sessions/create">
                <Plus className="h-4 w-4 mr-2" />
                Create Session
              </Link>
            </Button>
          )}
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {getStatsCards().map((stat, index) => {
          const IconComponent = stat.icon;
          return (
            <Card key={index} className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-slate-300">
                  {stat.title}
                </CardTitle>
                <IconComponent className={`h-4 w-4 ${stat.color}`} />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-white">{stat.value}</div>
                <p className="text-xs text-slate-400 mt-1">{stat.description}</p>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Main Content Tabs */}
      <Tabs defaultValue="overview" className="w-full">
        <TabsList className="grid w-full grid-cols-4 bg-slate-800/50">
          <TabsTrigger value="overview" className="text-slate-300 data-[state=active]:text-white">
            Overview
          </TabsTrigger>
          <TabsTrigger value="requests" className="text-slate-300 data-[state=active]:text-white">
            {userRole.isStudent ? 'Available Tests' : 'Testing Requests'}
          </TabsTrigger>
          <TabsTrigger value="sessions" className="text-slate-300 data-[state=active]:text-white">
            Sessions
          </TabsTrigger>
          <TabsTrigger value="feedback" className="text-slate-300 data-[state=active]:text-white">
            Feedback
          </TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Quick Actions */}
            <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="text-white flex items-center gap-2">
                  <TestTube className="h-5 w-5" />
                  Quick Actions
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {userRole.isStudent && (
                  <>
                    <Button asChild variant="outline" className="w-full justify-start">
                      <Link href="/dashboard/testing-lab/submit">
                        <Upload className="h-4 w-4 mr-2" />
                        Submit New Version
                      </Link>
                    </Button>
                    <Button asChild variant="outline" className="w-full justify-start">
                      <Link href="/dashboard/testing-lab/assignments">
                        <TestTube className="h-4 w-4 mr-2" />
                        View Testing Assignments
                      </Link>
                    </Button>
                  </>
                )}
                {(userRole.isProfessor || userRole.isAdmin) && (
                  <>
                    <Button asChild variant="outline" className="w-full justify-start">
                      <Link href="/dashboard/testing-lab/sessions">
                        <Calendar className="h-4 w-4 mr-2" />
                        Manage Sessions
                      </Link>
                    </Button>
                    <Button asChild variant="outline" className="w-full justify-start">
                      <Link href="/dashboard/testing-lab/attendance">
                        <Users className="h-4 w-4 mr-2" />
                        Attendance Reports
                      </Link>
                    </Button>
                  </>
                )}
                <Button asChild variant="outline" className="w-full justify-start">
                  <Link href="/dashboard/testing-lab/feedback">
                    <MessageSquare className="h-4 w-4 mr-2" />
                    View Feedback
                  </Link>
                </Button>
              </CardContent>
            </Card>

            {/* Recent Activity */}
            <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="text-white flex items-center gap-2">
                  <BarChart3 className="h-5 w-5" />
                  Recent Activity
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-3">
                  <div className="flex items-center gap-3 p-3 rounded-lg bg-slate-700/30">
                    <CheckCircle className="h-4 w-4 text-green-500" />
                    <div className="flex-1">
                      <p className="text-sm text-white">New testing session scheduled</p>
                      <p className="text-xs text-slate-400">2 hours ago</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3 p-3 rounded-lg bg-slate-700/30">
                    <Upload className="h-4 w-4 text-blue-500" />
                    <div className="flex-1">
                      <p className="text-sm text-white">Version 1.2.0 submitted for testing</p>
                      <p className="text-xs text-slate-400">4 hours ago</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3 p-3 rounded-lg bg-slate-700/30">
                    <MessageSquare className="h-4 w-4 text-orange-500" />
                    <div className="flex-1">
                      <p className="text-sm text-white">12 new feedback submissions</p>
                      <p className="text-xs text-slate-400">6 hours ago</p>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="requests" className="space-y-4">
          <div className="flex justify-between items-center">
            <h3 className="text-xl font-semibold text-white">
              {userRole.isStudent ? 'Available Testing Assignments' : 'Testing Requests'}
            </h3>
            {!userRole.isStudent && (
              <Button asChild size="sm">
                <Link href="/dashboard/testing-lab/requests/create">
                  <Plus className="h-4 w-4 mr-2" />
                  Create Request
                </Link>
              </Button>
            )}
          </div>
          
          <div className="grid gap-4">
            {/* Mock testing requests */}
            <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle className="text-white">Space Adventure v1.2</CardTitle>
                    <CardDescription className="text-slate-400">
                      Team: fa24-capstone-2024-25-t03
                    </CardDescription>
                  </div>
                  <Badge variant="secondary" className="bg-green-600/20 text-green-400">
                    Open
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-slate-300 text-sm mb-4">
                  2D space exploration game with puzzle elements. Test for gameplay balance and UI responsiveness.
                </p>
                <div className="flex justify-between items-center">
                  <div className="flex gap-4 text-sm text-slate-400">
                    <span>Due: Dec 15, 2024</span>
                    <span>Testers: 5/8</span>
                  </div>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline">
                      <Download className="h-4 w-4 mr-2" />
                      Download
                    </Button>
                    <Button size="sm">
                      <TestTube className="h-4 w-4 mr-2" />
                      Start Testing
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="sessions" className="space-y-4">
          <div className="flex justify-between items-center">
            <h3 className="text-xl font-semibold text-white">Testing Sessions</h3>
            {(userRole.isProfessor || userRole.isAdmin) && (
              <Button asChild size="sm">
                <Link href="/dashboard/testing-lab/sessions/create">
                  <Plus className="h-4 w-4 mr-2" />
                  Schedule Session
                </Link>
              </Button>
            )}
          </div>

          <div className="grid gap-4">
            {/* Mock sessions */}
            <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle className="text-white">Block 3 Testing Session</CardTitle>
                    <CardDescription className="text-slate-400">
                      Room A-204 • December 18, 2024 • 2:00 PM - 4:00 PM
                    </CardDescription>
                  </div>
                  <Badge variant="secondary" className="bg-blue-600/20 text-blue-400">
                    Upcoming
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex justify-between items-center">
                  <div className="flex gap-4 text-sm text-slate-400">
                    <span>Registered: 12/40</span>
                    <span>Projects: 6</span>
                  </div>
                  <Button size="sm">
                    View Details
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="feedback" className="space-y-4">
          <div className="flex justify-between items-center">
            <h3 className="text-xl font-semibold text-white">Feedback Management</h3>
            <div className="flex gap-2">
              <Badge variant="outline" className="text-slate-300">
                {stats.pendingFeedback} Pending Review
              </Badge>
            </div>
          </div>

          <div className="grid gap-4">
            {/* Mock feedback items */}
            <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle className="text-white">Feedback for Space Adventure v1.2</CardTitle>
                    <CardDescription className="text-slate-400">
                      From: john.doe@mymail.champlain.edu • 2 hours ago
                    </CardDescription>
                  </div>
                  <Badge variant="secondary" className="bg-orange-600/20 text-orange-400">
                    Pending Review
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-slate-300 text-sm mb-4">
                  Great game overall! The controls feel responsive and the art style is consistent...
                </p>
                <div className="flex justify-between items-center">
                  <div className="text-sm text-slate-400">
                    Rating: 4/5 stars
                  </div>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline">
                      Review
                    </Button>
                    <Button size="sm">
                      Approve
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
