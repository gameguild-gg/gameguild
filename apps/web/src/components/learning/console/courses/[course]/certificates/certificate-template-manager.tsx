'use client';

import { createCertificateTemplate, deleteCertificateTemplate } from '@/lib/learning/actions';
import type { CertificateTemplate } from '@/lib/learning/queries/assessments';
import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Award, Loader2, Plus, Trash2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import { useLearningBase } from '@/lib/learning/use-learning-base';

interface CertificateTemplateManagerProps {
  courseId: string;
  templates: CertificateTemplate[];
}

const defaultTemplateHtml = `
<section style="font-family: Inter, Arial, sans-serif; padding: 48px; border: 12px solid #111827; text-align: center;">
  <p style="letter-spacing: 0.18em; text-transform: uppercase; color: #6b7280;">Certificate of Completion</p>
  <h1 style="font-size: 42px; margin: 24px 0;">{{recipientName}}</h1>
  <p style="font-size: 18px; color: #374151;">has successfully completed</p>
  <h2 style="font-size: 30px; margin: 18px 0;">{{courseName}}</h2>
  <p style="color: #6b7280;">Issued on {{issuedAt}} - Certificate {{certificateNumber}}</p>
</section>
`.trim();

export function CertificateTemplateManager({ courseId, templates }: CertificateTemplateManagerProps) {
  const learningBase = useLearningBase();
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [items, setItems] = useState(templates);
  const [name, setName] = useState('');
  const [templateHtml, setTemplateHtml] = useState(defaultTemplateHtml);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const createTemplate = () => {
    setMessage(null);
    const submittedName = name.trim();
    startTransition(async () => {
      const result = await createCertificateTemplate({ courseId, name, templateHtml });
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      const now = new Date().toISOString();
      setItems((current) => [
        ...current,
        {
          id: result.data.id,
          courseId,
          name: submittedName,
          description: null,
          status: 'active',
          isDefault: false,
          issuedCount: 0,
          createdAt: now,
          updatedAt: now,
        },
      ]);
      setName('');
      setTemplateHtml(defaultTemplateHtml);
      setMessage({ type: 'success', text: 'Certificate template created.' });
      router.refresh();
    });
  };

  const removeTemplate = (templateId: string) => {
    setMessage(null);
    startTransition(async () => {
      const result = await deleteCertificateTemplate(courseId, templateId);
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setItems((current) => current.filter((template) => template.id !== templateId));
      setMessage({ type: 'success', text: 'Certificate template deleted.' });
      router.refresh();
    });
  };

  return (
    <div className="grid min-w-0 max-w-full gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(280px,360px)]">
      <Card className="min-w-0">
        <CardHeader className="min-w-0">
          <CardTitle>Certificate Templates</CardTitle>
          <CardDescription className="break-words">Templates define the credential design used when learners complete the course.</CardDescription>
        </CardHeader>
        <CardContent className="min-w-0 space-y-3">
          {items.length === 0 ? (
            <div className="min-w-0 rounded-lg border border-dashed p-8 text-center">
              <Award className="mx-auto mb-3 size-8 text-muted-foreground" />
              <p className="font-medium">No certificate templates yet</p>
              <p className="mt-1 text-sm text-muted-foreground">Create the first template to enable course completion credentials.</p>
            </div>
          ) : (
            items.map((template) => (
              <div key={template.id} className="flex min-w-0 flex-col gap-3 rounded-lg border p-4 md:flex-row md:items-center md:justify-between">
                <Link href={`${learningBase}/courses/${courseId}/certificates/${template.id}`} className="min-w-0 flex-1 space-y-1">
                  <p className="break-words font-medium">{template.name}</p>
                  <p className="break-words text-sm text-muted-foreground">{template.description ?? 'No description'}</p>
                </Link>
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant={template.status === 'active' ? 'default' : 'secondary'}>{template.status}</Badge>
                  <Badge variant="outline">{template.issuedCount} issued</Badge>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={isPending || template.issuedCount > 0}
                    onClick={() => removeTemplate(template.id)}
                    aria-label={`Delete ${template.name}`}
                  >
                    {isPending ? <Loader2 className="size-4 animate-spin" /> : <Trash2 className="size-4" />}
                  </Button>
                </div>
              </div>
            ))
          )}
        </CardContent>
      </Card>

      <Card className="min-w-0">
        <CardHeader className="min-w-0">
          <CardTitle className="flex items-center gap-2 text-lg">
            <Plus className="size-4" />
            New template
          </CardTitle>
          <CardDescription className="break-words">Create a live certificate template through the Learning.Certificates API.</CardDescription>
        </CardHeader>
        <CardContent className="min-w-0 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="certificate-template-name">Name</Label>
            <Input
              id="certificate-template-name"
              className="min-w-0"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Default completion certificate"
              disabled={isPending}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="certificate-template-html">HTML</Label>
            <Textarea
              id="certificate-template-html"
              className="min-w-0 max-w-full resize-y overflow-auto font-mono text-sm"
              value={templateHtml}
              onChange={(event) => setTemplateHtml(event.target.value)}
              rows={10}
              disabled={isPending}
            />
            <p className="text-xs text-muted-foreground">Supported merge tags: recipientName, courseName, issuedAt, certificateNumber.</p>
          </div>
          {message ? (
            <p role={message.type === 'success' ? 'status' : 'alert'} className={message.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>
              {message.text}
            </p>
          ) : null}
          <Button type="button" onClick={createTemplate} disabled={isPending} className="w-full">
            {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Plus className="mr-2 size-4" />}
            Create template
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
