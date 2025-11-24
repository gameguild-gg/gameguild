'use client';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { createTestingSessionAction, deleteTestingSessionAction } from '@/lib/admin/testing-lab/sessions/sessions.actions';
import { TestingLocation, TestingSession } from '@/lib/api/generated/types.gen';
import { TestingSessionCreateData, testingSessionCreateSchema } from '@/lib/schemas/testing-sessions.schema';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { TestingSessionsList } from './testing-sessions-list';

interface TestingSessionsManagementProps {
    initialSessions: TestingSession[];
    availableLocations: TestingLocation[];
}

function CreateSessionDialog({
    availableLocations,
    onSessionCreated
}: {
    availableLocations: TestingLocation[];
    onSessionCreated: (session: TestingSession) => void;
}) {
    const [open, setOpen] = useState(false);
    const [isCreating, setIsCreating] = useState(false);
    const [selectedLocation, setSelectedLocation] = useState<TestingLocation | null>(null);

    const form = useForm<TestingSessionCreateData>({
        resolver: zodResolver(testingSessionCreateSchema),
        defaultValues: {
            sessionName: '',
            sessionDate: '',
            startTime: '09:00',
            endTime: '17:00',
            maxTesters: 15,
            maxProjects: 4,
            locationId: '',
            managerUserId: '',
            status: 0, // Scheduled
        },
    });

    const handleLocationChange = (locationId: string) => {
        const location = availableLocations.find(l => l.id === locationId);
        setSelectedLocation(location || null);
        form.setValue('locationId', locationId);

        // Update max capacities based on location
        if (location) {
            form.setValue('maxTesters', Math.min(form.getValues('maxTesters'), location.maxTestersCapacity));
            form.setValue('maxProjects', Math.min(form.getValues('maxProjects'), location.maxProjectsCapacity));
        }
    };

    const onSubmit = async (data: TestingSessionCreateData) => {
        setIsCreating(true);

        try {
            const result = await createTestingSessionAction(data);

            if (result.success && result.data) {
                toast.success('Testing session created successfully');
                onSessionCreated(result.data);
                setOpen(false);
                form.reset();
                setSelectedLocation(null);
            } else {
                toast.error(result.error || 'Failed to create session');
            }
        } catch (error) {
            console.error('Error creating session:', error);
            toast.error('Failed to create session');
        } finally {
            setIsCreating(false);
        }
    };

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button>
                    <Plus className="h-4 w-4 mr-2" />
                    Create Session
                </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Create New Testing Session</DialogTitle>
                    <DialogDescription>
                        Create a new testing session that groups multiple testing requests together.
                        Sessions have shared capacity limits based on the selected location.
                    </DialogDescription>
                </DialogHeader>

                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="sessionName">Session Name</Label>
                            <Input
                                id="sessionName"
                                {...form.register('sessionName')}
                                placeholder="e.g., VR Gaming Session - Week 1"
                            />
                            {form.formState.errors.sessionName && (
                                <p className="text-sm text-destructive">{form.formState.errors.sessionName.message}</p>
                            )}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="sessionDate">Session Date</Label>
                            <Input
                                id="sessionDate"
                                type="date"
                                {...form.register('sessionDate')}
                            />
                            {form.formState.errors.sessionDate && (
                                <p className="text-sm text-destructive">{form.formState.errors.sessionDate.message}</p>
                            )}
                        </div>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="startTime">Start Time</Label>
                            <Input
                                id="startTime"
                                type="time"
                                {...form.register('startTime')}
                            />
                            {form.formState.errors.startTime && (
                                <p className="text-sm text-destructive">{form.formState.errors.startTime.message}</p>
                            )}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="endTime">End Time</Label>
                            <Input
                                id="endTime"
                                type="time"
                                {...form.register('endTime')}
                            />
                            {form.formState.errors.endTime && (
                                <p className="text-sm text-destructive">{form.formState.errors.endTime.message}</p>
                            )}
                        </div>
                    </div>

                    <div className="space-y-2">
                        <Label htmlFor="location">Testing Location</Label>
                        <Select onValueChange={handleLocationChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Select a testing location" />
                            </SelectTrigger>
                            <SelectContent>
                                {availableLocations.map((location) => (
                                    <SelectItem key={location.id} value={location.id || ''}>
                                        {location.name} (Max: {location.maxTestersCapacity} testers, {location.maxProjectsCapacity} projects)
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        {form.formState.errors.locationId && (
                            <p className="text-sm text-destructive">{form.formState.errors.locationId.message}</p>
                        )}
                    </div>

                    {selectedLocation && (
                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="maxTesters">Max Testers</Label>
                                <Input
                                    id="maxTesters"
                                    type="number"
                                    min="1"
                                    max={selectedLocation.maxTestersCapacity}
                                    {...form.register('maxTesters', { valueAsNumber: true })}
                                />
                                <p className="text-xs text-muted-foreground">
                                    Location capacity: {selectedLocation.maxTestersCapacity} testers
                                </p>
                                {form.formState.errors.maxTesters && (
                                    <p className="text-sm text-destructive">{form.formState.errors.maxTesters.message}</p>
                                )}
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="maxProjects">Max Projects</Label>
                                <Input
                                    id="maxProjects"
                                    type="number"
                                    min="1"
                                    max={selectedLocation.maxProjectsCapacity}
                                    {...form.register('maxProjects', { valueAsNumber: true })}
                                />
                                <p className="text-xs text-muted-foreground">
                                    Location capacity: {selectedLocation.maxProjectsCapacity} projects
                                </p>
                                {form.formState.errors.maxProjects && (
                                    <p className="text-sm text-destructive">{form.formState.errors.maxProjects.message}</p>
                                )}
                            </div>
                        </div>
                    )}

                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={() => setOpen(false)}>
                            Cancel
                        </Button>
                        <Button
                            type="submit"
                            disabled={isCreating}
                        >
                            {isCreating ? 'Creating...' : 'Create Session'}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}

export function TestingSessionsManagement({
    initialSessions,
    availableLocations,
}: TestingSessionsManagementProps) {
    const [sessions, setSessions] = useState<TestingSession[]>(initialSessions);
    const [isDeleting, setIsDeleting] = useState<string | null>(null);

    const handleSessionCreated = (newSession: TestingSession) => {
        setSessions(prev => [newSession, ...prev]);
    };

    const handleDeleteSession = async (sessionId: string) => {
        if (!confirm('Are you sure you want to delete this testing session?')) {
            return;
        }

        setIsDeleting(sessionId);

        try {
            const result = await deleteTestingSessionAction(sessionId);

            if (result.success) {
                toast.success('Session deleted successfully');
                setSessions(prev => prev.filter(s => s.id !== sessionId));
            } else {
                toast.error(result.error || 'Failed to delete session');
            }
        } catch (error) {
            console.error('Error deleting session:', error);
            toast.error('Failed to delete session');
        } finally {
            setIsDeleting(null);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <p className="text-muted-foreground">
                        {sessions.length} session{sessions.length !== 1 ? 's' : ''} total
                    </p>
                </div>
                <CreateSessionDialog
                    availableLocations={availableLocations}
                    onSessionCreated={handleSessionCreated}
                />
            </div>

            {/* Sessions List */}
            <TestingSessionsList sessions={sessions} />
        </div>
    );
}
