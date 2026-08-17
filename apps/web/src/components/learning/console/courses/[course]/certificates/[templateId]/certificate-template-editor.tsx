'use client';

import { updateCertificateTemplate } from '@/lib/learning/actions';
import type { CertificateTemplateDetail } from '@/lib/learning/queries/assessments';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Switch } from '@game-guild/ui/components/switch';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Award, Code2, Loader2 } from 'lucide-react';
import { useState, useTransition } from 'react';

export function CertificateTemplateEditor({ template }: { template: CertificateTemplateDetail }) {
  const [name, setName] = useState(template.name);
  const [description, setDescription] = useState(template.description ?? '');
  const [templateHtml, setTemplateHtml] = useState(template.templateHtml);
  const [templateStyles, setTemplateStyles] = useState(template.templateStyles ?? '');
  const [isDefault, setIsDefault] = useState(template.isDefault);
  const [isActive, setIsActive] = useState(template.status === 'active');
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [isPending, startTransition] = useTransition();
  const previewDocument = `<!doctype html><html><head><style>${templateStyles}</style></head><body>${templateHtml}</body></html>`;

  function save() {
    setFeedback(null);
    startTransition(async () => {
      const result = await updateCertificateTemplate({
        courseId: template.courseId,
        templateId: template.id,
        name: name.trim(),
        description: description.trim(),
        templateHtml: templateHtml.trim(),
        templateStyles: templateStyles.trim(),
        isDefault,
        isActive,
      });
      setFeedback(result.success
        ? { type: 'success', text: 'Certificate template saved.' }
        : { type: 'error', text: result.error });
    });
  }

  return (
    <div className="grid min-w-0 gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(340px,0.8fr)]">
      <Card className="min-w-0">
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><Code2 className="size-5" />Template source</CardTitle>
          <CardDescription>Edit the certificate content, styling, and delivery state.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2"><Label htmlFor="certificate-name">Template name</Label><Input id="certificate-name" value={name} onChange={(event) => setName(event.target.value)} disabled={isPending} /></div>
            <div className="space-y-2"><Label htmlFor="certificate-description">Description</Label><Input id="certificate-description" value={description} onChange={(event) => setDescription(event.target.value)} disabled={isPending} /></div>
          </div>
          <div className="space-y-2"><Label htmlFor="certificate-html">Template HTML</Label><Textarea id="certificate-html" className="min-h-64 resize-y font-mono text-sm" value={templateHtml} onChange={(event) => setTemplateHtml(event.target.value)} disabled={isPending} /></div>
          <div className="space-y-2"><Label htmlFor="certificate-styles">Template styles</Label><Textarea id="certificate-styles" className="min-h-36 resize-y font-mono text-sm" value={templateStyles} onChange={(event) => setTemplateStyles(event.target.value)} disabled={isPending} /></div>
          <div className="grid gap-3 border-t pt-5 sm:grid-cols-2">
            <div className="flex items-center justify-between gap-4 rounded-md border p-4"><div><Label htmlFor="certificate-active">Active template</Label><p className="text-sm text-muted-foreground">Available for issuing certificates.</p></div><Switch id="certificate-active" aria-label="Active template" checked={isActive} onCheckedChange={setIsActive} /></div>
            <div className="flex items-center justify-between gap-4 rounded-md border p-4"><div><Label htmlFor="certificate-default">Default template</Label><p className="text-sm text-muted-foreground">Use automatically for this course.</p></div><Switch id="certificate-default" aria-label="Default template" checked={isDefault} onCheckedChange={setIsDefault} /></div>
          </div>
          {feedback ? <p role={feedback.type === 'success' ? 'status' : 'alert'} className={feedback.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>{feedback.text}</p> : null}
          <div className="flex justify-end"><Button type="button" onClick={save} disabled={isPending}>{isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Save certificate template</Button></div>
        </CardContent>
      </Card>

      <Card className="min-w-0">
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><Award className="size-5" />Live preview</CardTitle>
          <CardDescription>Merge tags are replaced when a certificate is issued.</CardDescription>
        </CardHeader>
        <CardContent>
          <iframe title="Certificate preview" sandbox="" srcDoc={previewDocument} className="aspect-[1.4/1] w-full rounded-md border bg-white" />
          <p className="mt-3 text-xs text-muted-foreground">Supported tags: recipientName, courseName, issuedAt, certificateNumber.</p>
        </CardContent>
      </Card>
    </div>
  );
}
