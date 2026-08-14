'use client';

import { submitLaunchPadApplicationForm } from '@/lib/launch-pad/actions';
import type { TestingProjectVersionOption } from '@/lib/testing-lab/queries';
import { Button } from '@game-guild/ui/components/button';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { useState } from 'react';

export function LaunchPadApplicationForm({ eventId, versions }: { eventId: string; versions: TestingProjectVersionOption[] }) {
  const [versionId, setVersionId] = useState('');
  const selected = versions.find((version) => version.id === versionId);
  if (versions.length === 0) return <p className="text-sm text-slate-400">Create an accessible Project version before applying.</p>;

  return (
    <form action={submitLaunchPadApplicationForm} className="space-y-4">
      <input type="hidden" name="eventId" value={eventId} />
      <input type="hidden" name="projectId" value={selected?.projectId ?? ''} />
      <div className="space-y-2">
        <Label htmlFor="launch-project-version">Project version</Label>
        <select id="launch-project-version" name="projectVersionId" required value={versionId} onChange={(event) => setVersionId(event.target.value)}
          className="h-10 w-full rounded-md border border-white/15 bg-slate-950 px-3 text-sm">
          <option value="" disabled>Select a release</option>
          {versions.map((version) => <option key={version.id} value={version.id}>{version.projectTitle} · {version.versionNumber}</option>)}
        </select>
      </div>
      <div className="space-y-2"><Label htmlFor="launch-pitch">Pitch</Label><Textarea id="launch-pitch" name="pitch" rows={4} /></div>
      <Button type="submit">Submit Project application</Button>
    </form>
  );
}
