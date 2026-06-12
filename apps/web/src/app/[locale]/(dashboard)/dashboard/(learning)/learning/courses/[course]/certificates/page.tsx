import React from 'react';
import { getCourseCertificates } from '@/lib/learning';
import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Award, FileCheck2, Plus } from 'lucide-react';

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

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <Award className="size-5 text-primary" />
            <div>
              <p className="text-2xl font-semibold">{certificates.total}</p>
              <p className="text-sm text-muted-foreground">Templates</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <FileCheck2 className="size-5 text-emerald-600" />
            <div>
              <p className="text-2xl font-semibold">{certificates.issuedCount}</p>
              <p className="text-sm text-muted-foreground">Issued certificates</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center justify-between gap-3 p-4">
            <div>
              <p className="text-sm font-medium">Template API</p>
              <p className="text-sm text-muted-foreground">Connected to Learning.Certificates</p>
            </div>
            <Button size="sm" variant="outline">
              <Plus className="mr-2 size-4" />
              New template
            </Button>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Certificate Templates</CardTitle>
          <CardDescription>Templates define the certificate design used when learners complete the course.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {certificates.templates.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center">
              <Award className="mx-auto mb-3 size-8 text-muted-foreground" />
              <p className="font-medium">No certificate templates yet</p>
              <p className="mt-1 text-sm text-muted-foreground">Create the first template through the certificate template API.</p>
            </div>
          ) : (
            certificates.templates.map((template) => (
              <Link
                key={template.id}
                href={`/dashboard/learning/courses/${courseId}/certificates/${template.id}`}
                className="flex items-center justify-between rounded-lg border p-4 transition-colors hover:bg-muted/50"
              >
                <div>
                  <p className="font-medium">{template.name}</p>
                  <p className="text-sm text-muted-foreground">{template.description ?? 'No description'}</p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={template.status === 'active' ? 'default' : 'secondary'}>{template.status}</Badge>
                  <Badge variant="outline">{template.issuedCount} issued</Badge>
                </div>
              </Link>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
