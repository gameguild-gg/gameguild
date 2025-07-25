'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  BarChart3,
  Calendar,
  CalendarDays,
  CheckCircle,
  Clock,
  Download,
  Edit,
  ExternalLink,
  Eye,
  Gamepad2,
  MapPin,
  MoreHorizontal,
  Plus,
  RefreshCw,
  Timer,
  TrendingUp,
  Users,
  XCircle,
} from 'lucide-react';
import { SessionFilterControls } from './session-filter-controls';

interface TestingSession {
  id: string;
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  location?: {
    id: string;
    name: string;
    capacity: number;
  };
  maxTesters: number;
  registeredTesterCount: number;
  registeredProjectMemberCount: number;
  registeredProjectCount: number;
  status?: 'scheduled' | 'active' | 'completed' | 'cancelled';
  manager?: {
    id: string;
    name: string;
    email: string;
  };
  testingRequests?: {
    id: string;
    title: string;
    projectVersion: {
      versionNumber: string;
      project: {
        title: string;
      };
    };
  }[];
  description?: string;
  notes?: string;
  attendanceRate?: number;
  averageRating?: number;
  feedbackCount?: number;
}

interface TestingRequest {
  id?: string;
  title?: string;
  projectVersion?: {
    project?: {
      title?: string;
    };
  };
}

// Helper function to convert API SessionStatus (0,1,2,3) to frontend status strings

interface SessionRegistration {
  id: string;
  user: {
    id: string;
    name: string;
    email: string;
    avatar?: string;
  };
  registrationType: 'tester' | 'developer' | 'observer';
  status: 'pending' | 'confirmed' | 'cancelled' | 'attended' | 'no-show';
  registeredAt: string;
  attendedAt?: string;
  project?: {
    id: string;
    title: string;
  };
}

interface UserRole {
  type: 'student' | 'professor' | 'admin';
  isStudent: boolean;
  isProfessor: boolean;
  isAdmin: boolean;
}

interface EnhancedTestingSessionsListProps {
  initialSessions?: TestingSession[];
}

export function EnhancedTestingSessionsList({ initialSessions = [] }: EnhancedTestingSessionsListProps) {
  const { data: session } = useSession();
  const [sessions, setSessions] = useState<TestingSession[]>(Array.isArray(initialSessions) ? initialSessions : []);
  const [selectedSession, setSelectedSession] = useState<TestingSession | null>(null);
  const [registrations, setRegistrations] = useState<SessionRegistration[]>([]);
  const [loading, setLoading] = useState(false);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showDetailDialog, setShowDetailDialog] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string[]>([]);
  const [viewMode, setViewMode] = useState<'grid' | 'list' | 'table'>('grid');
  const [userRole, setUserRole] = useState<UserRole>({
    type: 'student',
    isStudent: true,
    isProfessor: false,
    isAdmin: false,
  });

  // Determine user role based on email domain
  useEffect(() => {
    if (session?.user?.email) {
      const email = session.user.email.toLowerCase();
      const isStudent = email.endsWith('@mymail.champlain.edu');
      const isProfessor = email.endsWith('@champlain.edu') && !email.endsWith('@mymail.champlain.edu');
      const isAdmin = isProfessor; // For now, professors are also admins

      // Override for development/testing - remove this in production
      const isDev = process.env.NODE_ENV === 'development';
      const devOverride = isDev; // Set to true to test as professor in dev

      const newUserRole = {
        type: (isAdmin || devOverride ? 'admin' : isProfessor || devOverride ? 'professor' : 'student') as 'admin' | 'professor' | 'student',
        isStudent: isStudent && !devOverride,
        isProfessor: isProfessor || devOverride,
        isAdmin: isAdmin || devOverride,
      };

      console.log('User role determined:', { email, newUserRole, isDev, devOverride });
      setUserRole(newUserRole);
    } else {
      // If no session, default to professor for testing
      if (process.env.NODE_ENV === 'development') {
        const testUserRole = {
          type: 'professor' as const,
          isStudent: false,
          isProfessor: true,
          isAdmin: true,
        };
        console.log('No session - using test user role:', testUserRole);
        setUserRole(testUserRole);
      }
    }
  }, [session]);

  // Set initial sessions when props change
  useEffect(() => {
    setSessions(initialSessions);
  }, [initialSessions]);

  const refreshSessions = async () => {
    setLoading(true);
    try {
      // Import the function dynamically to avoid build issues
      const { getTestingSessionsData } = await import('@/lib/testing-lab/testing-lab.actions');
      const sessionsData = await getTestingSessionsData();
      const apiSessions = sessionsData?.testingSessions || [];
      // Properly map the API response to match component interface
      setSessions(
        apiSessions.map((session: any) => ({
          id: session.id || '',
          sessionName: session.sessionName,
          sessionDate: session.sessionDate,
          startTime: session.startTime,
          endTime: session.endTime,
          location: session.location
            ? {
                id: session.location.id || '',
                name: session.location.name,
                capacity: session.location.capacity || 50,
              }
            : undefined,
          maxTesters: session.maxTesters,
          registeredTesterCount: session.registeredTesterCount || 0,
          registeredProjectMemberCount: session.registeredProjectMemberCount || 0,
          registeredProjectCount: session.registeredProjectCount || 0,
          status: typeof session.status === 'string' 
            ? session.status 
            : session.status === 0 ? 'scheduled'
            : session.status === 1 ? 'active'
            : session.status === 2 ? 'completed'
            : session.status === 3 ? 'cancelled'
            : 'scheduled',
          manager: session.manager
            ? {
                id: session.manager.id || '',
                name: session.manager.name,
                email: session.manager.email || '',
              }
            : undefined,
          testingRequests: session.testingRequests || session.testingRequest ? [session.testingRequest] : [],
          attendanceRate: session.attendanceRate || 85,
          averageRating: session.averageRating || 4.2,
        })),
      );
    } catch (error) {
      console.error('Error refreshing sessions:', error);
      // Don't reload the page, just keep the current sessions
      // window.location.reload();
    } finally {
      setLoading(false);
    }
  };

  const fetchSessionRegistrations = async () => {
    try {
      // Enhanced mock data with more realistic registrations
      setRegistrations([
        {
          id: 'reg1',
          user: {
            id: 'user1',
            name: 'John Doe',
            email: 'john.doe@mymail.champlain.edu',
            avatar: 'JD',
          },
          registrationType: 'tester',
          status: 'confirmed',
          registeredAt: '2024-12-15T10:30:00Z',
          project: {
            id: 'proj1',
            title: 'Space Adventure',
          },
        },
        {
          id: 'reg2',
          user: {
            id: 'user2',
            name: 'Sarah Wilson',
            email: 'sarah.wilson@mymail.champlain.edu',
            avatar: 'SW',
          },
          registrationType: 'developer',
          status: 'confirmed',
          registeredAt: '2024-12-14T14:20:00Z',
          project: {
            id: 'proj2',
            title: 'Puzzle Quest',
          },
        },
        {
          id: 'reg3',
          user: {
            id: 'user3',
            name: 'Mike Chen',
            email: 'mike.chen@mymail.champlain.edu',
            avatar: 'MC',
          },
          registrationType: 'tester',
          status: 'pending',
          registeredAt: '2024-12-16T09:15:00Z',
        },
      ]);
    } catch (error) {
      console.error('Error fetching registrations:', error);
    }
  };

  // Filter sessions based on search and status
  const filteredSessions = Array.isArray(sessions)
    ? sessions.filter((session) => {
        const matchesSearch =
          session.sessionName.toLowerCase().includes(searchTerm.toLowerCase()) ||
          session.location?.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
          session.manager?.name?.toLowerCase().includes(searchTerm.toLowerCase());
        const matchesStatus = statusFilter.length === 0 || (session.status && statusFilter.includes(session.status));
        return matchesSearch && matchesStatus;
      })
    : [];

  const getStatusColor = (status: string | null | undefined) => {
    switch (status) {
      case 'scheduled':
        return 'border-blue-500/20 text-blue-600 bg-blue-50 dark:bg-blue-950/20 dark:text-blue-400';
      case 'active':
        return 'border-green-500/20 text-green-600 bg-green-50 dark:bg-green-950/20 dark:text-green-400';
      case 'completed':
        return 'border-gray-500/20 text-gray-600 bg-gray-50 dark:bg-gray-950/20 dark:text-gray-400';
      case 'cancelled':
        return 'border-red-500/20 text-red-600 bg-red-50 dark:bg-red-950/20 dark:text-red-400';
      default:
        return 'border-gray-500/20 text-gray-600 bg-gray-50 dark:bg-gray-950/20 dark:text-gray-400';
    }
  };

  const getRegistrationTypeColor = (type: string) => {
    switch (type) {
      case 'tester':
        return 'border-green-500/20 text-green-600 bg-green-50 dark:bg-green-950/20 dark:text-green-400';
      case 'developer':
        return 'border-blue-500/20 text-blue-600 bg-blue-50 dark:bg-blue-950/20 dark:text-blue-400';
      case 'observer':
        return 'border-purple-500/20 text-purple-600 bg-purple-50 dark:bg-purple-950/20 dark:text-purple-400';
      default:
        return 'border-gray-500/20 text-gray-600 bg-gray-50 dark:bg-gray-950/20 dark:text-gray-400';
    }
  };

  const getRegistrationStatusColor = (status: string) => {
    switch (status) {
      case 'confirmed':
        return 'border-green-500/20 text-green-600 bg-green-50 dark:bg-green-950/20 dark:text-green-400';
      case 'pending':
        return 'border-yellow-500/20 text-yellow-600 bg-yellow-50 dark:bg-yellow-950/20 dark:text-yellow-400';
      case 'cancelled':
        return 'border-red-500/20 text-red-600 bg-red-50 dark:bg-red-950/20 dark:text-red-400';
      case 'attended':
        return 'border-emerald-500/20 text-emerald-600 bg-emerald-50 dark:bg-emerald-950/20 dark:text-emerald-400';
      case 'no-show':
        return 'border-orange-500/20 text-orange-600 bg-orange-50 dark:bg-orange-950/20 dark:text-orange-400';
      default:
        return 'border-gray-500/20 text-gray-600 bg-gray-50 dark:bg-gray-950/20 dark:text-gray-400';
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const formatTime = (timeString: string) => {
    const [hours, minutes] = timeString.split(':');
    const date = new Date();
    date.setHours(parseInt(hours), parseInt(minutes));
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  const getSessionProgress = (session: TestingSession) => {
    return Math.round((session.registeredTesterCount / session.maxTesters) * 100);
  };

  const isSessionFull = (session: TestingSession) => {
    return session.registeredTesterCount >= session.maxTesters;
  };

  const isUpcoming = (session: TestingSession) => {
    const sessionDateTime = new Date(`${session.sessionDate}T${session.startTime}`);
    return sessionDateTime > new Date() && session.status === 'scheduled';
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-400 mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading testing sessions...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-8">
        {/* Enhanced Header */}
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div>
            <h1 className="text-4xl font-bold">Testing Sessions</h1>
            <p className="text-muted-foreground mt-2 text-lg">
              {userRole.isStudent ? 'Find and join testing sessions for peer game projects' : 'Manage testing sessions and track participation'}
            </p>
          </div>

            <div className="flex flex-col sm:flex-row gap-4">
              {userRole.isProfessor && (
                <Dialog
                  open={showCreateDialog}
                  onOpenChange={(open) => {
                    console.log('Dialog state changing:', open);
                    setShowCreateDialog(open);
                  }}
                >
                  <DialogTrigger asChild>
                    <Button
                      onClick={() => {
                        console.log('Schedule Session button clicked');
                      }}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Schedule Session
                    </Button>
                  </DialogTrigger>
                  <CreateSessionDialog onClose={() => setShowCreateDialog(false)} onSave={refreshSessions} />
                </Dialog>
              )}

              {/* Always show button in development for testing */}
              {process.env.NODE_ENV === 'development' && !userRole.isProfessor && (
                <Dialog
                  open={showCreateDialog}
                  onOpenChange={(open) => {
                    console.log('Test Dialog state changing:', open);
                    setShowCreateDialog(open);
                  }}
                >
                  <DialogTrigger asChild>
                    <Button
                      variant="secondary"
                      onClick={() => {
                        console.log('Test Schedule Session button clicked');
                      }}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Test Create Session
                    </Button>
                  </DialogTrigger>
                  <CreateSessionDialog onClose={() => setShowCreateDialog(false)} onSave={refreshSessions} />
                </Dialog>
              )}

              {/* Debug info */}
              {process.env.NODE_ENV === 'development' && (
                <div className="text-xs text-muted-foreground bg-muted p-2 rounded">
                  User: {session?.user?.email || 'No session'} | Role: {userRole.type} | isProfessor: {userRole.isProfessor.toString()}
                </div>
              )}

              <Button variant="outline" onClick={() => refreshSessions()}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </div>
        </div>

          {/* Stats Overview */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">Total Sessions</p>
                    <p className="text-3xl font-bold">{Array.isArray(sessions) ? sessions.length : 0}</p>
                  </div>
                  <Calendar className="h-8 w-8 text-muted-foreground" />
                </div>
                <p className="text-xs text-muted-foreground mt-2">
                  {Array.isArray(sessions) ? sessions.filter((s) => s.status === 'scheduled').length : 0} scheduled
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">Active Participants</p>
                    <p className="text-3xl font-bold">
                      {Array.isArray(sessions) ? sessions.reduce((acc, s) => acc + s.registeredTesterCount, 0) : 0}
                    </p>
                  </div>
                  <Users className="h-8 w-8 text-muted-foreground" />
                </div>
                <p className="text-xs text-muted-foreground mt-2">Across all sessions</p>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">Projects Testing</p>
                    <p className="text-3xl font-bold">
                      {Array.isArray(sessions) ? sessions.reduce((acc, s) => acc + (s.testingRequests?.length || 0), 0) : 0}
                    </p>
                  </div>
                  <Gamepad2 className="h-8 w-8 text-muted-foreground" />
                </div>
                <p className="text-xs text-muted-foreground mt-2">Games being tested</p>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">Avg Attendance</p>
                    <p className="text-3xl font-bold">
                      {Array.isArray(sessions) && sessions.length > 0
                        ? Math.round(sessions.reduce((acc, s) => acc + (s.attendanceRate || 0), 0) / sessions.length)
                        : 0}
                      %
                    </p>
                  </div>
                  <TrendingUp className="h-8 w-8 text-muted-foreground" />
                </div>
                <p className="text-xs text-muted-foreground mt-2">Session participation</p>
              </CardContent>
            </Card>
          </div>

          {/* Enhanced Filters and Search */}
          <SessionFilterControls
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            onRefresh={refreshSessions}
            onAddSession={() => setShowCreateDialog(true)}
            showAddButton={userRole.isProfessor || process.env.NODE_ENV === 'development'}
          />

          {/* Sessions Display */}
          {filteredSessions.length === 0 ? (
            <Card>
              <CardContent className="p-12 text-center">
                <Calendar className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                <h3 className="text-xl font-semibold mb-2">No sessions found</h3>
                <p className="text-muted-foreground">
                  {searchTerm || statusFilter.length > 0 ? 'Try adjusting your search or filters' : 'No testing sessions have been scheduled yet'}
                </p>
              </CardContent>
            </Card>
          ) : viewMode === 'table' ? (
            <Card>
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Session</TableHead>
                      <TableHead>Date</TableHead>
                      <TableHead>Time</TableHead>
                      <TableHead>Location</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Participants</TableHead>
                      <TableHead>Manager</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredSessions.map((session) => (
                      <TableRow key={ session.id }>
                        <TableCell className="font-medium">
                          <div>
                            <div className="font-semibold">{session.sessionName}</div>
                            <div className="text-sm text-muted-foreground">
                              {session.testingRequests?.length || 0} project{(session.testingRequests?.length || 0) !== 1 ? 's' : ''}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Calendar className="h-4 w-4 text-muted-foreground"/>
                            <span>{formatDate(session.sessionDate)}</span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Clock className="h-4 w-4 text-muted-foreground"/>
                            <span>
                              {formatTime(session.startTime)} - {formatTime(session.endTime)}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <MapPin className="h-4 w-4 text-muted-foreground"/>
                            <div>
                              <div className="font-medium">{session.location?.name || 'Unknown Location'}</div>
                              <div
                                className="text-xs text-muted-foreground">Capacity: { session.location?.capacity || 0 }</div>
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className={getStatusColor(session.status)}>
                            {session.status ? session.status.charAt(0).toUpperCase() + session.status.slice(1) : 'Unknown'}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <div className="space-y-1">
                            <div className="flex items-center gap-2">
                              <Users className="h-4 w-4 text-muted-foreground"/>
                              <span className="font-medium">
                                {session.registeredTesterCount}/{session.maxTesters}
                              </span>
                            </div>
                            <div className="w-16 bg-muted rounded-full h-1">
                              <div
                                className={ `h-1 rounded-full ${ isSessionFull(session) ? 'bg-destructive' : 'bg-primary' }` }
                                style={{
                                  width: `${Math.min(getSessionProgress(session), 100)}%`,
                                }}
                              />
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div>
                            <div className="font-medium">{session.manager?.name || 'Unknown Manager'}</div>
                            <div
                              className="text-xs text-muted-foreground">{ session.manager?.email || 'No email' }</div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => {
                                setSelectedSession(session);
                                setShowDetailDialog(true);
                                fetchSessionRegistrations();
                              }}
                            >
                              <Eye className="h-4 w-4" />
                            </Button>
                            {userRole.isProfessor && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => {
                                  console.log('Edit session:', session.id);
                                }}
                              >
                                <Edit className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </Card>
          ) : (
            <div className={viewMode === 'grid' ? 'grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6' : 'space-y-4'}>
              {filteredSessions.map((session) => (
                <SessionCard
                  key={session.id}
                  session={session}
                  viewMode={viewMode}
                  userRole={userRole}
                  onViewDetails={(session) => {
                    setSelectedSession(session);
                    setShowDetailDialog(true);
                    fetchSessionRegistrations();
                  }}
                  onEdit={(session) => {
                    // Edit functionality
                    console.log('Edit session:', session.id);
                  }}
                  getStatusColor={getStatusColor}
                  formatDate={formatDate}
                  formatTime={formatTime}
                  getSessionProgress={getSessionProgress}
                  isSessionFull={isSessionFull}
                  isUpcoming={isUpcoming}
                />
              ))}
            </div>
          )}

          {/* Enhanced Session Detail Dialog */}
          {selectedSession && (
            <SessionDetailDialog
              session={selectedSession}
              registrations={registrations}
              open={showDetailDialog}
              onClose={() => {
                setShowDetailDialog(false);
                setSelectedSession(null);
              }}
              userRole={userRole}
              getRegistrationTypeColor={getRegistrationTypeColor}
              getRegistrationStatusColor={getRegistrationStatusColor}
            />
          )}
        </div>
      </div>
  );
}

// Session Card Component
interface SessionCardProps {
  session: TestingSession;
  viewMode: 'grid' | 'list' | 'table';
  userRole: UserRole;
  onViewDetails: (session: TestingSession) => void;
  onEdit: (session: TestingSession) => void;
  getStatusColor: (status: string | null | undefined) => string;
  formatDate: (date: string) => string;
  formatTime: (time: string) => string;
  getSessionProgress: (session: TestingSession) => number;
  isSessionFull: (session: TestingSession) => boolean;
  isUpcoming: (session: TestingSession) => boolean;
}

function SessionCard({
  session,
  viewMode,
  userRole,
  onViewDetails,
  onEdit,
  getStatusColor,
  formatDate,
  formatTime,
  getSessionProgress,
  isSessionFull,
  isUpcoming,
}: SessionCardProps) {
  if (viewMode === 'list') {
    return (
      <Card className="hover:bg-muted/50 transition-all duration-300">
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-6 flex-1">
              <div className="flex items-center gap-3">
                <div className="h-12 w-12 bg-primary rounded-lg flex items-center justify-center">
                  <Calendar className="h-6 w-6 text-primary-foreground"/>
                </div>
                <div>
                  <h3 className="text-lg font-semibold">{ session.sessionName }</h3>
                  <p className="text-muted-foreground text-sm">{ session.manager?.name || 'Unknown Manager' }</p>
                </div>
              </div>

              <div className="flex items-center gap-6 text-sm">
                <div className="flex items-center gap-2">
                  <CalendarDays className="h-4 w-4" />
                  {formatDate(session.sessionDate)}
                </div>
                <div className="flex items-center gap-2">
                  <Timer className="h-4 w-4" />
                  {formatTime(session.startTime)} - {formatTime(session.endTime)}
                </div>
                <div className="flex items-center gap-2">
                  <MapPin className="h-4 w-4" />
                  {session.location?.name || 'Unknown Location'}
                </div>
                <div className="flex items-center gap-2">
                  <Users className="h-4 w-4" />
                  {session.registeredTesterCount}/{session.maxTesters}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <Badge variant="outline" className={getStatusColor(session.status)}>
                {session.status || 'Unknown'}
              </Badge>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => onViewDetails(session)}>
                    <Eye className="h-4 w-4 mr-2" />
                    View Details
                  </DropdownMenuItem>
                  {userRole.isProfessor && (
                    <>
                      <DropdownMenuItem onClick={() => onEdit(session)}>
                        <Edit className="h-4 w-4 mr-2" />
                        Edit Session
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem>
                        <Download className="h-4 w-4 mr-2" />
                        Export Data
                      </DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="hover:shadow-md transition-all duration-300 group">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div
              className="h-10 w-10 bg-primary rounded-lg flex items-center justify-center group-hover:scale-110 transition-transform">
              <Calendar className="h-5 w-5 text-primary-foreground"/>
            </div>
            <div>
              <CardTitle className="text-lg leading-tight">{ session.sessionName }</CardTitle>
              <CardDescription className="text-sm">Managed
                by { session.manager?.name || 'Unknown Manager' }</CardDescription>
            </div>
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="h-8 w-8 p-0 opacity-0 group-hover:opacity-100 transition-opacity">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onViewDetails(session)}>
                <Eye className="h-4 w-4 mr-2" />
                View Details
              </DropdownMenuItem>
              {userRole.isProfessor && (
                <>
                  <DropdownMenuItem onClick={() => onEdit(session)}>
                    <Edit className="h-4 w-4 mr-2" />
                    Edit Session
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem>
                    <Download className="h-4 w-4 mr-2" />
                    Export Data
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Session Info */}
        <div className="space-y-3">
          <div className="flex items-center gap-2 text-sm">
            <CalendarDays className="h-4 w-4 text-muted-foreground"/>
            <span>{formatDate(session.sessionDate)}</span>
            <Badge variant="outline" className={getStatusColor(session.status)}>
              {session.status || 'Unknown'}
            </Badge>
          </div>

          <div className="flex items-center gap-2 text-sm">
            <Timer className="h-4 w-4 text-muted-foreground"/>
            <span>
              {formatTime(session.startTime)} - {formatTime(session.endTime)}
            </span>
          </div>

          <div className="flex items-center gap-2 text-sm">
            <MapPin className="h-4 w-4 text-muted-foreground"/>
            <span>{session.location?.name || 'Unknown Location'}</span>
            <span className="text-muted-foreground">({ session.location?.capacity || 0 } capacity)</span>
          </div>
        </div>

        {/* Registration Progress */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span>Registration</span>
            <span className="text-muted-foreground">
              {session.registeredTesterCount}/{session.maxTesters}
            </span>
          </div>
          <div className="w-full bg-muted rounded-full h-2">
            <div
              className={`h-2 rounded-full ${
                isSessionFull(session) ? 'bg-destructive' : 'bg-primary'
              }`}
              style={{ width: `${getSessionProgress(session)}%` }}
            />
          </div>
          { isSessionFull(session) && <p className="text-xs text-destructive">Session is full</p> }
        </div>

        {/* Testing Requests */}
        {(session.testingRequests?.length || 0) > 0 && (
          <div className="space-y-2">
            <p className="text-sm font-medium">Testing Projects:</p>
            <div className="flex flex-wrap gap-1">
              {session.testingRequests?.slice(0, 2).map((request) => (
                <Badge key={ request.id } variant="secondary" className="text-xs">
                  {request.title}
                </Badge>
              )) || []}
              {(session.testingRequests?.length || 0) > 2 && (
                <Badge variant="secondary" className="text-xs">
                  +{(session.testingRequests?.length || 0) - 2} more
                </Badge>
              )}
            </div>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex gap-2 pt-2">
          <Button
            size="sm"
            onClick={() => onViewDetails(session)}
            className="flex-1"
          >
            <Eye className="h-4 w-4 mr-2" />
            View Details
          </Button>

          {userRole.isStudent && isUpcoming(session) && !isSessionFull(session) && (
            <Button size="sm" variant="outline">
              Join
            </Button>
          )}
        </div>

        {/* Additional Info */}
        {session.attendanceRate && (
          <div className="flex items-center justify-between text-xs text-muted-foreground pt-2 border-t">
            <span>Avg Attendance: {session.attendanceRate}%</span>
            {session.averageRating && <span>Rating: {session.averageRating}/5</span>}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

// Create Session Dialog Component
interface CreateSessionDialogProps {
  onClose: () => void;
  onSave: () => void;
}

function CreateSessionDialog({ onClose, onSave }: CreateSessionDialogProps) {
  const [formData, setFormData] = useState({
    sessionName: '',
    locationId: '',
    sessionDate: '',
    startTime: '',
    endTime: '',
    description: '',
    maxTesters: '',
    sessionType: '',
    testingRequestId: '', // This should be selected from available testing requests
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [testingRequests, setTestingRequests] = useState<TestingRequest[]>([]);

  // Fetch testing requests when component mounts
  useEffect(() => {
    const fetchTestingRequests = async () => {
      try {
        const { getTestingRequestsData } = await import('@/lib/testing-lab/testing-lab.actions');
        const data = await getTestingRequestsData();
        setTestingRequests(data?.testingRequests || []);
      } catch (error) {
        console.error('Error fetching testing requests:', error);
      }
    };
    fetchTestingRequests();
  }, []);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.sessionName.trim()) {
      newErrors.sessionName = 'Session name is required';
    }
    if (!formData.sessionDate) {
      newErrors.sessionDate = 'Session date is required';
    }
    if (!formData.startTime) {
      newErrors.startTime = 'Start time is required';
    }
    if (!formData.endTime) {
      newErrors.endTime = 'End time is required';
    }
    if (!formData.maxTesters) {
      newErrors.maxTesters = 'Maximum testers is required';
    }
    if (!formData.testingRequestId) {
      newErrors.testingRequestId = 'Testing request is required';
    }

    // Validate that end time is after start time
    if (formData.startTime && formData.endTime && formData.startTime >= formData.endTime) {
      newErrors.endTime = 'End time must be after start time';
    }

    // Validate that session date is not in the past
    if (formData.sessionDate && new Date(formData.sessionDate) < new Date()) {
      newErrors.sessionDate = 'Session date cannot be in the past';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);
    try {
      // Import the createTestingSession function dynamically to avoid build issues
      const { createTestingSession } = await import('@/lib/testing-lab/testing-lab.actions');

      await createTestingSession({
        testingRequestId: formData.testingRequestId,
        locationId: formData.locationId || undefined,
        sessionName: formData.sessionName,
        sessionDate: formData.sessionDate,
        startTime: formData.startTime,
        endTime: formData.endTime,
        maxTesters: parseInt(formData.maxTesters),
      });

      // Close dialog first
      onClose();
      // Then refresh sessions
      try {
        await onSave();
      } catch (refreshError) {
        console.error('Error refreshing sessions after creation:', refreshError);
        // Session was created successfully, just refresh failed
      }
    } catch (error) {
      console.error('Error creating session:', error);
      setErrors({ general: error instanceof Error ? error.message : 'Failed to create session' });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={ true } onOpenChange={ onClose }>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl">Schedule New Testing Session</DialogTitle>
          <DialogDescription>Create a new testing session for students to participate in game
            testing</DialogDescription>
        </DialogHeader>

        { errors.general && (
          <div className="bg-destructive/15 border border-destructive/50 rounded-lg p-3 text-destructive text-sm">
            { errors.general }
          </div>
        ) }

        <div className="space-y-6">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="sessionName">
                Session Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="sessionName"
                placeholder="e.g., Block 3 Final Testing"
                value={ formData.sessionName }
                onChange={ (e) => setFormData({ ...formData, sessionName: e.target.value }) }
              />
              { errors.sessionName && <p className="text-destructive text-sm">{ errors.sessionName }</p> }
            </div>
            <div className="space-y-2">
              <Label htmlFor="testingRequest">
                Testing Request <span className="text-destructive">*</span>
              </Label>
            <Select value={formData.testingRequestId} onValueChange={(value) => setFormData({ ...formData, testingRequestId: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select testing request" />
              </SelectTrigger>
              <SelectContent>
                {testingRequests.length > 0 ? (
                  testingRequests.map((request) => (
                    <SelectItem key={request.id || `request-${Math.random()}`} value={request.id || ''}>
                      {request.title} - {request.projectVersion?.project?.title}
                    </SelectItem>
                  ))
                ) : (
                  <SelectItem value="no-requests" disabled>
                    No testing requests available
                  </SelectItem>
                )}
              </SelectContent>
            </Select>
              { errors.testingRequestId && <p className="text-destructive text-sm">{ errors.testingRequestId }</p> }
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="location">Location</Label>
          <Select value={formData.locationId} onValueChange={(value) => setFormData({ ...formData, locationId: value })}>
            <SelectTrigger>
              <SelectValue placeholder="Select room" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="room-a204">Room A-204 (40 capacity)</SelectItem>
              <SelectItem value="room-b105">Room B-105 (32 capacity)</SelectItem>
              <SelectItem value="room-c301">Room C-301 (25 capacity)</SelectItem>
              <SelectItem value="online">Online Session (50 capacity)</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div className="space-y-2">
            <Label htmlFor="sessionDate">
              Date <span className="text-destructive">*</span>
            </Label>
            <Input
              id="sessionDate"
              type="date"
              value={formData.sessionDate}
              onChange={(e) => setFormData({ ...formData, sessionDate: e.target.value })}
            />
            { errors.sessionDate && <p className="text-destructive text-sm">{ errors.sessionDate }</p> }
          </div>
          <div className="space-y-2">
            <Label htmlFor="startTime">
              Start Time <span className="text-destructive">*</span>
            </Label>
            <Input
              id="startTime"
              type="time"
              value={formData.startTime}
              onChange={(e) => setFormData({ ...formData, startTime: e.target.value })}
            />
            { errors.startTime && <p className="text-destructive text-sm">{ errors.startTime }</p> }
          </div>
          <div className="space-y-2">
            <Label htmlFor="endTime">
              End Time <span className="text-destructive">*</span>
            </Label>
            <Input
              id="endTime"
              type="time"
              value={formData.endTime}
              onChange={(e) => setFormData({ ...formData, endTime: e.target.value })}
            />
            { errors.endTime && <p className="text-destructive text-sm">{ errors.endTime }</p> }
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="description">Description</Label>
          <Textarea
            id="description"
            placeholder="Provide details about this testing session..."
            rows={3}
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="maxTesters">
              Maximum Testers <span className="text-destructive">*</span>
            </Label>
            <Select value={formData.maxTesters} onValueChange={(value) => setFormData({ ...formData, maxTesters: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select capacity" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="15">15 testers</SelectItem>
                <SelectItem value="20">20 testers</SelectItem>
                <SelectItem value="25">25 testers</SelectItem>
                <SelectItem value="30">30 testers</SelectItem>
                <SelectItem value="40">40 testers (full room)</SelectItem>
                <SelectItem value="50">50 testers (online)</SelectItem>
              </SelectContent>
            </Select>
            { errors.maxTesters && <p className="text-destructive text-sm">{ errors.maxTesters }</p> }
          </div>
          <div className="space-y-2">
            <Label htmlFor="sessionType">Session Type</Label>
            <Select value={formData.sessionType} onValueChange={(value) => setFormData({ ...formData, sessionType: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select type" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="regular">Regular Testing</SelectItem>
                <SelectItem value="final">Final Presentation</SelectItem>
                <SelectItem value="midterm">Midterm Review</SelectItem>
                <SelectItem value="playtesting">Playtesting Lab</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
        </div>

        <DialogFooter className="gap-2">
          <Button type="button" variant="outline" onClick={ onClose } disabled={ isSubmitting }>
            Cancel
          </Button>
            <Button type="button" onClick={ handleSubmit } disabled={ isSubmitting }>
              { isSubmitting ? (
                <>
                  <RefreshCw className="h-4 w-4 mr-2 animate-spin"/>
                  Creating...
                </>
              ) : (
                'Schedule Session'
              ) }
            </Button>
          </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// Session Detail Dialog Component
interface SessionDetailDialogProps {
  session: TestingSession;
  registrations: SessionRegistration[];
  open: boolean;
  onClose: () => void;
  userRole: UserRole;
  getRegistrationTypeColor: (type: string) => string;
  getRegistrationStatusColor: (status: string) => string;
}

function SessionDetailDialog({
  session,
  registrations,
  open,
  onClose,
  userRole,
  getRegistrationTypeColor,
  getRegistrationStatusColor,
}: SessionDetailDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-6xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-2xl flex items-center gap-3">
            <Calendar className="h-6 w-6 text-primary"/>
            {session.sessionName}
          </DialogTitle>
          <DialogDescription className="text-base">
            {session.description || 'Detailed view of testing session participants and metrics'}
          </DialogDescription>
        </DialogHeader>

        <Tabs defaultValue="overview" className="w-full">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="participants">Participants</TabsTrigger>
            <TabsTrigger value="projects">Projects</TabsTrigger>
            <TabsTrigger value="analytics">Analytics</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="space-y-6 mt-6">
            {/* Session Info Cards */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <Card>
                <CardContent className="p-4 text-center">
                  <Users className="h-8 w-8 text-primary mx-auto mb-2"/>
                  <div className="text-2xl font-bold">{ session.registeredTesterCount }</div>
                  <div className="text-sm text-muted-foreground">Testers</div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="p-4 text-center">
                  <Gamepad2 className="h-8 w-8 text-primary mx-auto mb-2"/>
                  <div className="text-2xl font-bold">{ session.registeredProjectMemberCount }</div>
                  <div className="text-sm text-muted-foreground">Developers</div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="p-4 text-center">
                  <BarChart3 className="h-8 w-8 text-primary mx-auto mb-2"/>
                  <div className="text-2xl font-bold">{ session.testingRequests?.length || 0 }</div>
                  <div className="text-sm text-muted-foreground">Projects</div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="p-4 text-center">
                  <TrendingUp className="h-8 w-8 text-primary mx-auto mb-2"/>
                  <div className="text-2xl font-bold">{ session.attendanceRate }%</div>
                  <div className="text-sm text-muted-foreground">Expected</div>
                </CardContent>
              </Card>
            </div>

            {/* Session Details */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Session Information</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex items-center gap-3">
                    <CalendarDays className="h-5 w-5 text-primary"/>
                    <div>
                      <p className="font-medium">
                        {new Date(session.sessionDate).toLocaleDateString('en-US', {
                          weekday: 'long',
                          year: 'numeric',
                          month: 'long',
                          day: 'numeric',
                        })}
                      </p>
                      <p className="text-muted-foreground text-sm">
                        {session.startTime} - {session.endTime}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <MapPin className="h-5 w-5 text-primary"/>
                    <div>
                      <p className="font-medium">{ session.location?.name || 'Unknown Location' }</p>
                      <p className="text-muted-foreground text-sm">Capacity: { session.location?.capacity || 0 }</p>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <Users className="h-5 w-5 text-primary"/>
                    <div>
                      <p className="font-medium">{ session.manager?.name || 'Unknown Manager' }</p>
                      <p className="text-muted-foreground text-sm">{ session.manager?.email || 'No email' }</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Registration Status</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <div className="flex justify-between text-sm">
                      <span>Capacity</span>
                      <span>
                        {session.registeredTesterCount}/{session.maxTesters}
                      </span>
                    </div>
                    <div className="w-full bg-muted rounded-full h-2">
                      <div
                        className="h-2 rounded-full bg-primary"
                        style={{ width: `${(session.registeredTesterCount / session.maxTesters) * 100}%` }}
                      />
                    </div>
                  </div>

                  <div className="pt-2 space-y-2">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Confirmed</span>
                      <span
                        className="text-green-600">{ registrations.filter((r) => r.status === 'confirmed').length }</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Pending</span>
                      <span
                        className="text-yellow-600">{ registrations.filter((r) => r.status === 'pending').length }</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          <TabsContent value="participants" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Registered Participants</CardTitle>
                <CardDescription>Manage participant registrations and attendance</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="rounded-md border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Participant</TableHead>
                        <TableHead>Email</TableHead>
                        <TableHead>Role</TableHead>
                        <TableHead>Project</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead>Registered</TableHead>
                        { userRole.isProfessor && <TableHead>Actions</TableHead> }
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {registrations.map((registration) => (
                        <TableRow key={ registration.id }>
                          <TableCell className="font-medium">
                            <div className="flex items-center gap-3">
                              <div
                                className="h-8 w-8 rounded-full bg-primary flex items-center justify-center text-primary-foreground text-sm font-medium">
                                {registration.user.avatar || registration.user.name.charAt(0)}
                              </div>
                              <span>{ registration.user.name }</span>
                            </div>
                          </TableCell>
                          <TableCell>{ registration.user.email }</TableCell>
                          <TableCell>
                            <Badge variant="outline" className={getRegistrationTypeColor(registration.registrationType)}>
                              {registration.registrationType}
                            </Badge>
                          </TableCell>
                          <TableCell>{ registration.project?.title || 'N/A' }</TableCell>
                          <TableCell>
                            <Badge variant="outline" className={getRegistrationStatusColor(registration.status)}>
                              {registration.status}
                            </Badge>
                          </TableCell>
                          <TableCell
                            className="text-muted-foreground">{ new Date(registration.registeredAt).toLocaleDateString() }</TableCell>
                          {userRole.isProfessor && (
                            <TableCell>
                              <div className="flex gap-1">
                                {registration.status === 'pending' && (
                                  <>
                                    <Button size="sm" variant="outline" className="h-8 w-8 p-0">
                                      <CheckCircle className="h-4 w-4 text-green-500"/>
                                    </Button>
                                    <Button size="sm" variant="outline" className="h-8 w-8 p-0">
                                      <XCircle className="h-4 w-4 text-red-500"/>
                                    </Button>
                                  </>
                                )}
                                <Button size="sm" variant="outline" className="h-8 w-8 p-0">
                                  <MoreHorizontal className="h-4 w-4" />
                                </Button>
                              </div>
                            </TableCell>
                          )}
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="projects" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Testing Projects</CardTitle>
                <CardDescription>Games and projects being tested in this session</CardDescription>
              </CardHeader>
              <CardContent>
                {(session.testingRequests?.length || 0) > 0 ? (
                  <div className="grid gap-4">
                    {session.testingRequests?.map((request) => (
                      <div key={ request.id } className="flex items-center justify-between p-4 border rounded-lg">
                        <div className="flex items-center gap-4">
                          <div className="h-12 w-12 bg-primary rounded-lg flex items-center justify-center">
                            <Gamepad2 className="h-6 w-6 text-primary-foreground"/>
                          </div>
                          <div>
                            <h4 className="font-medium">{ request.title }</h4>
                            <p className="text-muted-foreground text-sm">
                              Version {request.projectVersion.versionNumber} • {request.projectVersion.project.title}
                            </p>
                          </div>
                        </div>
                        <div className="flex gap-2">
                          <Button size="sm" variant="outline">
                            <ExternalLink className="h-4 w-4 mr-2" />
                            View Project
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <Gamepad2 className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                    <h3 className="font-medium mb-2">No projects assigned</h3>
                    <p className="text-muted-foreground">Projects will be assigned closer to the session date</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="analytics" className="mt-6">
            <div className="grid gap-6">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <Card>
                  <CardContent className="p-6 text-center">
                    <div className="text-3xl font-bold text-green-600 mb-2">{ session.attendanceRate }%</div>
                    <div className="text-muted-foreground">Expected Attendance</div>
                  </CardContent>
                </Card>

                <Card>
                  <CardContent className="p-6 text-center">
                    <div className="text-3xl font-bold text-blue-600 mb-2">{ session.averageRating }/5</div>
                    <div className="text-muted-foreground">Avg Session Rating</div>
                  </CardContent>
                </Card>

                <Card>
                  <CardContent className="p-6 text-center">
                    <div className="text-3xl font-bold text-purple-600 mb-2">{ session.feedbackCount }</div>
                    <div className="text-muted-foreground">Feedback Responses</div>
                  </CardContent>
                </Card>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle>Session Insights</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="flex items-center gap-3">
                      <TrendingUp className="h-5 w-5 text-green-500"/>
                      <span>High attendance rate expected based on historical data</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <Users className="h-5 w-5 text-blue-500"/>
                      <span>Optimal tester-to-project ratio achieved</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <BarChart3 className="h-5 w-5 text-purple-500"/>
                      <span>Session scheduled during peak availability window</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        </Tabs>

        <DialogFooter className="gap-2">
          <Button variant="outline" onClick={ onClose }>
            Close
          </Button>
          {userRole.isProfessor && (
            <>
              <Button variant="outline">
                <Download className="h-4 w-4 mr-2" />
                Export Data
              </Button>
              <Button>
                <Edit className="h-4 w-4 mr-2" />
                Edit Session
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
