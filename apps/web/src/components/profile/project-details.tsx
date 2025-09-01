'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { ArrowLeft, BarChart3, Calendar, CheckCircle, ChevronRight, Clock, Download, Edit2, Eye, FileText, Gamepad2, Package, Play, Settings, Share2, Star, TestTube, Users, X } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { toast } from 'sonner';
import { SubmitForTestingSheet } from './submit-for-testing-sheet';

interface Project {
    id: string;
    name: string;
    description: string;
    longDescription?: string;
    category: string;
    version: string;
    status: 'development' | 'beta' | 'released' | 'archived';
    createdAt: Date;
    lastUpdated: Date;
    rating?: number;
    isPublic: boolean;
    tags: string[];
    downloadUrl?: string;
    sourceCodeUrl?: string;
    websiteUrl?: string;
    screenshots: string[];
    systemRequirements?: {
        minimum: string;
        recommended: string;
    };
    changelog?: Array<{
        version: string;
        date: Date;
        changes: string[];
    }>;
    testingSessions?: Array<{
        id: string;
        title: string;
        status: 'pending' | 'active' | 'completed';
        participantCount: number;
        scheduledDate: Date;
    }>;
}

interface ProjectDetailsProps {
    projectId: string;
    username: string;
    isOwner: boolean;
}

// Mock data - replace with actual API call
const MOCK_PROJECT: Project = {
    id: '1',
    name: 'Puzzle Adventure Game',
    description: 'A challenging puzzle game with unique mechanics and beautiful art style.',
    longDescription: 'This is an immersive puzzle adventure game that combines traditional puzzle-solving mechanics with modern storytelling. Players embark on a journey through mystical lands, solving intricate puzzles to progress through the story. The game features hand-drawn artwork, original soundtrack, and innovative gameplay mechanics that challenge players to think creatively.',
    category: 'Puzzle',
    version: '1.2.0',
    status: 'released',
    createdAt: new Date('2024-01-15'),
    lastUpdated: new Date('2024-08-20'),
    rating: 4.5,
    isPublic: true,
    tags: ['puzzle', 'adventure', 'indie', 'single-player'],
    downloadUrl: 'https://example.com/download',
    sourceCodeUrl: 'https://github.com/user/puzzle-game',
    websiteUrl: 'https://puzzlegame.example.com',
    screenshots: [
        '/placeholder-screenshot1.jpg',
        '/placeholder-screenshot2.jpg',
        '/placeholder-screenshot3.jpg'
    ],
    systemRequirements: {
        minimum: 'Windows 10, 4GB RAM, DirectX 11',
        recommended: 'Windows 11, 8GB RAM, DirectX 12'
    },
    changelog: [
        {
            version: '1.2.0',
            date: new Date('2024-08-20'),
            changes: ['Added new puzzle mechanics', 'Fixed performance issues', 'Updated UI design']
        },
        {
            version: '1.1.0',
            date: new Date('2024-06-15'),
            changes: ['New levels added', 'Bug fixes', 'Improved sound effects']
        },
        {
            version: '1.0.0',
            date: new Date('2024-01-15'),
            changes: ['Initial release', 'Core gameplay mechanics', 'Basic UI implementation']
        }
    ],
    testingSessions: [
        {
            id: 'ts2',
            title: 'UI/UX Feedback Session',
            status: 'completed',
            participantCount: 8,
            scheduledDate: new Date('2024-08-10')
        }
    ]
};

// Mock available testing sessions
const AVAILABLE_TESTING_SESSIONS = [
    {
        id: 'session1',
        title: 'Weekly Beta Testing Session',
        description: 'Regular testing session for new features',
        scheduledDate: new Date('2024-09-15T14:00:00'),
        status: 'upcoming',
        maxParticipants: 20,
        currentParticipants: 5
    },
    {
        id: 'session2',
        title: 'UI/UX Feedback Session',
        description: 'Focused on user interface improvements',
        scheduledDate: new Date('2024-09-20T16:00:00'),
        status: 'upcoming',
        maxParticipants: 15,
        currentParticipants: 3
    },
    {
        id: 'session3',
        title: 'Performance Testing Session',
        description: 'Testing game performance and optimization',
        scheduledDate: new Date('2024-09-25T15:00:00'),
        status: 'upcoming',
        maxParticipants: 10,
        currentParticipants: 2
    }
];

const PROJECT_CATEGORIES = [
    'Action', 'Adventure', 'RPG', 'Strategy', 'Simulation', 'Puzzle',
    'Racing', 'Sports', 'Fighting', 'Platform', 'Horror', 'Indie'
];

const PROJECT_STATUSES = [
    { value: 'development', label: 'In Development', color: 'bg-blue-500' },
    { value: 'beta', label: 'Beta', color: 'bg-yellow-500' },
    { value: 'released', label: 'Released', color: 'bg-green-500' },
    { value: 'archived', label: 'Archived', color: 'bg-gray-500' }
];

export function ProjectDetails({ projectId, username, isOwner }: ProjectDetailsProps) {
    const router = useRouter();
    const [project, setProject] = useState<Project>(MOCK_PROJECT);
    const [isEditing, setIsEditing] = useState(false);
    const [showTestingDialog, setShowTestingDialog] = useState(false);
    const [showNewVersionDialog, setShowNewVersionDialog] = useState(false);
    const [activeSection, setActiveSection] = useState<'overview' | 'testing' | 'analytics' | 'versions' | 'settings'>('overview');
    const [editForm, setEditForm] = useState({
        name: project.name,
        description: project.description,
        longDescription: project.longDescription || '',
        category: project.category,
        version: project.version,
        status: project.status,
        isPublic: project.isPublic,
        tags: project.tags.join(', '),
        sourceCodeUrl: project.sourceCodeUrl || '',
        websiteUrl: project.websiteUrl || '',
        downloadUrl: project.downloadUrl || ''
    });
    const [newVersionForm, setNewVersionForm] = useState({
        version: '',
        changes: '',
        releaseDate: new Date().toISOString().split('T')[0]
    });

    const handleEdit = () => {
        // Simulate API call
        setProject({
            ...project,
            ...editForm,
            tags: editForm.tags.split(',').map((tag: string) => tag.trim()).filter((tag: string) => tag),
            lastUpdated: new Date()
        });
        setIsEditing(false);
        toast.success('Project updated successfully!');
    };

    const handleNewVersion = () => {
        if (!newVersionForm.version || !newVersionForm.changes) {
            toast.error('Please fill in all required fields');
            return;
        }

        const releaseDate = newVersionForm.releaseDate ? new Date(newVersionForm.releaseDate) : new Date();
        const newChange = {
            version: newVersionForm.version,
            date: releaseDate,
            changes: newVersionForm.changes.split('\n').filter((change: string) => change.trim())
        };

        setProject({
            ...project,
            version: newVersionForm.version,
            lastUpdated: releaseDate,
            changelog: [newChange, ...(project.changelog || [])]
        });

        setNewVersionForm({
            version: '',
            changes: '',
            releaseDate: new Date().toISOString().split('T')[0]
        });

        setShowNewVersionDialog(false);
        toast.success('New version created successfully!');
    };

    const handleSubmitForTesting = (submissionData: {
        sessionId: string;
        projectVersion: string;
    }) => {
        // Check if project already has a pending or future testing session
        if (pendingSession) {
            toast.error(`Project is already submitted for testing in "${pendingSession.title}". Please wait for the current session to complete.`);
            setShowTestingDialog(false);
            return;
        }

        // Submit to existing session
        const selectedSession = AVAILABLE_TESTING_SESSIONS.find(s => s.id === submissionData.sessionId);
        if (selectedSession) {
            console.log('Submitting to existing session:', {
                sessionId: submissionData.sessionId,
                sessionTitle: selectedSession.title,
                projectVersion: submissionData.projectVersion,
                projectId: project.id,
                projectName: project.name
            });

            // Simulate adding the project to the testing session
            const newTestingSession = {
                id: `ts_${Date.now()}`,
                title: selectedSession.title,
                status: 'pending' as const,
                participantCount: 0,
                scheduledDate: selectedSession.scheduledDate
            };

            // Update project with new testing session
            setProject({
                ...project,
                testingSessions: [...(project.testingSessions || []), newTestingSession]
            });

            toast.success(`Project v${submissionData.projectVersion} submitted to "${selectedSession.title}" successfully!`);
        }

        setShowTestingDialog(false);
    };

    const getStatusColor = (status: string) => {
        const statusConfig = PROJECT_STATUSES.find(s => s.value === status);
        return statusConfig?.color || 'bg-gray-500';
    };

    const getStatusLabel = (status: string) => {
        const statusConfig = PROJECT_STATUSES.find(s => s.value === status);
        return statusConfig?.label || status;
    };

    // Check if project has pending testing sessions
    const getPendingTestingSession = () => {
        return project.testingSessions?.find(session =>
            session.status === 'pending' ||
            (session.status === 'active' && session.scheduledDate > new Date())
        );
    };

    const pendingSession = getPendingTestingSession();

    return (
        <div className="min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
            {/* Header */}
            <div className="border-b border-slate-700 bg-slate-900/50 backdrop-blur-sm sticky top-0 z-40">
                <div className="max-w-7xl mx-auto px-6 py-4">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-4">
                            <Link
                                href={`/users/${username}?tab=projects`}
                                className="flex items-center gap-2 text-slate-400 hover:text-white transition-colors"
                            >
                                <ArrowLeft className="w-4 h-4" />
                                Back to Projects
                            </Link>
                            <div className="h-6 w-px bg-slate-600" />
                            <div className="flex items-center gap-2">
                                <Gamepad2 className="w-5 h-5 text-purple-400" />
                                <h1 className="text-xl font-semibold text-white">{project.name}</h1>
                                <Badge className={`${getStatusColor(project.status)} text-white`}>
                                    {getStatusLabel(project.status)}
                                </Badge>
                            </div>
                        </div>

                        {isOwner && (
                            <div className="flex items-center gap-3">
                                <Button
                                    variant="outline"
                                    onClick={() => setIsEditing(true)}
                                    className="border-slate-600 text-slate-300 hover:bg-slate-700"
                                >
                                    <Edit2 className="w-4 h-4 mr-2" />
                                    Edit Project
                                </Button>

                                {/* Testing Status or Submit Button */}
                                {pendingSession ? (
                                    <div className="flex items-center gap-3 px-4 py-3 bg-slate-800/50 border border-slate-600 rounded-lg">
                                        <div className="flex items-center gap-2">
                                            {pendingSession.status === 'pending' ? (
                                                <>
                                                    <Clock className="w-4 h-4 text-yellow-400" />
                                                    <div className="flex flex-col">
                                                        <span className="text-yellow-400 font-medium text-sm">Awaiting Approval</span>
                                                        <span className="text-xs text-slate-400">Submission under review</span>
                                                    </div>
                                                </>
                                            ) : (
                                                <>
                                                    <CheckCircle className="w-4 h-4 text-green-400" />
                                                    <div className="flex flex-col">
                                                        <span className="text-green-400 font-medium text-sm">Approved for Testing</span>
                                                        <span className="text-xs text-slate-400">Ready to start</span>
                                                    </div>
                                                </>
                                            )}
                                        </div>
                                        <div className="h-8 w-px bg-slate-600" />
                                        <div className="text-sm text-slate-300">
                                            <div className="font-medium">{pendingSession.title}</div>
                                            <div className="text-xs text-slate-400">
                                                {pendingSession.scheduledDate.toLocaleDateString()}
                                            </div>
                                        </div>
                                        <div className="ml-auto flex items-center gap-2">
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                onClick={() => setActiveSection('testing')}
                                                className="text-slate-400 hover:text-white hover:bg-slate-700"
                                            >
                                                <Eye className="w-4 h-4 mr-1" />
                                                View Details
                                            </Button>
                                            {pendingSession.status === 'pending' && (
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={() => {
                                                        // Remove pending session
                                                        setProject({
                                                            ...project,
                                                            testingSessions: project.testingSessions?.filter(s => s.id !== pendingSession.id) || []
                                                        });
                                                        toast.success('Testing submission cancelled successfully');
                                                    }}
                                                    className="text-red-400 hover:text-red-300 hover:bg-red-900/20"
                                                >
                                                    <X className="w-4 h-4 mr-1" />
                                                    Cancel
                                                </Button>
                                            )}
                                        </div>
                                    </div>
                                ) : (
                                    <Button
                                        onClick={() => setShowTestingDialog(true)}
                                        className="bg-purple-600 hover:bg-purple-700"
                                    >
                                        <Play className="w-4 h-4 mr-2" />
                                        Submit for Testing
                                    </Button>
                                )}
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* Main Content */}
            <div className="max-w-7xl mx-auto px-6 py-8">
                <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
                    {/* Sidebar Navigation */}
                    <div className="lg:col-span-1">
                        <Card className="bg-slate-800/50 border-purple-500/20 sticky top-8">
                            <CardHeader>
                                <CardTitle className="text-white text-sm font-medium">Project Navigation</CardTitle>
                            </CardHeader>
                            <CardContent className="p-0">
                                <nav className="space-y-1">
                                    <button
                                        onClick={() => setActiveSection('overview')}
                                        className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${activeSection === 'overview'
                                            ? 'bg-purple-600 text-white border-r-2 border-purple-400'
                                            : 'text-slate-300 hover:bg-slate-700/50 hover:text-white'
                                            }`}
                                    >
                                        <Eye className="w-4 h-4" />
                                        <span>Overview</span>
                                        {activeSection === 'overview' && <ChevronRight className="w-4 h-4 ml-auto" />}
                                    </button>
                                    <button
                                        onClick={() => setActiveSection('testing')}
                                        className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${activeSection === 'testing'
                                            ? 'bg-purple-600 text-white border-r-2 border-purple-400'
                                            : 'text-slate-300 hover:bg-slate-700/50 hover:text-white'
                                            }`}
                                    >
                                        <TestTube className="w-4 h-4" />
                                        <span>Testing Sessions</span>
                                        {pendingSession && (
                                            <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30 text-xs px-1.5 py-0.5">
                                                Pending
                                            </Badge>
                                        )}
                                        {activeSection === 'testing' && <ChevronRight className="w-4 h-4 ml-auto" />}
                                    </button>
                                    <button
                                        onClick={() => setActiveSection('analytics')}
                                        className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${activeSection === 'analytics'
                                            ? 'bg-purple-600 text-white border-r-2 border-purple-400'
                                            : 'text-slate-300 hover:bg-slate-700/50 hover:text-white'
                                            }`}
                                    >
                                        <BarChart3 className="w-4 h-4" />
                                        <span>Analytics</span>
                                        {activeSection === 'analytics' && <ChevronRight className="w-4 h-4 ml-auto" />}
                                    </button>
                                    <button
                                        onClick={() => setActiveSection('versions')}
                                        className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${activeSection === 'versions'
                                            ? 'bg-purple-600 text-white border-r-2 border-purple-400'
                                            : 'text-slate-300 hover:bg-slate-700/50 hover:text-white'
                                            }`}
                                    >
                                        <Package className="w-4 h-4" />
                                        <span>Versions</span>
                                        {activeSection === 'versions' && <ChevronRight className="w-4 h-4 ml-auto" />}
                                    </button>
                                    <button
                                        onClick={() => setActiveSection('settings')}
                                        className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${activeSection === 'settings'
                                            ? 'bg-purple-600 text-white border-r-2 border-purple-400'
                                            : 'text-slate-300 hover:bg-slate-700/50 hover:text-white'
                                            }`}
                                    >
                                        <Settings className="w-4 h-4" />
                                        <span>Settings</span>
                                        {activeSection === 'settings' && <ChevronRight className="w-4 h-4 ml-auto" />}
                                    </button>
                                </nav>
                            </CardContent>
                        </Card>
                    </div>

                    {/* Main Content */}
                    <div className="lg:col-span-3 space-y-6">
                        {activeSection === 'overview' && (
                            <div className="space-y-6">
                                {/* Project Description */}
                                <Card className="bg-slate-800/50 border-purple-500/20">
                                    <CardHeader>
                                        <CardTitle className="text-white">About This Project</CardTitle>
                                    </CardHeader>
                                    <CardContent className="text-slate-300">
                                        <p className="mb-4">{project.description}</p>
                                        {project.longDescription && (
                                            <p className="text-sm leading-relaxed">{project.longDescription}</p>
                                        )}
                                    </CardContent>
                                </Card>

                                {/* Screenshots */}
                                {project.screenshots.length > 0 && (
                                    <Card className="bg-slate-800/50 border-purple-500/20">
                                        <CardHeader>
                                            <CardTitle className="text-white">Screenshots</CardTitle>
                                        </CardHeader>
                                        <CardContent>
                                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                                                {project.screenshots.map((screenshot, index) => (
                                                    <div
                                                        key={index}
                                                        className="aspect-video bg-slate-700 rounded-lg flex items-center justify-center"
                                                    >
                                                        <span className="text-slate-400">Screenshot {index + 1}</span>
                                                    </div>
                                                ))}
                                            </div>
                                        </CardContent>
                                    </Card>
                                )}

                                {/* Changelog */}
                                {project.changelog && project.changelog.length > 0 && (
                                    <Card className="bg-slate-800/50 border-purple-500/20">
                                        <CardHeader>
                                            <CardTitle className="text-white">Changelog</CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-4">
                                            {project.changelog.map((entry, index) => (
                                                <div key={index} className="border-l-2 border-purple-500/30 pl-4">
                                                    <div className="flex items-center gap-2 mb-2">
                                                        <span className="font-medium text-white">v{entry.version}</span>
                                                        <span className="text-sm text-slate-400">
                                                            {entry.date.toLocaleDateString()}
                                                        </span>
                                                    </div>
                                                    <ul className="text-sm text-slate-300 space-y-1">
                                                        {entry.changes.map((change, changeIndex) => (
                                                            <li key={changeIndex}>• {change}</li>
                                                        ))}
                                                    </ul>
                                                </div>
                                            ))}
                                        </CardContent>
                                    </Card>
                                )}
                            </div>
                        )}

                        {activeSection === 'testing' && (
                            <div className="space-y-6">
                                {/* Current Testing Sessions */}
                                <Card className="bg-slate-800/50 border-purple-500/20">
                                    <CardHeader>
                                        <CardTitle className="text-white">Testing Sessions</CardTitle>
                                        <CardDescription>
                                            Manage your project testing sessions and gather feedback from the community.
                                        </CardDescription>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        {project.testingSessions && project.testingSessions.length > 0 ? (
                                            project.testingSessions.map((session) => {
                                                const isPending = session.status === 'pending';
                                                const isUpcoming = session.status === 'active' && session.scheduledDate > new Date();
                                                const isCurrentPending = session.id === pendingSession?.id;

                                                return (
                                                    <div
                                                        key={session.id}
                                                        className={`p-4 rounded-lg border transition-all ${isCurrentPending
                                                            ? 'bg-slate-700/80 border-purple-500/50 ring-1 ring-purple-500/20'
                                                            : 'bg-slate-700/50 border-slate-600'
                                                            }`}
                                                    >
                                                        <div className="flex items-center justify-between mb-2">
                                                            <h4 className="font-medium text-white">{session.title}</h4>
                                                            <div className="flex items-center gap-2">
                                                                {isCurrentPending && (
                                                                    <div className="flex items-center gap-1 text-xs px-2 py-1 bg-purple-600/20 text-purple-300 rounded-full">
                                                                        <div className="w-1.5 h-1.5 bg-purple-400 rounded-full animate-pulse"></div>
                                                                        Current
                                                                    </div>
                                                                )}
                                                                <Badge
                                                                    className={`${isPending ? 'bg-yellow-500/20 text-yellow-300 border-yellow-500/30' :
                                                                        isUpcoming ? 'bg-green-500/20 text-green-300 border-green-500/30' :
                                                                            session.status === 'completed' ? 'bg-blue-500/20 text-blue-300 border-blue-500/30' :
                                                                                'bg-gray-500/20 text-gray-300 border-gray-500/30'
                                                                        } border`}
                                                                >
                                                                    {isPending ? 'Awaiting Approval' :
                                                                        isUpcoming ? 'Approved' :
                                                                            session.status === 'completed' ? 'Completed' :
                                                                                session.status}
                                                                </Badge>
                                                            </div>
                                                        </div>
                                                        <div className="flex items-center gap-4 text-sm text-slate-400">
                                                            <span className="flex items-center gap-1">
                                                                <Users className="w-4 h-4" />
                                                                {session.participantCount} participants
                                                            </span>
                                                            <span className="flex items-center gap-1">
                                                                <Calendar className="w-4 h-4" />
                                                                {session.scheduledDate.toLocaleDateString()}
                                                            </span>
                                                        </div>
                                                        {isCurrentPending && (
                                                            <div className="mt-3 p-2 bg-purple-600/10 border border-purple-500/20 rounded-md">
                                                                <p className="text-xs text-purple-300">
                                                                    {isPending
                                                                        ? '⏳ Your project is currently under review for this testing session.'
                                                                        : '✅ Your project has been approved! Testing session will start soon.'
                                                                    }
                                                                </p>
                                                            </div>
                                                        )}
                                                    </div>
                                                );
                                            })
                                        ) : (
                                            <p className="text-slate-400 text-center py-8">
                                                No testing sessions yet. Submit your project for testing to get community feedback!
                                            </p>
                                        )}
                                    </CardContent>
                                </Card>
                            </div>
                        )}

                        {activeSection === 'analytics' && (
                            <div className="space-y-6">
                                <Card className="bg-slate-800/50 border-purple-500/20">
                                    <CardHeader>
                                        <CardTitle className="text-white">Project Analytics</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                                            <div className="p-4 bg-slate-700/50 rounded-lg">
                                                <div className="flex items-center gap-2 mb-2">
                                                    <Eye className="w-5 h-5 text-blue-400" />
                                                    <span className="text-white font-medium">Views</span>
                                                </div>
                                                <p className="text-2xl font-bold text-white">1,234</p>
                                                <p className="text-sm text-slate-400">+12% this week</p>
                                            </div>
                                            <div className="p-4 bg-slate-700/50 rounded-lg">
                                                <div className="flex items-center gap-2 mb-2">
                                                    <Download className="w-5 h-5 text-green-400" />
                                                    <span className="text-white font-medium">Downloads</span>
                                                </div>
                                                <p className="text-2xl font-bold text-white">567</p>
                                                <p className="text-sm text-slate-400">+8% this week</p>
                                            </div>
                                            <div className="p-4 bg-slate-700/50 rounded-lg">
                                                <div className="flex items-center gap-2 mb-2">
                                                    <Star className="w-5 h-5 text-yellow-400" />
                                                    <span className="text-white font-medium">Rating</span>
                                                </div>
                                                <p className="text-2xl font-bold text-white">{project.rating || 'N/A'}</p>
                                                <p className="text-sm text-slate-400">Based on 23 reviews</p>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>
                        )}

                        {activeSection === 'versions' && (
                            <div className="space-y-6">
                                {/* Current Version */}
                                <Card className="bg-slate-800/50 border-purple-500/20">
                                    <CardHeader>
                                        <div className="flex items-center justify-between">
                                            <CardTitle className="text-white">Version Management</CardTitle>
                                            <Button
                                                className="bg-purple-600 hover:bg-purple-700"
                                                onClick={() => setShowNewVersionDialog(true)}
                                            >
                                                New Version
                                            </Button>
                                        </div>
                                        <CardDescription className="text-slate-400">
                                            Manage your project versions and releases
                                        </CardDescription>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="bg-slate-700/50 rounded-lg p-4 border-l-4 border-purple-500">
                                            <div className="flex items-center justify-between mb-2">
                                                <h4 className="text-lg font-semibold text-white">v{project.version} (Current)</h4>
                                                <Badge className="bg-green-600/20 text-green-300">Latest</Badge>
                                            </div>
                                            <p className="text-slate-300 mb-3">Released on {project.lastUpdated.toLocaleDateString()}</p>
                                            <div className="flex gap-2">
                                                <Button variant="outline" size="sm" className="border-slate-600 text-slate-300 hover:bg-slate-700">
                                                    <Download className="w-4 h-4 mr-2" />
                                                    Download
                                                </Button>
                                                <Button variant="outline" size="sm" className="border-slate-600 text-slate-300 hover:bg-slate-700">
                                                    <FileText className="w-4 h-4 mr-2" />
                                                    Release Notes
                                                </Button>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>

                                {/* Version History */}
                                <Card className="bg-slate-800/50 border-purple-500/20">
                                    <CardHeader>
                                        <CardTitle className="text-white">Version History</CardTitle>
                                        <CardDescription className="text-slate-400">
                                            Previous releases of your project
                                        </CardDescription>
                                    </CardHeader>
                                    <CardContent>
                                        {project.changelog && project.changelog.length > 0 ? (
                                            <div className="space-y-4">
                                                {project.changelog.map((version, index) => (
                                                    <div key={version.version} className="border-l-2 border-slate-600 pl-4 pb-4 last:pb-0">
                                                        <div className="flex items-center justify-between mb-2">
                                                            <div>
                                                                <h5 className="font-medium text-white">v{version.version}</h5>
                                                                <p className="text-sm text-slate-400">Released on {version.date.toLocaleDateString()}</p>
                                                            </div>
                                                            <div className="flex gap-2">
                                                                <Button variant="ghost" size="sm" className="text-slate-400 hover:text-white">
                                                                    <Download className="w-4 h-4" />
                                                                </Button>
                                                                <Button variant="ghost" size="sm" className="text-slate-400 hover:text-white">
                                                                    <FileText className="w-4 h-4" />
                                                                </Button>
                                                            </div>
                                                        </div>
                                                        <div className="space-y-1">
                                                            {version.changes.map((change, changeIndex) => (
                                                                <div key={changeIndex} className="flex items-start gap-2">
                                                                    <div className="w-1.5 h-1.5 bg-purple-500 rounded-full mt-2 flex-shrink-0"></div>
                                                                    <p className="text-sm text-slate-300">{change}</p>
                                                                </div>
                                                            ))}
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>
                                        ) : (
                                            <div className="text-center py-8">
                                                <Package className="w-12 h-12 text-slate-500 mx-auto mb-3" />
                                                <p className="text-slate-400 mb-2">No previous versions</p>
                                                <p className="text-sm text-slate-500">Version history will appear here as you create new releases</p>
                                            </div>
                                        )}
                                    </CardContent>
                                </Card>

                                {/* Version Statistics */}
                                <Card className="bg-slate-800/50 border-purple-500/20">
                                    <CardHeader>
                                        <CardTitle className="text-white">Version Statistics</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                            <div className="text-center p-4 bg-slate-700/50 rounded-lg">
                                                <div className="text-2xl font-bold text-white mb-1">
                                                    {project.changelog ? project.changelog.length + 1 : 1}
                                                </div>
                                                <div className="text-sm text-slate-400">Total Versions</div>
                                            </div>
                                            <div className="text-center p-4 bg-slate-700/50 rounded-lg">
                                                <div className="text-2xl font-bold text-white mb-1">
                                                    {Math.floor((new Date().getTime() - project.createdAt.getTime()) / (1000 * 60 * 60 * 24))}
                                                </div>
                                                <div className="text-sm text-slate-400">Days Since First Release</div>
                                            </div>
                                            <div className="text-center p-4 bg-slate-700/50 rounded-lg">
                                                <div className="text-2xl font-bold text-white mb-1">
                                                    {Math.floor((new Date().getTime() - project.lastUpdated.getTime()) / (1000 * 60 * 60 * 24))}
                                                </div>
                                                <div className="text-sm text-slate-400">Days Since Last Update</div>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>
                        )}

                        {activeSection === 'settings' && (
                            <div className="space-y-6">
                                <Card className="bg-slate-800/50 border-purple-500/20">
                                    <CardHeader>
                                        <CardTitle className="text-white">Project Settings</CardTitle>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        <div className="flex items-center justify-between p-4 bg-slate-700/50 rounded-lg">
                                            <div>
                                                <h4 className="font-medium text-white">Project Visibility</h4>
                                                <p className="text-sm text-slate-400">
                                                    {project.isPublic ? 'Public - Visible to everyone' : 'Private - Only visible to you'}
                                                </p>
                                            </div>
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                className="border-slate-600 text-slate-300 hover:bg-slate-700"
                                            >
                                                Change
                                            </Button>
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>
                        )}
                    </div>

                    {/* Right Sidebar */}
                    <div className="lg:col-span-1 space-y-6">
                        {/* Project Info */}
                        <Card className="bg-slate-800/50 border-purple-500/20">
                            <CardHeader>
                                <CardTitle className="text-white">Project Info</CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div>
                                    <Label className="text-slate-400">Category</Label>
                                    <p className="text-white">{project.category}</p>
                                </div>
                                <div>
                                    <Label className="text-slate-400">Version</Label>
                                    <p className="text-white">v{project.version}</p>
                                </div>
                                <div>
                                    <Label className="text-slate-400">Created</Label>
                                    <p className="text-white">{project.createdAt.toLocaleDateString()}</p>
                                </div>
                                <div>
                                    <Label className="text-slate-400">Last Updated</Label>
                                    <p className="text-white">{project.lastUpdated.toLocaleDateString()}</p>
                                </div>
                                {project.tags && project.tags.length > 0 && (
                                    <div>
                                        <Label className="text-slate-400">Tags</Label>
                                        <div className="flex flex-wrap gap-2 mt-2">
                                            {project.tags.map((tag) => (
                                                <Badge key={tag} variant="secondary" className="bg-purple-600/20 text-purple-300">
                                                    {tag}
                                                </Badge>
                                            ))}
                                        </div>
                                    </div>
                                )}
                            </CardContent>
                        </Card>

                        {/* Quick Actions */}
                        <Card className="bg-slate-800/50 border-purple-500/20">
                            <CardHeader>
                                <CardTitle className="text-white">Quick Actions</CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-3">
                                {project.downloadUrl && (
                                    <Button
                                        variant="outline"
                                        className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-700"
                                        asChild
                                    >
                                        <a href={project.downloadUrl} target="_blank" rel="noopener noreferrer">
                                            <Download className="w-4 h-4 mr-2" />
                                            Download
                                        </a>
                                    </Button>
                                )}
                                {project.sourceCodeUrl && (
                                    <Button
                                        variant="outline"
                                        className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-700"
                                        asChild
                                    >
                                        <a href={project.sourceCodeUrl} target="_blank" rel="noopener noreferrer">
                                            <FileText className="w-4 h-4 mr-2" />
                                            Source Code
                                        </a>
                                    </Button>
                                )}
                                {project.websiteUrl && (
                                    <Button
                                        variant="outline"
                                        className="w-full justify-start border-slate-600 text-slate-300 hover:bg-slate-700"
                                        asChild
                                    >
                                        <a href={project.websiteUrl} target="_blank" rel="noopener noreferrer">
                                            <Share2 className="w-4 h-4 mr-2" />
                                            Website
                                        </a>
                                    </Button>
                                )}
                            </CardContent>
                        </Card>

                        {/* System Requirements */}
                        {project.systemRequirements && (
                            <Card className="bg-slate-800/50 border-purple-500/20">
                                <CardHeader>
                                    <CardTitle className="text-white">System Requirements</CardTitle>
                                </CardHeader>
                                <CardContent className="space-y-3">
                                    <div>
                                        <Label className="text-slate-400">Minimum</Label>
                                        <p className="text-sm text-slate-300">{project.systemRequirements.minimum}</p>
                                    </div>
                                    <div>
                                        <Label className="text-slate-400">Recommended</Label>
                                        <p className="text-sm text-slate-300">{project.systemRequirements.recommended}</p>
                                    </div>
                                </CardContent>
                            </Card>
                        )}
                    </div>
                </div>
            </div>

            {/* Edit Dialog */}
            <Dialog open={isEditing} onOpenChange={setIsEditing}>
                <DialogContent className="max-w-2xl bg-slate-900 border-slate-700">
                    <DialogHeader>
                        <DialogTitle className="text-white">Edit Project</DialogTitle>
                        <DialogDescription>
                            Update your project details and information.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4 max-h-[60vh] overflow-y-auto">
                        <div>
                            <Label htmlFor="name" className="text-slate-300">Project Name</Label>
                            <Input
                                id="name"
                                value={editForm.name}
                                onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white"
                            />
                        </div>
                        <div>
                            <Label htmlFor="description" className="text-slate-300">Short Description</Label>
                            <Textarea
                                id="description"
                                value={editForm.description}
                                onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white"
                                rows={3}
                            />
                        </div>
                        <div>
                            <Label htmlFor="longDescription" className="text-slate-300">Detailed Description</Label>
                            <Textarea
                                id="longDescription"
                                value={editForm.longDescription}
                                onChange={(e) => setEditForm({ ...editForm, longDescription: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white"
                                rows={4}
                            />
                        </div>
                        <div className="grid grid-cols-2 gap-4">
                            <div>
                                <Label htmlFor="category" className="text-slate-300">Category</Label>
                                <Select value={editForm.category} onValueChange={(value) => setEditForm({ ...editForm, category: value })}>
                                    <SelectTrigger className="bg-slate-800 border-slate-600 text-white">
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent className="bg-slate-800 border-slate-600">
                                        {PROJECT_CATEGORIES.map((category) => (
                                            <SelectItem key={category} value={category} className="text-white">
                                                {category}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <div>
                                <Label htmlFor="version" className="text-slate-300">Version</Label>
                                <Input
                                    id="version"
                                    value={editForm.version}
                                    onChange={(e) => setEditForm({ ...editForm, version: e.target.value })}
                                    className="bg-slate-800 border-slate-600 text-white"
                                />
                            </div>
                        </div>
                        <div>
                            <Label htmlFor="status" className="text-slate-300">Status</Label>
                            <Select value={editForm.status} onValueChange={(value) => setEditForm({ ...editForm, status: value as any })}>
                                <SelectTrigger className="bg-slate-800 border-slate-600 text-white">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent className="bg-slate-800 border-slate-600">
                                    {PROJECT_STATUSES.map((status) => (
                                        <SelectItem key={status.value} value={status.value} className="text-white">
                                            {status.label}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>
                        <div>
                            <Label htmlFor="tags" className="text-slate-300">Tags (comma separated)</Label>
                            <Input
                                id="tags"
                                value={editForm.tags}
                                onChange={(e) => setEditForm({ ...editForm, tags: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white"
                                placeholder="puzzle, adventure, indie"
                            />
                        </div>
                        <div>
                            <Label htmlFor="downloadUrl" className="text-slate-300">Download URL</Label>
                            <Input
                                id="downloadUrl"
                                value={editForm.downloadUrl}
                                onChange={(e) => setEditForm({ ...editForm, downloadUrl: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white"
                                placeholder="https://..."
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setIsEditing(false)} className="border-slate-600 text-slate-300">
                            Cancel
                        </Button>
                        <Button onClick={handleEdit} className="bg-purple-600 hover:bg-purple-700">
                            Save Changes
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* New Version Dialog */}
            <Dialog open={showNewVersionDialog} onOpenChange={setShowNewVersionDialog}>
                <DialogContent className="max-w-2xl bg-slate-900 border-slate-700">
                    <DialogHeader>
                        <DialogTitle className="text-white">Create New Version</DialogTitle>
                        <DialogDescription className="text-slate-400">
                            Release a new version of your project with updated features and fixes.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                        <div>
                            <Label htmlFor="newVersion" className="text-slate-300">Version Number*</Label>
                            <Input
                                id="newVersion"
                                value={newVersionForm.version}
                                onChange={(e) => setNewVersionForm({ ...newVersionForm, version: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white"
                                placeholder="e.g., 1.3.0"
                            />
                        </div>
                        <div>
                            <Label htmlFor="releaseDate" className="text-slate-300">Release Date</Label>
                            <Input
                                id="releaseDate"
                                type="date"
                                value={newVersionForm.releaseDate}
                                onChange={(e) => setNewVersionForm({ ...newVersionForm, releaseDate: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white"
                            />
                        </div>
                        <div>
                            <Label htmlFor="changes" className="text-slate-300">Changelog*</Label>
                            <Textarea
                                id="changes"
                                value={newVersionForm.changes}
                                onChange={(e) => setNewVersionForm({ ...newVersionForm, changes: e.target.value })}
                                className="bg-slate-800 border-slate-600 text-white min-h-[120px]"
                                placeholder="List the changes, one per line:
• Added new feature
• Fixed bug with...
• Improved performance"
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setShowNewVersionDialog(false)} className="border-slate-600 text-slate-300">
                            Cancel
                        </Button>
                        <Button onClick={handleNewVersion} className="bg-purple-600 hover:bg-purple-700">
                            Create Version
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Submit for Testing Dialog */}
            <SubmitForTestingSheet
                open={showTestingDialog}
                onOpenChange={setShowTestingDialog}
                project={project}
                availableSessions={AVAILABLE_TESTING_SESSIONS}
                onSubmit={handleSubmitForTesting}
            />
        </div>
    );
}
