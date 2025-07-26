import React from 'react';
import { notFound } from 'next/navigation';
import { AttendanceTracker } from '@/components/testing-lab/attendance-tracker/attendance-tracker';
import { getTestingAttendanceBySession, getTestingSessionBySlug } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithSlugParams } from '@/types';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

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
