'use client';

import { useState } from 'react';
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
  const [loading, setLoading] = useState(true);
  const [selectedSession, setSelectedSession] = useState<string>('all');
  const [selectedBlock, setSelectedBlock] = useState<string>('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [viewMode, setViewMode] = useState<'sessions' | 'students'>('sessions');

  useEffect(() => {
    fetchAttendanceData();
  }, [selectedSession, selectedBlock]);

  const fetchAttendanceData = async () => {
    try {
      setLoading(true);

      // Load attendance data from API
      try {
        const studentData = await testingLabApi.getStudentAttendanceReport();
        const sessionData = await testingLabApi.getSessionAttendanceReport();

        // Transform API data to component format
        setStudentProgress(
          studentData.map((student) => ({
            user: {
              id: student.id,
              name: student.name,
              email: student.email,
              studentId: student.team,
            },
            totalSessionsScheduled: student.totalSessions,
            totalSessionsAttended: student.totalSessions,
            attendanceRate: Math.round((student.totalSessions / Math.max(student.totalSessions, 1)) * 100),
            totalGamesTermd: student.gamesTested,
            totalFeedbackSubmitted: student.gamesTested,
            currentBlock: 3, // Default to current block
            isOnTrack: student.status === 'onTrack',
          })),
        );

        setSessionAttendance(
          sessionData.map((session) => ({
            id: session.id,
            sessionName: session.sessionName,
            date: session.date,
            location: session.location,
            totalCapacity: session.totalCapacity,
            studentsRegistered: session.studentsRegistered,
            studentsAttended: session.studentsAttended,
            attendanceRate: session.attendanceRate,
            gamesTested: session.gamesTested,
          })),
        );
      } catch (apiError) {
        console.error('API call failed, using mock data:', apiError);
        // Fallback to mock data
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
              sessionName: 'Block 3 Testing Session',
              sessionDate: '2024-12-15',
              startTime: '14:00',
              endTime: '16:00',
              location: { name: 'Room A-204' },
            },
            attendanceStatus: 'present',
            checkInTime: '2024-12-15T14:05:00Z',
            checkOutTime: '2024-12-15T15:55:00Z',
            gamesTestingCompleted: 3,
            feedbackSubmitted: 3,
          },
          {
            id: 'att2',
            user: {
              id: 'user2',
              name: 'Jane Smith',
              email: 'jane.smith@mymail.champlain.edu',
              studentId: 'STU002',
            },
            session: {
              id: 'session1',
              sessionName: 'Block 3 Testing Session',
              sessionDate: '2024-12-15',
              startTime: '14:00',
              endTime: '16:00',
              location: { name: 'Room A-204' },
            },
            attendanceStatus: 'late',
            checkInTime: '2024-12-15T14:25:00Z',
            checkOutTime: '2024-12-15T16:00:00Z',
            gamesTestingCompleted: 2,
            feedbackSubmitted: 2,
            notes: 'Arrived 25 minutes late due to previous class',
          },
        ];

        const mockProgress: StudentProgress[] = [
          {
            user: {
              id: 'user1',
              name: 'John Doe',
              email: 'john.doe@mymail.champlain.edu',
              studentId: 'STU001',
            },
            totalSessionsAttended: 8,
            totalSessionsScheduled: 10,
            totalGamesTermd: 24,
            totalFeedbackSubmitted: 24,
            currentBlock: 3,
            attendanceRate: 80,
            isOnTrack: true,
          },
          {
            user: {
              id: 'user2',
              name: 'Jane Smith',
              email: 'jane.smith@mymail.champlain.edu',
              studentId: 'STU002',
            },
            totalSessionsAttended: 6,
            totalSessionsScheduled: 10,
            totalGamesTermd: 18,
            totalFeedbackSubmitted: 16,
            currentBlock: 3,
            attendanceRate: 60,
            isOnTrack: false,
          },
        ];

        setAttendanceRecords(mockAttendance);
        setStudentProgress(mockProgress);

        // Calculate stats
        const totalSessions = 12;
        const totalAttendees = mockAttendance.filter((a) => a.attendanceStatus === 'present' || a.attendanceStatus === 'late').length;
        const averageAttendance = 75; // Mock calculation
        const onTrackStudents = mockProgress.filter((p) => p.isOnTrack).length;
        const totalStudents = mockProgress.length;

        setStats({
          totalSessions,
          totalAttendees,
          averageAttendance,
          onTrackStudents,
          totalStudents,
        });
      }
    } catch (error) {
      console.error('Error fetching attendance data:', error);
    } finally {
      setLoading(false);
    }
  };

  const getAttendanceStatusColor = (status: string) => {
    switch (status) {
      case 'present':
        return 'bg-green-600/20 text-green-400';
      case 'late':
        return 'bg-yellow-600/20 text-yellow-400';
      case 'absent':
        return 'bg-red-600/20 text-red-400';
      case 'excused':
        return 'bg-blue-600/20 text-blue-400';
      default:
        return 'bg-gray-600/20 text-gray-400';
    }
  };

  const exportAttendanceReport = () => {
    // Mock export functionality
    const csvContent = [
      [
        'Student Name',
        'Student ID',
        'Email',
        'Sessions Attended',
        'Sessions Scheduled',
        'Attendance Rate',
        'Games Tested',
        'Feedback Submitted',
        'On Track',
      ].join(','),
      ...studentProgress.map((p) =>
        [
          p.user.name,
          p.user.studentId || '',
          p.user.email,
          p.totalSessionsAttended,
          p.totalSessionsScheduled,
          `${p.attendanceRate}%`,
          p.totalGamesTermd,
          p.totalFeedbackSubmitted,
          p.isOnTrack ? 'Yes' : 'No',
        ].join(','),
      ),
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `attendance-report-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);
  };

  const filteredRecords = attendanceRecords.filter(
    (record) =>
      (selectedSession === 'all' || record.session.id === selectedSession) &&
      (searchTerm === '' ||
        record.user.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        record.user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
        record.user.studentId?.toLowerCase().includes(searchTerm.toLowerCase())),
  );

  const filteredProgress = studentProgress.filter(
    (progress) =>
      searchTerm === '' ||
      progress.user.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      progress.user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      progress.user.studentId?.toLowerCase().includes(searchTerm.toLowerCase()),
  );

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
          <h1 className="text-3xl font-bold text-white">Attendance Tracking</h1>
          <p className="text-slate-400 mt-1">Monitor student attendance and testing progress</p>
        </div>
        <div className="flex gap-3">
          <Button onClick={exportAttendanceReport} variant="outline">
            <FileDown className="h-4 w-4 mr-2" />
            Export Report
          </Button>
          <Select value={viewMode} onValueChange={(value: 'sessions' | 'students') => setViewMode(value)}>
            <SelectTrigger className="w-40 bg-slate-700 border-slate-600 text-white">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="sessions">Session View</SelectItem>
              <SelectItem value="students">Student Progress</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-white">{stats.totalSessions}</div>
              <div className="text-sm text-slate-400">Total Sessions</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-blue-400">{stats.averageAttendance}%</div>
              <div className="text-sm text-slate-400">Avg Attendance</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-400">{stats.onTrackStudents}</div>
              <div className="text-sm text-slate-400">On Track</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-white">{stats.totalStudents}</div>
              <div className="text-sm text-slate-400">Total Students</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-purple-400">4</div>
              <div className="text-sm text-slate-400">Blocks Total</div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex gap-4 items-center">
        <div className="flex-1">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 h-4 w-4" />
            <Input
              placeholder="Search students..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10 bg-slate-700 border-slate-600 text-white"
            />
          </div>
        </div>
        {viewMode === 'sessions' && (
          <>
            <Select value={selectedSession} onValueChange={setSelectedSession}>
              <SelectTrigger className="w-48 bg-slate-700 border-slate-600 text-white">
                <Filter className="h-4 w-4 mr-2" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Sessions</SelectItem>
                <SelectItem value="session1">Block 3 Testing Session</SelectItem>
                <SelectItem value="session2">Block 2 Testing Session</SelectItem>
              </SelectContent>
            </Select>
            <Select value={selectedBlock} onValueChange={setSelectedBlock}>
              <SelectTrigger className="w-32 bg-slate-700 border-slate-600 text-white">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Blocks</SelectItem>
                <SelectItem value="1">Block 1</SelectItem>
                <SelectItem value="2">Block 2</SelectItem>
                <SelectItem value="3">Block 3</SelectItem>
                <SelectItem value="4">Block 4</SelectItem>
              </SelectContent>
            </Select>
          </>
        )}
      </div>

      {/* Session View */}
      {viewMode === 'sessions' && (
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-white flex items-center gap-2">
              <Calendar className="h-5 w-5" />
              Session Attendance Records
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow className="border-slate-700">
                  <TableHead className="text-slate-300">Student</TableHead>
                  <TableHead className="text-slate-300">Session</TableHead>
                  <TableHead className="text-slate-300">Status</TableHead>
                  <TableHead className="text-slate-300">Check In/Out</TableHead>
                  <TableHead className="text-slate-300">Games Tested</TableHead>
                  <TableHead className="text-slate-300">Feedback</TableHead>
                  <TableHead className="text-slate-300">Notes</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredRecords.map((record) => (
                  <TableRow key={record.id} className="border-slate-700">
                    <TableCell className="text-white">
                      <div>
                        <div className="font-medium">{record.user.name}</div>
                        <div className="text-sm text-slate-400">{record.user.studentId}</div>
                      </div>
                    </TableCell>
                    <TableCell className="text-slate-300">
                      <div>
                        <div className="font-medium">{record.session.sessionName}</div>
                        <div className="text-sm text-slate-400">
                          {new Date(record.session.sessionDate).toLocaleDateString()} • {record.session.location.name}
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge className={getAttendanceStatusColor(record.attendanceStatus)}>{record.attendanceStatus}</Badge>
                    </TableCell>
                    <TableCell className="text-slate-300">
                      {record.checkInTime && record.checkOutTime ? (
                        <div className="text-sm">
                          <div>
                            In:{' '}
                            {new Date(record.checkInTime).toLocaleTimeString('en-US', {
                              hour: '2-digit',
                              minute: '2-digit',
                            })}
                          </div>
                          <div>
                            Out:{' '}
                            {new Date(record.checkOutTime).toLocaleTimeString('en-US', {
                              hour: '2-digit',
                              minute: '2-digit',
                            })}
                          </div>
                        </div>
                      ) : (
                        <span className="text-slate-500">-</span>
                      )}
                    </TableCell>
                    <TableCell className="text-white text-center">{record.gamesTestingCompleted}</TableCell>
                    <TableCell className="text-white text-center">{record.feedbackSubmitted}</TableCell>
                    <TableCell className="text-slate-400 text-sm max-w-48 truncate">{record.notes || '-'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* Student Progress View */}
      {viewMode === 'students' && (
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-white flex items-center gap-2">
              <Users className="h-5 w-5" />
              Student Progress Overview
            </CardTitle>
            <CardDescription className="text-slate-400">Track individual student progress and completion requirements</CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow className="border-slate-700">
                  <TableHead className="text-slate-300">Student</TableHead>
                  <TableHead className="text-slate-300">Attendance</TableHead>
                  <TableHead className="text-slate-300">Games Tested</TableHead>
                  <TableHead className="text-slate-300">Feedback</TableHead>
                  <TableHead className="text-slate-300">Current Block</TableHead>
                  <TableHead className="text-slate-300">Progress</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredProgress.map((progress) => (
                  <TableRow key={progress.user.id} className="border-slate-700">
                    <TableCell className="text-white">
                      <div>
                        <div className="font-medium">{progress.user.name}</div>
                        <div className="text-sm text-slate-400">{progress.user.studentId}</div>
                        <div className="text-xs text-slate-500">{progress.user.email}</div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="space-y-1">
                        <div className="text-white">
                          {progress.totalSessionsAttended}/{progress.totalSessionsScheduled}
                        </div>
                        <div className="text-sm">
                          <Badge className={progress.attendanceRate >= 75 ? 'bg-green-600/20 text-green-400' : 'bg-red-600/20 text-red-400'}>
                            {progress.attendanceRate}%
                          </Badge>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className="text-white text-center">
                      {progress.totalGamesTermd}
                      <div className="text-xs text-slate-400">
                        {progress.totalGamesTermd >= 8 * progress.currentBlock ? '✓' : `Need ${8 * progress.currentBlock - progress.totalGamesTermd} more`}
                      </div>
                    </TableCell>
                    <TableCell className="text-white text-center">
                      {progress.totalFeedbackSubmitted}
                      <div className="text-xs text-slate-400">{progress.totalFeedbackSubmitted >= progress.totalGamesTermd ? '✓' : 'Incomplete'}</div>
                    </TableCell>
                    <TableCell className="text-center">
                      <Badge variant="outline" className="text-slate-300">
                        Block {progress.currentBlock}/4
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {progress.isOnTrack ? (
                          <div className="flex items-center gap-1 text-green-400">
                            <CheckCircle className="h-4 w-4" />
                            <span className="text-sm">On Track</span>
                          </div>
                        ) : (
                          <div className="flex items-center gap-1 text-red-400">
                            <XCircle className="h-4 w-4" />
                            <span className="text-sm">Behind</span>
                          </div>
                        )}
                      </div>
                    </TableCell>
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
