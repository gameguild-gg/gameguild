import React from 'react';
import { getCourseCertificates } from '@/lib/learning';

/**
 * Certificates List Page
 *
 * Route: /courses/[course]/certificates
 * Condition: course.features.hasCertificate = true (checked in layout)
 */
export default async function CertificatesPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/certificates'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const certificates = await getCourseCertificates(courseId);

  // ==========================================================================
  // DATA: CourseCertificates
  // templates: [{ id, name, status, design, issuedCount }]
  // issuedCount (total across all templates)
  // ==========================================================================
  void certificates;

  return <div>Certificates Page - UI not implemented</div>;
}
