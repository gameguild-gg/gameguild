'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Calendar, Users, Clock, MapPin, Plus, Edit, Trash2, UserCheck, CheckCircle, XCircle } from 'lucide-react';
import { testingLabApi } from '@/lib/api/testing-lab/testing-lab-api';

interface TestingSession {
  id: string;
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  location: {
    id: string;
    name: string;
    capacity: number;
  };
  maxTesters: number;
  registeredTesterCount: number;
  registeredProjectMemberCount: number;
  registeredProjectCount: number;
  status: 'scheduled' | 'inProgress' | 'completed' | 'cancelled';
  manager: {
    id: string;
    name: string;
    email: string;
  };
  testingRequests: {
    id: string;
    title: string;
    projectVersion: {
      versionNumber: string;
      project: {
        title: string;
      };
    };
  }[];
}

interface SessionRegistration {
  id: string;
  user: {
    id: string;
    name: string;
    email: string;
  };
  registrationType: 'tester' | 'developer' | 'observer';
  status: 'pending' | 'confirmed' | 'cancelled';
  registeredAt: string;
}

export function TestingSessionsManager() {
  const [sessions, setSessions] = useState<TestingSession[]>([]);
  const [selectedSession, setSelectedSession] = useState<TestingSession | null>(null);
  const [registrations, setRegistrations] = useState<SessionRegistration[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateDialog, setShowCreateDialog] = useState(false);

  useEffect(() => {
    fetchSessions();
  }, []);

  const fetchSessions = async () => {
    try {
      setLoading(true);
      // Load sessions from API
      const data = await testingLabApi.getTestingSessions();
      setSessions(data.map(session => ({
        id: session.id,
        sessionName: session.sessionName,
        sessionDate: session.sessionDate,
        startTime: session.startTime,
        endTime: session.endTime,
        location: session.location,
        maxTesters: session.maxTesters,
        registeredTesterCount: session.registeredTesterCount,
        registeredProjectMemberCount: 0, // Default value
        registeredProjectCount: 0, // Default value
        status: session.status,
        manager: {
          id: 'manager-id',
          name: 'Session Manager',
          email: 'manager@champlain.edu'
        },
        testingRequests: []
      })));
    } catch (error) {
      console.error('Error fetching sessions:', error);
      // Fallback to mock data
      setSessions([
        {
          id: '1',
          sessionName: 'Block 3 Testing Session',
          sessionDate: '2024-12-18',
          startTime: '14:00',
          endTime: '16:00',
          location: {
            id: 'room-a204',
            name: 'Room A-204',
            capacity: 40,
          },
          maxTesters: 40,
          registeredTesterCount: 12,
          registeredProjectMemberCount: 8,
          registeredProjectCount: 6,
          status: 'scheduled',
          manager: {
            id: 'prof1',
            name: 'Dr. Smith',
            email: 'smith@champlain.edu',
          },
          testingRequests: [
            {
              id: 'req1',
              title: 'Space Adventure v1.2',
              projectVersion: {
                versionNumber: '1.2.0',
                project: { title: 'Space Adventure' },
              },
            },
          ],
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const fetchSessionRegistrations = async (sessionId: string) => {
    try {
      // Mock data - replace with actual API call
      setRegistrations([
        {
          id: 'reg1',
          user: {
            id: 'user1',
            name: 'John Doe',
            email: 'john.doe@mymail.champlain.edu',
          },
          registrationType: 'tester',
          status: 'confirmed',
          registeredAt: '2024-12-15T10:30:00Z',
        },
      ]);
    } catch (error) {
      console.error('Error fetching registrations:', error);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'scheduled':
        return 'bg-blue-600/20 text-blue-400';
      case 'inProgress':
        return 'bg-green-600/20 text-green-400';
      case 'completed':
        return 'bg-gray-600/20 text-gray-400';
      case 'cancelled':
        return 'bg-red-600/20 text-red-400';
      default:
        return 'bg-gray-600/20 text-gray-400';
    }
  };

  const getRegistrationTypeColor = (type: string) => {
    switch (type) {
      case 'tester':
        return 'bg-green-600/20 text-green-400';
      case 'developer':
        return 'bg-blue-600/20 text-blue-400';
      case 'observer':
        return 'bg-purple-600/20 text-purple-400';
      default:
        return 'bg-gray-600/20 text-gray-400';
    }
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
          <h1 className="text-3xl font-bold text-white">Testing Sessions</h1>
          <p className="text-slate-400 mt-1">
            Manage testing sessions and track attendance
          </p>
        </div>
        <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
          <DialogTrigger asChild>
            <Button className="bg-blue-600 hover:bg-blue-700">
              <Plus className="h-4 w-4 mr-2" />
              Schedule Session
            </Button>
          </DialogTrigger>
          <DialogContent className="bg-slate-800 border-slate-700 text-white max-w-2xl">
            <DialogHeader>
              <DialogTitle>Schedule New Testing Session</DialogTitle>
              <DialogDescription className="text-slate-400">
                Create a new testing session for students to participate in
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="sessionName">Session Name</Label>
                  <Input
                    id="sessionName"
                    placeholder="e.g., Block 3 Testing Session"
                    className="bg-slate-700 border-slate-600"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="location">Location</Label>
                  <Select>
                    <SelectTrigger className="bg-slate-700 border-slate-600">
                      <SelectValue placeholder="Select room" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="room-a204">Room A-204 (40 capacity)</SelectItem>
                      <SelectItem value="room-b105">Room B-105 (32 capacity)</SelectItem>
                      <SelectItem value="online">Online Session</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="grid grid-cols-3 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="sessionDate">Date</Label>
                  <Input
                    id="sessionDate"
                    type="date"
                    className="bg-slate-700 border-slate-600"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="startTime">Start Time</Label>
                  <Input
                    id="startTime"
                    type="time"
                    className="bg-slate-700 border-slate-600"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="endTime">End Time</Label>
                  <Input
                    id="endTime"
                    type="time"
                    className="bg-slate-700 border-slate-600"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="maxTesters">Maximum Testers</Label>
                <Select>
                  <SelectTrigger className="bg-slate-700 border-slate-600">
                    <SelectValue placeholder="Select capacity" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="20">20 testers</SelectItem>
                    <SelectItem value="30">30 testers</SelectItem>
                    <SelectItem value="40">40 testers (full room)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setShowCreateDialog(false)}>
                Cancel
              </Button>
              <Button className="bg-blue-600 hover:bg-blue-700">
                Schedule Session
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      {/* Sessions List */}
      <div className="grid gap-4">
        {sessions.map((session) => (
          <Card key={session.id} className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
            <CardHeader>
              <div className="flex justify-between items-start">
                <div>
                  <CardTitle className="text-white flex items-center gap-3">
                    <Calendar className="h-5 w-5" />
                    {session.sessionName}
                    <Badge variant="secondary" className={getStatusColor(session.status)}>
                      {session.status}
                    </Badge>
                  </CardTitle>
                  <CardDescription className="text-slate-400 flex items-center gap-4 mt-2">
                    <span className="flex items-center gap-1">
                      <MapPin className="h-4 w-4" />
                      {session.location.name}
                    </span>
                    <span className="flex items-center gap-1">
                      <Clock className="h-4 w-4" />
                      {new Date(session.sessionDate).toLocaleDateString()} • {session.startTime} - {session.endTime}
                    </span>
                    <span className="flex items-center gap-1">
                      <Users className="h-4 w-4" />
                      {session.registeredTesterCount}/{session.maxTesters} registered
                    </span>
                  </CardDescription>
                </div>
                <div className="flex gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      setSelectedSession(session);
                      fetchSessionRegistrations(session.id);
                    }}
                  >
                    <UserCheck className="h-4 w-4 mr-2" />
                    Manage
                  </Button>
                  <Button size="sm" variant="outline">
                    <Edit className="h-4 w-4 mr-2" />
                    Edit
                  </Button>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <div className="flex justify-between items-center text-sm">
                  <span className="text-slate-300">Manager: {session.manager.name}</span>
                  <span className="text-slate-400">{session.registeredProjectCount} projects scheduled</span>
                </div>
                
                {session.testingRequests.length > 0 && (
                  <div>
                    <p className="text-sm text-slate-300 mb-2">Testing Requests:</p>
                    <div className="flex flex-wrap gap-2">
                      {session.testingRequests.map((request) => (
                        <Badge key={request.id} variant="outline" className="text-slate-300">
                          {request.title} v{request.projectVersion.versionNumber}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Session Detail Dialog */}
      {selectedSession && (
        <Dialog open={!!selectedSession} onOpenChange={() => setSelectedSession(null)}>
          <DialogContent className="bg-slate-800 border-slate-700 text-white max-w-4xl max-h-[80vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <Calendar className="h-5 w-5" />
                {selectedSession.sessionName} - Registrations
              </DialogTitle>
              <DialogDescription className="text-slate-400">
                Manage participant registrations and attendance
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-4">
              {/* Registration Summary */}
              <div className="grid grid-cols-3 gap-4">
                <Card className="bg-slate-700/50 border-slate-600">
                  <CardContent className="p-4">
                    <div className="text-center">
                      <div className="text-2xl font-bold text-white">{selectedSession.registeredTesterCount}</div>
                      <div className="text-sm text-slate-400">Testers</div>
                    </div>
                  </CardContent>
                </Card>
                <Card className="bg-slate-700/50 border-slate-600">
                  <CardContent className="p-4">
                    <div className="text-center">
                      <div className="text-2xl font-bold text-white">{selectedSession.registeredProjectMemberCount}</div>
                      <div className="text-sm text-slate-400">Developers</div>
                    </div>
                  </CardContent>
                </Card>
                <Card className="bg-slate-700/50 border-slate-600">
                  <CardContent className="p-4">
                    <div className="text-center">
                      <div className="text-2xl font-bold text-white">{selectedSession.registeredProjectCount}</div>
                      <div className="text-sm text-slate-400">Projects</div>
                    </div>
                  </CardContent>
                </Card>
              </div>

              {/* Registrations Table */}
              <div className="border border-slate-700 rounded-lg">
                <Table>
                  <TableHeader>
                    <TableRow className="border-slate-700">
                      <TableHead className="text-slate-300">Participant</TableHead>
                      <TableHead className="text-slate-300">Email</TableHead>
                      <TableHead className="text-slate-300">Type</TableHead>
                      <TableHead className="text-slate-300">Status</TableHead>
                      <TableHead className="text-slate-300">Registered</TableHead>
                      <TableHead className="text-slate-300">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {registrations.map((registration) => (
                      <TableRow key={registration.id} className="border-slate-700">
                        <TableCell className="text-white">{registration.user.name}</TableCell>
                        <TableCell className="text-slate-300">{registration.user.email}</TableCell>
                        <TableCell>
                          <Badge className={getRegistrationTypeColor(registration.registrationType)}>
                            {registration.registrationType}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <Badge className={registration.status === 'confirmed' ? 'bg-green-600/20 text-green-400' : 'bg-yellow-600/20 text-yellow-400'}>
                            {registration.status}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-slate-400">
                          {new Date(registration.registeredAt).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-1">
                            {registration.status === 'pending' && (
                              <>
                                <Button size="sm" variant="outline" className="h-8 w-8 p-0">
                                  <CheckCircle className="h-4 w-4 text-green-500" />
                                </Button>
                                <Button size="sm" variant="outline" className="h-8 w-8 p-0">
                                  <XCircle className="h-4 w-4 text-red-500" />
                                </Button>
                              </>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => setSelectedSession(null)}>
                Close
              </Button>
              <Button className="bg-blue-600 hover:bg-blue-700">
                Export Attendance
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
