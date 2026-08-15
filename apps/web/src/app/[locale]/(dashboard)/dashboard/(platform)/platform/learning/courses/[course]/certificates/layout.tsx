import { getCourse, getCourseCertificates } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';

/**
 * Certificates Layout
 *
 * Routes:
 * - /certificates - Certificate templates list
 * - /certificates/[templateId] - Template editor
 *
 * Condition: course.features.hasCertificate = true
 */
export default async function CertificatesLayout({
  children,
  params,
}: LayoutProps<'/[locale]/dashboard/platform/learning/courses/[course]/certificates'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);

  if (!course || !course.features.hasCertificate) {
    notFound();
  }

  // Preload certificates
  getCourseCertificates(courseId);

  return <>{children}</>;
}
