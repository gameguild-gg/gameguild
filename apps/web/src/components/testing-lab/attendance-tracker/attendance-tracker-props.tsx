'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Calendar, CheckCircle, FileDown, Filter, Search, Users, XCircle } from 'lucide-react';

interface AttendanceRecord {
  id: string;
  user: {
    id: string;
    name: string;
    email: string;
    studentId?: string;
  };
  session: {
    id: string;
    sessionName: string;
    sessionDate: string;
    startTime: string;
    endTime: string;
    location: {
      name: string;
    };
  };
  attendanceStatus: 'present' | 'absent' | 'late' | 'excused';
  checkInTime?: string;
  checkOutTime?: string;
  gamesTestingCompleted: number;
  feedbackSubmitted: number;
  notes?: string;
}

interface StudentProgress {
  user: {
    id: string;
    name: string;
    email: string;
    studentId?: string;
  };
  totalSessionsAttended: number;
  totalSessionsScheduled: number;
  totalGamesTermd: number;
  totalFeedbackSubmitted: number;
  currentBlock: number;
  attendanceRate: number;
  isOnTrack: boolean;
}

interface SessionAttendance {
  id: string;
  sessionName: string;
  date: string;
  location: string;
  totalCapacity: number;
  studentsRegistered: number;
  studentsAttended: number;
  attendanceRate: number;
  gamesTested: number;
}

interface AttendanceStats {
  totalSessions: number;
  totalAttendees: number;
  averageAttendance: number;
  onTrackStudents: number;
  totalStudents: number;
}

interface AttendanceTrackerProps {
  studentData?: unknown[];
  sessionData?: unknown[];
  sessionInfo?: {
    id: string;
    sessionName: string;
  };
}

export function AttendanceTracker({ studentData = [], sessionData = [], sessionInfo }: AttendanceTrackerProps = {}) {
  const [attendanceRecords, setAttendanceRecords] = useState<AttendanceRecord[]>([]);
  const [studentProgress, setStudentProgress] = useState<StudentProgress[]>([]);
  const [sessionAttendance, setSessionAttendance] = useState<SessionAttendance[]>([]);
  const [stats, setStats] = useState<AttendanceStats>({
    totalSessions: 0,
    totalAttendees: 0,
    averageAttendance: 0,
    onTrackStudents: 0,
    totalStudents: 0,
  });
  const [loading, setLoading] = useState(false);
  const [selectedSession, setSelectedSession] = useState<string>('all');
  const [selectedBlock, setSelectedBlock] = useState<string>('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [viewMode, setViewMode] = useState<'sessions' | 'students'>('sessions');

  // Process the data from props
  useEffect(() => {
    try {
      setLoading(true);

      // Transform student data to component format
      const transformedStudentData = studentData.map((student: unknown) => {
        const studentRecord = student as {
          id: string;
          name: string;
          email: string;
          team: string;
          totalSessions: number;
          gamesTested: number;
          status: string;
        };

        return {
          user: {
            id: studentRecord.id,
            name: studentRecord.name,
            email: studentRecord.email,
            studentId: studentRecord.team,
          },
          totalSessionsScheduled: studentRecord.totalSessions,
          totalSessionsAttended: studentRecord.totalSessions,
          attendanceRate: Math.round((studentRecord.totalSessions / Math.max(studentRecord.totalSessions, 1)) * 100),
          totalGamesTermd: studentRecord.gamesTested,
          totalFeedbackSubmitted: studentRecord.gamesTested,
          currentBlock: 3, // Default to current block
          isOnTrack: studentRecord.status === 'onTrack',
        };
      });

      // Transform session data to component format
      const transformedSessionData = sessionData.map((session: unknown) => {
        const sessionRecord = session as {
          id: string;
          sessionName: string;
          date: string;
          location: string;
          totalCapacity: number;
          studentsRegistered: number;
          studentsAttended: number;
          attendanceRate: number;
          gamesTested: number;
        };

        return {
          id: sessionRecord.id,
          sessionName: sessionRecord.sessionName,
          date: sessionRecord.date,
          location: sessionRecord.location,
          totalCapacity: sessionRecord.totalCapacity,
          studentsRegistered: sessionRecord.studentsRegistered,
          studentsAttended: sessionRecord.studentsAttended,
          attendanceRate: sessionRecord.attendanceRate,
          gamesTested: sessionRecord.gamesTested,
        };
      });

      setStudentProgress(transformedStudentData);
      setSessionAttendance(transformedSessionData);

      // Create mock attendance records for display
      const mockAttendance: AttendanceRecord[] = [
        {
          id: 'att1',
          user: {
            id: 'user1',
            name: 'John Doe',
            email: 'john.doe@mymail.champlain.edu',
            studentId: 'STU001',
          },
          session: {
            id: 'session1',
            sessionName: sessionInfo?.sessionName || 'Testing Session',
            sessionDate: '2024-12-15',
            startTime: '14:00',
            endTime: '16:00',
            location: {
              name: 'Room 101',
            },
          },
          attendanceStatus: 'late',
          checkInTime: '2024-12-15T14:25:00Z',
          checkOutTime: '2024-12-15T16:00:00Z',
          gamesTestingCompleted: 2,
          feedbackSubmitted: 2,
          notes: 'Arrived 25 minutes late due to previous class',
        },
      ];

      setAttendanceRecords(mockAttendance);

      // Calculate stats
      const totalSessions = transformedSessionData.length || 12;
      const totalAttendees = mockAttendance.filter((a) => a.attendanceStatus === 'present' || a.attendanceStatus === 'late').length;
      const averageAttendance = transformedSessionData.length > 0 
        ? Math.round(transformedSessionData.reduce((sum, s) => sum + s.attendanceRate, 0) / transformedSessionData.length)
        : 75;
      const onTrackStudents = transformedStudentData.filter((p) => p.isOnTrack).length;
      const totalStudents = transformedStudentData.length;

      setStats({
        totalSessions,
        totalAttendees,
        averageAttendance,
        onTrackStudents,
        totalStudents,
      });
    } catch (error) {
      console.error('Error processing attendance data:', error);
    } finally {
      setLoading(false);
    }
  }, [studentData, sessionData, sessionInfo]);

  // Filter functions
  const filteredStudentProgress = studentProgress.filter((student) => {
    const matchesSearch = student.user.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         student.user.email.toLowerCase().includes(searchTerm.toLowerCase());
    return matchesSearch;
  });

  const filteredSessionAttendance = sessionAttendance.filter((session) => {
    const matchesSearch = session.sessionName.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesSession = selectedSession === 'all' || session.id === selectedSession;
    return matchesSearch && matchesSession;
  });

  const getAttendanceStatusColor = (status: string) => {
    switch (status) {
      case 'present':
        return 'bg-green-100 text-green-800';
      case 'absent':
        return 'bg-red-100 text-red-800';
      case 'late':
        return 'bg-yellow-100 text-yellow-800';
      case 'excused':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'present':
        return <CheckCircle className="h-4 w-4 text-green-500" />;
      case 'late':
        return <CheckCircle className="h-4 w-4 text-yellow-500" />;
      case 'absent':
        return <XCircle className="h-4 w-4 text-red-500" />;
      case 'excused':
        return <CheckCircle className="h-4 w-4 text-blue-500" />;
      default:
        return <XCircle className="h-4 w-4 text-gray-500" />;
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading attendance data...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Title */}
      <div className="flex flex-col space-y-2">
        <h1 className="text-3xl font-bold">Attendance Tracker</h1>
        {sessionInfo && (
          <p className="text-muted-foreground">
            Attendance data for session: {sessionInfo.sessionName}
          </p>
        )}
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Sessions</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.totalSessions}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Attendees</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.totalAttendees}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Average Attendance</CardTitle>
            <CheckCircle className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.averageAttendance}%</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">On Track Students</CardTitle>
            <CheckCircle className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.onTrackStudents}</div>
            <p className="text-xs text-muted-foreground">of {stats.totalStudents} total</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Export Data</CardTitle>
            <FileDown className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <Button variant="outline" size="sm" className="w-full">
              <FileDown className="h-4 w-4 mr-2" />
              Export
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Controls */}
      <div className="flex flex-col sm:flex-row gap-4">
        <div className="flex items-center space-x-2 flex-1">
          <Search className="h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search students or sessions..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="flex-1"
          />
        </div>
        
        <div className="flex gap-2">
          <Select value={selectedSession} onValueChange={setSelectedSession}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Filter by session" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Sessions</SelectItem>
              {sessionAttendance.map((session) => (
                <SelectItem key={session.id} value={session.id}>
                  {session.sessionName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={selectedBlock} onValueChange={setSelectedBlock}>
            <SelectTrigger className="w-[120px]">
              <SelectValue placeholder="Block" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Blocks</SelectItem>
              <SelectItem value="1">Block 1</SelectItem>
              <SelectItem value="2">Block 2</SelectItem>
              <SelectItem value="3">Block 3</SelectItem>
              <SelectItem value="4">Block 4</SelectItem>
            </SelectContent>
          </Select>

          <div className="flex border rounded-lg">
            <Button
              variant={viewMode === 'students' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('students')}
              className="rounded-r-none"
            >
              Students
            </Button>
            <Button
              variant={viewMode === 'sessions' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('sessions')}
              className="rounded-l-none"
            >
              Sessions
            </Button>
          </div>
        </div>
      </div>

      {/* Content based on view mode */}
      {viewMode === 'students' ? (
        <Card>
          <CardHeader>
            <CardTitle>Student Progress</CardTitle>
            <CardDescription>
              Track individual student attendance and testing progress
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Student</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Attendance Rate</TableHead>
                  <TableHead>Sessions</TableHead>
                  <TableHead>Games Tested</TableHead>
                  <TableHead>Feedback</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredStudentProgress.map((student) => (
                  <TableRow key={student.user.id}>
                    <TableCell className="font-medium">
                      <div>
                        <div>{student.user.name}</div>
                        {student.user.studentId && (
                          <div className="text-sm text-muted-foreground">
                            {student.user.studentId}
                          </div>
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {student.user.email}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <div className="w-20 bg-gray-200 rounded-full h-2">
                          <div
                            className={`h-2 rounded-full ${
                              student.attendanceRate >= 80
                                ? 'bg-green-500'
                                : student.attendanceRate >= 60
                                ? 'bg-yellow-500'
                                : 'bg-red-500'
                            }`}
                            style={{ width: `${student.attendanceRate}%` }}
                          />
                        </div>
                        <span className="text-sm">{student.attendanceRate}%</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      {student.totalSessionsAttended}/{student.totalSessionsScheduled}
                    </TableCell>
                    <TableCell>{student.totalGamesTermd}</TableCell>
                    <TableCell>{student.totalFeedbackSubmitted}</TableCell>
                    <TableCell>
                      <Badge
                        variant={student.isOnTrack ? 'default' : 'destructive'}
                      >
                        {student.isOnTrack ? 'On Track' : 'At Risk'}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>Session Attendance</CardTitle>
            <CardDescription>
              View attendance rates and statistics for each testing session
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Session</TableHead>
                  <TableHead>Date</TableHead>
                  <TableHead>Location</TableHead>
                  <TableHead>Capacity</TableHead>
                  <TableHead>Registered</TableHead>
                  <TableHead>Attended</TableHead>
                  <TableHead>Attendance Rate</TableHead>
                  <TableHead>Games Tested</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredSessionAttendance.map((session) => (
                  <TableRow key={session.id}>
                    <TableCell className="font-medium">
                      {session.sessionName}
                    </TableCell>
                    <TableCell>{session.date}</TableCell>
                    <TableCell>{session.location}</TableCell>
                    <TableCell>{session.totalCapacity}</TableCell>
                    <TableCell>{session.studentsRegistered}</TableCell>
                    <TableCell>{session.studentsAttended}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <div className="w-16 bg-gray-200 rounded-full h-2">
                          <div
                            className={`h-2 rounded-full ${
                              session.attendanceRate >= 80
                                ? 'bg-green-500'
                                : session.attendanceRate >= 60
                                ? 'bg-yellow-500'
                                : 'bg-red-500'
                            }`}
                            style={{ width: `${session.attendanceRate}%` }}
                          />
                        </div>
                        <span className="text-sm">{session.attendanceRate}%</span>
                      </div>
                    </TableCell>
                    <TableCell>{session.gamesTested}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
