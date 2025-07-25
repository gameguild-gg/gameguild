import React from 'react';
import { notFound } from 'next/navigation';
import { AttendanceTracker } from '@/components/testing-lab/attendance-tracker/attendance-tracker';
import { getTestingAttendanceBySession, getTestingSessionBySlug } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithSlugParams } from '@/types';

export default async function AttendancePage({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  const session = await getTestingSessionBySlug(slug);

  if (!session) notFound();

  const attendanceData = await getTestingAttendanceBySession(slug);

  return (
    <div className="container space-y-6">
      <AttendanceTracker data={attendanceData} />
    </div>
  );
}
