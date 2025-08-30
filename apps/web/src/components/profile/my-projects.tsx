'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Calendar, Edit2, Eye, Gamepad2, Plus, Star, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';
import { toast } from 'sonner';

interface Project {
    id: string;
    name: string;
    description: string;
    category: string;
    version: string;
    status: 'development' | 'beta' | 'released' | 'archived';
    createdAt: Date;
    lastUpdated: Date;
    rating?: number;
    isPublic: boolean;
}

interface MyProjectsProps {
    userId: string;
    username: string;
}

const MOCK_PROJECTS: Project[] = [
    {
        id: '1',
        name: 'Puzzle Adventure Game',
        description: 'A challenging puzzle game with unique mechanics and beautiful art style.',
        category: 'Puzzle',
        version: '1.2.0',
        status: 'released',
        createdAt: new Date('2024-01-15'),
        lastUpdated: new Date('2024-08-20'),
        rating: 4.5,
        isPublic: true
    },
    {
        id: '2',
        name: 'RPG Character System',
        description: 'A flexible character progression system for RPG games.',
        category: 'RPG',
        version: '0.8.0',
        status: 'beta',
        createdAt: new Date('2024-05-10'),
        lastUpdated: new Date('2024-08-25'),
        isPublic: true
    },
    {
        id: '3',
        name: 'Physics Platformer',
        description: 'Experimental platformer with realistic physics simulation.',
        category: 'Platformer',
        version: '0.3.0',
        status: 'development',
        createdAt: new Date('2024-07-01'),
        lastUpdated: new Date('2024-08-28'),
        isPublic: false
    }
];

export function MyProjects({ userId, username }: MyProjectsProps) {
    const [projects, setProjects] = useState<Project[]>(MOCK_PROJECTS);
    const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
    const [editingProject, setEditingProject] = useState<Project | null>(null);
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        category: '',
        version: '1.0.0',
        status: 'development' as const,
        isPublic: true
    });

    const getStatusColor = (status: string) => {
        switch (status) {
            case 'released': return 'bg-green-100 text-green-800';
            case 'beta': return 'bg-blue-100 text-blue-800';
            case 'development': return 'bg-yellow-100 text-yellow-800';
            case 'archived': return 'bg-gray-100 text-gray-800';
            default: return 'bg-gray-100 text-gray-800';
        }
    };

    const getStatusText = (status: string) => {
        switch (status) {
            case 'released': return 'Released';
            case 'beta': return 'Beta';
            case 'development': return 'In Development';
            case 'archived': return 'Archived';
            default: return status;
        }
    };

    const handleCreateProject = () => {
        if (!formData.name.trim()) {
            toast.error('Project name is required');
            return;
        }

        const newProject: Project = {
            id: Date.now().toString(),
            name: formData.name,
            description: formData.description,
            category: formData.category,
            version: formData.version,
            status: formData.status,
            createdAt: new Date(),
            lastUpdated: new Date(),
            isPublic: formData.isPublic
        };

        setProjects([...projects, newProject]);
        setIsCreateDialogOpen(false);
        setFormData({
            name: '',
            description: '',
            category: '',
            version: '1.0.0',
            status: 'development',
            isPublic: true
        });
        toast.success('Project created successfully!');
    };

    const handleEditProject = () => {
        if (!editingProject || !formData.name.trim()) {
            toast.error('Project name is required');
            return;
        }

        const updatedProject: Project = {
            ...editingProject,
            name: formData.name,
            description: formData.description,
            category: formData.category,
            version: formData.version,
            status: formData.status,
            lastUpdated: new Date(),
            isPublic: formData.isPublic
        };

        setProjects(projects.map(p => p.id === editingProject.id ? updatedProject : p));
        setIsEditDialogOpen(false);
        setEditingProject(null);
        toast.success('Project updated successfully!');
    };

    const handleDeleteProject = (projectId: string) => {
        setProjects(projects.filter(p => p.id !== projectId));
        toast.success('Project deleted successfully!');
    };

    const openEditDialog = (project: Project) => {
        setEditingProject(project);
        setFormData({
            name: project.name,
            description: project.description,
            category: project.category,
            version: project.version,
            status: project.status,
            isPublic: project.isPublic
        });
        setIsEditDialogOpen(true);
    };

    const formatDate = (date: Date) => {
        return date.toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h2 className="text-2xl font-bold text-white">My Projects</h2>
                    <p className="text-gray-400 mt-1">
                        Manage and showcase your game development projects
                    </p>
                </div>
                <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
                    <DialogTrigger asChild>
                        <Button className="bg-purple-600 hover:bg-purple-700">
                            <Plus className="h-4 w-4 mr-2" />
                            New Project
                        </Button>
                    </DialogTrigger>
                    <DialogContent className="sm:max-w-[425px]">
                        <DialogHeader>
                            <DialogTitle>Create New Project</DialogTitle>
                            <DialogDescription>
                                Add a new project to your portfolio. You can edit these details later.
                            </DialogDescription>
                        </DialogHeader>
                        <div className="grid gap-4 py-4">
                            <div className="grid grid-cols-4 items-center gap-4">
                                <Label htmlFor="name" className="text-right">
                                    Name
                                </Label>
                                <Input
                                    id="name"
                                    value={formData.name}
                                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                                    className="col-span-3"
                                />
                            </div>
                            <div className="grid grid-cols-4 items-center gap-4">
                                <Label htmlFor="category" className="text-right">
                                    Category
                                </Label>
                                <Input
                                    id="category"
                                    value={formData.category}
                                    onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                                    className="col-span-3"
                                    placeholder="e.g., RPG, Puzzle, Platformer"
                                />
                            </div>
                            <div className="grid grid-cols-4 items-center gap-4">
                                <Label htmlFor="version" className="text-right">
                                    Version
                                </Label>
                                <Input
                                    id="version"
                                    value={formData.version}
                                    onChange={(e) => setFormData({ ...formData, version: e.target.value })}
                                    className="col-span-3"
                                />
                            </div>
                            <div className="grid grid-cols-4 items-center gap-4">
                                <Label htmlFor="description" className="text-right">
                                    Description
                                </Label>
                                <Textarea
                                    id="description"
                                    value={formData.description}
                                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                    className="col-span-3"
                                    rows={3}
                                />
                            </div>
                        </div>
                        <DialogFooter>
                            <Button onClick={handleCreateProject}>Create Project</Button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>
            </div>

            {/* Projects Grid */}
            <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
                {projects.length === 0 ? (
                    <Card className="bg-slate-800/50 border-purple-500/20 md:col-span-2 lg:col-span-3">
                        <CardContent className="flex flex-col items-center justify-center py-12">
                            <Gamepad2 className="h-12 w-12 text-gray-400 mb-4" />
                            <h3 className="text-lg font-semibold text-gray-300 mb-2">No projects yet</h3>
                            <p className="text-gray-400 text-center mb-4">
                                Start creating your first game project to showcase your work
                            </p>
                            <Button
                                onClick={() => setIsCreateDialogOpen(true)}
                                className="bg-purple-600 hover:bg-purple-700"
                            >
                                <Plus className="h-4 w-4 mr-2" />
                                Create First Project
                            </Button>
                        </CardContent>
                    </Card>
                ) : (
                    projects.map((project) => (
                        <Card key={project.id} className="bg-slate-800/50 border-purple-500/20 hover:border-purple-400/40 transition-colors">
                            <CardHeader>
                                <div className="flex items-start justify-between">
                                    <div className="space-y-2 flex-1">
                                        <CardTitle className="text-white flex items-center gap-2">
                                            <Gamepad2 className="h-4 w-4" />
                                            {project.name}
                                        </CardTitle>
                                        <div className="flex items-center gap-2">
                                            <Badge variant="outline" className={getStatusColor(project.status)}>
                                                {getStatusText(project.status)}
                                            </Badge>
                                            <span className="text-xs text-gray-400">v{project.version}</span>
                                            {!project.isPublic && (
                                                <Badge variant="secondary" className="text-xs">
                                                    Private
                                                </Badge>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </CardHeader>
                            <CardContent className="space-y-3">
                                <CardDescription className="text-gray-300">
                                    {project.description || 'No description provided.'}
                                </CardDescription>

                                <div className="space-y-2 text-sm text-gray-400">
                                    <div className="flex items-center gap-2">
                                        <span className="font-medium">Category:</span>
                                        <span>{project.category || 'Uncategorized'}</span>
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Calendar className="h-3 w-3" />
                                        <span>Created: {formatDate(project.createdAt)}</span>
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Calendar className="h-3 w-3" />
                                        <span>Updated: {formatDate(project.lastUpdated)}</span>
                                    </div>
                                    {project.rating && (
                                        <div className="flex items-center gap-2">
                                            <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
                                            <span>{project.rating.toFixed(1)} rating</span>
                                        </div>
                                    )}
                                </div>
                            </CardContent>
                            <CardFooter className="flex items-center justify-between">
                                <Button variant="outline" size="sm" className="text-gray-300" asChild>
                                    <Link href={`/users/${username}/projects/${project.id}`}>
                                        <Eye className="h-4 w-4 mr-1" />
                                        View Details
                                    </Link>
                                </Button>
                                <div className="flex gap-1">
                                    <Button
                                        variant="ghost"
                                        size="sm"
                                        onClick={() => openEditDialog(project)}
                                    >
                                        <Edit2 className="h-4 w-4" />
                                    </Button>
                                    <Button
                                        variant="ghost"
                                        size="sm"
                                        onClick={() => handleDeleteProject(project.id)}
                                        className="text-red-400 hover:text-red-300"
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </Button>
                                </div>
                            </CardFooter>
                        </Card>
                    ))
                )}
            </div>

            {/* Edit Dialog */}
            <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
                <DialogContent className="sm:max-w-[425px]">
                    <DialogHeader>
                        <DialogTitle>Edit Project</DialogTitle>
                        <DialogDescription>
                            Update your project details and settings.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4 py-4">
                        <div className="grid grid-cols-4 items-center gap-4">
                            <Label htmlFor="edit-name" className="text-right">
                                Name
                            </Label>
                            <Input
                                id="edit-name"
                                value={formData.name}
                                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                                className="col-span-3"
                            />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                            <Label htmlFor="edit-category" className="text-right">
                                Category
                            </Label>
                            <Input
                                id="edit-category"
                                value={formData.category}
                                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                                className="col-span-3"
                            />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                            <Label htmlFor="edit-version" className="text-right">
                                Version
                            </Label>
                            <Input
                                id="edit-version"
                                value={formData.version}
                                onChange={(e) => setFormData({ ...formData, version: e.target.value })}
                                className="col-span-3"
                            />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                            <Label htmlFor="edit-description" className="text-right">
                                Description
                            </Label>
                            <Textarea
                                id="edit-description"
                                value={formData.description}
                                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                className="col-span-3"
                                rows={3}
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button onClick={handleEditProject}>Save Changes</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
