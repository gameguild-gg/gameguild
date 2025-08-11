'use client';

import { useState } from 'react';
import { useSession } from 'next-auth/react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Progress } from '@/components/ui/progress';
import { toast } from 'sonner';
import { Calendar, Clock, MapPin, TestTube, User, Users } from 'lucide-react';
import Link from 'next/link';
import type { TestingSession } from '@/lib/api/generated/types.gen';

interface TestingSessionDetailsProps {
  data: TestingSession;
}

export function TestingSessionDetails({ data: session }: TestingSessionDetailsProps) {
  const { data: userSession } = useSession();
  const [loading, setLoading] = useState(false);

  const getStatusBadge = (status: number) => {
    switch (status) {
      case 0:
        return <Badge variant="outline">Scheduled</Badge>;
      case 1:
        return <Badge variant="default">In Progress</Badge>;
      case 2:
        return <Badge variant="secondary">Completed</Badge>;
      case 3:
        return <Badge variant="destructive">Cancelled</Badge>;
      default:
        return <Badge variant="outline">Unknown</Badge>;
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const formatTime = (timeString: string) => {
    return new Date(`1970-01-01T${timeString}`).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const handleJoinSession = async () => {
    if (!userSession?.user?.id) {
      toast.error('Please log in to join this session');
      return;
    }

    setLoading(true);
    try {
      // Implement join session logic here
      toast.success('Successfully joined the testing session!');
    } catch {
      toast.error('Failed to join session. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mx-auto py-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{session.sessionName}</h1>
          <p className="text-muted-foreground mt-2">Testing Session Details</p>
        </div>
        <div className="flex items-center gap-2">
          {getStatusBadge(session.status || 0)}
          <Badge variant="outline">
            <TestTube className="w-4 h-4 mr-1" />
            Testing Session
          </Badge>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Session Overview */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Calendar className="w-5 h-5" />
                Session Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <div className="flex items-center gap-2 text-sm">
                    <Calendar className="w-4 h-4 text-muted-foreground" />
                    <span className="font-medium">Date:</span>
                    <span>{formatDate(session.sessionDate || '')}</span>
                  </div>
                  <div className="flex items-center gap-2 text-sm">
                    <Clock className="w-4 h-4 text-muted-foreground" />
                    <span className="font-medium">Time:</span>
                    <span>
                      {formatTime(session.startTime || '')} - {formatTime(session.endTime || '')}
                    </span>
                  </div>
                </div>
                <div className="space-y-2">
                  <div className="flex items-center gap-2 text-sm">
                    <Users className="w-4 h-4 text-muted-foreground" />
                    <span className="font-medium">Max Testers:</span>
                    <span>{session.maxTesters}</span>
                  </div>
                  {session.locationId && (
                    <div className="flex items-center gap-2 text-sm">
                      <MapPin className="w-4 h-4 text-muted-foreground" />
                      <span className="font-medium">Location:</span>
                      <span>{session.locationId}</span>
                    </div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Testing Request Information */}
          {session.testingRequestId && (
            <Card>
              <CardHeader>
                <CardTitle>Related Testing Request</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-medium">Request ID: {session.testingRequestId}</p>
                    <p className="text-sm text-muted-foreground">View the testing request for more details about what needs to be tested.</p>
                  </div>
                  <Link href={`/dashboard/testing-lab/requests/${session.testingRequestId}`}>
                    <Button variant="outline">View Request</Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Manager Information */}
          {session.managerId && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <User className="w-5 h-5" />
                  Session Manager
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center">
                    <User className="w-6 h-6 text-primary" />
                  </div>
                  <div>
                    <p className="font-medium">Manager ID: {session.managerId}</p>
                    <p className="text-sm text-muted-foreground">Contact this person for any questions about the session.</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {session.status === 0 && (
                <Button onClick={handleJoinSession} disabled={loading} className="w-full">
                  {loading ? 'Joining...' : 'Join Session'}
                </Button>
              )}

              <Button variant="outline" className="w-full" asChild>
                <Link href="/dashboard/testing-lab/sessions">Back to Sessions</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Session Progress */}
          <Card>
            <CardHeader>
              <CardTitle>Session Progress</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <div className="flex justify-between text-sm mb-2">
                  <span>Participants</span>
                  <span>0/{session.maxTesters}</span>
                </div>
                <Progress value={0} className="h-2" />
              </div>

              <Separator />

              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Session ID:</span>
                  <span className="font-mono">{session.id}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Created:</span>
                  <span>{session.createdAt ? formatDate(session.createdAt) : 'N/A'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Updated:</span>
                  <span>{session.updatedAt ? formatDate(session.updatedAt) : 'N/A'}</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
