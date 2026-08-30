'use client';

import { configureTestingEvent } from '@/lib/testing-lab/events-actions';
import type {
  TestingLabQuestionnaireSchema,
  TestingLabTestingEventConfigurationProjection,
  TestingLabTestingEventStatus,
} from '@game-guild/client';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, CheckCircle2, FileLock2, Loader2 } from 'lucide-react';
import { useState, useTransition, type FormEvent } from 'react';
import { QuestionnaireBuilder } from './questionnaire-builder';

function emptySchema(title: string): TestingLabQuestionnaireSchema {
  return { title, questions: [] };
}

export function TestingEventConfigurationEditor({
  eventId,
  status,
  configuration,
}: {
  eventId: string;
  status?: TestingLabTestingEventStatus;
  configuration?: TestingLabTestingEventConfigurationProjection;
}) {
  const editable = (status ?? 'Draft') === 'Draft';
  const [applicationSchema, setApplicationSchema] = useState<TestingLabQuestionnaireSchema>(
    configuration?.projectApplicationSchema ?? emptySchema('Project application'),
  );
  const [registrationSchema, setRegistrationSchema] = useState<TestingLabQuestionnaireSchema>(
    configuration?.testerRegistrationSchema ?? emptySchema('Tester registration'),
  );
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<Awaited<ReturnType<typeof configureTestingEvent>> | null>(null);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    formData.set('projectApplicationSchemaJson', JSON.stringify(applicationSchema));
    formData.set('testerRegistrationSchemaJson', JSON.stringify(registrationSchema));
    startTransition(async () => setResult(await configureTestingEvent(formData)));
  }

  if (!editable) {
    return (
      <section className="space-y-5 rounded-md border p-5" aria-labelledby="event-configuration-heading">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 id="event-configuration-heading" className="font-semibold">Rules, instructions, and forms</h2>
            <p className="mt-1 text-sm text-muted-foreground">This snapshot was frozen when applications opened.</p>
          </div>
          <Badge variant="secondary"><FileLock2 className="mr-1 size-3.5" /> Frozen</Badge>
        </div>
        <div className="grid gap-5 lg:grid-cols-3">
          <ReadOnlyText title="General rules" value={configuration?.generalRules} />
          <ReadOnlyText title="Candidate instructions" value={configuration?.candidateInstructions} />
          <ReadOnlyText title="Tester instructions" value={configuration?.testerInstructions} />
        </div>
        <dl className="grid gap-3 rounded-md bg-muted/40 p-4 text-sm sm:grid-cols-2">
          <div><dt className="text-muted-foreground">Project application form</dt><dd className="font-medium">{configuration?.projectApplicationSchema?.questions?.length ?? 0} questions</dd></div>
          <div><dt className="text-muted-foreground">Tester registration form</dt><dd className="font-medium">{configuration?.testerRegistrationSchema?.questions?.length ?? 0} questions</dd></div>
          {configuration?.sourceTemplateRevisionId ? <div className="sm:col-span-2"><dt className="text-muted-foreground">Template revision</dt><dd className="font-mono text-xs">{configuration.sourceTemplateRevisionId}</dd></div> : null}
        </dl>
      </section>
    );
  }

  return (
    <form className="space-y-8 rounded-md border p-5" onSubmit={submit}>
      <input type="hidden" name="eventId" value={eventId} />
      <div>
        <h2 id="event-configuration-heading" className="font-semibold">Rules, instructions, and forms</h2>
        <p className="mt-1 text-sm text-muted-foreground">Edit this draft package before opening applications. Opening applications creates an immutable snapshot.</p>
      </div>
      <section className="grid gap-4 lg:grid-cols-3" aria-labelledby="event-copy-heading">
        <h3 id="event-copy-heading" className="sr-only">Event copy</h3>
        <TextField name="generalRules" label="General rules" defaultValue={configuration?.generalRules} placeholder="Participation, conduct, confidentiality, and completion rules." />
        <TextField name="candidateInstructions" label="Candidate instructions" defaultValue={configuration?.candidateInstructions} placeholder="What project teams must prepare before applying." />
        <TextField name="testerInstructions" label="Tester instructions" defaultValue={configuration?.testerInstructions} placeholder="What assigned testers must do during and after the session." />
      </section>
      <section className="space-y-4 border-t pt-6" aria-labelledby="application-form-heading">
        <div><h3 id="application-form-heading" className="font-medium">Project application form</h3><p className="text-sm text-muted-foreground">Extra event-specific questions answered by student project teams.</p></div>
        <QuestionnaireBuilder value={applicationSchema} onChange={setApplicationSchema} />
      </section>
      <section className="space-y-4 border-t pt-6" aria-labelledby="registration-form-heading">
        <div><h3 id="registration-form-heading" className="font-medium">Tester registration form</h3><p className="text-sm text-muted-foreground">Extra event-specific questions answered by testers when they register.</p></div>
        <QuestionnaireBuilder value={registrationSchema} onChange={setRegistrationSchema} />
      </section>
      <div className="flex flex-wrap items-center gap-3 border-t pt-5">
        <Button type="submit" disabled={pending}>{pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Save draft configuration</Button>
        <p className="text-xs text-muted-foreground">Rules, instructions, and both schemas are validated again by the API.</p>
      </div>
      {result ? <Alert variant={result.success ? 'default' : 'destructive'}>{result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}<AlertDescription>{result.success ? result.message : result.error}</AlertDescription></Alert> : null}
    </form>
  );
}

function TextField({ name, label, defaultValue, placeholder }: { name: string; label: string; defaultValue?: string | null; placeholder: string }) {
  return <div className="space-y-2"><Label htmlFor={`event-${name}`}>{label}</Label><Textarea id={`event-${name}`} name={name} rows={8} defaultValue={defaultValue ?? ''} placeholder={placeholder} required /></div>;
}

function ReadOnlyText({ title, value }: { title: string; value?: string | null }) {
  return <div><h3 className="text-sm font-medium">{title}</h3><p className="mt-2 whitespace-pre-wrap text-sm text-muted-foreground">{value || 'Not provided'}</p></div>;
}
