'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { Program } from '@/lib/api/generated/types.gen';
import { Skeleton } from '@/components/ui/skeleton';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';

export default function EditCoursePage() {
  const params = useParams();
  const slug = params.slug as string;

  const [program, setProgram] = useState<Program | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchProgram = async () => {
      try {
        setLoading(true);
        setError(null);

        const result = await getProgramBySlugService(slug);
        if (result.success && result.data) {
          setProgram(result.data);
        } else {
          setError(result.error || 'Program not found');
        }
      } catch (err) {
        console.error('Error fetching program:', err);
        setError('Failed to load program');
      } finally {
        setLoading(false);
      }
    };

    if (slug) {
      fetchProgram();
    }
  }, [slug]);

  if (loading) {
    return (
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>
            <Skeleton className="h-8 w-48" />
          </DashboardPageTitle>
          <DashboardPageDescription>
            <Skeleton className="h-4 w-64" />
          </DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <div className="space-y-8">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              <div className="lg:col-span-2 space-y-8">
                <Skeleton className="h-96 w-full" />
                <Skeleton className="h-64 w-full" />
              </div>
              <div className="space-y-8">
                <Skeleton className="h-48 w-full" />
                <Skeleton className="h-32 w-full" />
              </div>
            </div>
          </div>
        </DashboardPageContent>
      </DashboardPage>
    );
  }

  if (error || !program) {
    return (
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Program Not Found</DashboardPageTitle>
          <DashboardPageDescription>{error || 'The requested program could not be found.'}</DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <div className="text-center py-8">
            <p className="text-muted-foreground mb-4">{error || 'The requested program could not be found.'}</p>
            <Link href="/dashboard/courses" className="text-primary hover:underline">
              Return to Programs
            </Link>
          </div>
        </DashboardPageContent>
      </DashboardPage>
    );
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Edit Program: {program.title}</DashboardPageTitle>
        <DashboardPageDescription>Edit and manage your program content</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <div className="space-y-8">
          {/* Program Editor would go here */}
          <div className="bg-card rounded-lg border p-6">
            <h3 className="text-lg font-semibold mb-4">Program Details</h3>
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium">Title</label>
                <p className="text-sm text-muted-foreground">{program.title}</p>
              </div>
              {program.description && (
                <div>
                  <label className="text-sm font-medium">Description</label>
                  <p className="text-sm text-muted-foreground">{program.description}</p>
                </div>
              )}
              <div>
                <label className="text-sm font-medium">Status</label>
                <p className="text-sm text-muted-foreground capitalize">{program.status || 'draft'}</p>
              </div>
              <div>
                <label className="text-sm font-medium">Category</label>
                <p className="text-sm text-muted-foreground">{program.category || 'Not specified'}</p>
              </div>
              <div>
                <label className="text-sm font-medium">Difficulty</label>
                <p className="text-sm text-muted-foreground">{program.difficulty || 'Not specified'}</p>
              </div>
            </div>
          </div>
        </div>
      </DashboardPageContent>
    </DashboardPage>
  );
}
