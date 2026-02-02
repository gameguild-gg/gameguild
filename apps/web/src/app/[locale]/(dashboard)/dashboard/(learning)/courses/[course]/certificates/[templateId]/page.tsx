import React from 'react';
import { notFound } from 'next/navigation';
import { getCertificateTemplate } from '@/lib/learning';

/**
 * Certificate Template Editor Page
 *
 * Route: /courses/[course]/certificates/[templateId]
 */
export default async function CertificateTemplateDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/certificates/[templateId]'>): Promise<React.JSX.Element> {
  const { templateId } = await params;

  const template = await getCertificateTemplate(templateId);

  if (!template) {
    notFound();
  }

  // ==========================================================================
  // DATA: CertificateTemplateDetail
  // name, description, status, design: { templateType, backgroundColor, logoUrl, signatureUrl, signatureName }
  // fields: [{ type, value, position, style }]
  // previewUrl, issuedCertificates: [{ studentName, issuedAt, downloadUrl }]
  // ==========================================================================
  void template;

  return <div>Certificate Template Editor Page - UI not implemented</div>;
}
