import { Link } from '@/i18n/navigation';
import type { WorkspaceProjectVersion } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CheckCircle2, Circle, TestTube2 } from 'lucide-react';

interface ProjectTestingReadinessProps {
  projectId: string;
  projectSlug: string;
  versions: readonly WorkspaceProjectVersion[];
}

export function ProjectTestingReadiness({ projectId, projectSlug, versions }: ProjectTestingReadinessProps) {
  const eligibleVersion = versions.find((version) => {
    const status = version.status.toLowerCase();
    return status === 'readyfortesting' || status === 'released';
  });
  const versionsHref = `/workspace/projects/${projectSlug}/versions-builds`;
  const action = versions.length === 0
    ? { label: 'Create first version', href: versionsHref }
    : eligibleVersion
      ? { label: 'Find Testing Lab events', href: `/testing-lab/events?projectId=${projectId}` }
      : { label: 'Prepare version for testing', href: versionsHref };
  const steps = [
    { label: 'Project details', complete: true },
    { label: 'Version created', complete: versions.length > 0 },
    { label: 'Ready for testing', complete: Boolean(eligibleVersion) },
    { label: 'Apply to an event', complete: false },
  ];

  return (
    <Card aria-label="Testing readiness" role="region">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <div className="flex items-center gap-2">
              <TestTube2 className="size-5" aria-hidden="true" />
              <CardTitle>Testing Lab readiness</CardTitle>
            </div>
            <CardDescription className="mt-2">
              Testing applications use a fixed Project version so testers always know which build to evaluate.
            </CardDescription>
          </div>
          <Badge variant={eligibleVersion ? 'default' : 'secondary'}>
            {eligibleVersion ? `${eligibleVersion.versionNumber} eligible` : 'Action required'}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-5">
        <ol className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {steps.map((step, index) => (
            <li key={step.label} className="flex items-center gap-2 rounded-md border p-3 text-sm">
              {step.complete
                ? <CheckCircle2 className="size-4 shrink-0 text-primary" aria-hidden="true" />
                : <Circle className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />}
              <span><span className="sr-only">Step {index + 1}: </span>{step.label}</span>
            </li>
          ))}
        </ol>
        <Button asChild>
          <Link href={action.href}>{action.label}</Link>
        </Button>
      </CardContent>
    </Card>
  );
}
