import React from 'react';
import { completeLaunchChecklistItem, createLaunchPlan, publishLaunchPlan } from '@/lib/launch-pad/actions';
import { getLaunchPadDashboard, getLaunchProjectOptions, getPlanReadiness, normalizeLaunchStatus, type LaunchPlan } from '@/lib/launch-pad';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CalendarDays, CheckCircle2, Rocket, Target } from 'lucide-react';

const defaultChecklistText = [
  'Storefront: Landing page copy and media approved',
  'Quality: Release build or playable package smoke tested',
  'Distribution: Distribution channels and launch date confirmed',
  'Support: Support intake and known-issues process ready',
  'Analytics: Launch metrics and post-launch review prepared',
].join('\n');

const channelOptions = ['Website', 'Steam', 'Itch.io', 'Discord', 'Newsletter', 'Press kit'];

function PlanCard({ plan }: { plan: LaunchPlan }) {
  const readiness = getPlanReadiness(plan);
  const status = normalizeLaunchStatus(plan.status);
  const checklist = plan.checklistItems ?? [];
  const canPublish = status === 'Ready';

  return (
    <Card className="min-w-0">
      <CardHeader className="space-y-3">
        <div className="flex min-w-0 items-start justify-between gap-4">
          <div className="min-w-0">
            <CardTitle className="flex min-w-0 items-center gap-2 break-words">
              <Rocket className="size-5" />
              {plan.name}
            </CardTitle>
            <CardDescription className="break-words">{plan.project?.title ?? plan.project?.name ?? plan.projectId}</CardDescription>
          </div>
          <Badge variant={status === 'Launched' ? 'default' : 'outline'}>{status}</Badge>
        </div>
        <div>
          <div className="mb-2 flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Readiness</span>
            <span className="font-semibold">{readiness}%</span>
          </div>
          <div className="h-2 rounded-full bg-muted">
            <div className="h-2 rounded-full bg-primary" style={{ width: `${readiness}%` }} />
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-3 text-sm md:grid-cols-2">
          <div className="rounded-lg border p-3">
            <p className="flex items-center gap-2 text-muted-foreground">
              <CalendarDays className="size-4" />
              Target launch
            </p>
            <p className="mt-1 font-medium">
              {plan.targetLaunchAt ? new Date(plan.targetLaunchAt).toLocaleDateString('en-US') : 'Not scheduled'}
            </p>
          </div>
          <div className="rounded-lg border p-3">
            <p className="flex items-center gap-2 text-muted-foreground">
              <Target className="size-4" />
              Channels
            </p>
            <p className="mt-1 font-medium">{(plan.channels ?? []).length > 0 ? plan.channels?.join(', ') : 'None selected'}</p>
          </div>
        </div>

        {plan.positioning ? <p className="text-sm text-muted-foreground">{plan.positioning}</p> : null}

        <div className="space-y-2">
          {checklist.map((item) => (
            <div key={item.id} className="flex items-center justify-between gap-3 rounded-lg border p-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium">{item.title}</p>
                <p className="text-xs text-muted-foreground">{item.category}</p>
              </div>
              {item.isComplete ? (
                <Badge className="gap-1">
                  <CheckCircle2 className="size-3" />
                  Done
                </Badge>
              ) : (
                <form action={completeLaunchChecklistItem}>
                  <input type="hidden" name="planId" value={plan.id} />
                  <input type="hidden" name="itemId" value={item.id} />
                  <Button type="submit" size="sm" variant="outline">Mark done</Button>
                </form>
              )}
            </div>
          ))}
        </div>

        <form action={publishLaunchPlan}>
          <input type="hidden" name="planId" value={plan.id} />
          <Button type="submit" disabled={!canPublish} className="w-full">
            Publish launch
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

export default async function LaunchPadPage(): Promise<React.JSX.Element> {
  const [plans, projects] = await Promise.all([getLaunchPadDashboard(), getLaunchProjectOptions()]);

  return (
    <div className="flex min-w-0 max-w-full flex-col gap-6 p-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Launch Pad</h1>
        <p className="text-muted-foreground">Prepare project launches with checklist, channel, and readiness tracking.</p>
      </div>

      <div className="grid min-w-0 max-w-full gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(300px,420px)]">
        <section className="min-w-0 space-y-4">
          {plans.length === 0 ? (
            <Card>
              <CardHeader>
                <CardTitle>No launch plans yet</CardTitle>
                <CardDescription>Create a launch plan from an existing project to start readiness tracking.</CardDescription>
              </CardHeader>
            </Card>
          ) : (
            plans.map((plan) => <PlanCard key={plan.id} plan={plan} />)
          )}
        </section>

        <Card className="h-fit min-w-0">
          <CardHeader className="min-w-0">
            <CardTitle>Create launch plan</CardTitle>
            <CardDescription className="break-words">Connect a project to its launch checklist and channels.</CardDescription>
          </CardHeader>
          <CardContent className="min-w-0">
            <form action={createLaunchPlan} className="space-y-5">
              <div className="space-y-2">
                <Label>Project</Label>
                <div className="grid gap-2">
                  {projects.length === 0 ? (
                    <p className="rounded-lg border p-3 text-sm text-muted-foreground">Create a project first, then return here to prepare its launch.</p>
                  ) : (
                    projects.slice(0, 6).map((project, index) => (
                      <label key={project.id} className="flex cursor-pointer items-center gap-3 rounded-lg border p-3">
                        <input type="radio" name="projectId" value={project.id} defaultChecked={index === 0} className="size-4" />
                        <span className="min-w-0">
                          <span className="block truncate text-sm font-medium">{project.title}</span>
                          <span className="block truncate text-xs text-muted-foreground">{project.slug ?? project.id}</span>
                        </span>
                      </label>
                    ))
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="launch-name">Launch name</Label>
                <Input id="launch-name" name="name" placeholder="Steam early access launch" required />
              </div>

              <div className="space-y-2">
                <Label htmlFor="launch-positioning">Positioning</Label>
                <Textarea id="launch-positioning" name="positioning" rows={3} placeholder="Who this launch is for, why it matters, and the main promise." />
              </div>

              <div className="space-y-2">
                <Label htmlFor="launch-target">Target launch date</Label>
                <Input id="launch-target" name="targetLaunchAt" type="datetime-local" />
              </div>

              <div className="space-y-2">
                <Label>Channels</Label>
                <div className="grid gap-2 sm:grid-cols-2">
                  {channelOptions.map((channel) => (
                    <label key={channel} className="flex items-center gap-2 rounded-lg border p-2 text-sm">
                      <input type="checkbox" name="channels" value={channel} className="size-4 rounded border-input accent-primary" />
                      {channel}
                    </label>
                  ))}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="launch-checklist">Checklist</Label>
                <Textarea id="launch-checklist" name="checklist" rows={6} defaultValue={defaultChecklistText} />
              </div>

              <Button type="submit" className="w-full" disabled={projects.length === 0}>
                Create launch plan
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
