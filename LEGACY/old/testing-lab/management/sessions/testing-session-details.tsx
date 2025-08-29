'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';
import { Separator } from '@/components/ui/separator';
import { getTestingSessionRegistrations, updateTestingSession } from '@/lib/admin/testing-lab/sessions/testing-sessions.actions';
import type { TestingSession } from '@/lib/api/generated/types.gen';
import { getTestingLabManagers, getTestingLocations } from '@/lib/api/testing-lab';
import { Calendar, Clock, ListChecks, Loader2, MapPin, Pencil, Save, TestTube, User, Users, X } from 'lucide-react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';
// NOTE: Textarea currently unused; remove import if not needed later.
// import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

interface TestingSessionDetailsProps {
  data: TestingSession;
}

export function TestingSessionDetails({ data: session }: TestingSessionDetailsProps) {
  const NONE_VALUE = '__none'; // internal sentinel for "no selection"
  const { data: userSession } = useSession();
  const [loading, setLoading] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [saving, setSaving] = useState(false);
  const [participants, setParticipants] = useState<{ id: string; userId?: string; notes?: string }[]>([]);
  const [participantsLoading, setParticipantsLoading] = useState(false);
  const [locations, setLocations] = useState<{ id: string; name?: string }[]>([]);
  const [managers, setManagers] = useState<{ id: string; firstName?: string; lastName?: string; email?: string }[]>([]);
  const [metaLoading, setMetaLoading] = useState(false);

  // Local editable state
  const [form, setForm] = useState({
    sessionName: session.sessionName || '',
    sessionDate: session.sessionDate ? new Date(session.sessionDate).toISOString().slice(0, 10) : '',
    startTime: session.startTime ? new Date(session.startTime).toISOString().slice(11, 16) : '',
    endTime: session.endTime ? new Date(session.endTime).toISOString().slice(11, 16) : '',
    maxTesters: session.maxTesters || 0,
    locationId: session.locationId || '',
    managerId: (session as any).managerId || (session as any).managerUserId || '',
  });

  useEffect(() => {
    // Load participants (registrations)
    const load = async () => {
      if (!session.id) return;
      setParticipantsLoading(true);
      try {
        const regs = await getTestingSessionRegistrations(session.id);
        setParticipants(regs.map(r => ({ id: r.id || crypto.randomUUID(), userId: (r as any).userId, notes: (r as any).notes })));
      } catch (e) {
        console.warn('Failed to load participants', e);
      } finally {
        setParticipantsLoading(false);
      }
    };
    load();
  }, [session.id]);

  useEffect(() => {
    // Load locations & managers for dropdowns
    const loadMeta = async () => {
      setMetaLoading(true);
      try {
        const [locs, mgrs] = await Promise.all([getTestingLocations(0, 100).catch(() => []), getTestingLabManagers().catch(() => [])]);
        setLocations(locs as any);
        setManagers(mgrs as any);
      } catch (e) {
        console.warn('Failed loading locations/managers', e);
      } finally {
        setMetaLoading(false);
      }
    };
    if (editMode) loadMeta();
  }, [editMode]);

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

  const handleEditToggle = () => {
    setEditMode(e => !e);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setForm(f => ({ ...f, [name]: name === 'maxTesters' ? Number(value) : value }));
  };

  const handleSave = async () => {
    if (!session.id) return;
    setSaving(true);
    try {
      await updateTestingSession(session.id, {
        sessionName: form.sessionName,
        sessionDate: form.sessionDate ? new Date(form.sessionDate).toISOString() : undefined,
        startTime: form.startTime ? new Date(`${form.sessionDate}T${form.startTime}:00Z`).toISOString() : undefined,
        endTime: form.endTime ? new Date(`${form.sessionDate}T${form.endTime}:00Z`).toISOString() : undefined,
        maxTesters: form.maxTesters,
        locationId: (form.locationId && form.locationId !== NONE_VALUE) ? form.locationId : undefined,
        managerId: (form.managerId && form.managerId !== NONE_VALUE) ? form.managerId : undefined,
      });
      toast.success('Session updated');
      setEditMode(false);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Failed to update');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="container mx-auto py-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          {editMode ? (
            <Input name="sessionName" value={form.sessionName} onChange={handleChange} className="text-3xl font-bold h-12" />
          ) : (
            <h1 className="text-3xl font-bold">{session.sessionName}</h1>
          )}
          <p className="text-muted-foreground mt-2">Testing Session Details & Management</p>
        </div>
        <div className="flex items-center gap-2">
          {getStatusBadge(session.status || 0)}
          <Badge variant="outline">
            <TestTube className="w-4 h-4 mr-1" />
            Testing Session
          </Badge>
          <Button variant={editMode ? 'destructive' : 'outline'} size="sm" onClick={handleEditToggle} className="ml-2">
            {editMode ? (
              <>
                <X className="w-4 h-4 mr-1" /> Cancel
              </>
            ) : (
              <>
                <Pencil className="w-4 h-4 mr-1" /> Edit
              </>
            )}
          </Button>
          {editMode && (
            <Button disabled={saving} size="sm" onClick={handleSave}>
              {saving ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : <Save className="w-4 h-4 mr-1" />}
              {saving ? 'Saving...' : 'Save'}
            </Button>
          )}
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
              {editMode ? (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-4">
                    <div className="flex flex-col gap-1">
                      <Label htmlFor="sessionDate">Date</Label>
                      <Input type="date" id="sessionDate" name="sessionDate" value={form.sessionDate} onChange={handleChange} />
                    </div>
                    <div className="flex gap-2">
                      <div className="flex-1 flex flex-col gap-1">
                        <Label htmlFor="startTime">Start Time</Label>
                        <Input type="time" id="startTime" name="startTime" value={form.startTime} onChange={handleChange} />
                      </div>
                      <div className="flex-1 flex flex-col gap-1">
                        <Label htmlFor="endTime">End Time</Label>
                        <Input type="time" id="endTime" name="endTime" value={form.endTime} onChange={handleChange} />
                      </div>
                    </div>
                  </div>
                  <div className="space-y-4">
                    <div className="flex flex-col gap-1">
                      <Label htmlFor="maxTesters">Max Testers</Label>
                      <Input type="number" id="maxTesters" name="maxTesters" value={form.maxTesters} onChange={handleChange} />
                    </div>
                    <div className="flex flex-col gap-1">
                      <Label htmlFor="locationId">Location</Label>
                      <Select value={form.locationId || NONE_VALUE} onValueChange={(v) => setForm(f => ({ ...f, locationId: v === NONE_VALUE ? '' : v }))}>
                        <SelectTrigger id="locationId">
                          <SelectValue placeholder={metaLoading ? 'Loading...' : 'Select location'} />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value={NONE_VALUE}>None</SelectItem>
                          {locations.map(l => (
                            <SelectItem key={l.id} value={l.id}>{l.name || l.id}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="flex flex-col gap-1">
                      <Label htmlFor="managerId">Manager</Label>
                      <Select value={form.managerId || NONE_VALUE} onValueChange={(v) => setForm(f => ({ ...f, managerId: v === NONE_VALUE ? '' : v }))}>
                        <SelectTrigger id="managerId">
                          <SelectValue placeholder={metaLoading ? 'Loading...' : 'Select manager'} />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value={NONE_VALUE}>None</SelectItem>
                          {managers.map(m => (
                            <SelectItem key={m.id} value={m.id}>{m.firstName || m.lastName ? `${m.firstName || ''} ${m.lastName || ''}`.trim() : (m.email || m.id)}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </div>
              ) : (
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
              )}
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

          {/* Participants List */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users className="w-5 h-5" /> Participants
              </CardTitle>
            </CardHeader>
            <CardContent>
              {participantsLoading ? (
                <div className="text-sm text-muted-foreground flex items-center gap-2"><Loader2 className="w-4 h-4 animate-spin" /> Loading participants...</div>
              ) : participants.length === 0 ? (
                <p className="text-sm text-muted-foreground">No participants registered yet.</p>
              ) : (
                <ul className="space-y-2">
                  {participants.map(p => (
                    <li key={p.id} className="flex items-center justify-between rounded border px-3 py-2 text-sm">
                      <span className="font-medium">{p.userId || 'Unknown User'}</span>
                      {p.notes && <span className="text-muted-foreground truncate max-w-[200px]">{p.notes}</span>}
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>

          {/* Related Testing Requests Placeholder (future enhancement) */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ListChecks className="w-5 h-5" /> Related Requests
              </CardTitle>
            </CardHeader>
            <CardContent>
              {session.testingRequestId ? (
                <div className="text-sm">Linked to request <Link href={`/dashboard/testing-lab/requests/${session.testingRequestId}`} className="underline">{session.testingRequestId}</Link></div>
              ) : (
                <p className="text-sm text-muted-foreground">No request linked.</p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
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
                  <span>{participants.length}/{session.maxTesters}</span>
                </div>
                <Progress value={session.maxTesters ? (participants.length / session.maxTesters) * 100 : 0} className="h-2" />
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
