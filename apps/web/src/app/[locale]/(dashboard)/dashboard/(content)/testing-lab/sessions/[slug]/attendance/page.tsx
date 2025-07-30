import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { AttendanceTracker } from '@/components/testing-lab';
import { getTestingAttendanceBySession, getTestingSessionBySlug } from '@/lib/admin';
import { PropsWithSlugParams } from '@/types';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function AttendancePage({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  const session = await getTestingSessionBySlug(slug);

  if (!session) notFound();

  const attendanceData = await getTestingAttendanceBySession(slug);

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Lab</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <AttendanceTracker data={attendanceData} />
      </DashboardPageContent>
    </DashboardPage>
  );
}
