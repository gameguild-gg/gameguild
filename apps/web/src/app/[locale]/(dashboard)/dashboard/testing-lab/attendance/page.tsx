'use client';

import { useState, useEffect } from 'react';
import { useSession } from 'next-auth/react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { BarChart3, Download, Filter, Users, Calendar, CheckCircle, XCircle, Clock } from 'lucide-react';
import { testingLabApi, StudentAttendanceData, SessionAttendanceData } from '@/lib/api/testing-lab/testing-lab-api';
import Link from 'next/link';

interface StudentAttendance {
  id: string;
  name: string;
  email: string;
  team: string;
  block1Sessions: number;
  block2Sessions: number;
  block3Sessions: number;
  block4Sessions: number;
  totalSessions: number;
  totalGamesRequested: number;
  totalGamesTested: number;
  averageRating: number;
  status: 'onTrack' | 'atRisk' | 'failing';
}

interface SessionReport {
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

export default function AttendanceReportsPage() {
  const { data: session } = useSession();
  const [studentData, setStudentData] = useState<StudentAttendance[]>([]);
  const [sessionData, setSessionData] = useState<SessionReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterBlock, setFilterBlock] = useState<string>('all');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    fetchAttendanceData();
  }, []);

  const fetchAttendanceData = async () => {
    try {
      setLoading(true);
      
      // For now, use mock data. In production, these would be API calls
      setStudentData([
        {
          id: '1',
          name: 'John Developer',
          email: 'john.dev@mymail.champlain.edu',
          team: 'fa23-capstone-2023-24-t01',
          block1Sessions: 2,
          block2Sessions: 1,
          block3Sessions: 0,
          block4Sessions: 0,
          totalSessions: 3,
          totalGamesRequested: 2,
          totalGamesTested: 8,
          averageRating: 4.2,
          status: 'onTrack',
        },
        {
          id: '2',
          name: 'Jane Smith',
          email: 'jane.smith@mymail.champlain.edu',
          team: 'fa23-capstone-2023-24-t02',
          block1Sessions: 1,
          block2Sessions: 1,
          block3Sessions: 0,
          block4Sessions: 0,
          totalSessions: 2,
          totalGamesRequested: 1,
          totalGamesTested: 4,
          averageRating: 3.8,
          status: 'atRisk',
        },
        {
          id: '3',
          name: 'Mike Johnson',
          email: 'mike.j@mymail.champlain.edu',
          team: 'fa23-capstone-2023-24-t03',
          block1Sessions: 0,
          block2Sessions: 1,
          block3Sessions: 0,
          block4Sessions: 0,
          totalSessions: 1,
          totalGamesRequested: 0,
          totalGamesTested: 2,
          averageRating: 0,
          status: 'failing',
        },
      ]);

      setSessionData([
        {
          id: 's1',
          sessionName: 'Block 1 Testing Session',
          date: '2024-01-18',
          location: 'Room A101',
          totalCapacity: 16,
          studentsRegistered: 12,
          studentsAttended: 10,
          attendanceRate: 83.3,
          gamesTested: 8,
        },
        {
          id: 's2',
          sessionName: 'Block 1 Testing Session #2',
          date: '2024-01-25',
          location: 'Room A101 & B202',
          totalCapacity: 32,
          studentsRegistered: 24,
          studentsAttended: 22,
          attendanceRate: 91.7,
          gamesTested: 12,
        },
      ]);
    } catch (error) {
      console.error('Error fetching attendance data:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'onTrack':
        return 'bg-green-100 text-green-800';
      case 'atRisk':
        return 'bg-yellow-100 text-yellow-800';
      case 'failing':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'onTrack':
        return <CheckCircle className="h-4 w-4 text-green-600" />;
      case 'atRisk':
        return <Clock className="h-4 w-4 text-yellow-600" />;
      case 'failing':
        return <XCircle className="h-4 w-4 text-red-600" />;
      default:
        return <Clock className="h-4 w-4 text-gray-600" />;
    }
  };

  const filteredStudents = studentData.filter(student => {
    const matchesSearch = student.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         student.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         student.team.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = filterStatus === 'all' || student.status === filterStatus;
    return matchesSearch && matchesStatus;
  });

  const exportToCSV = () => {
    const headers = ['Name', 'Email', 'Team', 'Block 1', 'Block 2', 'Block 3', 'Block 4', 'Total Sessions', 'Games Tested', 'Status'];
    const csvContent = [
      headers.join(','),
      ...filteredStudents.map(student => [
        student.name,
        student.email,
        student.team,
        student.block1Sessions,
        student.block2Sessions,
        student.block3Sessions,
        student.block4Sessions,
        student.totalSessions,
        student.totalGamesTested,
        student.status,
      ].join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `testing-lab-attendance-${new Date().toISOString().split('T')[0]}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  };

  if (loading) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center">Loading attendance reports...</div>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-3xl font-bold mb-2">Testing Lab Attendance Reports</h1>
          <p className="text-muted-foreground">
            Track student participation and attendance for grading purposes
          </p>
        </div>
        
        <div className="flex gap-2">
          <Button onClick={exportToCSV} variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export CSV
          </Button>
          <Link href="/dashboard/testing-lab">
            <Button variant="ghost">
              Back to Testing Lab
            </Button>
          </Link>
        </div>
      </div>

      <Tabs defaultValue="students" className="space-y-6">
        <TabsList>
          <TabsTrigger value="students">Student Attendance</TabsTrigger>
          <TabsTrigger value="sessions">Session Reports</TabsTrigger>
          <TabsTrigger value="summary">Semester Summary</TabsTrigger>
        </TabsList>

        <TabsContent value="students" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Users className="mr-2 h-5 w-5" />
                Student Attendance Tracking
              </CardTitle>
              <CardDescription>
                Monitor individual student progress across the 4 semester blocks
              </CardDescription>
            </CardHeader>
            <CardContent>
              {/* Filters */}
              <div className="flex gap-4 mb-6">
                <div className="flex-1">
                  <Input
                    placeholder="Search students..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="max-w-sm"
                  />
                </div>
                <Select value={filterStatus} onValueChange={setFilterStatus}>
                  <SelectTrigger className="w-48">
                    <SelectValue placeholder="Filter by status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Students</SelectItem>
                    <SelectItem value="onTrack">On Track</SelectItem>
                    <SelectItem value="atRisk">At Risk</SelectItem>
                    <SelectItem value="failing">Failing</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Student Table */}
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Student</TableHead>
                      <TableHead>Team</TableHead>
                      <TableHead className="text-center">Block 1</TableHead>
                      <TableHead className="text-center">Block 2</TableHead>
                      <TableHead className="text-center">Block 3</TableHead>
                      <TableHead className="text-center">Block 4</TableHead>
                      <TableHead className="text-center">Total</TableHead>
                      <TableHead className="text-center">Games Tested</TableHead>
                      <TableHead className="text-center">Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredStudents.map((student) => (
                      <TableRow key={student.id}>
                        <TableCell>
                          <div>
                            <div className="font-medium">{student.name}</div>
                            <div className="text-sm text-muted-foreground">{student.email}</div>
                          </div>
                        </TableCell>
                        <TableCell className="text-sm">{student.team}</TableCell>
                        <TableCell className="text-center">{student.block1Sessions}</TableCell>
                        <TableCell className="text-center">{student.block2Sessions}</TableCell>
                        <TableCell className="text-center">{student.block3Sessions}</TableCell>
                        <TableCell className="text-center">{student.block4Sessions}</TableCell>
                        <TableCell className="text-center font-medium">{student.totalSessions}</TableCell>
                        <TableCell className="text-center">{student.totalGamesTested}</TableCell>
                        <TableCell className="text-center">
                          <Badge className={getStatusColor(student.status)}>
                            <div className="flex items-center gap-1">
                              {getStatusIcon(student.status)}
                              {student.status}
                            </div>
                          </Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="sessions" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Calendar className="mr-2 h-5 w-5" />
                Testing Session Reports
              </CardTitle>
              <CardDescription>
                Track attendance and participation for each testing session
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Session</TableHead>
                      <TableHead>Date</TableHead>
                      <TableHead>Location</TableHead>
                      <TableHead className="text-center">Capacity</TableHead>
                      <TableHead className="text-center">Registered</TableHead>
                      <TableHead className="text-center">Attended</TableHead>
                      <TableHead className="text-center">Attendance Rate</TableHead>
                      <TableHead className="text-center">Games Tested</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {sessionData.map((session) => (
                      <TableRow key={session.id}>
                        <TableCell className="font-medium">{session.sessionName}</TableCell>
                        <TableCell>{new Date(session.date).toLocaleDateString()}</TableCell>
                        <TableCell>{session.location}</TableCell>
                        <TableCell className="text-center">{session.totalCapacity}</TableCell>
                        <TableCell className="text-center">{session.studentsRegistered}</TableCell>
                        <TableCell className="text-center">{session.studentsAttended}</TableCell>
                        <TableCell className="text-center">
                          <Badge className={session.attendanceRate >= 80 ? 'bg-green-100 text-green-800' : 
                                          session.attendanceRate >= 60 ? 'bg-yellow-100 text-yellow-800' : 
                                          'bg-red-100 text-red-800'}>
                            {session.attendanceRate.toFixed(1)}%
                          </Badge>
                        </TableCell>
                        <TableCell className="text-center">{session.gamesTested}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="summary" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Total Students</CardTitle>
                <Users className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{studentData.length}</div>
                <p className="text-xs text-muted-foreground">
                  Across all teams
                </p>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">On Track</CardTitle>
                <CheckCircle className="h-4 w-4 text-green-600" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-green-600">
                  {studentData.filter(s => s.status === 'onTrack').length}
                </div>
                <p className="text-xs text-muted-foreground">
                  Meeting requirements
                </p>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">At Risk</CardTitle>
                <Clock className="h-4 w-4 text-yellow-600" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-yellow-600">
                  {studentData.filter(s => s.status === 'atRisk').length}
                </div>
                <p className="text-xs text-muted-foreground">
                  Need attention
                </p>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Failing</CardTitle>
                <XCircle className="h-4 w-4 text-red-600" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-red-600">
                  {studentData.filter(s => s.status === 'failing').length}
                </div>
                <p className="text-xs text-muted-foreground">
                  Immediate intervention
                </p>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Semester Requirements Overview</CardTitle>
              <CardDescription>
                Student progress toward semester testing requirements
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="text-sm">
                  <h4 className="font-medium mb-2">Testing Requirements per Student:</h4>
                  <ul className="list-disc list-inside space-y-1 text-muted-foreground">
                    <li>Attend at least 1 testing session per block (4 blocks total)</li>
                    <li>Test a minimum of 8 games throughout the semester</li>
                    <li>Provide constructive feedback for each game tested</li>
                    <li>Submit at least 1 version of their team's game for testing</li>
                  </ul>
                </div>
                
                <div className="pt-4 border-t">
                  <h4 className="font-medium mb-2">Grade Integration Notes:</h4>
                  <p className="text-sm text-muted-foreground">
                    This attendance data can be exported as CSV and integrated into your course grading system. 
                    Students failing to meet the minimum requirements should receive targeted support to ensure 
                    they complete the testing lab component successfully.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
