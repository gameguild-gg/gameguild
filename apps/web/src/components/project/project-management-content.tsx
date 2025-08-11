'use client';

import { useState, useEffect } from 'react';
import { Plus, RefreshCw, Search, Filter, Eye, Edit, Trash2, Archive, Upload } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { toast } from 'sonner';
import { getProjectsData, createProject, updateProject, deleteProject, publishProject, unpublishProject, archiveProject } from '@/lib/projects/projects.actions';
import type { Project } from '@/lib/api/generated/types.gen';

interface ProjectManagementContentProps {
  initialProjects: Project[];
}

export function ProjectManagementContent({ initialProjects }: ProjectManagementContentProps) {
  const [projects, setProjects] = useState<Project[]>(initialProjects);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);

  // Form state for create/edit
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    shortDescription: '',
    visibility: 1,
    category: '',
    type: '',
    developmentStatus: '',
    websiteUrl: '',
    repositoryUrl: '',
    downloadUrl: '',
    tags: '',
    imageUrl: '',
  });

  const resetForm = () => {
    setFormData({
      title: '',
      description: '',
      shortDescription: '',
      visibility: 1,
      category: '',
      type: '',
      developmentStatus: '',
      websiteUrl: '',
      repositoryUrl: '',
      downloadUrl: '',
      tags: '',
      imageUrl: '',
    });
  };

  const refreshData = async () => {
    setLoading(true);
    try {
      const result = await getProjectsData({
        searchTerm: searchTerm || undefined,
        skip: 0,
        take: 100,
      });
      setProjects(result.projects);
    } catch (error) {
      toast.error('Failed to refresh projects');
      console.error('Error refreshing projects:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    try {
      setLoading(true);
      const tagsArray = formData.tags ? formData.tags.split(',').map((tag) => tag.trim()) : [];

      await createProject({
        ...formData,
        tags: tagsArray,
      });

      toast.success('Project created successfully');
      setIsCreateDialogOpen(false);
      resetForm();
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to create project');
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = async () => {
    if (!selectedProject) return;

    try {
      setLoading(true);
      const tagsArray = formData.tags ? formData.tags.split(',').map((tag) => tag.trim()) : [];

      await updateProject(selectedProject.id!, {
        ...formData,
        tags: tagsArray,
      });

      toast.success('Project updated successfully');
      setIsEditDialogOpen(false);
      setSelectedProject(null);
      resetForm();
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to update project');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (project: Project) => {
    if (!confirm(`Are you sure you want to delete "${project.title}"? This action cannot be undone.`)) {
      return;
    }

    try {
      setLoading(true);
      await deleteProject(project.id!);
      toast.success('Project deleted successfully');
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to delete project');
    } finally {
      setLoading(false);
    }
  };

  const handlePublish = async (project: Project) => {
    try {
      setLoading(true);
      await publishProject(project.id!);
      toast.success('Project published successfully');
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to publish project');
    } finally {
      setLoading(false);
    }
  };

  const handleUnpublish = async (project: Project) => {
    try {
      setLoading(true);
      await unpublishProject(project.id!);
      toast.success('Project unpublished successfully');
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to unpublish project');
    } finally {
      setLoading(false);
    }
  };

  const handleArchive = async (project: Project) => {
    try {
      setLoading(true);
      await archiveProject(project.id!);
      toast.success('Project archived successfully');
      await refreshData();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to archive project');
    } finally {
      setLoading(false);
    }
  };

  const openEditDialog = (project: Project) => {
    setSelectedProject(project);
    setFormData({
      title: project.title || '',
      description: project.description || '',
      shortDescription: project.shortDescription || '',
      visibility: project.visibility || 1,
      category: project.categoryId || '',
      type: project.type?.toString() || '',
      developmentStatus: project.developmentStatus?.toString() || '',
      websiteUrl: project.websiteUrl || '',
      repositoryUrl: project.repositoryUrl || '',
      downloadUrl: project.downloadUrl || '',
      tags: project.tags || '',
      imageUrl: project.imageUrl || '',
    });
    setIsEditDialogOpen(true);
  };

  const filteredProjects = projects.filter((project) => {
    const matchesSearch = !searchTerm || project.title?.toLowerCase().includes(searchTerm.toLowerCase()) || project.description?.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus = statusFilter === 'all' || project.status?.toString() === statusFilter;

    const matchesType = typeFilter === 'all' || project.type?.toString() === typeFilter;

    return matchesSearch && matchesStatus && matchesType;
  });

  const getStatusBadge = (status: any) => {
    const statusMap: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
      '0': { label: 'Draft', variant: 'outline' },
      '1': { label: 'Published', variant: 'default' },
      '2': { label: 'Archived', variant: 'secondary' },
      '3': { label: 'Deleted', variant: 'destructive' },
    };

    const statusInfo = statusMap[status?.toString()] || { label: 'Unknown', variant: 'outline' as const };
    return <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-blue-100 rounded-lg">
            <Upload className="h-6 w-6 text-blue-600" />
          </div>
          <div>
            <h2 className="text-xl font-semibold">Projects ({projects.length})</h2>
            <p className="text-sm text-gray-600">Manage all projects in the system</p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" size="sm" onClick={refreshData} disabled={loading}>
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
            <DialogTrigger asChild>
              <Button size="sm">
                <Plus className="h-4 w-4 mr-2" />
                Add Project
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>Create New Project</DialogTitle>
                <DialogDescription>Add a new project to the system</DialogDescription>
              </DialogHeader>

              <div className="grid gap-4 py-4">
                <div className="grid gap-2">
                  <Label htmlFor="title">Title *</Label>
                  <Input id="title" value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} placeholder="Project title" />
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="shortDescription">Short Description</Label>
                  <Input id="shortDescription" value={formData.shortDescription} onChange={(e) => setFormData((prev) => ({ ...prev, shortDescription: e.target.value }))} placeholder="Brief description" />
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="description">Description</Label>
                  <Textarea id="description" value={formData.description} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} placeholder="Detailed description" rows={4} />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="websiteUrl">Website URL</Label>
                    <Input id="websiteUrl" value={formData.websiteUrl} onChange={(e) => setFormData((prev) => ({ ...prev, websiteUrl: e.target.value }))} placeholder="https://example.com" />
                  </div>

                  <div className="grid gap-2">
                    <Label htmlFor="repositoryUrl">Repository URL</Label>
                    <Input id="repositoryUrl" value={formData.repositoryUrl} onChange={(e) => setFormData((prev) => ({ ...prev, repositoryUrl: e.target.value }))} placeholder="https://github.com/user/repo" />
                  </div>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="tags">Tags (comma-separated)</Label>
                  <Input id="tags" value={formData.tags} onChange={(e) => setFormData((prev) => ({ ...prev, tags: e.target.value }))} placeholder="game, unity, indie" />
                </div>
              </div>

              <div className="flex justify-end space-x-2">
                <Button
                  variant="outline"
                  onClick={() => {
                    setIsCreateDialogOpen(false);
                    resetForm();
                  }}
                >
                  Cancel
                </Button>
                <Button onClick={handleCreate} disabled={loading || !formData.title}>
                  {loading ? 'Creating...' : 'Create Project'}
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input placeholder="Search projects..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-10" />
            </div>

            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Filter by status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value="0">Draft</SelectItem>
                <SelectItem value="1">Published</SelectItem>
                <SelectItem value="2">Archived</SelectItem>
              </SelectContent>
            </Select>

            <Select value={typeFilter} onValueChange={setTypeFilter}>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Filter by type" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Types</SelectItem>
                <SelectItem value="0">Game</SelectItem>
                <SelectItem value="1">Tool</SelectItem>
                <SelectItem value="2">Library</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Projects Table */}
      <Card>
        <CardHeader>
          <CardTitle>Projects List</CardTitle>
          <CardDescription>{filteredProjects.length} projects found</CardDescription>
        </CardHeader>
        <CardContent>
          {filteredProjects.length === 0 ? (
            <div className="text-center py-8">
              <Upload className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No projects found</h3>
              <p className="text-gray-600 mb-4">Get started by creating your first project.</p>
              <Button onClick={() => setIsCreateDialogOpen(true)}>
                <Plus className="h-4 w-4 mr-2" />
                Create Project
              </Button>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredProjects.map((project) => (
                  <TableRow key={project.id}>
                    <TableCell>
                      <div>
                        <div className="font-medium">{project.title}</div>
                        {project.shortDescription && <div className="text-sm text-gray-600">{project.shortDescription}</div>}
                      </div>
                    </TableCell>
                    <TableCell>{getStatusBadge(project.status)}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{project.type?.toString() || 'Unknown'}</Badge>
                    </TableCell>
                    <TableCell>{project.createdAt ? new Date(project.createdAt).toLocaleDateString() : 'Unknown'}</TableCell>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        <Button variant="ghost" size="sm" onClick={() => openEditDialog(project)}>
                          <Edit className="h-4 w-4" />
                        </Button>
                        {project.status?.toString() === '0' && (
                          <Button variant="ghost" size="sm" onClick={() => handlePublish(project)}>
                            <Upload className="h-4 w-4" />
                          </Button>
                        )}
                        {project.status?.toString() === '1' && (
                          <Button variant="ghost" size="sm" onClick={() => handleUnpublish(project)}>
                            <Archive className="h-4 w-4" />
                          </Button>
                        )}
                        <Button variant="ghost" size="sm" onClick={() => handleDelete(project)}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Edit Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit Project</DialogTitle>
            <DialogDescription>Update project information</DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="edit-title">Title *</Label>
              <Input id="edit-title" value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} placeholder="Project title" />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-shortDescription">Short Description</Label>
              <Input id="edit-shortDescription" value={formData.shortDescription} onChange={(e) => setFormData((prev) => ({ ...prev, shortDescription: e.target.value }))} placeholder="Brief description" />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-description">Description</Label>
              <Textarea id="edit-description" value={formData.description} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} placeholder="Detailed description" rows={4} />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label htmlFor="edit-websiteUrl">Website URL</Label>
                <Input id="edit-websiteUrl" value={formData.websiteUrl} onChange={(e) => setFormData((prev) => ({ ...prev, websiteUrl: e.target.value }))} placeholder="https://example.com" />
              </div>

              <div className="grid gap-2">
                <Label htmlFor="edit-repositoryUrl">Repository URL</Label>
                <Input id="edit-repositoryUrl" value={formData.repositoryUrl} onChange={(e) => setFormData((prev) => ({ ...prev, repositoryUrl: e.target.value }))} placeholder="https://github.com/user/repo" />
              </div>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-tags">Tags (comma-separated)</Label>
              <Input id="edit-tags" value={formData.tags} onChange={(e) => setFormData((prev) => ({ ...prev, tags: e.target.value }))} placeholder="game, unity, indie" />
            </div>
          </div>

          <div className="flex justify-end space-x-2">
            <Button
              variant="outline"
              onClick={() => {
                setIsEditDialogOpen(false);
                setSelectedProject(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button onClick={handleEdit} disabled={loading || !formData.title}>
              {loading ? 'Updating...' : 'Update Project'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
