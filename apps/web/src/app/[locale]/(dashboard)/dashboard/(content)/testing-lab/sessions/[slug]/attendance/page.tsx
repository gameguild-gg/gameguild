import React from 'react';
import { notFound } from 'next/navigation';
import { AttendanceTracker } from '@/components/testing-lab/attendance-tracker';
import { getTestingAttendanceBySession, getTestingSessionBySlug } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithSlugParams } from '@/types';

export default async function AttendancePage({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  // Verify session exists
  const session = await getTestingSessionBySlug(slug);
  if (!session) notFound();

  // Get attendance data filtered by session
  const attendanceData = await getTestingAttendanceBySession(slug);

  return (
    <div className="container space-y-6">
      <AttendanceTracker
        studentData={Array.isArray(attendanceData.students) ? attendanceData.students : []}
        sessionData={Array.isArray(attendanceData.sessions) ? attendanceData.sessions : []}
        sessionInfo={{
          id: session.id || '',
          sessionName: session.sessionName || 'Unknown Session',
        }}
      />
    </div>
  );
}
