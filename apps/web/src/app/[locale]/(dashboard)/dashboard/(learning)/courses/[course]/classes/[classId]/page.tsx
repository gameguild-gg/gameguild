import React from 'react';
import { notFound, forbidden } from 'next/navigation';
import { getCourse, getCourseClass } from '@/lib/learning';

/**
 * L6a: Single Class Detail/Editor Page
 *
 * Route: /dashboard/learning/courses/[course]/classes/[classId]
 *
 * Displays full class detail for viewing or editing.
 * Only available when course.features.hasClasses = true.
 *
 * Data Pattern:
 * - getCourseClass(classId) is NOT preloaded by layout (classId unknown)
 * - Validates course exists AND has classes feature
 * - Then fetches specific class detail
 *
 * UI Responsibility (not implemented here):
 * - Class info editing (title, description, schedule)
 * - Location management (virtual meeting URL / physical address)
 * - Attendee list with status (registered, attended, absent)
 * - Materials management (slides, documents, links)
 * - Session controls (start live, end session, enable recording)
 * - Post-session: link recording, attendance report
 */
export default async function ClassDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/classes/[classId]'>): Promise<React.JSX.Element> {
  const { course: courseId, classId } = await params;

  // First validate course and feature access
  const course = await getCourse(courseId);

  if (!course) {
    notFound();
  }

  if (!course.features.hasClasses) {
    forbidden(); // 403 - route not applicable for this course type
  }

  // Now fetch the specific class
  const classDetail = await getCourseClass(classId);

  if (!classDetail) {
    notFound();
  }

  // ==========================================================================
  // DATA AVAILABLE FOR UI:
  // - course: CourseDetails (for breadcrumb context)
  // - classDetail: CourseClassDetail
  //   {
  //     id, title, description, status, scheduledAt, duration, timezone,
  //     location: { type, address, roomName, meetingUrl, meetingId },
  //     instructor: { id, name, avatarUrl },
  //     attendeeCount, maxAttendees, recordingUrl, materials,
  //     attendees: [{ id, userId, userName, status, joinedAt, leftAt }],
  //     settings: { allowLateJoin, recordSession, enableChat, enableQA, reminderSchedule }
  //   }
  //
  // Status-based actions:
  //   scheduled → Edit, Cancel, Start Session
  //   live → End Session, View Attendees
  //   completed → View Recording, Attendance Report
  //   cancelled → Reschedule
  // ==========================================================================
  void course;
  void classDetail;

  return <div>Class Detail Page - UI not implemented</div>;
}
