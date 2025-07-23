import { auth } from '@/auth';
import { redirect } from 'next/navigation';
import { ProjectManagementContent } from '@/components/projects/project-management-content';
import { getProjectsData } from '@/lib/projects/projects.actions';

export default async function ProjectsPage() {
  const session = await auth();

  if (!session) {
    redirect('/login');
  }

  try {
    // Fetch initial data
    const projectsResult = await getProjectsData({ take: 100 });

    return (
      <div className="container mx-auto py-6">
        <ProjectManagementContent initialProjects={projectsResult.projects} />
      </div>
    );
  } catch (error) {
    console.error('Error fetching projects data:', error);

    return (
      <div className="container mx-auto py-6">
        <div className="text-center py-8">
          <h2 className="text-xl font-semibold text-red-600 mb-2">Error Loading Projects</h2>
          <p className="text-gray-600 mb-4">Failed to load projects data. Please check your connection and try again.</p>
          <p className="text-sm text-gray-500">Error: {error instanceof Error ? error.message : 'Unknown error'}</p>
        </div>
      </div>
    );
  }
}
