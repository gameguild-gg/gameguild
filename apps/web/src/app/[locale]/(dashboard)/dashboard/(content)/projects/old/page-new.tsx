import { auth } from '@/auth';
import { ProjectManagementContent } from '@/components/project/project-management-content';
import { getProjectsData } from '@/lib/projects/projects.actions';
import type { Project } from '@/lib/api/generated/types.gen';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Loader2 } from 'lucide-react';

export default async function ProjectsPage() {
  const session = await auth();

  if (!session?.api.accessToken) {
    return (
      <div className="container mx-auto p-6">
        <div className="flex items-center justify-center min-h-64">
          <div className="text-center">
            <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
            <p className="text-gray-600">Loading authentication...</p>
          </div>
        </div>
      </div>
    );
  }

  let projects: Project[] = [];
  let error: string | null = null;

  try {
    // Fetch all projects from the system
    const projectsData = await getProjectsData({
      take: 100, // Load first 100 projects
    });
    projects = projectsData.projects;
  } catch (err) {
    error = err instanceof Error ? err.message : 'Failed to load projects';
  }

  if (error) {
    return (
      <div className="container mx-auto p-6">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Project Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage game projects, tools, and libraries across the platform.</p>
        </div>

        <Alert variant="destructive">
          <AlertDescription>Failed to load projects: {error}</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Project Management</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">Manage game projects, tools, and libraries across the platform.</p>
      </div>

      <ProjectManagementContent initialProjects={projects} />
    </div>
  );
}
