'use client';

import { updateCourseIntegrationSettings } from '@/lib/learning/actions';
import type { CourseIntegrationSettings } from '@/lib/learning/queries/settings';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Switch } from '@game-guild/ui/components/switch';
import { Loader2, Plus, Trash2, Webhook } from 'lucide-react';
import { useState, useTransition } from 'react';

export function IntegrationSettingsEditor({ settings }: { settings: CourseIntegrationSettings }) {
  const [integrations, setIntegrations] = useState(settings.integrations);
  const [webhooks, setWebhooks] = useState(settings.webhooks);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [url, setUrl] = useState('');
  const [events, setEvents] = useState('course.updated');
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [isPending, startTransition] = useTransition();

  function addWebhook() {
    if (!url.trim()) return;
    setWebhooks((current) => [...current, {
      id: `webhook-${Date.now()}`,
      url: url.trim(),
      events: [...new Set(events.split(/[,;\n]/).map((event) => event.trim()).filter(Boolean))],
      enabled: true,
    }]);
    setUrl('');
    setEvents('course.updated');
    setDialogOpen(false);
  }

  function save() {
    setFeedback(null);
    startTransition(async () => {
      const result = await updateCourseIntegrationSettings(settings.courseId, { integrations, webhooks });
      setFeedback(result.success
        ? { type: 'success', text: 'Integration settings saved.' }
        : { type: 'error', text: result.error });
    });
  }

  return (
    <Card>
      <CardHeader className="flex-row items-start justify-between gap-4">
        <div>
          <CardTitle className="flex items-center gap-2"><Webhook className="size-5" />Course integrations</CardTitle>
          <CardDescription>Configure delivery providers and outbound course events.</CardDescription>
        </div>
        <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
          <DialogTrigger asChild><Button type="button" variant="outline"><Plus className="mr-2 size-4" />Add webhook</Button></DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>Add webhook</DialogTitle><DialogDescription>Send selected course events to an HTTPS endpoint.</DialogDescription></DialogHeader>
            <div className="space-y-4">
              <div className="space-y-2"><Label htmlFor="webhook-url">Webhook URL</Label><Input id="webhook-url" type="url" value={url} onChange={(event) => setUrl(event.target.value)} placeholder="https://example.com/course-events" /></div>
              <div className="space-y-2"><Label htmlFor="webhook-events">Events</Label><Input id="webhook-events" value={events} onChange={(event) => setEvents(event.target.value)} /><p className="text-xs text-muted-foreground">Comma-separated event names.</p></div>
            </div>
            <DialogFooter><Button type="button" onClick={addWebhook}>Add to course</Button></DialogFooter>
          </DialogContent>
        </Dialog>
      </CardHeader>
      <CardContent className="space-y-8">
        <section className="space-y-3">
          <h2 className="font-semibold">Providers</h2>
          {integrations.map((integration, index) => (
            <div key={integration.id} className="flex flex-col gap-3 rounded-md border p-4 sm:flex-row sm:items-center sm:justify-between">
              <div><div className="flex items-center gap-2"><p className="font-medium">{integration.name}</p><Badge variant="outline">{integration.type}</Badge></div><p className="text-sm text-muted-foreground">{integration.enabled ? 'Enabled for this course' : 'Not enabled'}</p></div>
              <Switch aria-label={`Enable ${integration.name}`} checked={integration.enabled} onCheckedChange={(checked) => setIntegrations((current) => current.map((item, itemIndex) => itemIndex === index ? { ...item, enabled: checked, status: checked ? 'connected' : 'disconnected' } : item))} />
            </div>
          ))}
        </section>

        <section className="space-y-3 border-t pt-6">
          <h2 className="font-semibold">Outbound webhooks</h2>
          {webhooks.length === 0 ? <div className="rounded-md border border-dashed p-6 text-center text-sm text-muted-foreground">No outbound course webhooks are configured.</div> : webhooks.map((webhook, index) => (
            <div key={webhook.id} className="flex flex-col gap-3 rounded-md border p-4 md:flex-row md:items-center md:justify-between">
              <div className="min-w-0"><p className="truncate font-medium">{webhook.url}</p><p className="text-sm text-muted-foreground">{webhook.events.join(', ') || 'No events selected'}</p></div>
              <div className="flex items-center gap-2">
                <Switch aria-label={`Enable webhook ${webhook.url}`} checked={webhook.enabled} onCheckedChange={(checked) => setWebhooks((current) => current.map((item, itemIndex) => itemIndex === index ? { ...item, enabled: checked } : item))} />
                <Button type="button" size="icon" variant="ghost" aria-label={`Remove webhook ${webhook.url}`} onClick={() => setWebhooks((current) => current.filter((_, itemIndex) => itemIndex !== index))}><Trash2 className="size-4" /></Button>
              </div>
            </div>
          ))}
        </section>

        {feedback ? <p role={feedback.type === 'success' ? 'status' : 'alert'} className={feedback.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>{feedback.text}</p> : null}
        <div className="flex justify-end"><Button type="button" onClick={save} disabled={isPending}>{isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Save integration settings</Button></div>
      </CardContent>
    </Card>
  );
}
