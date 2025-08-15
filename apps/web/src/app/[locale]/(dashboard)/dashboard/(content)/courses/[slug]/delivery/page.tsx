// Server Component

import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { scheduleProgram } from '@/lib/content-management/programs/programs.actions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function CourseDeliveryPage({ params }: PageProps) {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function schedule(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const publishAt = String(formData.get('publishAt') || '');
    if (!publishAt) return;
    await scheduleProgram(program.id, publishAt);
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Delivery & Schedule</DashboardPageTitle>
        <DashboardPageDescription>Configure course availability and publication schedule.</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        {program ? (
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Publication schedule</CardTitle>
              </CardHeader>
              <CardContent>
                <form action={schedule} className="grid grid-cols-1 md:grid-cols-[1fr_auto] gap-3 items-end">
                  <div className="space-y-2">
                    <Label htmlFor="publishAt">Publish at</Label>
                    <Input id="publishAt" name="publishAt" type="datetime-local" />
                  </div>
                  <Button type="submit">Schedule</Button>
                </form>
                <div className="text-sm text-muted-foreground mt-3">Current status: {program.status ?? '—'}</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Enrollment</CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm text-muted-foreground">
                <div>
                  <div className="font-medium text-foreground">Status</div>
                  <div>{program.enrollmentStatus ?? '—'}</div>
                </div>
                <div>
                  <div className="font-medium text-foreground">Max enrollments</div>
                  <div>{program.maxEnrollments ?? '—'}</div>
                </div>
                <div>
                  <div className="font-medium text-foreground">Deadline</div>
                  <div>{program.enrollmentDeadline ?? '—'}</div>
                </div>
              </CardContent>
            </Card>
          </div>
        ) : (
          <div className="text-muted-foreground">Program not found.</div>
        )}
      </DashboardPageContent>
    </DashboardPage>
  );
}

