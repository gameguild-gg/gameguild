import React from 'react';
import { notFound } from 'next/navigation';
import { getCertificateTemplate } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Award, Code2 } from 'lucide-react';

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

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="lg:col-span-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Award className="size-5" />
            {template.name}
          </CardTitle>
          <CardDescription>{template.description ?? 'Certificate template details and preview source.'}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-2">
            <Badge variant={template.status === 'active' ? 'default' : 'secondary'}>{template.status}</Badge>
            <Badge variant="outline">{template.issuedCount} issued</Badge>
          </div>
          <div className="overflow-hidden rounded-lg border">
            <div
              className="prose prose-sm max-w-none bg-background p-6 dark:prose-invert"
              dangerouslySetInnerHTML={{ __html: template.templateHtml || '<p>Template has no HTML body.</p>' }}
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <Code2 className="size-4" />
            Source
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 text-sm">
          <div>
            <p className="text-muted-foreground">Template ID</p>
            <p className="break-all font-mono text-xs">{template.id}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Course ID</p>
            <p className="break-all font-mono text-xs">{template.courseId}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Updated</p>
            <p>{new Date(template.updatedAt).toLocaleString('en-US')}</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
