'use client';

import { useRouter } from '@/i18n/navigation';
import {
  saveTestingEventTemplate,
  setTestingEventTemplateArchived,
} from '@/lib/testing-lab/events-actions';
import type {
  TestingLabQuestionnaireSchema,
  TestingLabTestingEventTemplateProjection,
} from '@game-guild/client';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, Archive, CheckCircle2, Loader2, Plus, RotateCcw } from 'lucide-react';
import { useState, useTransition, type FormEvent } from 'react';
import { QuestionnaireBuilder } from './questionnaire-builder';

function emptySchema(title: string): TestingLabQuestionnaireSchema {
  return { title, questions: [] };
}

export function TestingEventTemplateManagement({ templates }: { templates: TestingLabTestingEventTemplateProjection[] }) {
  const [selectedId, setSelectedId] = useState<string | null>(templates.find((template) => !template.isArchived)?.id ?? null);
  const selected = templates.find((template) => template.id === selectedId) ?? null;

  return (
    <div className="grid gap-6 xl:grid-cols-[280px_minmax(0,1fr)]">
      <aside className="space-y-3">
        <Button type="button" variant={selectedId === null ? 'secondary' : 'outline'} className="w-full justify-start" onClick={() => setSelectedId(null)}>
          <Plus className="mr-2 size-4" /> New template
        </Button>
        <div className="space-y-2" aria-label="Event templates">
          {templates.map((template) => (
            <button
              key={template.id}
              type="button"
              onClick={() => setSelectedId(template.id ?? null)}
              className={`w-full rounded-md border p-3 text-left transition-colors ${selectedId === template.id ? 'border-foreground bg-muted' : 'hover:bg-muted/50'}`}
            >
              <span className="flex items-center justify-between gap-2"><span className="truncate text-sm font-medium">{template.name || 'Untitled template'}</span>{template.isArchived ? <Badge variant="outline">Archived</Badge> : null}</span>
              <span className="mt-1 block text-xs text-muted-foreground">Revision {template.currentRevisionNumber ?? 1}</span>
            </button>
          ))}
          {templates.length === 0 ? <p className="rounded-md border border-dashed p-4 text-sm text-muted-foreground">No templates yet. Create the first reusable event package.</p> : null}
        </div>
      </aside>
      <TemplateEditor key={selected?.id ?? 'new'} template={selected} />
    </div>
  );
}

function TemplateEditor({ template }: { template: TestingLabTestingEventTemplateProjection | null }) {
  const revision = template?.currentRevision;
  const [applicationSchema, setApplicationSchema] = useState<TestingLabQuestionnaireSchema>(revision?.projectApplicationSchema ?? emptySchema('Project application'));
  const [registrationSchema, setRegistrationSchema] = useState<TestingLabQuestionnaireSchema>(revision?.testerRegistrationSchema ?? emptySchema('Tester registration'));
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<Awaited<ReturnType<typeof saveTestingEventTemplate>> | null>(null);
  const router = useRouter();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    formData.set('projectApplicationSchemaJson', JSON.stringify(applicationSchema));
    formData.set('testerRegistrationSchemaJson', JSON.stringify(registrationSchema));
    startTransition(async () => {
      const next = await saveTestingEventTemplate(formData);
      setResult(next);
      if (next.success) router.refresh();
    });
  }

  function toggleArchived() {
    if (!template?.id) return;
    const formData = new FormData();
    formData.set('templateId', template.id);
    if (template.isArchived) formData.set('restore', 'true');
    startTransition(async () => {
      const next = await setTestingEventTemplateArchived(formData);
      setResult(next);
      if (next.success) router.refresh();
    });
  }

  return (
    <form className="space-y-8 rounded-md border p-5" onSubmit={submit}>
      {template?.id ? <input type="hidden" name="templateId" value={template.id} /> : null}
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div><h2 className="font-semibold">{template ? 'Create a new template revision' : 'Create an event template'}</h2><p className="mt-1 text-sm text-muted-foreground">Events receive an independent copy of the selected revision. Later template edits never change existing events.</p></div>
        {template ? <Button type="button" size="sm" variant="outline" disabled={pending} onClick={toggleArchived}>{template.isArchived ? <RotateCcw className="mr-2 size-4" /> : <Archive className="mr-2 size-4" />}{template.isArchived ? 'Restore' : 'Archive'}</Button> : null}
      </div>
      <section className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2"><Label htmlFor="template-name">Name</Label><Input id="template-name" name="name" defaultValue={template?.name ?? ''} required /></div>
        <div className="space-y-2"><Label htmlFor="template-description">Description</Label><Input id="template-description" name="description" defaultValue={template?.description ?? ''} /></div>
        <div className="space-y-2 sm:col-span-2"><Label htmlFor="template-rules">General rules</Label><Textarea id="template-rules" name="generalRules" rows={5} defaultValue={revision?.generalRules ?? ''} required /></div>
        <div className="space-y-2"><Label htmlFor="template-candidate-instructions">Candidate instructions</Label><Textarea id="template-candidate-instructions" name="candidateInstructions" rows={6} defaultValue={revision?.candidateInstructions ?? ''} required /></div>
        <div className="space-y-2"><Label htmlFor="template-tester-instructions">Tester instructions</Label><Textarea id="template-tester-instructions" name="testerInstructions" rows={6} defaultValue={revision?.testerInstructions ?? ''} required /></div>
      </section>
      <section className="grid gap-4 border-t pt-6 sm:grid-cols-2">
        <div className="space-y-2"><Label htmlFor="template-mode">Default event mode</Label><select id="template-mode" name="defaultMode" defaultValue={revision?.defaultMode ?? 'Online'} className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"><option value="Online">Online</option><option value="InPerson">In person</option><option value="Hybrid">Hybrid</option></select></div>
        <div className="space-y-2"><Label htmlFor="template-approval">Default approval</Label><select id="template-approval" name="defaultApprovalMode" defaultValue={revision?.defaultApprovalMode ?? 'ManagerOnly'} className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"><option value="ManagerOnly">Manager only</option><option value="Committee">Committee</option></select></div>
        <label className="flex items-center gap-2 text-sm sm:col-span-2"><input type="checkbox" name="defaultRequiresFeedback" defaultChecked={revision?.defaultRequiresFeedback ?? true} /> Require developer feedback questionnaire by default</label>
      </section>
      <section className="space-y-4 border-t pt-6"><div><h3 className="font-medium">Project application form</h3><p className="text-sm text-muted-foreground">Questions added by Testing Lab managers for project teams.</p></div><QuestionnaireBuilder value={applicationSchema} onChange={setApplicationSchema} /></section>
      <section className="space-y-4 border-t pt-6"><div><h3 className="font-medium">Tester registration form</h3><p className="text-sm text-muted-foreground">Questions added by Testing Lab managers for testers.</p></div><QuestionnaireBuilder value={registrationSchema} onChange={setRegistrationSchema} /></section>
      <div className="flex flex-wrap items-center gap-3 border-t pt-5"><Button type="submit" disabled={pending || template?.isArchived}>{pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}{template ? 'Save new revision' : 'Create template'}</Button>{template?.isArchived ? <p className="text-xs text-muted-foreground">Restore this template before creating another revision.</p> : null}</div>
      {result ? <Alert variant={result.success ? 'default' : 'destructive'}>{result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}<AlertDescription>{result.success ? result.message : result.error}</AlertDescription></Alert> : null}
    </form>
  );
}
