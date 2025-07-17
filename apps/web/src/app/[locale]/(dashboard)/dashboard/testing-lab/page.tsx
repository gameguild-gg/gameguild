'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { BarChart3, Calendar, FileText, TestTube, Upload, Users } from 'lucide-react';
import { testingLabApi, TestingRequest, TestingSession } from '@/lib/api/testing-lab/testing-lab-api';
import Link from 'next/link';

export default function TestingLabPage() {
  const { data: session } = useSession();
  const [testingRequests, setTestingRequests] = useState<TestingRequest[]>([]);
  const [testingSessions, setTestingSessions] = useState<TestingSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [userRole, setUserRole] = useState<'student' | 'professor' | 'admin'>('student');

  useEffect(() => {
    // Determine user role based on email domain
    if (session?.user?.email) {
      const email = session.user.email;
      if (email.endsWith('@champlain.edu')) {
        setUserRole('professor');
      } else if (email.endsWith('@mymail.champlain.edu')) {
        setUserRole('student');
      } else {
        setUserRole('admin'); // or default role
      }
    }

    fetchTestingData();
  }, [session]);

  const fetchTestingData = async () => {
    try {
      setLoading(true);

      // Fetch data from API
      const [requestsData, sessionsData] = await Promise.all([testingLabApi.getAvailableTestingRequests(), testingLabApi.getTestingSessions()]);

      setTestingRequests(requestsData);
      setTestingSessions(sessionsData);
    } catch (error) {
      console.error('Error fetching testing data:', error);

      // Fallback to mock data for now
      setTestingRequests([
        {
          id: '1',
          title: 'Alpha Build Testing - Team 01',
          description: 'Testing the core gameplay mechanics for our RPG game',
          projectVersionId: 'pv1',
          downloadUrl: 'https://drive.google.com/file/d/example123/view',
          instructionsType: 'inline',
          instructionsContent: 'Test the game thoroughly...',
          feedbackFormContent: 'Please rate the game...',
          maxTesters: 8,
          currentTesterCount: 3,
          startDate: '2024-01-15',
          endDate: '2024-01-22',
          status: 'open',
          createdBy: {
            id: 'u1',
            name: 'John Developer',
            email: 'john.dev@mymail.champlain.edu',
          },
          projectVersion: {
            id: 'pv1',
            versionNumber: 'v0.1.0-alpha',
            project: {
              id: 'p1',
              title: 'fa23-capstone-2023-24-t01',
            },
          },
        },
      ]);

      setTestingSessions([
        {
          id: 's1',
          sessionName: 'Block 1 Testing Session',
          sessionDate: '2024-01-18',
          startTime: '10:00',
          endTime: '12:00',
          maxTesters: 16,
          registeredTesterCount: 12,
          status: 'scheduled',
          location: {
            id: 'l1',
            name: 'Room A101',
            capacity: 16,
          },
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
      case 'scheduled':
      case 'in-progress':
        return 'bg-green-100 text-green-800';
      case 'draft':
        return 'bg-yellow-100 text-yellow-800';
      case 'completed':
        return 'bg-blue-100 text-blue-800';
      case 'cancelled':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center">Loading Testing Lab...</div>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-3xl font-bold mb-2">Testing Lab</h1>
          <p className="text-muted-foreground">Submit, test, and manage game projects for Capstone teams</p>
        </div>

        {(userRole === 'student' || userRole === 'professor') && (
          <div className="flex gap-2">
            <Link href="/dashboard/testing-lab/submit">
              <Button>
                <Upload className="mr-2 h-4 w-4" />
                Submit Version
              </Button>
            </Link>
            {userRole === 'professor' && (
              <Link href="/dashboard/testing-lab/sessions/create">
                <Button variant="outline">
                  <Calendar className="mr-2 h-4 w-4" />
                  Create Session
                </Button>
              </Link>
            )}
          </div>
        )}
      </div>

      <Tabs defaultValue="testing-requests" className="space-y-6">
        <TabsList>
          <TabsTrigger value="testing-requests">Testing Requests</TabsTrigger>
          <TabsTrigger value="sessions">Testing Sessions</TabsTrigger>
          {userRole === 'professor' && <TabsTrigger value="attendance">Attendance Reports</TabsTrigger>}
        </TabsList>

        <TabsContent value="testing-requests" className="space-y-4">
          <div className="grid gap-4">
            {testingRequests.map((request) => (
              <Card key={request.id}>
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle className="text-lg">{request.title}</CardTitle>
                      <CardDescription>
                        {request.projectTitle} - {request.versionNumber}
                      </CardDescription>
                    </div>
                    <Badge className={getStatusColor(request.status)}>{request.status}</Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground mb-4">{request.description}</p>

                  <div className="flex justify-between items-center text-sm">
                    <div className="flex gap-4">
                      <span>
                        📅 {new Date(request.startDate).toLocaleDateString()} - {new Date(request.endDate).toLocaleDateString()}
                      </span>
                      <span>
                        👥 {request.currentTesterCount}/{request.maxTesters || 'Unlimited'} testers
                      </span>
                      <span>👨‍💻 {request.createdBy.name}</span>
                    </div>

                    <div className="flex gap-2">
                      <Link href={`/dashboard/testing-lab/requests/${request.id}`}>
                        <Button size="sm" variant="outline">
                          <TestTube className="mr-2 h-4 w-4" />
                          Test Now
                        </Button>
                      </Link>
                      <Link href={`/dashboard/testing-lab/requests/${request.id}/details`}>
                        <Button size="sm" variant="ghost">
                          <FileText className="mr-2 h-4 w-4" />
                          View Details
                        </Button>
                      </Link>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}

            {testingRequests.length === 0 && (
              <Card>
                <CardContent className="text-center py-8">
                  <TestTube className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                  <h3 className="text-lg font-medium mb-2">No Testing Requests</h3>
                  <p className="text-muted-foreground mb-4">No active testing requests at the moment.</p>
                  {userRole === 'student' && (
                    <Link href="/dashboard/testing-lab/submit">
                      <Button>
                        <Upload className="mr-2 h-4 w-4" />
                        Submit Your First Version
                      </Button>
                    </Link>
                  )}
                </CardContent>
              </Card>
            )}
          </div>
        </TabsContent>

        <TabsContent value="sessions" className="space-y-4">
          <div className="grid gap-4">
            {testingSessions.map((session) => (
              <Card key={session.id}>
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle className="text-lg">{session.sessionName}</CardTitle>
                      <CardDescription>
                        {session.location.name} - {session.maxTesters} capacity
                      </CardDescription>
                    </div>
                    <Badge className={getStatusColor(session.status)}>{session.status}</Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="flex justify-between items-center text-sm">
                    <div className="flex gap-4">
                      <span>📅 {new Date(session.sessionDate).toLocaleDateString()}</span>
                      <span>
                        🕐 {session.startTime} - {session.endTime}
                      </span>
                      <span>
                        👥 {session.registeredTesterCount}/{session.maxTesters} registered
                      </span>
                    </div>

                    <div className="flex gap-2">
                      <Link href={`/dashboard/testing-lab/sessions/${session.id}/register`}>
                        <Button size="sm" variant="outline">
                          <Users className="mr-2 h-4 w-4" />
                          Register
                        </Button>
                      </Link>
                      <Link href={`/dashboard/testing-lab/sessions/${session.id}`}>
                        <Button size="sm" variant="ghost">
                          <Calendar className="mr-2 h-4 w-4" />
                          View Details
                        </Button>
                      </Link>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}

            {testingSessions.length === 0 && (
              <Card>
                <CardContent className="text-center py-8">
                  <Calendar className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                  <h3 className="text-lg font-medium mb-2">No Testing Sessions</h3>
                  <p className="text-muted-foreground mb-4">No testing sessions scheduled at the moment.</p>
                  {userRole === 'professor' && (
                    <Link href="/dashboard/testing-lab/sessions/create">
                      <Button>
                        <Calendar className="mr-2 h-4 w-4" />
                        Create First Session
                      </Button>
                    </Link>
                  )}
                </CardContent>
              </Card>
            )}
          </div>
        </TabsContent>

        {userRole === 'professor' && (
          <TabsContent value="attendance" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <BarChart3 className="mr-2 h-5 w-5" />
                  Attendance Reports
                </CardTitle>
                <CardDescription>Track student attendance and testing participation for grading purposes</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-center py-8">
                  <BarChart3 className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                  <h3 className="text-lg font-medium mb-2">Attendance Tracking</h3>
                  <p className="text-muted-foreground mb-4">View detailed attendance reports for your students across testing sessions.</p>
                  <Link href="/dashboard/testing-lab/attendance">
                    <Button>
                      <BarChart3 className="mr-2 h-4 w-4" />
                      View Full Reports
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}
