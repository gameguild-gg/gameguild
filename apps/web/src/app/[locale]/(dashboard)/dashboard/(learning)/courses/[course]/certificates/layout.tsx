import React from 'react';
import { notFound, forbidden } from 'next/navigation';
import { getCourse, getCourseCertificates } from '@/lib/learning';

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
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string; course: string }>;
}): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  
  if (!course) {
    notFound();
  }
  
  if (!course.features.hasCertificate) {
    forbidden();
  }

  // Preload certificates
  getCourseCertificates(courseId);

  void course;

  return <>{children}</>;
}
