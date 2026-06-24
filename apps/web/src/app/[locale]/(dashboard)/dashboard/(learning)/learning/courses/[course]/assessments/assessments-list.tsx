'use client';

import { Link, usePathname, useRouter } from '@/i18n/navigation';
import { createAssessment, createAssessmentGroup } from '@/lib/learning/actions';
import type { Assessment, AssessmentGroup, AssessmentType } from '@/lib/learning/queries/assessments';
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Switch } from '@game-guild/ui/components/switch';
import { ChevronDown, ClipboardList, FileText, GripVertical, LinkIcon, Loader2, Plus, Target, Trophy } from 'lucide-react';
import React, { useState, useTransition } from 'react';

const ASSESSMENT_TYPE_OPTIONS: { value: AssessmentType; label: string }[] = [
  { value: 'Quiz', label: 'Quiz' },
  { value: 'Exam', label: 'Exam' },
  { value: 'Assignment', label: 'Assignment' },
  { value: 'Project', label: 'Project' },
  { value: 'PeerReview', label: 'Peer Review' },
  { value: 'SelfAssessment', label: 'Self Assessment' },
];

function typeIcon(type: AssessmentType) {
  switch (type) {
    case 'Quiz':
      return <ClipboardList className="size-4" />;
    case 'Exam':
      return <FileText className="size-4" />;
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
    case 'Exam':
      return 'default';
    default:
      return 'outline';
  }
}

interface AssessmentsListProps {
  courseId: string;
  assessments: Assessment[];
  total: number;
  contentItems?: ContentItem[];
  assessmentGroups?: AssessmentGroup[];
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
  return `${Number.isInteger(weightPercent) ? weightPercent : weightPercent.toFixed(1)}% of Total`;
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

export function AssessmentsList({
  courseId,
  assessments,
  total,
  contentItems = [],
  assessmentGroups = [],
}: AssessmentsListProps) {
  const router = useRouter();
  const pathname = usePathname();
  const [showCreate, setShowCreate] = useState(false);
  const [showCreateGroup, setShowCreateGroup] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [isGroupPending, startGroupTransition] = useTransition();

  // Create form state
  const [newTitle, setNewTitle] = useState('');
  const [newType, setNewType] = useState<AssessmentType>('Quiz');
  const [newGroupId, setNewGroupId] = useState('none');
  const [newMaxScore, setNewMaxScore] = useState('100');
  const [newPassingScore, setNewPassingScore] = useState('70');
  const [newIsRequired, setNewIsRequired] = useState(true);
  const [createError, setCreateError] = useState<string | null>(null);
  const [newGroupName, setNewGroupName] = useState('');
  const [newGroupWeight, setNewGroupWeight] = useState('20');
  const [groupError, setGroupError] = useState<string | null>(null);

  // Build content item lookup for linked assessment indicators
  const contentMap = React.useMemo(() => {
    const map = new Map<string, ContentItem>();
    for (const item of contentItems) {
      map.set(item.id, item);
    }
    return map;
  }, [contentItems]);

  const groupedAssessments = React.useMemo(
    () => buildGroupedAssessments(assessments, assessmentGroups),
    [assessments, assessmentGroups],
  );

  function handleCreate() {
    if (!newTitle.trim()) {
      setCreateError('Title is required.');
      return;
    }
    setCreateError(null);

    startTransition(async () => {
      const result = await createAssessment({
        courseId,
        title: newTitle.trim(),
        type: newType,
        assessmentGroupId: newGroupId === 'none' ? null : newGroupId,
        maxScore: Number(newMaxScore) || 100,
        passingScore: Number(newPassingScore) || 70,
        isRequired: newIsRequired,
      });

      if (result.success) {
        setShowCreate(false);
        setNewTitle('');
        setNewType('Quiz');
        setNewGroupId('none');
        setNewMaxScore('100');
        setNewPassingScore('70');
        setNewIsRequired(true);
        router.refresh();
      } else {
        setCreateError(result.error);
      }
    });
  }

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

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h2 className="text-lg font-semibold">Assessments</h2>
          <Badge variant="secondary">{total}</Badge>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button size="sm" variant="outline" onClick={() => setShowCreateGroup(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Add Group
          </Button>
          <Button size="sm" onClick={() => setShowCreate(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Add Assessment
          </Button>
        </div>
      </div>

      {/* Empty state */}
      {assessments.length === 0 && assessmentGroups.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <ClipboardList className="text-muted-foreground mb-4 size-12" />
            <h3 className="text-lg font-medium">No assessments yet</h3>
            <p className="text-muted-foreground mt-1 text-sm">
              Create quizzes, exams, and assignments to evaluate student learning.
            </p>
            <Button className="mt-4" size="sm" onClick={() => setShowCreate(true)}>
              <Plus className="mr-2 h-4 w-4" />
              Create First Assessment
            </Button>
          </CardContent>
        </Card>
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
                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  className="size-8 p-0"
                  aria-label={`Add activity to ${group.name}`}
                  onClick={() => {
                    setNewGroupId(group.id === 'ungrouped' ? 'none' : group.id);
                    setShowCreate(true);
                  }}
                >
                  <Plus className="size-4" aria-hidden="true" />
                </Button>
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
                        {assessment.contentId ? (
                          <span className="inline-flex items-center gap-1">
                            <LinkIcon className="size-3" aria-hidden="true" />
                            {contentMap.get(assessment.contentId)?.title ?? 'Linked content'}
                          </span>
                        ) : (
                          <span>Course-level activity</span>
                        )}
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

      {/* Create Assessment Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Assessment</DialogTitle>
            <DialogDescription>
              Add a new quiz, exam, assignment, or project to this course.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label htmlFor="assessment-title">Title</Label>
              <Input
                id="assessment-title"
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                placeholder="e.g. Midterm Exam"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="assessment-type">Type</Label>
              <Select value={newType} onValueChange={(v) => setNewType(v as AssessmentType)}>
                <SelectTrigger id="assessment-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ASSESSMENT_TYPE_OPTIONS.map((opt) => (
                    <SelectItem key={opt.value} value={opt.value}>
                      {opt.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="assessment-group">Grade group</Label>
              <Select value={newGroupId} onValueChange={setNewGroupId}>
                <SelectTrigger id="assessment-group">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">No group</SelectItem>
                  {assessmentGroups.map((group) => (
                    <SelectItem key={group.id} value={group.id}>
                      {group.name} ({formatWeight(group.weightPercent)})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="max-score">Max Score</Label>
                <Input
                  id="max-score"
                  type="number"
                  min={1}
                  value={newMaxScore}
                  onChange={(e) => setNewMaxScore(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="passing-score">Passing Score</Label>
                <Input
                  id="passing-score"
                  type="number"
                  min={0}
                  value={newPassingScore}
                  onChange={(e) => setNewPassingScore(e.target.value)}
                />
              </div>
            </div>

            <div className="flex items-center justify-between">
              <Label htmlFor="is-required">Required</Label>
              <Switch
                id="is-required"
                checked={newIsRequired}
                onCheckedChange={setNewIsRequired}
              />
            </div>

            {createError && <p className="text-destructive text-sm">{createError}</p>}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCreate(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={isPending}>
              {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
