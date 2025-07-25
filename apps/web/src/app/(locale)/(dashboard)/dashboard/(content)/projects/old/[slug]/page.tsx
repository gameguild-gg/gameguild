'use client';

import React, { useEffect, useState } from 'react';
import { notFound, useRouter } from 'next/navigation';
import { useSession } from 'next-auth/react';
import { PropsWithLocaleSlugParams } from '@/types';
import { getProjectBySlug } from '@/components/legacy/projects/actions';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CalendarIcon, Download, Edit, MessageSquare } from 'lucide-react';
import Link from 'next/link';

// Simple project type based on the current CMS structure
type Project = {
  id: string;
  title: string;
  description?: string;
  shortDescription?: string;
  status: 'Draft' | 'UnderReview' | 'Published' | 'Archived';
  visibility: 'Private' | 'Public' | 'Restricted';
  websiteUrl?: string;
  repositoryUrl?: string;
  createdAt?: string;
  updatedAt?: string;
  createdBy?: {
    id: string;
    name: string;
  };
};

// get locale and slug from the URL
export default function Page(paramsProps: PropsWithLocaleSlugParams) {
  const params = React.use(paramsProps.params);
  const { slug, locale } = params;
  const [project, setProject] = useState<Project | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const { data: session, status } = useSession();
  const router = useRouter();

  // Manual refresh function
  const handleRefresh = () => {
    setRefreshKey((prev) => prev + 1);
  };

  // Load project data
  useEffect(() => {
    const loadProject = async () => {
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

        // Add a timestamp to force fresh data
        const timestamp = Date.now();
        console.log(`Loading project with slug: ${slug} at ${timestamp}`);

        // Fetch the actual project from the CMS API
        const projectData = await getProjectBySlug(slug);

        if (!projectData) {
          setError('Project not found');
          return;
        }

        setProject(projectData as Project);
      } catch (err) {
        setError('Failed to load project');
        console.error('Error loading project:', err);
      } finally {
        setLoading(false);
      }
    };

    loadProject();
  }, [slug, status, refreshKey]); // Include refreshKey in dependencies

  if (loading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-muted-foreground">Loading project...</p>
        </div>
      </div>
    );
  }

  if (error || !project) {
    notFound();
  }

  return (
    <div className="min-h-screen bg-background">
      {' '}
      {/* Banner */}
      <div className="relative h-64 md:h-96 bg-gradient-to-br from-zinc-800 to-zinc-900">
        {/* CSS-based placeholder banner */}
        <div className="absolute inset-0 bg-gradient-to-br from-zinc-800 via-zinc-700 to-zinc-900 opacity-90" />
        <div className="absolute inset-0 flex items-center justify-center">
          <h1 className="text-4xl md:text-6xl font-bold text-white text-center px-4 drop-shadow-lg">{project.title}</h1>
        </div>
      </div>
      {/* Content */}
      <div className="container mx-auto px-4 py-8">
        {/* Debug and Refresh Panel */}
        <div className="mb-6 p-4 bg-muted rounded-lg">
          <div className="flex justify-between items-center">
            <div className="text-sm text-muted-foreground">
              <p>Session Status: {status}</p>
              <p>User: {session?.user?.email || 'Not authenticated'}</p>
              <p>Has Access Token: {session?.accessToken ? 'Yes' : 'No'}</p>
              <p>Project ID: {project.id}</p>
              <p>Project Status: {project.status}</p>
              <p>Last Refresh: {new Date().toLocaleTimeString()}</p>
            </div>
            <Button onClick={handleRefresh} disabled={loading} variant="outline">
              {loading ? 'Loading...' : 'Refresh Data'}
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Main Content */}
          <div className="lg:col-span-2">
            {/* Featured Media */}
            <div className="mb-8">
              <div className="aspect-video mb-4">
                <div className="w-full h-full bg-muted rounded-lg flex items-center justify-center">
                  <p className="text-muted-foreground">Project Video/Demo</p>
                </div>
              </div>

              {/* Screenshots */}
              <div className="grid grid-cols-2 gap-4">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="aspect-video bg-muted rounded-lg flex items-center justify-center">
                    <p className="text-muted-foreground text-sm">Screenshot {i}</p>
                  </div>
                ))}
              </div>
            </div>

            <Card>
              <CardHeader>
                <CardTitle>About this Project</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground mb-4">{project.description || project.shortDescription}</p>

                {project.websiteUrl && (
                  <div className="mb-4">
                    <h3 className="font-semibold mb-2">Website</h3>
                    <Link href={project.websiteUrl} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
                      {project.websiteUrl}
                    </Link>
                  </div>
                )}

                {project.repositoryUrl && (
                  <div className="mb-4">
                    <h3 className="font-semibold mb-2">Repository</h3>
                    <Link href={project.repositoryUrl} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
                      {project.repositoryUrl}
                    </Link>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Versions */}
            <Card className="mt-6">
              <CardHeader>
                <CardTitle>Versions</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <Card className="border">
                    <CardContent className="p-4">
                      <div className="flex justify-between items-center">
                        <div>
                          <h4 className="font-semibold">Version 1.0.0</h4>
                          <p className="text-sm text-muted-foreground">Initial release with core features</p>
                          <div className="flex items-center mt-2 text-xs text-muted-foreground">
                            <CalendarIcon className="mr-1 h-3 w-3" />
                            {new Date().toLocaleDateString()}
                          </div>
                        </div>
                        <Button size="sm" variant="outline">
                          <Download className="mr-2 h-4 w-4" />
                          Download
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Project Actions */}
            <Card>
              <CardHeader>
                <CardTitle>Project Actions</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <Button className="w-full" size="lg">
                  <Download className="mr-2 h-4 w-4" />
                  Download Latest
                </Button>
                <Button className="w-full" variant="outline">
                  <MessageSquare className="mr-2 h-4 w-4" />
                  Comment
                </Button>
                <Button className="w-full" variant="outline" onClick={() => router.push(`/dashboard/projects/${slug}/edit`)}>
                  <Edit className="mr-2 h-4 w-4" />
                  Edit Project
                </Button>
              </CardContent>
            </Card>

            {/* Project Info */}
            <Card>
              <CardHeader>
                <CardTitle>Project Info</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-center text-sm text-muted-foreground">
                  <CalendarIcon className="mr-2 h-4 w-4" />
                  Created {project.createdAt ? new Date(project.createdAt).toLocaleDateString() : 'Unknown'}
                </div>
                <div className="text-sm">
                  <span className="font-semibold">Status:</span>{' '}
                  <span
                    className={`px-2 py-1 rounded-full text-xs ${
                      project.status === 'Published'
                        ? 'bg-green-100 text-green-800'
                        : project.status === 'Draft'
                          ? 'bg-gray-100 text-gray-800'
                          : project.status === 'UnderReview'
                            ? 'bg-yellow-100 text-yellow-800'
                            : 'bg-red-100 text-red-800'
                    }`}
                  >
                    {project.status}
                  </span>
                </div>
                <div className="text-sm">
                  <span className="font-semibold">Visibility:</span>{' '}
                  <span
                    className={`px-2 py-1 rounded-full text-xs ${
                      project.visibility === 'Public'
                        ? 'bg-blue-100 text-blue-800'
                        : project.visibility === 'Private'
                          ? 'bg-gray-100 text-gray-800'
                          : 'bg-orange-100 text-orange-800'
                    }`}
                  >
                    {project.visibility}
                  </span>
                </div>
                {project.createdBy && (
                  <div className="text-sm">
                    <span className="font-semibold">Created by:</span> {project.createdBy.name}
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Project Stats */}
            <Card>
              <CardHeader>
                <CardTitle>Project Stats</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Downloads</span>
                    <span className="font-semibold">1,234</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Views</span>
                    <span className="font-semibold">5,678</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Likes</span>
                    <span className="font-semibold">89</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
