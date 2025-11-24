'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { ArrowRight, Calendar, CalendarDays, CheckCircle2, Clock, Filter, TestTube, Users } from 'lucide-react';
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
    gameCount?: number;
    tags?: string[];
}

interface SubmitForTestingSheetProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    project: Project;
    availableSessions: TestingSession[];
    onSubmit: (submissionData: {
        sessionId: string;
        projectVersion: string;
    }) => void;
}

export function SubmitForTestingSheet({
    open,
    onOpenChange,
    project,
    availableSessions,
    onSubmit
}: SubmitForTestingSheetProps) {
    const [selectedSessionId, setSelectedSessionId] = useState<string>('');
    const [selectedVersion, setSelectedVersion] = useState<string>(project.version);
    const [searchDate, setSearchDate] = useState<string>('');
    const [showFilters, setShowFilters] = useState<boolean>(false);

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

    const filteredSessions = getFilteredSessions();
    const selectedSession = filteredSessions.find(s => s.id === selectedSessionId);

    // Auto-select nearest session when sheet opens
    useEffect(() => {
        if (open && !selectedSessionId && filteredSessions.length > 0) {
            const firstSession = filteredSessions[0];
            if (firstSession) {
                setSelectedSessionId(firstSession.id);
            }
        }
    }, [open, filteredSessions.length, selectedSessionId]);

    const handleSubmit = () => {
        if (!selectedSessionId) {
            toast.error('Please select a testing session');
            return;
        }

        onSubmit({
            sessionId: selectedSessionId,
            projectVersion: selectedVersion
        });

        handleClose();
    };

    const handleClose = () => {
        onOpenChange(false);
        // Reset state
        setSelectedSessionId('');
        setSelectedVersion(project.version);
        setSearchDate('');
        setShowFilters(false);
    };

    const clearFilters = () => {
        setSearchDate('');
        setShowFilters(false);
    };

    return (
        <Sheet open={open} onOpenChange={onOpenChange}>
            <SheetContent side="right" className="w-full sm:max-w-2xl bg-slate-900 border-slate-700 overflow-y-auto p-0">
                <div className="px-6 py-6">
                    <SheetHeader className="space-y-4 pb-6">
                        <div className="flex items-center gap-3">
                            <div className="w-12 h-12 bg-purple-500/20 rounded-lg flex items-center justify-center">
                                <TestTube className="w-6 h-6 text-purple-400" />
                            </div>
                            <div>
                                <SheetTitle className="text-xl text-white">Submit for Testing</SheetTitle>
                                <SheetDescription className="text-slate-400">
                                    Join a testing session to get community feedback on your project
                                </SheetDescription>
                            </div>
                        </div>

                        {/* Project Info */}
                        <Card className="bg-slate-800/50 border-slate-600">
                            <CardContent className="p-4">
                                <div className="flex items-center justify-between">
                                    <div>
                                        <h3 className="text-white font-medium">{project.name}</h3>
                                        <p className="text-sm text-slate-400">Current version: v{project.version}</p>
                                    </div>
                                    <Badge variant="outline" className="border-purple-500 text-purple-400">
                                        Ready to test
                                    </Badge>
                                </div>
                            </CardContent>
                        </Card>
                    </SheetHeader>

                    <div className="space-y-8 pb-8">
                        {/* Version Selection */}
                        <div>
                            <Label className="text-slate-300 font-medium">Select Version</Label>
                            <p className="text-xs text-slate-400 mb-3">Choose which version to submit for testing</p>
                            <Select value={selectedVersion} onValueChange={setSelectedVersion}>
                                <SelectTrigger className="bg-slate-800 border-slate-600 text-white">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent className="bg-slate-800 border-slate-700">
                                    {project.changelog?.map((change) => (
                                        <SelectItem key={change.version} value={change.version} className="text-white hover:bg-slate-700">
                                            <div className="flex items-center justify-between w-full">
                                                <span>v{change.version}</span>
                                                <span className="text-xs text-slate-400 ml-4">
                                                    {change.date.toLocaleDateString()}
                                                </span>
                                            </div>
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>

                        <Separator className="bg-slate-700" />

                        {/* Filters */}
                        <div>
                            <div className="flex items-center justify-between mb-3">
                                <Label className="text-slate-300 font-medium">Available Sessions</Label>
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setShowFilters(!showFilters)}
                                    className="text-slate-400 hover:text-white"
                                >
                                    <Filter className="w-4 h-4 mr-2" />
                                    Filters
                                </Button>
                            </div>

                            {showFilters && (
                                <Card className="bg-slate-800/30 border-slate-600 mb-6">
                                    <CardContent className="p-5">
                                        <div className="space-y-4">
                                            <div>
                                                <Label className="text-slate-400 text-sm">Filter by Date</Label>
                                                <div className="flex items-center gap-2 mt-1">
                                                    <div className="relative flex-1">
                                                        <CalendarDays className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
                                                        <Input
                                                            type="date"
                                                            value={searchDate}
                                                            onChange={(e) => setSearchDate(e.target.value)}
                                                            className="bg-slate-700 border-slate-600 text-white pl-10"
                                                        />
                                                    </div>
                                                    {searchDate && (
                                                        <Button
                                                            variant="outline"
                                                            size="sm"
                                                            onClick={clearFilters}
                                                            className="border-slate-600 text-slate-300"
                                                        >
                                                            Clear
                                                        </Button>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            )}

                            <div className="text-xs text-slate-400 mb-4 flex items-center justify-between">
                                <span>
                                    {searchDate
                                        ? `Showing sessions for ${new Date(searchDate).toLocaleDateString('pt-BR')}`
                                        : `${filteredSessions.length} sessions available`
                                    }
                                </span>
                                {selectedSession && (
                                    <Badge variant="secondary" className="bg-green-500/20 text-green-400">
                                        Session selected
                                    </Badge>
                                )}
                            </div>
                        </div>

                        {/* Sessions List */}
                        <div className="space-y-4">
                            {filteredSessions.length === 0 ? (
                                <Card className="bg-slate-800/30 border-slate-600">
                                    <CardContent className="p-8 text-center">
                                        <TestTube className="w-8 h-8 mx-auto mb-3 text-slate-400" />
                                        <h3 className="text-white font-medium mb-2">No sessions available</h3>
                                        <p className="text-sm text-slate-400 mb-4">
                                            {searchDate
                                                ? 'No sessions found for this date. Try selecting a different date.'
                                                : 'There are no testing sessions currently available.'
                                            }
                                        </p>
                                        {searchDate && (
                                            <Button variant="outline" onClick={clearFilters} className="border-slate-600 text-slate-300">
                                                Clear date filter
                                            </Button>
                                        )}
                                    </CardContent>
                                </Card>
                            ) : (
                                filteredSessions.map((session) => (
                                    <div
                                        key={session.id}
                                        className={`border rounded-lg p-5 cursor-pointer transition-all duration-200 ${selectedSessionId === session.id
                                            ? 'border-purple-500 bg-purple-500/10'
                                            : 'border-slate-600 hover:border-slate-500 hover:bg-slate-800/30'
                                            }`}
                                        onClick={() => setSelectedSessionId(session.id)}
                                    >
                                        <div className="flex items-start justify-between mb-4">
                                            <div className="flex-1">
                                                <h4 className="text-white font-semibold text-base mb-1 flex items-center gap-2">
                                                    {session.title}
                                                    {selectedSessionId === session.id && (
                                                        <CheckCircle2 className="w-4 h-4 text-purple-400" />
                                                    )}
                                                </h4>
                                                <p className="text-sm text-slate-400 mb-4">{session.description}</p>
                                            </div>
                                        </div>

                                        <div className="grid grid-cols-3 gap-4 text-sm">
                                            <div className="flex items-center gap-2 text-slate-300">
                                                <Calendar className="w-4 h-4 text-purple-400" />
                                                <div>
                                                    <p className="font-medium">{session.scheduledDate.toLocaleDateString('pt-BR')}</p>
                                                    <p className="text-xs text-slate-500">Session date</p>
                                                </div>
                                            </div>
                                            <div className="flex items-center gap-2 text-slate-300">
                                                <Clock className="w-4 h-4 text-purple-400" />
                                                <div>
                                                    <p className="font-medium">
                                                        {session.scheduledDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                                    </p>
                                                    <p className="text-xs text-slate-500">Start time</p>
                                                </div>
                                            </div>
                                            <div className="flex items-center gap-2 text-slate-300">
                                                <Users className="w-4 h-4 text-purple-400" />
                                                <div>
                                                    <p className="font-medium">{session.currentParticipants}/{session.maxParticipants} participants</p>
                                                    <Badge className="bg-green-500/20 text-green-400 border-green-500/30 text-xs mt-1">
                                                        Available
                                                    </Badge>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))
                            )}
                        </div>
                    </div>
                </div>

                <SheetFooter className="px-6 py-4 border-t border-slate-700 bg-slate-900/50">
                    <div className="flex flex-col sm:flex-row gap-3 w-full">
                        <Button
                            variant="outline"
                            onClick={handleClose}
                            className="border-slate-600 text-slate-300 hover:bg-slate-800 flex-1"
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleSubmit}
                            disabled={!selectedSessionId || filteredSessions.length === 0}
                            className="bg-purple-600 hover:bg-purple-700 text-white flex-1 flex items-center gap-2"
                        >
                            Submit Request
                            <ArrowRight className="w-4 h-4" />
                        </Button>
                    </div>

                    {selectedSession && (
                        <div className="mt-4 p-4 bg-slate-800/50 rounded-lg">
                            <p className="text-xs text-slate-400 mb-1">Submitting:</p>
                            <p className="text-sm text-white">
                                <span className="font-medium">{project.name}</span> v{selectedVersion} → {selectedSession.title}
                            </p>
                        </div>
                    )}
                </SheetFooter>
            </SheetContent>
        </Sheet>
    );
}
