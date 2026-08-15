import React from 'react';
import { notFound } from 'next/navigation';
import { getCertificateTemplate } from '@/lib/learning';
import { CertificateTemplateEditor } from './certificate-template-editor';

/**
 * Certificate Template Editor Page
 *
 * Route: /courses/[course]/certificates/[templateId]
 */
export default async function CertificateTemplateDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/platform/learning/courses/[course]/certificates/[templateId]'>): Promise<React.JSX.Element> {
  const { templateId } = await params;

  const template = await getCertificateTemplate(templateId);

  if (!template) {
    notFound();
  }

  return <CertificateTemplateEditor template={template} />;
}
