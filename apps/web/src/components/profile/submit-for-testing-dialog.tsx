'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Carousel, CarouselContent, CarouselItem, CarouselNext, CarouselPrevious } from '@/components/ui/carousel';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Calendar, CalendarDays, Clock, TestTube, Users } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface Project {
    id: string;
    name: string;
    version: string;
    changelog?: Array<{
        version: string;
        date: Date;
        changes: string[];
    }>;
}

interface TestingSession {
    id: string;
    title: string;
    description: string;
    scheduledDate: Date;
    status: string;
    maxParticipants: number;
    currentParticipants: number;
}

interface SubmitForTestingDialogProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    project: Project;
    availableSessions: TestingSession[];
    onSubmit: (submissionData: {
        sessionId: string;
        projectVersion: string;
    }) => void;
}

interface TestingFormData {
    sessionId: string;
    projectVersion: string;
}

export function SubmitForTestingDialog({
    open,
    onOpenChange,
    project,
    availableSessions,
    onSubmit
}: SubmitForTestingDialogProps) {
    const [formData, setFormData] = useState<TestingFormData>({
        sessionId: '',
        projectVersion: project.version
    });

    const [searchDate, setSearchDate] = useState<string>('');

    // Filter and sort sessions
    const getFilteredSessions = () => {
        let filtered = availableSessions.filter(session =>
            session.currentParticipants < session.maxParticipants &&
            session.status === 'upcoming'
        );

        // Filter by date if search date is provided
        if (searchDate) {
            const searchDateObj = new Date(searchDate);
            filtered = filtered.filter(session => {
                const sessionDate = new Date(session.scheduledDate);
                return sessionDate.toDateString() === searchDateObj.toDateString();
            });
        }

        // Sort by date (nearest first)
        return filtered.sort((a, b) =>
            new Date(a.scheduledDate).getTime() - new Date(b.scheduledDate).getTime()
        );
    };

    // Set nearest session as default when dialog opens or sessions change
    useEffect(() => {
        if (open && !formData.sessionId) {
            const filteredSessions = getFilteredSessions();
            const firstSession = filteredSessions[0];
            if (firstSession) {
                setFormData(prev => ({ ...prev, sessionId: firstSession.id }));
            }
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [open, availableSessions, searchDate]);

    const handleSubmit = () => {
        if (!formData.sessionId) {
            toast.error('Please select a testing session');
            return;
        }

        const selectedSession = availableSessions.find(s => s.id === formData.sessionId);
        if (!selectedSession) {
            toast.error('Selected session not found');
            return;
        }

        onSubmit({
            sessionId: formData.sessionId,
            projectVersion: formData.projectVersion
        });

        // Reset form
        setFormData({
            sessionId: '',
            projectVersion: project.version
        });
    };

    const handleClose = () => {
        onOpenChange(false);
        // Reset form when closing
        setFormData({
            sessionId: '',
            projectVersion: project.version
        });
    };

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-4xl bg-slate-900 border-slate-700">
                <DialogHeader>
                    <DialogTitle className="text-white">Submit Testing Request</DialogTitle>
                    <DialogDescription>
                        Submit a request to join a testing session and get feedback from the community.
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-6">
                    {/* Project Version Selection */}
                    <div>
                        <Label className="text-slate-300 text-sm font-medium">Project Version</Label>
                        <Select
                            value={formData.projectVersion}
                            onValueChange={(value) => setFormData({ ...formData, projectVersion: value })}
                        >
                            <SelectTrigger className="bg-slate-800 border-slate-600 text-white">
                                <SelectValue placeholder="Select version to submit" />
                            </SelectTrigger>
                            <SelectContent className="bg-slate-800 border-slate-700">
                                {project.changelog?.map((change) => (
                                    <SelectItem key={change.version} value={change.version} className="text-white hover:bg-slate-700">
                                        <div className="flex items-center justify-between w-full">
                                            <span>v{change.version}</span>
                                            <span className="text-xs text-slate-400 ml-2">
                                                {change.date.toLocaleDateString()}
                                            </span>
                                        </div>
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                    {/* Date Filter */}
                    <div>
                        <Label className="text-slate-300 text-sm font-medium">Filter by Date (Optional)</Label>
                        <div className="flex items-center gap-2 mt-1">
                            <div className="relative flex-1">
                                <CalendarDays className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
                                <Input
                                    type="date"
                                    value={searchDate}
                                    onChange={(e) => setSearchDate(e.target.value)}
                                    className="bg-slate-800 border-slate-600 text-white pl-10"
                                    placeholder="Filter sessions by date"
                                />
                            </div>
                            {searchDate && (
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => setSearchDate('')}
                                    className="border-slate-600 text-slate-300 hover:bg-slate-700"
                                >
                                    Clear
                                </Button>
                            )}
                        </div>
                        <p className="text-xs text-slate-400 mt-1">
                            {searchDate ? `Showing sessions for ${new Date(searchDate).toLocaleDateString('pt-BR')}` : 'Showing all available sessions'}
                        </p>
                    </div>

                    {/* Available Sessions Carousel */}
                    <div>
                        <Label className="text-slate-300 text-sm font-medium">Available Testing Sessions</Label>
                        <p className="text-xs text-slate-400 mt-1 mb-4">
                            Browse through available sessions. The nearest session is selected by default.
                        </p>

                        {(() => {
                            const filteredSessions = getFilteredSessions();

                            if (filteredSessions.length === 0) {
                                return (
                                    <Card className="bg-slate-800/50 border-slate-600">
                                        <CardContent className="p-8 text-center">
                                            <TestTube className="w-12 h-12 mx-auto mb-4 text-slate-400" />
                                            <p className="text-slate-300 text-lg mb-2">No testing sessions available</p>
                                            <p className="text-sm text-slate-400">
                                                {searchDate ? 'Try selecting a different date or clear the date filter' : 'Check back later for new sessions'}
                                            </p>
                                        </CardContent>
                                    </Card>
                                );
                            }

                            return (
                                <Carousel className="w-full">
                                    <CarouselContent className="-ml-2 md:-ml-4">
                                        {filteredSessions.map((session) => (
                                            <CarouselItem key={session.id} className="pl-2 md:pl-4 md:basis-1/2 lg:basis-1/3">
                                                <Card
                                                    className={`cursor-pointer transition-all duration-200 h-full ${formData.sessionId === session.id
                                                        ? 'border-purple-500 bg-purple-500/10 shadow-lg ring-2 ring-purple-500/20'
                                                        : 'border-slate-600 hover:border-slate-500 hover:bg-slate-800/30'
                                                        }`}
                                                    onClick={() => setFormData({ ...formData, sessionId: session.id })}
                                                >
                                                    <CardContent className="p-5">
                                                        <div className="flex items-start justify-between mb-3">
                                                            <div className="flex-1">
                                                                <h4 className="text-white font-semibold text-base mb-2">{session.title}</h4>
                                                                <p className="text-sm text-slate-300 mb-4 line-clamp-3">{session.description}</p>
                                                            </div>
                                                            {formData.sessionId === session.id && (
                                                                <div className="ml-3 flex-shrink-0">
                                                                    <div className="w-5 h-5 rounded-full bg-purple-500 flex items-center justify-center">
                                                                        <div className="w-2 h-2 rounded-full bg-white"></div>
                                                                    </div>
                                                                </div>
                                                            )}
                                                        </div>

                                                        <div className="space-y-3">
                                                            <div className="grid grid-cols-2 gap-3 text-sm">
                                                                <div className="flex items-center gap-2 text-slate-400">
                                                                    <Calendar className="w-4 h-4 text-purple-400" />
                                                                    <span>{session.scheduledDate.toLocaleDateString('pt-BR')}</span>
                                                                </div>
                                                                <div className="flex items-center gap-2 text-slate-400">
                                                                    <Clock className="w-4 h-4 text-purple-400" />
                                                                    <span>{session.scheduledDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                                                                </div>
                                                            </div>
                                                            <div className="flex items-center justify-between">
                                                                <div className="flex items-center gap-2 text-slate-400">
                                                                    <Users className="w-4 h-4 text-purple-400" />
                                                                    <span>{session.currentParticipants}/{session.maxParticipants} participants</span>
                                                                </div>
                                                                <Badge
                                                                    variant="secondary"
                                                                    className="bg-green-500/20 text-green-400"
                                                                >
                                                                    Available
                                                                </Badge>
                                                            </div>
                                                        </div>
                                                    </CardContent>
                                                </Card>
                                            </CarouselItem>
                                        ))}
                                    </CarouselContent>
                                    {filteredSessions.length > 3 && (
                                        <>
                                            <CarouselPrevious className="bg-slate-800 border-slate-600 text-slate-300 hover:bg-slate-700" />
                                            <CarouselNext className="bg-slate-800 border-slate-600 text-slate-300 hover:bg-slate-700" />
                                        </>
                                    )}
                                </Carousel>
                            );
                        })()}
                    </div>
                </div>
                <DialogFooter className="flex items-center justify-between">
                    <div className="text-sm text-slate-400">
                        {formData.sessionId && (() => {
                            const filteredSessions = getFilteredSessions();
                            const selectedSession = filteredSessions.find(s => s.id === formData.sessionId);
                            return selectedSession ? (
                                <span>Selected: v{formData.projectVersion} for {selectedSession.title}</span>
                            ) : null;
                        })()}
                    </div>
                    <div className="flex gap-3">
                        <Button variant="outline" onClick={handleClose} className="border-slate-600 text-slate-300">
                            Cancel
                        </Button>
                        <Button
                            onClick={handleSubmit}
                            className="bg-purple-600 hover:bg-purple-700"
                            disabled={!formData.sessionId || getFilteredSessions().length === 0}
                        >
                            Submit Request
                        </Button>
                    </div>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
