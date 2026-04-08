'use client';

import React, { useState, useTransition } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Switch } from '@game-guild/ui/components/switch';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@game-guild/ui/components/dialog';
import { ClipboardList, FileText, LinkIcon, Loader2, Plus, Target, Trophy } from 'lucide-react';
import type { Assessment, AssessmentType } from '@/lib/learning/queries/assessments';
import type { ContentItem } from '@/lib/learning/types';
import { createAssessment } from '@/lib/learning/actions';

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
}

export function AssessmentsList({ courseId, assessments, total, contentItems = [] }: AssessmentsListProps) {
  const router = useRouter();
  const pathname = usePathname();
  const [showCreate, setShowCreate] = useState(false);
  const [isPending, startTransition] = useTransition();

  // Create form state
  const [newTitle, setNewTitle] = useState('');
  const [newType, setNewType] = useState<AssessmentType>('Quiz');
  const [newMaxScore, setNewMaxScore] = useState('100');
  const [newPassingScore, setNewPassingScore] = useState('70');
  const [newIsRequired, setNewIsRequired] = useState(true);
  const [createError, setCreateError] = useState<string | null>(null);

  // Build content item lookup for linked assessment indicators
  const contentMap = React.useMemo(() => {
    const map = new Map<string, ContentItem>();
    for (const item of contentItems) {
      map.set(item.id, item);
    }
    return map;
  }, [contentItems]);

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
        maxScore: Number(newMaxScore) || 100,
        passingScore: Number(newPassingScore) || 70,
        isRequired: newIsRequired,
      });

      if (result.success) {
        setShowCreate(false);
        setNewTitle('');
        setNewType('Quiz');
        setNewMaxScore('100');
        setNewPassingScore('70');
        setNewIsRequired(true);
        router.refresh();
      } else {
        setCreateError(result.error);
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
        <Button size="sm" onClick={() => setShowCreate(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Add Assessment
        </Button>
      </div>

      {/* Empty state */}
      {assessments.length === 0 && (
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

      {/* Assessment cards */}
      {assessments.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {assessments
            .sort((a, b) => a.order - b.order)
            .map((assessment) => (
              <Link key={assessment.id} href={`${pathname}/${assessment.id}`}>
                <Card className="hover:border-primary/50 h-full transition-colors">
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        {typeIcon(assessment.type)}
                        <CardTitle className="text-base">{assessment.title}</CardTitle>
                      </div>
                      <Badge variant={typeBadgeVariant(assessment.type)}>
                        {assessment.type}
                      </Badge>
                    </div>
                  </CardHeader>
                  <CardContent>
                    {assessment.description && (
                      <p className="text-muted-foreground mb-3 line-clamp-2 text-sm">
                        {assessment.description}
                      </p>
                    )}
                    {assessment.contentId ? (
                      <div className="mb-3 flex items-center gap-1.5 text-xs text-blue-600">
                        <LinkIcon className="size-3" />
                        <span>Linked to: {contentMap.get(assessment.contentId)?.title ?? 'Unknown item'}</span>
                      </div>
                    ) : (
                      <div className="mb-3 flex items-center gap-1.5 text-xs text-muted-foreground">
                        <LinkIcon className="size-3" />
                        <span>Not linked to content</span>
                      </div>
                    )}
                    <div className="text-muted-foreground flex flex-wrap gap-x-4 gap-y-1 text-xs">
                      <span>
                        Score: {assessment.passingScore}/{assessment.maxScore}
                      </span>
                      {assessment.timeLimitMinutes && (
                        <span>{assessment.timeLimitMinutes} min</span>
                      )}
                      {assessment.maxAttempts && (
                        <span>{assessment.maxAttempts} attempts</span>
                      )}
                      {assessment.isRequired && (
                        <Badge variant="outline" className="text-xs">
                          Required
                        </Badge>
                      )}
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
        </div>
      )}

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
