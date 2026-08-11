'use client';

import { Link, usePathname, useRouter } from '@/i18n/navigation';
import { createAssessmentGroup, deleteAssessmentGroup, updateAssessmentGroup } from '@/lib/learning/actions';
import type { Assessment, AssessmentGroup, AssessmentType, CourseAssessmentAnalytics } from '@/lib/learning/queries/assessments';
import type { ContentItem } from '@/lib/learning/types';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { AlertTriangle, BarChart3, ChevronDown, ClipboardList, GripVertical, Loader2, Pencil, Plus, Target, Trash2, Trophy } from 'lucide-react';
import React, { useState, useTransition } from 'react';

function typeIcon(type: AssessmentType) {
  switch (type) {
    case 'Quiz':
      return <ClipboardList className="size-4" />;
    case 'Project':
      return <Trophy className="size-4" />;
    default:
      return <Target className="size-4" />;
  }
}

function typeBadgeVariant(type: AssessmentType): 'default' | 'secondary' | 'outline' {
  switch (type) {
    case 'Quiz':
      return 'secondary';
    default:
      return 'outline';
  }
}

interface AssessmentsListProps {
  courseId: string;
  assessments: Assessment[];
  total: number;
  gradedContentItems?: ContentItem[];
  assessmentGroups?: AssessmentGroup[];
  analytics?: CourseAssessmentAnalytics | null;
}

interface AssessmentGroupView {
  id: string;
  name: string;
  description: string | null;
  weightPercent: number | null;
  order: number;
  assessments: Assessment[];
}

function formatWeight(weightPercent: number | null) {
  if (weightPercent == null) return 'Ungraded';
  return `${formatPercent(weightPercent)} of Total`;
}

function formatPercent(value: number) {
  return `${Number.isInteger(value) ? value : value.toFixed(1)}%`;
}

function buildGroupedAssessments(assessments: Assessment[], assessmentGroups: AssessmentGroup[]): AssessmentGroupView[] {
  const groups = new Map<string, AssessmentGroupView>();

  for (const group of assessmentGroups) {
    groups.set(group.id, {
      id: group.id,
      name: group.name,
      description: group.description,
      weightPercent: group.weightPercent,
      order: group.order,
      assessments: [],
    });
  }

  for (const assessment of assessments) {
    const groupId = assessment.assessmentGroupId ?? 'ungrouped';
    if (!groups.has(groupId)) {
      groups.set(groupId, {
        id: groupId,
        name: assessment.assessmentGroupName ?? 'Ungrouped activities',
        description: groupId === 'ungrouped' ? 'Activities that do not yet count toward a weighted grade group.' : null,
        weightPercent: assessment.assessmentGroupWeightPercent,
        order: assessment.assessmentGroupOrder ?? Number.MAX_SAFE_INTEGER,
        assessments: [],
      });
    }

    groups.get(groupId)!.assessments.push(assessment);
  }

  return [...groups.values()]
    .filter((group) => group.assessments.length > 0 || group.id !== 'ungrouped')
    .sort((a, b) => a.order - b.order || a.name.localeCompare(b.name))
    .map((group) => ({
      ...group,
      assessments: [...group.assessments].sort((a, b) => a.order - b.order || a.title.localeCompare(b.title)),
    }));
}

function AssessmentAnalyticsPanel({ analytics }: { analytics: CourseAssessmentAnalytics }) {
  const maxBucketCount = Math.max(1, ...analytics.distribution.map((bucket) => bucket.count));

  return (
    <Card>
      <CardContent className="space-y-5 p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3 className="flex items-center gap-2 text-base font-semibold">
              <BarChart3 className="size-4" aria-hidden="true" />
              Score distribution
            </h3>
            <p className="text-muted-foreground mt-1 text-sm">
              Assessment outcomes by score bucket and weighted grade group.
            </p>
          </div>
          <div className="grid grid-cols-3 gap-2 text-right text-sm">
            <div>
              <p className="text-muted-foreground text-xs">Average</p>
              <p className="font-semibold">{formatPercent(analytics.averagePercent)}</p>
            </div>
            <div>
              <p className="text-muted-foreground text-xs">Pass rate</p>
              <p className="font-semibold">{formatPercent(analytics.passRate)}</p>
            </div>
            <div>
              <p className="text-muted-foreground text-xs">Grading</p>
              <p className="font-semibold">{analytics.gradedCount} graded</p>
            </div>
          </div>
        </div>

        {analytics.gradedCount === 0 ? (
          <div className="rounded-lg border border-dashed p-5 text-center">
            <p className="text-sm font-medium">No graded scores yet</p>
            <p className="text-muted-foreground mt-1 text-xs">
              Score distribution appears after submissions are graded.
            </p>
          </div>
        ) : (
          <div className="grid gap-3 md:grid-cols-5">
            {analytics.distribution.map((bucket) => (
              <div key={bucket.label} className="rounded-lg border bg-muted/20 p-3">
                <div className="mb-2 flex items-center justify-between text-xs">
                  <span className="font-medium">{bucket.label}</span>
                  <span className="text-muted-foreground">{bucket.count}</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div
                    className="h-full rounded-full bg-primary"
                    style={{ width: `${Math.max(6, (bucket.count / maxBucketCount) * 100)}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        )}

        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {analytics.groups.map((group) => (
            <div key={group.groupId ?? group.groupName} className="rounded-lg border p-3">
              <div className="flex items-start justify-between gap-2">
                <div>
                  <p className="text-sm font-semibold">{group.groupName}</p>
                  <p className="text-muted-foreground text-xs">
                    {group.gradedCount} graded · {group.ungradedCount} ungraded
                  </p>
                </div>
                <Badge variant="outline">{group.weightPercent == null ? 'Ungraded' : formatPercent(group.weightPercent)}</Badge>
              </div>
              <div className="mt-3 grid grid-cols-2 gap-2 text-sm">
                <div>
                  <p className="text-muted-foreground text-xs">Average</p>
                  <p className="font-semibold">{formatPercent(group.averagePercent)}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-xs">Pass rate</p>
                  <p className="font-semibold">{formatPercent(group.passRate)}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

export function AssessmentsList({
  courseId,
  assessments,
  total,
  gradedContentItems = [],
  assessmentGroups = [],
  analytics = null,
}: AssessmentsListProps) {
  const router = useRouter();
  const pathname = usePathname();
  const [showCreateGroup, setShowCreateGroup] = useState(false);
  const [isGroupPending, startGroupTransition] = useTransition();

  const [newGroupName, setNewGroupName] = useState('');
  const [newGroupWeight, setNewGroupWeight] = useState('20');
  const [groupError, setGroupError] = useState<string | null>(null);
  const [editingGroup, setEditingGroup] = useState<AssessmentGroup | null>(null);
  const [editGroupName, setEditGroupName] = useState('');
  const [editGroupDescription, setEditGroupDescription] = useState('');
  const [editGroupWeight, setEditGroupWeight] = useState('');
  const [editGroupError, setEditGroupError] = useState<string | null>(null);
  const [deletingGroup, setDeletingGroup] = useState<AssessmentGroup | null>(null);
  const [deleteGroupError, setDeleteGroupError] = useState<string | null>(null);

  const groupedAssessments = React.useMemo(
    () => buildGroupedAssessments(assessments, assessmentGroups),
    [assessments, assessmentGroups],
  );
  const contentOwnedActivities = React.useMemo(() => {
    const assessmentContentIds = new Set(assessments.map((assessment) => assessment.contentId).filter(Boolean));
    return gradedContentItems.filter((item) => item.gradingConfig?.enabled && !assessmentContentIds.has(item.id));
  }, [assessments, gradedContentItems]);
  const visibleTotal = total + contentOwnedActivities.length;
  const weightTotal = React.useMemo(
    () => assessmentGroups.reduce((sum, group) => sum + group.weightPercent, 0),
    [assessmentGroups],
  );
  const hasWeightWarning = assessmentGroups.length > 0 && Math.round(weightTotal * 100) / 100 !== 100;

  function handleCreateGroup() {
    if (!newGroupName.trim()) {
      setGroupError('Group name is required.');
      return;
    }

    const weight = Number(newGroupWeight);
    if (!Number.isFinite(weight) || weight < 0 || weight > 100) {
      setGroupError('Weight must be between 0 and 100.');
      return;
    }

    setGroupError(null);
    startGroupTransition(async () => {
      const result = await createAssessmentGroup({
        courseId,
        name: newGroupName.trim(),
        weightPercent: weight,
        order: assessmentGroups.length + 1,
      });

      if (result.success) {
        setShowCreateGroup(false);
        setNewGroupName('');
        setNewGroupWeight('20');
        router.refresh();
      } else {
        setGroupError(result.error);
      }
    });
  }

  function openEditGroup(group: AssessmentGroupView) {
    if (group.id === 'ungrouped' || group.weightPercent == null) return;

    const source = assessmentGroups.find((item) => item.id === group.id) ?? {
      id: group.id,
      courseId,
      name: group.name,
      description: group.description,
      weightPercent: group.weightPercent,
      order: group.order,
    };

    setEditingGroup(source);
    setEditGroupName(source.name);
    setEditGroupDescription(source.description ?? '');
    setEditGroupWeight(String(source.weightPercent));
    setEditGroupError(null);
  }

  function handleUpdateGroup() {
    if (!editingGroup) return;

    if (!editGroupName.trim()) {
      setEditGroupError('Group name is required.');
      return;
    }

    const weight = Number(editGroupWeight);
    if (!Number.isFinite(weight) || weight < 0 || weight > 100) {
      setEditGroupError('Weight must be between 0 and 100.');
      return;
    }

    setEditGroupError(null);
    startGroupTransition(async () => {
      const result = await updateAssessmentGroup({
        courseId,
        groupId: editingGroup.id,
        name: editGroupName.trim(),
        description: editGroupDescription.trim() || null,
        weightPercent: weight,
        order: editingGroup.order,
      });

      if (result.success) {
        setEditingGroup(null);
        router.refresh();
      } else {
        setEditGroupError(result.error);
      }
    });
  }

  function openDeleteGroup(group: AssessmentGroupView) {
    if (group.id === 'ungrouped' || group.weightPercent == null) return;

    const source = assessmentGroups.find((item) => item.id === group.id) ?? {
      id: group.id,
      courseId,
      name: group.name,
      description: group.description,
      weightPercent: group.weightPercent,
      order: group.order,
    };

    setDeletingGroup(source);
    setDeleteGroupError(null);
  }

  function handleDeleteGroup() {
    if (!deletingGroup) return;

    setDeleteGroupError(null);
    startGroupTransition(async () => {
      const result = await deleteAssessmentGroup(courseId, deletingGroup.id);

      if (result.success) {
        setDeletingGroup(null);
        router.refresh();
      } else {
        setDeleteGroupError(result.error);
      }
    });
  }

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h2 className="text-lg font-semibold">Assessments</h2>
          <Badge variant="secondary">{visibleTotal}</Badge>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button size="sm" variant="outline" onClick={() => setShowCreateGroup(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Add Group
          </Button>
        </div>
      </div>

      {hasWeightWarning && (
        <Card className="border-amber-500/50 bg-amber-500/10">
          <CardContent className="flex items-start gap-3 p-4">
            <AlertTriangle className="mt-0.5 size-4 shrink-0 text-amber-500" aria-hidden="true" />
            <div>
              <p className="text-sm font-semibold">Grade weights total {formatPercent(weightTotal)}.</p>
              <p className="text-muted-foreground text-sm">Adjust groups until they equal 100%.</p>
            </div>
          </CardContent>
        </Card>
      )}

      {analytics && <AssessmentAnalyticsPanel analytics={analytics} />}

      {/* Empty state */}
      {visibleTotal === 0 && assessmentGroups.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <ClipboardList className="text-muted-foreground mb-4 size-12" />
            <h3 className="text-lg font-medium">No assessments yet</h3>
            <p className="text-muted-foreground mt-1 text-sm">
              Graded content will appear here after grading is enabled from the content editor.
            </p>
          </CardContent>
        </Card>
      )}

      {contentOwnedActivities.length > 0 && (
        <div className="overflow-hidden rounded-xl border bg-card">
          <section data-testid="content-owned-graded-activities">
            <div className="flex min-h-12 items-center gap-3 bg-muted/60 px-4 py-3">
              <GripVertical className="text-muted-foreground size-4 shrink-0" aria-hidden="true" />
              <ChevronDown className="text-muted-foreground size-4 shrink-0" aria-hidden="true" />
              <div className="min-w-0 flex-1">
                <h3 className="truncate text-sm font-semibold">Content grading</h3>
                <p className="text-muted-foreground mt-0.5 truncate text-xs">
                  Content-owned activities configured from the content editor.
                </p>
              </div>
              <Badge variant="outline" className="shrink-0 rounded-full bg-background">
                Temporary
              </Badge>
            </div>

            <div className="divide-y">
              {contentOwnedActivities.map((item) => (
                <Link
                  key={item.id}
                  href={`/dashboard/learning/courses/${courseId}/content/${item.id}`}
                  className="group flex min-h-16 items-center gap-3 px-4 py-3 transition hover:bg-muted/45"
                >
                  <GripVertical className="text-muted-foreground/70 size-4 shrink-0" aria-hidden="true" />
                  <span className="bg-muted text-muted-foreground flex size-9 shrink-0 items-center justify-center rounded-md">
                    <ClipboardList className="size-4" aria-hidden="true" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm font-semibold underline-offset-2 group-hover:underline">
                      {item.title}
                    </span>
                    <span className="text-muted-foreground mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
                      <span>{item.gradingConfig?.score.maxScore ?? item.maxPoints ?? 0} pts</span>
                      <span>{item.gradingConfig?.outcome.uses.includes('gradebook') ? 'gradebook' : 'feedback'}</span>
                    </span>
                  </span>
                  <Badge variant="secondary" className="hidden shrink-0 sm:inline-flex">
                    {item.type === 'Questionnaire' ? 'Quiz' : item.type}
                  </Badge>
                  <Badge variant="outline" className="hidden shrink-0 sm:inline-flex">
                    Content
                  </Badge>
                </Link>
              ))}
            </div>
          </section>
        </div>
      )}

      {/* Weighted grade groups */}
      {groupedAssessments.length > 0 && (
        <div className="overflow-hidden rounded-xl border bg-card">
          {groupedAssessments.map((group) => (
            <section key={group.id} data-testid={`assessment-group-${group.id}`} className="border-b last:border-b-0">
              <div className="flex min-h-12 items-center gap-3 bg-muted/60 px-4 py-3">
                <GripVertical className="text-muted-foreground size-4 shrink-0" aria-hidden="true" />
                <ChevronDown className="text-muted-foreground size-4 shrink-0" aria-hidden="true" />
                <div className="min-w-0 flex-1">
                  <h3 className="truncate text-sm font-semibold">{group.name}</h3>
                  {group.description && <p className="text-muted-foreground mt-0.5 truncate text-xs">{group.description}</p>}
                </div>
                <Badge variant="outline" className="shrink-0 rounded-full bg-background">
                  {formatWeight(group.weightPercent)}
                </Badge>
                {group.id !== 'ungrouped' && (
                  <>
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      className="size-8 p-0"
                      aria-label={`Edit group ${group.name}`}
                      onClick={() => openEditGroup(group)}
                    >
                      <Pencil className="size-4" aria-hidden="true" />
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      className="size-8 p-0 text-destructive hover:text-destructive"
                      aria-label={`Delete group ${group.name}`}
                      onClick={() => openDeleteGroup(group)}
                    >
                      <Trash2 className="size-4" aria-hidden="true" />
                    </Button>
                  </>
                )}
              </div>

              <div className="divide-y">
                {group.assessments.map((assessment) => (
                  <Link
                    key={assessment.id}
                    href={`${pathname}/${assessment.id}`}
                    className="group flex min-h-16 items-center gap-3 px-4 py-3 transition hover:bg-muted/45"
                  >
                    <GripVertical className="text-muted-foreground/70 size-4 shrink-0" aria-hidden="true" />
                    <span className="bg-muted text-muted-foreground flex size-9 shrink-0 items-center justify-center rounded-md">
                      {typeIcon(assessment.type)}
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-semibold underline-offset-2 group-hover:underline">
                        {assessment.title}
                      </span>
                      <span className="text-muted-foreground mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
                        {assessment.timeLimitMinutes && <span>{assessment.timeLimitMinutes}m</span>}
                        {assessment.maxAttempts && <span>{assessment.maxAttempts} attempts</span>}
                        <span>{assessment.maxScore} pts</span>
                      </span>
                    </span>
                    <Badge variant={typeBadgeVariant(assessment.type)} className="hidden shrink-0 sm:inline-flex">
                      {assessment.type}
                    </Badge>
                    <Badge variant={assessment.isAvailable ? 'secondary' : 'outline'} className="hidden shrink-0 sm:inline-flex">
                      {assessment.isAvailable ? 'available' : 'scheduled'}
                    </Badge>
                  </Link>
                ))}
              </div>
            </section>
          ))}
        </div>
      )}

      {/* Create Assessment Group Dialog */}
      <Dialog open={showCreateGroup} onOpenChange={setShowCreateGroup}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Assessment Group</DialogTitle>
            <DialogDescription>
              Group graded activities into weighted blocks such as quizzes, midterms, projects, or attendance.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label htmlFor="assessment-group-name">Group name</Label>
              <Input
                id="assessment-group-name"
                value={newGroupName}
                onChange={(e) => setNewGroupName(e.target.value)}
                placeholder="e.g. Final Project"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="assessment-group-weight">Weight percent</Label>
              <Input
                id="assessment-group-weight"
                type="number"
                min={0}
                max={100}
                value={newGroupWeight}
                onChange={(e) => setNewGroupWeight(e.target.value)}
              />
            </div>
            {groupError && <p className="text-destructive text-sm">{groupError}</p>}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCreateGroup(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreateGroup} disabled={isGroupPending}>
              {isGroupPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Create Group
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Assessment Group Dialog */}
      <Dialog open={Boolean(editingGroup)} onOpenChange={(open) => !open && setEditingGroup(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Assessment Group</DialogTitle>
            <DialogDescription>
              Update the weighted grading block used to calculate course outcomes.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label htmlFor="edit-assessment-group-name">Group name</Label>
              <Input
                id="edit-assessment-group-name"
                value={editGroupName}
                onChange={(e) => setEditGroupName(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-assessment-group-description">Description</Label>
              <Input
                id="edit-assessment-group-description"
                value={editGroupDescription}
                onChange={(e) => setEditGroupDescription(e.target.value)}
                placeholder="Optional grading guidance"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-assessment-group-weight">Weight percent</Label>
              <Input
                id="edit-assessment-group-weight"
                type="number"
                min={0}
                max={100}
                value={editGroupWeight}
                onChange={(e) => setEditGroupWeight(e.target.value)}
              />
            </div>
            {editGroupError && <p className="text-destructive text-sm">{editGroupError}</p>}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setEditingGroup(null)}>
              Cancel
            </Button>
            <Button onClick={handleUpdateGroup} disabled={isGroupPending}>
              {isGroupPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Save Group
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Assessment Group Dialog */}
      <Dialog open={Boolean(deletingGroup)} onOpenChange={(open) => !open && setDeletingGroup(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Assessment Group</DialogTitle>
            <DialogDescription>
              This removes "{deletingGroup?.name}" and will move existing assessments to ungrouped work.
            </DialogDescription>
          </DialogHeader>

          {deleteGroupError && <p className="text-destructive text-sm">{deleteGroupError}</p>}

          <DialogFooter>
            <Button variant="outline" onClick={() => setDeletingGroup(null)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDeleteGroup} disabled={isGroupPending}>
              {isGroupPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Delete Group
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

    </div>
  );
}
