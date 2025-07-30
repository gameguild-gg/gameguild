'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Calendar, Clock, Edit, Globe, MapPin, Monitor, Plus, Trash2, User, Users, Video } from 'lucide-react';
import { useCourseEditor } from '@/components/courses/editor/context/course-editor-provider';
import { useState } from 'react';
import { format } from 'date-fns';

const DELIVERY_MODES = [
  {
    value: 'online' as const,
    label: 'Online (On-Demand)',
    description: 'Self-paced online learning with immediate access',
    icon: Globe,
    color: 'text-blue-600',
    bgColor: 'bg-blue-50',
    borderColor: 'border-blue-200',
  },
  {
    value: 'live' as const,
    label: 'Live (Scheduled)',
    description: 'Live webinars or virtual sessions at specific times',
    icon: Video,
    color: 'text-green-600',
    bgColor: 'bg-green-50',
    borderColor: 'border-green-200',
  },
  {
    value: 'hybrid' as const,
    label: 'Hybrid',
    description: 'Combination of live sessions and self-paced content',
    icon: Monitor,
    color: 'text-purple-600',
    bgColor: 'bg-purple-50',
    borderColor: 'border-purple-200',
  },
  {
    value: 'offline' as const,
    label: 'Offline (In-Person)',
    description: 'Physical classroom or workshop sessions',
    icon: User,
    color: 'text-orange-600',
    bgColor: 'bg-orange-50',
    borderColor: 'border-orange-200',
  },
];

const TIMEZONES = ['UTC', 'America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles', 'Europe/London', 'Europe/Paris', 'Asia/Tokyo', 'Asia/Shanghai', 'Australia/Sydney'];

export default function CourseDeliveryPage() {
  const { state, setDeliveryMode, setAccessWindow, setEnrollmentWindow, addSession, updateSession, removeSession, setTimezone } = useCourseEditor();

  const [showAddSession, setShowAddSession] = useState(false);
  const [newSession, setNewSession] = useState({
    title: '',
    startDate: '',
    endDate: '',
    capacity: 20,
    location: {
      type: 'virtual' as 'virtual' | 'physical',
      address: '',
      meetingUrl: '',
    },
  });

  const selectedMode = DELIVERY_MODES.find((mode) => mode.value === state.delivery.mode);
  const requiresSessions = state.delivery.mode === 'live' || state.delivery.mode === 'offline' || state.delivery.mode === 'hybrid';

  const handleModeChange = (mode: typeof state.delivery.mode) => {
    setDeliveryMode(mode);
  };

  const handleAddSession = () => {
    if (newSession.title && newSession.startDate && newSession.endDate) {
      addSession({
        title: newSession.title,
        startDate: new Date(newSession.startDate),
        endDate: new Date(newSession.endDate),
        capacity: newSession.capacity,
        enrolled: 0,
        location: newSession.location,
      });

      // Reset form
      setNewSession({
        title: '',
        startDate: '',
        endDate: '',
        capacity: 20,
        location: {
          type: 'virtual',
          address: '',
          meetingUrl: '',
        },
      });
      setShowAddSession(false);
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Sticky Header */}
      <div className="sticky top-0 z-10 border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Delivery & Schedule</h1>
              <p className="text-sm text-muted-foreground">Configure how students access and experience your course</p>
            </div>
            <div className="flex items-center gap-2">
              <Badge variant="outline" className="gap-1">
                <Calendar className="h-3 w-3" />
                {state.delivery.sessions.length} Sessions
              </Badge>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto space-y-6">
          {/* Delivery Mode Selection */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">🚀 Course Delivery Mode</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {DELIVERY_MODES.map((mode) => {
                  const Icon = mode.icon;
                  const isSelected = state.delivery.mode === mode.value;

                  return (
                    <Card key={mode.value} className={`cursor-pointer transition-all ${isSelected ? `ring-2 ring-primary ${mode.bgColor} ${mode.borderColor}` : 'hover:shadow-md border-border'}`} onClick={() => handleModeChange(mode.value)}>
                      <CardContent className="p-4">
                        <div className="flex items-start gap-3">
                          <Icon className={`h-5 w-5 ${mode.color}`} />
                          <div className="flex-1">
                            <h3 className="font-semibold">{mode.label}</h3>
                            <p className="text-sm text-muted-foreground mt-1">{mode.description}</p>
                          </div>
                          {isSelected && (
                            <Badge variant="default" className="ml-auto">
                              Selected
                            </Badge>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          {/* Access Window (Online Mode) */}
          {state.delivery.mode === 'online' && (
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">⏰ Access Window</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Limit access period</Label>
                  <Switch
                    checked={!!state.delivery.accessWindow}
                    onCheckedChange={(checked) =>
                      setAccessWindow(
                        checked
                          ? {
                              startDate: undefined,
                              endDate: undefined,
                            }
                          : undefined,
                      )
                    }
                  />
                </div>

                {state.delivery.accessWindow && (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <Label htmlFor="access-start">Available From</Label>
                      <Input
                        id="access-start"
                        type="datetime-local"
                        value={state.delivery.accessWindow.startDate ? state.delivery.accessWindow.startDate.toISOString().slice(0, 16) : ''}
                        onChange={(e) =>
                          setAccessWindow({
                            ...state.delivery.accessWindow!,
                            startDate: e.target.value ? new Date(e.target.value) : undefined,
                          })
                        }
                      />
                    </div>
                    <div>
                      <Label htmlFor="access-end">Available Until</Label>
                      <Input
                        id="access-end"
                        type="datetime-local"
                        value={state.delivery.accessWindow.endDate ? state.delivery.accessWindow.endDate.toISOString().slice(0, 16) : ''}
                        onChange={(e) =>
                          setAccessWindow({
                            ...state.delivery.accessWindow!,
                            endDate: e.target.value ? new Date(e.target.value) : undefined,
                          })
                        }
                      />
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Enrollment Window */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">📝 Enrollment Window</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <Label className="text-sm">Set enrollment period</Label>
                <Switch
                  checked={!!state.enrollment.enrollmentWindow}
                  onCheckedChange={(checked) =>
                    setEnrollmentWindow(
                      checked
                        ? {
                            opensAt: undefined,
                            closesAt: undefined,
                          }
                        : undefined,
                    )
                  }
                />
              </div>

              {state.enrollment.enrollmentWindow && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <Label htmlFor="enrollment-start">Enrollment Opens</Label>
                    <Input
                      id="enrollment-start"
                      type="datetime-local"
                      value={state.enrollment.enrollmentWindow.opensAt ? state.enrollment.enrollmentWindow.opensAt.toISOString().slice(0, 16) : ''}
                      onChange={(e) =>
                        setEnrollmentWindow({
                          ...state.enrollment.enrollmentWindow!,
                          opensAt: e.target.value ? new Date(e.target.value) : undefined,
                        })
                      }
                    />
                  </div>
                  <div>
                    <Label htmlFor="enrollment-end">Enrollment Closes</Label>
                    <Input
                      id="enrollment-end"
                      type="datetime-local"
                      value={state.enrollment.enrollmentWindow.closesAt ? state.enrollment.enrollmentWindow.closesAt.toISOString().slice(0, 16) : ''}
                      onChange={(e) =>
                        setEnrollmentWindow({
                          ...state.enrollment.enrollmentWindow!,
                          closesAt: e.target.value ? new Date(e.target.value) : undefined,
                        })
                      }
                    />
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Sessions Management */}
          {requiresSessions && (
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center justify-between">
                  <span className="flex items-center gap-2">📅 Course Sessions</span>
                  <Button onClick={() => setShowAddSession(true)} className="gap-2">
                    <Plus className="h-4 w-4" />
                    Add Session
                  </Button>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Timezone */}
                <div className="flex items-center gap-4">
                  <Label className="text-sm font-medium">Timezone:</Label>
                  <Select value={state.delivery.timezone} onValueChange={setTimezone}>
                    <SelectTrigger className="w-48">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {TIMEZONES.map((tz) => (
                        <SelectItem key={tz} value={tz}>
                          {tz}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <Separator />

                {/* Add Session Form */}
                {showAddSession && (
                  <Card className="bg-muted/30 border-dashed">
                    <CardContent className="p-4 space-y-4">
                      <h4 className="font-semibold">New Session</h4>

                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="md:col-span-2">
                          <Label htmlFor="session-title">Session Title</Label>
                          <Input id="session-title" value={newSession.title} onChange={(e) => setNewSession({ ...newSession, title: e.target.value })} placeholder="e.g., Week 1: Introduction" />
                        </div>

                        <div>
                          <Label htmlFor="session-start">Start Date & Time</Label>
                          <Input id="session-start" type="datetime-local" value={newSession.startDate} onChange={(e) => setNewSession({ ...newSession, startDate: e.target.value })} />
                        </div>

                        <div>
                          <Label htmlFor="session-end">End Date & Time</Label>
                          <Input id="session-end" type="datetime-local" value={newSession.endDate} onChange={(e) => setNewSession({ ...newSession, endDate: e.target.value })} />
                        </div>

                        <div>
                          <Label htmlFor="session-capacity">Capacity</Label>
                          <Input
                            id="session-capacity"
                            type="number"
                            value={newSession.capacity}
                            onChange={(e) =>
                              setNewSession({
                                ...newSession,
                                capacity: parseInt(e.target.value) || 20,
                              })
                            }
                            min="1"
                            max="1000"
                          />
                        </div>

                        <div>
                          <Label>Location Type</Label>
                          <Select
                            value={newSession.location.type}
                            onValueChange={(value: 'virtual' | 'physical') =>
                              setNewSession({
                                ...newSession,
                                location: { ...newSession.location, type: value },
                              })
                            }
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="virtual">Virtual/Online</SelectItem>
                              <SelectItem value="physical">Physical Location</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>

                        {newSession.location.type === 'virtual' ? (
                          <div className="md:col-span-2">
                            <Label htmlFor="meeting-url">Meeting URL</Label>
                            <Input
                              id="meeting-url"
                              value={newSession.location.meetingUrl}
                              onChange={(e) =>
                                setNewSession({
                                  ...newSession,
                                  location: { ...newSession.location, meetingUrl: e.target.value },
                                })
                              }
                              placeholder="https://zoom.us/j/..."
                            />
                          </div>
                        ) : (
                          <div className="md:col-span-2">
                            <Label htmlFor="address">Address</Label>
                            <Input
                              id="address"
                              value={newSession.location.address}
                              onChange={(e) =>
                                setNewSession({
                                  ...newSession,
                                  location: { ...newSession.location, address: e.target.value },
                                })
                              }
                              placeholder="123 Main St, City, State"
                            />
                          </div>
                        )}
                      </div>

                      <div className="flex gap-2">
                        <Button onClick={handleAddSession} disabled={!newSession.title || !newSession.startDate || !newSession.endDate}>
                          Add Session
                        </Button>
                        <Button variant="outline" onClick={() => setShowAddSession(false)}>
                          Cancel
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Sessions List */}
                {state.delivery.sessions.length > 0 ? (
                  <div className="space-y-3">
                    {state.delivery.sessions.map((session) => (
                      <Card key={session.id} className="bg-background">
                        <CardContent className="p-4">
                          <div className="flex items-start justify-between">
                            <div className="flex-1">
                              <h4 className="font-semibold">{session.title}</h4>
                              <div className="flex items-center gap-4 mt-2 text-sm text-muted-foreground">
                                <div className="flex items-center gap-1">
                                  <Calendar className="h-3 w-3" />
                                  {format(session.startDate, 'MMM d, yyyy')}
                                </div>
                                <div className="flex items-center gap-1">
                                  <Clock className="h-3 w-3" />
                                  {format(session.startDate, 'h:mm a')} - {format(session.endDate, 'h:mm a')}
                                </div>
                                <div className="flex items-center gap-1">
                                  <Users className="h-3 w-3" />
                                  {session.enrolled}/{session.capacity}
                                </div>
                                {session.location && (
                                  <div className="flex items-center gap-1">
                                    <MapPin className="h-3 w-3" />
                                    {session.location.type === 'virtual' ? 'Virtual' : 'In-Person'}
                                  </div>
                                )}
                              </div>
                            </div>
                            <div className="flex gap-2">
                              <Button variant="ghost" size="sm">
                                <Edit className="h-3 w-3" />
                              </Button>
                              <Button variant="ghost" size="sm" onClick={() => removeSession(session.id)} className="text-destructive hover:text-destructive">
                                <Trash2 className="h-3 w-3" />
                              </Button>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Calendar className="h-12 w-12 mx-auto mb-4 opacity-50" />
                    <p>No sessions scheduled yet.</p>
                    <p className="text-sm">Add your first session to get started.</p>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Preview Section */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">👁️ Student Preview</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="p-6 bg-muted/30 rounded-lg border-2 border-dashed">
                <h3 className="font-semibold mb-2">How students will see this course:</h3>
                <div className="space-y-2 text-sm">
                  <p>
                    <strong>Delivery:</strong> {selectedMode?.label}
                  </p>
                  <p>
                    <strong>Format:</strong> {selectedMode?.description}
                  </p>
                  {state.delivery.sessions.length > 0 && (
                    <p>
                      <strong>Sessions:</strong> {state.delivery.sessions.length} scheduled sessions
                    </p>
                  )}
                  {state.enrollment.enrollmentWindow && (
                    <p>
                      <strong>Enrollment:</strong> {state.enrollment.enrollmentWindow.opensAt ? `Opens ${format(state.enrollment.enrollmentWindow.opensAt, 'MMM d, yyyy')}` : 'Open enrollment'}
                    </p>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
