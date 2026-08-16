import React from 'react';
import { getCourseCertificates } from '@/lib/learning';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Award, FileCheck2, Plus } from 'lucide-react';
import { CertificateTemplateManager } from './certificate-template-manager';

/**
 * Certificates List Page
 *
 * Route: /courses/[course]/certificates
 * Condition: course.features.hasCertificate = true (checked in layout)
 */
export default async function CertificatesPage({
  params,
}: PageProps<'/[locale]/dashboard/platform/learning/courses/[course]/certificates'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const certificates = await getCourseCertificates(courseId);

  return (
    <div className="flex min-w-0 max-w-full flex-col gap-6">
      <div className="grid min-w-0 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <Card className="min-w-0">
          <CardContent className="flex min-w-0 items-center gap-3 p-4">
            <Award className="size-5 text-primary" />
            <div className="min-w-0">
              <p className="text-2xl font-semibold">{certificates.total}</p>
              <p className="text-sm text-muted-foreground">Templates</p>
            </div>
          </CardContent>
        </Card>
        <Card className="min-w-0">
          <CardContent className="flex min-w-0 items-center gap-3 p-4">
            <FileCheck2 className="size-5 text-emerald-600" />
            <div className="min-w-0">
              <p className="text-2xl font-semibold">{certificates.issuedCount}</p>
              <p className="text-sm text-muted-foreground">Issued certificates</p>
            </div>
          </CardContent>
        </Card>
        <Card className="min-w-0 sm:col-span-2 xl:col-span-1">
          <CardContent className="flex min-w-0 flex-col items-start justify-between gap-3 p-4 sm:flex-row sm:items-center">
            <div className="min-w-0">
              <p className="text-sm font-medium">Template API</p>
              <p className="break-words text-sm text-muted-foreground">Connected to Learning.Certificates</p>
            </div>
            <Button size="sm" variant="outline" className="w-full shrink-0 sm:w-auto">
              <Plus className="mr-2 size-4" />
              New template
            </Button>
          </CardContent>
        </Card>
      </div>

      <CertificateTemplateManager courseId={courseId} templates={certificates.templates} />
    </div>
  );
}
