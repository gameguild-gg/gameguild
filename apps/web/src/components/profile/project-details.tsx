'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { ArrowLeft, Calendar, Download, Edit2, Eye, FileText, Gamepad2, Play, Share2, Star, Users } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { toast } from 'sonner';
import { SubmitForTestingDialog } from './submit-for-testing-dialog';

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
            id: 'ts1',
            title: 'Beta Testing - Version 1.3.0',
            status: 'active',
            participantCount: 12,
            scheduledDate: new Date('2024-09-15')
        },
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
    const [editForm, setEditForm] = useState({
        name: project.name,
        description: project.description,
        longDescription: project.longDescription || '',
        category: project.category,
        version: project.version,
        status: project.status,
        isPublic: project.isPublic,
        tags: project.tags.join(', '),
        downloadUrl: project.downloadUrl || '',
        sourceCodeUrl: project.sourceCodeUrl || '',
        websiteUrl: project.websiteUrl || ''
    });

    const handleEdit = () => {
        // Simulate API call
        setProject({
            ...project,
            ...editForm,
            tags: editForm.tags.split(',').map(tag => tag.trim()).filter(tag => tag),
            lastUpdated: new Date()
        });
        setIsEditing(false);
        toast.success('Project updated successfully!');
    };

    const handleSubmitForTesting = (submissionData: {
        sessionType: 'existing' | 'new';
        sessionId?: string;
        projectVersion: string;
        title?: string;
        description?: string;
        scheduledDate?: string;
        maxParticipants?: string;
        requirements?: string;
    }) => {
        if (submissionData.sessionType === 'existing' && submissionData.sessionId) {
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

                toast.success(`Project v${submissionData.projectVersion} submitted to "${selectedSession.title}" successfully!`);
            }
        } else if (submissionData.sessionType === 'new') {
            // Create new session
            const newSession = {
                id: `ts${Date.now()}`,
                title: submissionData.title || '',
                status: 'pending' as const,
                participantCount: 0,
                scheduledDate: new Date(submissionData.scheduledDate || '')
            };

            setProject({
                ...project,
                testingSessions: [...(project.testingSessions || []), newSession]
            });

            console.log('Creating new session:', {
                session: newSession,
                projectVersion: submissionData.projectVersion,
                projectId: project.id,
                projectName: project.name
            });

            toast.success(`New testing session created for v${submissionData.projectVersion} successfully!`);
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
                                <Button
                                    onClick={() => setShowTestingDialog(true)}
                                    className="bg-purple-600 hover:bg-purple-700"
                                >
                                    <Play className="w-4 h-4 mr-2" />
                                    Submit for Testing
                                </Button>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* Main Content */}
            <div className="max-w-7xl mx-auto px-6 py-8">
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                    {/* Main Content */}
                    <div className="lg:col-span-2 space-y-6">
                        <Tabs defaultValue="overview" className="space-y-6">
                            <TabsList className="bg-slate-800/50 border-purple-500/20">
                                <TabsTrigger value="overview" className="data-[state=active]:bg-purple-600">
                                    Overview
                                </TabsTrigger>
                                <TabsTrigger value="testing" className="data-[state=active]:bg-purple-600">
                                    Testing Sessions
                                </TabsTrigger>
                                <TabsTrigger value="analytics" className="data-[state=active]:bg-purple-600">
                                    Analytics
                                </TabsTrigger>
                                <TabsTrigger value="settings" className="data-[state=active]:bg-purple-600">
                                    Settings
                                </TabsTrigger>
                            </TabsList>

                            <TabsContent value="overview" className="space-y-6">
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
                            </TabsContent>

                            <TabsContent value="testing" className="space-y-6">
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
                                            project.testingSessions.map((session) => (
                                                <div
                                                    key={session.id}
                                                    className="p-4 bg-slate-700/50 rounded-lg border border-slate-600"
                                                >
                                                    <div className="flex items-center justify-between mb-2">
                                                        <h4 className="font-medium text-white">{session.title}</h4>
                                                        <Badge
                                                            className={
                                                                session.status === 'active' ? 'bg-green-500' :
                                                                    session.status === 'pending' ? 'bg-yellow-500' :
                                                                        'bg-gray-500'
                                                            }
                                                        >
                                                            {session.status}
                                                        </Badge>
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
                                                </div>
                                            ))
                                        ) : (
                                            <p className="text-slate-400 text-center py-8">
                                                No testing sessions yet. Submit your project for testing to get community feedback!
                                            </p>
                                        )}
                                    </CardContent>
                                </Card>
                            </TabsContent>

                            <TabsContent value="analytics" className="space-y-6">
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
                            </TabsContent>

                            <TabsContent value="settings" className="space-y-6">
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
                            </TabsContent>
                        </Tabs>
                    </div>

                    {/* Sidebar */}
                    <div className="space-y-6">
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

            {/* Submit for Testing Dialog */}
            <SubmitForTestingDialog
                open={showTestingDialog}
                onOpenChange={setShowTestingDialog}
                project={project}
                availableSessions={AVAILABLE_TESTING_SESSIONS}
                onSubmit={handleSubmitForTesting}
            />
        </div>
    );
}
