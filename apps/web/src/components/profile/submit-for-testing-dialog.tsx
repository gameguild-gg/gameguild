'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Calendar, Clock, TestTube, Users } from 'lucide-react';
import { useState } from 'react';
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
        sessionType: 'existing' | 'new';
        sessionId?: string;
        projectVersion: string;
        title?: string;
        description?: string;
        scheduledDate?: string;
        maxParticipants?: string;
        requirements?: string;
    }) => void;
}

interface TestingFormData {
    sessionId: string;
    sessionType: 'existing' | 'new';
    projectVersion: string;
    title: string;
    description: string;
    scheduledDate: string;
    maxParticipants: string;
    requirements: string;
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
        sessionType: 'existing',
        projectVersion: project.version,
        title: '',
        description: '',
        scheduledDate: '',
        maxParticipants: '10',
        requirements: ''
    });

    const handleSubmit = () => {
        if (formData.sessionType === 'existing') {
            const selectedSession = availableSessions.find(s => s.id === formData.sessionId);
            if (!selectedSession) {
                toast.error('Please select a testing session');
                return;
            }

            onSubmit({
                sessionType: 'existing',
                sessionId: formData.sessionId,
                projectVersion: formData.projectVersion
            });
        } else {
            if (!formData.title.trim()) {
                toast.error('Please enter a session title');
                return;
            }
            if (!formData.scheduledDate) {
                toast.error('Please select a scheduled date');
                return;
            }

            onSubmit({
                sessionType: 'new',
                projectVersion: formData.projectVersion,
                title: formData.title,
                description: formData.description,
                scheduledDate: formData.scheduledDate,
                maxParticipants: formData.maxParticipants,
                requirements: formData.requirements
            });
        }

        // Reset form
        setFormData({
            sessionId: '',
            sessionType: 'existing',
            projectVersion: project.version,
            title: '',
            description: '',
            scheduledDate: '',
            maxParticipants: '10',
            requirements: ''
        });
    };

    const handleClose = () => {
        onOpenChange(false);
        // Reset form when closing
        setFormData({
            sessionId: '',
            sessionType: 'existing',
            projectVersion: project.version,
            title: '',
            description: '',
            scheduledDate: '',
            maxParticipants: '10',
            requirements: ''
        });
    };

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-3xl bg-slate-900 border-slate-700">
                <DialogHeader>
                    <DialogTitle className="text-white">Submit for Testing</DialogTitle>
                    <DialogDescription>
                        Submit your project to a testing session to gather feedback from the community.
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

                    {/* Session Type Selection */}
                    <div>
                        <Label className="text-slate-300 text-sm font-medium">Testing Session</Label>
                        <div className="grid grid-cols-2 gap-4 mt-2">
                            <Card
                                className={`cursor-pointer transition-colors ${formData.sessionType === 'existing'
                                        ? 'border-purple-500 bg-purple-500/10'
                                        : 'border-slate-600 hover:border-slate-500'
                                    }`}
                                onClick={() => setFormData({ ...formData, sessionType: 'existing' })}
                            >
                                <CardContent className="p-4 text-center">
                                    <TestTube className="w-6 h-6 mx-auto mb-2 text-purple-400" />
                                    <h3 className="text-white font-medium">Join Existing Session</h3>
                                    <p className="text-xs text-slate-400 mt-1">Submit to an upcoming session</p>
                                </CardContent>
                            </Card>
                            <Card
                                className={`cursor-pointer transition-colors ${formData.sessionType === 'new'
                                        ? 'border-purple-500 bg-purple-500/10'
                                        : 'border-slate-600 hover:border-slate-500'
                                    }`}
                                onClick={() => setFormData({ ...formData, sessionType: 'new' })}
                            >
                                <CardContent className="p-4 text-center">
                                    <Calendar className="w-6 h-6 mx-auto mb-2 text-purple-400" />
                                    <h3 className="text-white font-medium">Create New Session</h3>
                                    <p className="text-xs text-slate-400 mt-1">Schedule your own session</p>
                                </CardContent>
                            </Card>
                        </div>
                    </div>

                    {/* Existing Session Selection */}
                    {formData.sessionType === 'existing' && (
                        <div className="space-y-4">
                            <div>
                                <Label className="text-slate-300">Available Sessions</Label>
                                <Select
                                    value={formData.sessionId}
                                    onValueChange={(value) => setFormData({ ...formData, sessionId: value })}
                                >
                                    <SelectTrigger className="bg-slate-800 border-slate-600 text-white">
                                        <SelectValue placeholder="Choose a testing session" />
                                    </SelectTrigger>
                                    <SelectContent className="bg-slate-800 border-slate-700">
                                        {availableSessions.map((session) => (
                                            <SelectItem key={session.id} value={session.id} className="text-white hover:bg-slate-700">
                                                <div className="flex flex-col">
                                                    <span className="font-medium">{session.title}</span>
                                                    <div className="flex items-center gap-2 text-xs text-slate-400">
                                                        <Calendar className="w-3 h-3" />
                                                        {session.scheduledDate.toLocaleDateString()} at {session.scheduledDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                                        <Users className="w-3 h-3 ml-2" />
                                                        {session.currentParticipants}/{session.maxParticipants}
                                                    </div>
                                                </div>
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>

                            {formData.sessionId && (
                                <Card className="bg-slate-800/50 border-slate-600">
                                    <CardContent className="p-4">
                                        {(() => {
                                            const selectedSession = availableSessions.find(s => s.id === formData.sessionId);
                                            if (!selectedSession) return null;

                                            return (
                                                <div>
                                                    <h4 className="text-white font-medium mb-2">{selectedSession.title}</h4>
                                                    <p className="text-sm text-slate-300 mb-3">{selectedSession.description}</p>
                                                    <div className="grid grid-cols-2 gap-4 text-sm">
                                                        <div className="flex items-center gap-2 text-slate-400">
                                                            <Calendar className="w-4 h-4" />
                                                            <span>{selectedSession.scheduledDate.toLocaleDateString()}</span>
                                                        </div>
                                                        <div className="flex items-center gap-2 text-slate-400">
                                                            <Clock className="w-4 h-4" />
                                                            <span>{selectedSession.scheduledDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                                                        </div>
                                                        <div className="flex items-center gap-2 text-slate-400">
                                                            <Users className="w-4 h-4" />
                                                            <span>{selectedSession.currentParticipants}/{selectedSession.maxParticipants} participants</span>
                                                        </div>
                                                        <div className="flex items-center gap-2 text-slate-400">
                                                            <Badge variant="secondary" className="bg-green-500/20 text-green-400">
                                                                {selectedSession.status}
                                                            </Badge>
                                                        </div>
                                                    </div>
                                                </div>
                                            );
                                        })()}
                                    </CardContent>
                                </Card>
                            )}
                        </div>
                    )}

                    {/* New Session Creation */}
                    {formData.sessionType === 'new' && (
                        <div className="space-y-4">
                            <div>
                                <Label htmlFor="testingTitle" className="text-slate-300">Session Title</Label>
                                <Input
                                    id="testingTitle"
                                    value={formData.title}
                                    onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                                    className="bg-slate-800 border-slate-600 text-white"
                                    placeholder="e.g., Beta Testing - Version 1.3.0"
                                />
                            </div>
                            <div>
                                <Label htmlFor="testingDescription" className="text-slate-300">Description</Label>
                                <Textarea
                                    id="testingDescription"
                                    value={formData.description}
                                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                    className="bg-slate-800 border-slate-600 text-white"
                                    rows={3}
                                    placeholder="What specific feedback are you looking for?"
                                />
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="scheduledDate" className="text-slate-300">Scheduled Date</Label>
                                    <Input
                                        id="scheduledDate"
                                        type="datetime-local"
                                        value={formData.scheduledDate}
                                        onChange={(e) => setFormData({ ...formData, scheduledDate: e.target.value })}
                                        className="bg-slate-800 border-slate-600 text-white"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="maxParticipants" className="text-slate-300">Max Participants</Label>
                                    <Input
                                        id="maxParticipants"
                                        type="number"
                                        value={formData.maxParticipants}
                                        onChange={(e) => setFormData({ ...formData, maxParticipants: e.target.value })}
                                        className="bg-slate-800 border-slate-600 text-white"
                                    />
                                </div>
                            </div>
                            <div>
                                <Label htmlFor="requirements" className="text-slate-300">Requirements</Label>
                                <Textarea
                                    id="requirements"
                                    value={formData.requirements}
                                    onChange={(e) => setFormData({ ...formData, requirements: e.target.value })}
                                    className="bg-slate-800 border-slate-600 text-white"
                                    rows={2}
                                    placeholder="Any specific requirements for testers?"
                                />
                            </div>
                        </div>
                    )}
                </div>
                <DialogFooter>
                    <Button variant="outline" onClick={handleClose} className="border-slate-600 text-slate-300">
                        Cancel
                    </Button>
                    <Button
                        onClick={handleSubmit}
                        className="bg-purple-600 hover:bg-purple-700"
                        disabled={formData.sessionType === 'existing' && !formData.sessionId}
                    >
                        {formData.sessionType === 'existing' ? 'Submit to Session' : 'Create Session'}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
