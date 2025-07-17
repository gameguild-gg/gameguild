'use client';

import { useEffect, useState } from 'react';
import { signIn, signOut, useSession } from 'next-auth/react';
import { clearProjectCache, getProjects, ProjectListItem, revalidateProjects } from '@/components/legacy/projects/actions';
import { ProjectList } from '@/components/legacy/projects/project-list';
import { useRouter } from 'next/navigation';

// Utility function to force fresh authentication
const forceReAuthentication = async () => {
  console.log('🔄 Forcing fresh authentication...');
  await signOut({ redirect: false });
  await new Promise((resolve) => setTimeout(resolve, 1000)); // Wait for signOut to complete
  await signIn();
};

export default function ProjectsPage() {
  const [projects, setProjects] = useState<ProjectListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const { data: session, status } = useSession();
  const router = useRouter();

  const createTestProject = async () => {
    try {
      setCreating(true);
      console.log('Creating test project...');

      const { testCreateProject } = await import('@/lib/core/actions');

      const result = await testCreateProject({
        title: `Test Project ${new Date().toLocaleTimeString()}`,
        description: 'This is a test project created to verify authentication and data flow',
        shortDescription: 'Test project for verification',
        status: 'not-started',
        visibility: 'Public',
        tags: ['test', 'verification'],
        technologies: ['Next.js', 'TypeScript'],
        difficulty: 'Beginner',
        estimatedHours: 1,
        category: 'Web Development',
        websiteUrl: 'https://example.com',
        repositoryUrl: 'https://github.com/example/test',
      });

      console.log('Project created successfully:', result); // Clear all project-related cache
      await clearProjectCache();

      // Revalidate Next.js cache
      await revalidateProjects();

      // Force router refresh to clear any cached data
      router.refresh();

      // Add a small delay to ensure cache clearing takes effect
      await new Promise((resolve) => setTimeout(resolve, 300));

      // Refresh the projects list
      const projectsData = await getProjects();
      setProjects(projectsData);
    } catch (err) {
      console.error('Error creating project:', err);
      setError('Error creating test project');
    } finally {
      setCreating(false);
    }
  };
  const handleRefreshProjects = async () => {
    try {
      setLoading(true);
      setError(null);

      // Clear all project-related cache
      await clearProjectCache();

      // Revalidate Next.js cache
      await revalidateProjects();

      // Force router refresh
      router.refresh();

      // Add a small delay to ensure cache clearing takes effect
      await new Promise((resolve) => setTimeout(resolve, 500));

      // Fetch fresh data
      const projectsData = await getProjects();
      setProjects(projectsData);
    } catch (err) {
      console.error('Error refreshing projects:', err);
      setError('Failed to refresh projects');
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => {
    const fetchProjects = async () => {
      try {
        setLoading(true);
        setError(null);

        // Wait for session to load
        if (status === 'loading') {
          return;
        }

        // Check if user is authenticated
        if (status === 'unauthenticated') {
          setError('Please sign in to view projects');
          return;
        }
        // Check for token refresh errors
        if ((session as any)?.error === 'RefreshTokenError') {
          console.log('🔄 Token refresh error detected, forcing fresh authentication...');
          setError('Your session has expired. Signing you out and redirecting to login...');
          await forceReAuthentication();
          return;
        }

        console.log('Fetching projects for authenticated user...');
        const projectsData = await getProjects();
        setProjects(projectsData);
      } catch (err) {
        console.error('Error fetching projects:', err);
        // Check if it's an authentication error
        if (
          err instanceof Error &&
          (err.message.includes('Authentication token expired') || err.message.includes('401') || err.message.includes('Unauthorized'))
        ) {
          console.log('🔄 Authentication error, forcing fresh authentication...');
          setError('Your session has expired. Signing you out and redirecting to login...');
          await forceReAuthentication();
        } else {
          setError('Failed to load projects');
        }
      } finally {
        setLoading(false);
      }
    };

    fetchProjects();
  }, [status, session]);

  if (loading) {
    return (
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-muted-foreground">Loading projects...</p>
        </div>
      </div>
    );
  }
  if (error) {
    return (
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="text-center">
          <p className="text-red-500 mb-4">{error}</p>
          <p className="text-muted-foreground mb-2">Session Status: {status}</p>
          <p className="text-muted-foreground mb-2">User: {session?.user?.email || 'Not authenticated'}</p>
          <p className="text-muted-foreground mb-4">Has Access Token: {session?.accessToken ? 'Yes' : 'No'}</p>
          <p className="text-muted-foreground mb-4">Session Error: {(session as any)?.error || 'None'}</p>

          {(status === 'unauthenticated' ||
            (session as any)?.error === 'RefreshTokenError' ||
            error.includes('session has expired') ||
            error.includes('sign in again')) && (
            <button onClick={() => signIn()} className="px-6 py-2 bg-blue-500 text-white rounded hover:bg-blue-600">
              Sign In Again
            </button>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col flex-1 items-center justify-center">
      <div className="flex flex-col flex-1 container">
        {' '}
        <div className="mb-4 p-4 bg-muted rounded-lg">
          <div className="flex justify-between items-center">
            <div className="text-sm text-muted-foreground">
              <p>Session Status: {status}</p>
              <p>User: {session?.user?.email || 'Not authenticated'}</p>
              <p>Has Access Token: {session?.accessToken ? 'Yes' : 'No'}</p>
              <p>Projects Found: {projects.length}</p>
              <p>Last Fetch: {new Date().toLocaleTimeString()}</p>
            </div>
            <div className="flex gap-2">
              <button
                onClick={createTestProject}
                disabled={creating || status !== 'authenticated'}
                className="px-4 py-2 bg-blue-500 text-white rounded disabled:opacity-50"
              >
                {creating ? 'Creating...' : 'Create Test Project'}
              </button>{' '}
              <button onClick={handleRefreshProjects} disabled={loading} className="px-4 py-2 bg-gray-500 text-white rounded disabled:opacity-50">
                {loading ? 'Refreshing...' : 'Force Refresh & Revalidate'}
              </button>
              <button onClick={forceReAuthentication} className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600">
                Force Re-Auth
              </button>
            </div>
          </div>
        </div>
        <ProjectList initialProjects={projects} />
      </div>
    </div>
  );
}

// 'use client'; // todo: I dont think all things from dashboard should be client side
//
// import React, { useEffect, useState } from 'react';
// import Link from 'next/link';
// import { Button } from '@/components/ui/button';
// import { GameCard } from '@/components/testing-lab/game-card';
// import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
// import ProjectForm from '@/components/projects/project-form';
// import { getSession } from 'next-auth/react';
// import { useRouter } from 'next/navigation';
// import { Api, ProjectApi } from '@game-guild/apiclient';
// import ProjectEntity = Api.ProjectEntity;
//
// export default async function Page() {
//   const [projects, setProjects] = useState<ProjectEntity[] | null>(null);
//   const router = useRouter();
//
//   const fetchProjects = async () => {
//     if (projects === null) {
//       const session = await getSession();
//       const api = new ProjectApi({ basePath: process.env.NEXT_PUBLIC_API_URL });
//       const projects = await api.getManyBaseProjectControllerProjectEntity({}, { headers: { Authorization: `Bearer ${session?.user?.accessToken}` } });
//       if (projects.status == 401) {
//         router.push('/disconnect');
//       } else if (projects.status >= 400) {
//         console.error(projects.body);
//       }
//       setProjects(projects.body as ProjectEntity[]);
//     }
//   };
//
//   useEffect(() => {
//     fetchProjects();
//   });
//
//   return (
//     <div className="flex flex-col flex-1">
//       <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
//         <div className="bg-white shadow rounded-lg overflow-hidden">
//           <div className="p-6">
//             <h1 className="text-2xl font-semibold text-gray-900">Creator Dashboard</h1>
//             <div className="mt-4 flex justify-end space-x-4">
//               <div className="text-center">
//                 <div className="text-2xl font-bold">0</div>
//                 <div className="text-sm text-gray-500">Views</div>
//               </div>
//               <div className="text-center">
//                 <div className="text-2xl font-bold">0</div>
//                 <div className="text-sm text-gray-500">Downloads</div>
//               </div>
//               <div className="text-center">
//                 <div className="text-2xl font-bold">0</div>
//                 <div className="text-sm text-gray-500">Followers</div>
//               </div>
//             </div>
//           </div>
//           <div className="border-t border-gray-200">
//             <nav className="flex">
//               <Link href="#" className="px-6 py-3 text-sm font-medium text-pink-500 border-b-2 border-pink-500">
//                 Projects
//               </Link>
//               <Link href="#" className="px-6 py-3 text-sm font-medium text-gray-500 hover:text-gray-700">
//                 Analytics
//               </Link>
//               <Link href="#" className="px-6 py-3 text-sm font-medium text-gray-500 hover:text-gray-700">
//                 Earnings
//               </Link>
//               <Link href="#" className="px-6 py-3 text-sm font-medium text-gray-500 hover:text-gray-700">
//                 Promotions
//               </Link>
//               <Link href="#" className="px-6 py-3 text-sm font-medium text-gray-500 hover:text-gray-700">
//                 Posts
//               </Link>
//               <Link href="#" className="px-6 py-3 text-sm font-medium text-gray-500 hover:text-gray-700">
//                 Game jams
//               </Link>
//               <Link href="#" className="px-6 py-3 text-sm font-medium text-gray-500 hover:text-gray-700">
//                 More
//               </Link>
//             </nav>
//           </div>
//           <div className="p-6">
//             {/*<div className="bg-pink-50 border border-pink-100 rounded-md p-4 mb-6">*/}
//             {/*  <p className="text-pink-800">*/}
//             {/*    <span className="font-semibold">itch.io tips:</span> Tell us how*/}
//             {/*    you build your projects • Try the Engines, Tools, & Devices*/}
//             {/*    classification{' '}*/}
//             {/*    <Link href="#" className="text-pink-600 hover:underline">*/}
//             {/*      learn more →*/}
//             {/*    </Link>*/}
//             {/*  </p>*/}
//             {/*</div>*/}
//             {projects?.length === 0 && (
//               <div className="text-center py-12">
//                 <h2 className="text-2xl font-semibold text-gray-700 mb-6">Are you a developer? Upload your first game</h2>
//                 <Sheet>
//                   <SheetTrigger asChild>
//                     <Button className="bg-pink-500 text-white hover:bg-pink-600">Create new project</Button>
//                   </SheetTrigger>
//                   <SheetContent className="min-w-full">
//                     <ProjectForm action={'update'} />
//                   </SheetContent>
//                 </Sheet>
//                 <div className="mt-4">
//                   <Link href="#" className="text-sm text-gray-500 hover:underline">
//                     Nah, take me to the games feed
//                   </Link>
//                 </div>
//               </div>
//             )}
//             {projects?.map((project) => (
//               <Link key={project.slug} href={`/project/${project.slug}`}>
//                 <GameCard game={project} />
//               </Link>
//             ))}
//           </div>
//         </div>
//       </main>
//     </div>
//   );
// }
