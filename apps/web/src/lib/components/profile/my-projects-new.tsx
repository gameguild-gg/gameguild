'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { createProject, deleteProject, getProjectsByUser, updateProject } from '@/lib/api/projects-server.actions';
import { type Project } from '@/lib/api/projects-simple';
import { Calendar, Edit2, Eye, Gamepad2, Plus, Star, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface MyProjectsProps {
    userId: string;
    username: string;
}

export function MyProjects({ userId, username }: MyProjectsProps) {
    const [projects, setProjects] = useState<Project[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
    const [editingProject, setEditingProject] = useState<Project | null>(null);
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        category: '',
        gameVersion: '1.0.0',
        status: 'development' as 'development' | 'beta' | 'released' | 'archived',
        isPublic: true
    });

    // Load projects from API
    useEffect(() => {
        const loadProjects = async () => {
            setIsLoading(true);
            try {
                // Use the proper API call to get projects for this user
                const projectsData = await getProjectsByUser(userId);
                setProjects(projectsData);
            } catch (error) {
                console.error('Error loading projects:', error);
                toast.error('Failed to load projects');
            } finally {
                setIsLoading(false);
            }
        };

        loadProjects();
    }, [userId, username]);

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

    const handleCreateProject = async () => {
        if (!formData.name.trim()) {
            toast.error('Project name is required');
            return;
        }

        try {
            // Use the proper API call to create a project
            const newProject = await createProject({
                name: formData.name,
                description: formData.description,
                category: formData.category,
                gameVersion: formData.gameVersion,
                isPublic: formData.isPublic,
                tags: []
            });

            if (newProject) {
                setProjects([...projects, newProject]);
                setIsCreateDialogOpen(false);
                setFormData({
                    name: '',
                    description: '',
                    category: '',
                    gameVersion: '1.0.0',
                    status: 'development',
                    isPublic: true
                });
                toast.success('Project created successfully!');
            } else {
                toast.error('Failed to create project');
            }
        } catch (error) {
            console.error('Error creating project:', error);
            toast.error('Failed to create project');
        }
    };

    const handleEditProject = async () => {
        if (!editingProject || !formData.name.trim()) {
            toast.error('Project name is required');
            return;
        }

        try {
            // Use the proper API call to update a project
            const updatedProject = await updateProject(editingProject.id, {
                name: formData.name,
                description: formData.description,
                category: formData.category,
                gameVersion: formData.gameVersion,
                isPublic: formData.isPublic,
                expectedVersion: editingProject.version
            });

            if (updatedProject) {
                setProjects(projects.map(p => p.id === editingProject.id ? updatedProject : p));
                setIsEditDialogOpen(false);
                setEditingProject(null);
                toast.success('Project updated successfully!');
            } else {
                toast.error('Failed to update project');
            }
        } catch (error) {
            console.error('Error updating project:', error);
            toast.error('Failed to update project');
        }
    };

    const handleDeleteProject = async (projectId: string) => {
        try {
            // Use the proper API call to delete a project
            const success = await deleteProject(projectId);

            if (success) {
                setProjects(projects.filter(p => p.id !== projectId));
                toast.success('Project deleted successfully!');
            } else {
                toast.error('Failed to delete project');
            }
        } catch (error) {
            console.error('Error deleting project:', error);
            toast.error('Failed to delete project');
        }
    };

    const openEditDialog = (project: Project) => {
        setEditingProject(project);
        setFormData({
            name: project.name,
            description: project.description,
            category: project.category,
            gameVersion: project.gameVersion || '1.0.0',
            status: project.status,
            isPublic: project.isPublic
        });
        setIsEditDialogOpen(true);
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    if (isLoading) {
        return (
            <div className="space-y-6">
                <div className="flex items-center justify-between">
                    <div>
                        <h2 className="text-2xl font-bold text-white">My Projects</h2>
                        <p className="text-gray-400 mt-1">Loading projects...</p>
                    </div>
                </div>
                <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                    {[1, 2, 3].map((i) => (
                        <Card key={i} className="bg-slate-800/50 border-purple-500/20 animate-pulse">
                            <CardHeader>
                                <div className="h-6 bg-gray-700 rounded"></div>
                                <div className="h-4 bg-gray-700 rounded w-3/4"></div>
                            </CardHeader>
                            <CardContent>
                                <div className="h-4 bg-gray-700 rounded mb-2"></div>
                                <div className="h-4 bg-gray-700 rounded w-1/2"></div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            </div>
        );
    }

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
                                Add a new project to your portfolio. Share your game development journey.
                            </DialogDescription>
                        </DialogHeader>
                        <div className="grid gap-4 py-4">
                            <div className="grid gap-2">
                                <Label htmlFor="name">Project Name</Label>
                                <Input
                                    id="name"
                                    value={formData.name}
                                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                                    placeholder="Enter project name"
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="description">Description</Label>
                                <Textarea
                                    id="description"
                                    value={formData.description}
                                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                    placeholder="Describe your project"
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="category">Category</Label>
                                <Input
                                    id="category"
                                    value={formData.category}
                                    onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                                    placeholder="e.g., Action, RPG, Puzzle"
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="version">Version</Label>
                                <Input
                                    id="version"
                                    value={formData.gameVersion}
                                    onChange={(e) => setFormData({ ...formData, gameVersion: e.target.value })}
                                    placeholder="e.g., 1.0.0"
                                />
                            </div>
                        </div>
                        <DialogFooter>
                            <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
                                Cancel
                            </Button>
                            <Button onClick={handleCreateProject} className="bg-purple-600 hover:bg-purple-700">
                                Create Project
                            </Button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>
            </div>

            {/* Projects Grid */}
            {projects.length === 0 ? (
                <Card className="bg-slate-800/50 border-purple-500/20">
                    <CardContent className="flex flex-col items-center justify-center py-12 text-center">
                        <Gamepad2 className="h-12 w-12 text-gray-500 mb-4" />
                        <h3 className="text-lg font-medium text-white mb-2">No projects yet</h3>
                        <p className="text-gray-400 mb-4">
                            Start building your portfolio by creating your first project.
                        </p>
                        <Button
                            onClick={() => setIsCreateDialogOpen(true)}
                            className="bg-purple-600 hover:bg-purple-700"
                        >
                            <Plus className="h-4 w-4 mr-2" />
                            Create Your First Project
                        </Button>
                    </CardContent>
                </Card>
            ) : (
                <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                    {projects.map((project) => (
                        <Card key={project.id} className="bg-slate-800/50 border-purple-500/20 hover:border-purple-400/30 transition-colors">
                            <CardHeader>
                                <div className="flex items-start justify-between">
                                    <div>
                                        <CardTitle className="text-white text-lg mb-1">
                                            {project.name}
                                        </CardTitle>
                                        <div className="flex items-center space-x-2">
                                            <Badge variant="outline" className={getStatusColor(project.status)}>
                                                {getStatusText(project.status)}
                                            </Badge>
                                            {project.rating && (
                                                <div className="flex items-center">
                                                    <Star className="h-3 w-3 text-yellow-400 fill-current" />
                                                    <span className="text-xs text-gray-400 ml-1">
                                                        {project.rating.toFixed(1)}
                                                    </span>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                    <div className="flex space-x-1">
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            onClick={() => openEditDialog(project)}
                                            className="h-8 w-8 p-0 text-gray-400 hover:text-white"
                                        >
                                            <Edit2 className="h-3 w-3" />
                                        </Button>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            onClick={() => handleDeleteProject(project.id)}
                                            className="h-8 w-8 p-0 text-gray-400 hover:text-red-400"
                                        >
                                            <Trash2 className="h-3 w-3" />
                                        </Button>
                                    </div>
                                </div>
                            </CardHeader>
                            <CardContent>
                                <CardDescription className="text-gray-400 mb-4 line-clamp-3">
                                    {project.description}
                                </CardDescription>
                                <div className="space-y-2 text-xs text-gray-500">
                                    <div className="flex items-center justify-between">
                                        <span>Category: {project.category}</span>
                                        <span>v{project.gameVersion}</span>
                                    </div>
                                    <div className="flex items-center justify-between">
                                        <span className="flex items-center">
                                            <Calendar className="h-3 w-3 mr-1" />
                                            Created: {formatDate(project.createdAt)}
                                        </span>
                                    </div>
                                    <div className="flex items-center justify-between">
                                        <span>Updated: {formatDate(project.updatedAt)}</span>
                                        <span className={project.isPublic ? 'text-green-400' : 'text-yellow-400'}>
                                            {project.isPublic ? 'Public' : 'Private'}
                                        </span>
                                    </div>
                                </div>
                            </CardContent>
                            <CardFooter className="pt-4">
                                <Link
                                    href={`/users/${username}/projects/${project.id}`}
                                    className="w-full"
                                >
                                    <Button variant="outline" className="w-full border-purple-500/20 hover:border-purple-400/30">
                                        <Eye className="h-4 w-4 mr-2" />
                                        View Details
                                    </Button>
                                </Link>
                            </CardFooter>
                        </Card>
                    ))}
                </div>
            )}

            {/* Edit Project Dialog */}
            <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
                <DialogContent className="sm:max-w-[425px]">
                    <DialogHeader>
                        <DialogTitle>Edit Project</DialogTitle>
                        <DialogDescription>
                            Update your project information.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4 py-4">
                        <div className="grid gap-2">
                            <Label htmlFor="edit-name">Project Name</Label>
                            <Input
                                id="edit-name"
                                value={formData.name}
                                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                                placeholder="Enter project name"
                            />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="edit-description">Description</Label>
                            <Textarea
                                id="edit-description"
                                value={formData.description}
                                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                placeholder="Describe your project"
                            />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="edit-category">Category</Label>
                            <Input
                                id="edit-category"
                                value={formData.category}
                                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                                placeholder="e.g., Action, RPG, Puzzle"
                            />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="edit-version">Version</Label>
                            <Input
                                id="edit-version"
                                value={formData.gameVersion}
                                onChange={(e) => setFormData({ ...formData, gameVersion: e.target.value })}
                                placeholder="e.g., 1.0.0"
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setIsEditDialogOpen(false)}>
                            Cancel
                        </Button>
                        <Button onClick={handleEditProject} className="bg-purple-600 hover:bg-purple-700">
                            Save Changes
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
