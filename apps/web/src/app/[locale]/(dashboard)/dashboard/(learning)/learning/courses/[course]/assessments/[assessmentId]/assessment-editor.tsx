'use client';

import React, { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Switch } from '@game-guild/ui/components/switch';
import { Separator } from '@game-guild/ui/components/separator';
import { ArrowLeft, Clock, Loader2, Save, Trash2 } from 'lucide-react';
import type { Assessment, AssessmentGroup, AssessmentType } from '@/lib/learning/queries/assessments';
import { updateAssessment, deleteAssessment } from '@/lib/learning/actions';

const ASSESSMENT_TYPE_OPTIONS: { value: AssessmentType; label: string }[] = [
  { value: 'Quiz', label: 'Quiz' },
  { value: 'Exam', label: 'Exam' },
  { value: 'Assignment', label: 'Assignment' },
  { value: 'Project', label: 'Project' },
  { value: 'PeerReview', label: 'Peer Review' },
  { value: 'SelfAssessment', label: 'Self Assessment' },
];

interface AssessmentEditorProps {
  courseId: string;
  assessment: Assessment;
  assessmentGroups?: AssessmentGroup[];
}

function formatWeight(weightPercent: number) {
  return `${Number.isInteger(weightPercent) ? weightPercent : weightPercent.toFixed(1)}% of Total`;
}

export function AssessmentEditor({ courseId, assessment, assessmentGroups = [] }: AssessmentEditorProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [isDeleting, startDeleteTransition] = useTransition();

  const [title, setTitle] = useState(assessment.title);
  const [description, setDescription] = useState(assessment.description ?? '');
  const [maxScore, setMaxScore] = useState(String(assessment.maxScore));
  const [passingScore, setPassingScore] = useState(String(assessment.passingScore));
  const [timeLimitMinutes, setTimeLimitMinutes] = useState(
    assessment.timeLimitMinutes != null ? String(assessment.timeLimitMinutes) : '',
  );
  const [maxAttempts, setMaxAttempts] = useState(
    assessment.maxAttempts != null ? String(assessment.maxAttempts) : '',
  );
  const [isRequired, setIsRequired] = useState(assessment.isRequired);
  const [assessmentGroupId, setAssessmentGroupId] = useState(assessment.assessmentGroupId ?? 'none');
  const [availableFrom, setAvailableFrom] = useState(
    assessment.availableFrom ? assessment.availableFrom.slice(0, 16) : '',
  );
  const [availableUntil, setAvailableUntil] = useState(
    assessment.availableUntil ? assessment.availableUntil.slice(0, 16) : '',
  );

  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  function handleSave() {
    if (!title.trim()) {
      setError('Title is required.');
      return;
    }
    setError(null);
    setSaved(false);

    startTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId: assessment.id,
        title: title.trim(),
        description: description.trim() || undefined,
        maxScore: Number(maxScore) || undefined,
        passingScore: Number(passingScore) || undefined,
        timeLimitMinutes: timeLimitMinutes ? Number(timeLimitMinutes) : null,
        maxAttempts: maxAttempts ? Number(maxAttempts) : null,
        isRequired,
        availableFrom: availableFrom || null,
        availableUntil: availableUntil || null,
        assessmentGroupId: assessmentGroupId === 'none' ? null : assessmentGroupId,
        clearAssessmentGroupId: assessmentGroupId === 'none',
      });

      if (result.success) {
        setSaved(true);
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleDelete() {
    if (!confirm('Are you sure you want to delete this assessment?')) return;

    startDeleteTransition(async () => {
      const result = await deleteAssessment(courseId, assessment.id);
      if (result.success) {
        router.back();
      } else {
        setError(result.error);
      }
    });
  }

  function handleBack() {
    router.back();
  }

  const typeLabel = ASSESSMENT_TYPE_OPTIONS.find((o) => o.value === assessment.type)?.label ?? assessment.type;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={handleBack}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back
        </Button>
        <div className="flex-1">
          <p className="text-muted-foreground text-sm">Assessment Editor</p>
          <h1 className="text-2xl font-bold">{assessment.title}</h1>
        </div>
        <Badge variant="secondary">{typeLabel}</Badge>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main editor */}
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle>Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="title">Title</Label>
                <Input
                  id="title"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Assessment title"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Instructions or description for students"
                  rows={4}
                />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Scoring</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="max-score">Max Score</Label>
                  <Input
                    id="max-score"
                    type="number"
                    min={1}
                    value={maxScore}
                    onChange={(e) => setMaxScore(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="passing-score">Passing Score</Label>
                  <Input
                    id="passing-score"
                    type="number"
                    min={0}
                    value={passingScore}
                    onChange={(e) => setPassingScore(e.target.value)}
                  />
                </div>
              </div>
              <p className="text-muted-foreground text-xs">
                Students need at least {passingScore || 0} out of {maxScore || 0} points to pass (
                {maxScore && Number(maxScore) > 0
                  ? Math.round((Number(passingScore || 0) / Number(maxScore)) * 100)
                  : 0}
                %).
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Availability</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="available-from">Available From</Label>
                  <Input
                    id="available-from"
                    type="datetime-local"
                    value={availableFrom}
                    onChange={(e) => setAvailableFrom(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="available-until">Available Until</Label>
                  <Input
                    id="available-until"
                    type="datetime-local"
                    value={availableUntil}
                    onChange={(e) => setAvailableUntil(e.target.value)}
                  />
                </div>
              </div>
              <p className="text-muted-foreground text-xs">
                Leave empty to make the assessment always available.
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Settings</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>Type</Label>
                <Select value={assessment.type} disabled>
                  <SelectTrigger>
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
                <p className="text-muted-foreground text-xs">
                  Type cannot be changed after creation.
                </p>
              </div>

              <Separator />

              <div className="space-y-2">
                <Label htmlFor="grade-group">Grade group</Label>
                <Select value={assessmentGroupId} onValueChange={setAssessmentGroupId}>
                  <SelectTrigger id="grade-group">
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
                <p className="text-muted-foreground text-xs">
                  Choose the weighted block this activity contributes to.
                </p>
              </div>

              <Separator />

              <div className="space-y-2">
                <Label htmlFor="time-limit">
                  <Clock className="mr-1 inline h-3 w-3" />
                  Time Limit (minutes)
                </Label>
                <Input
                  id="time-limit"
                  type="number"
                  min={0}
                  value={timeLimitMinutes}
                  onChange={(e) => setTimeLimitMinutes(e.target.value)}
                  placeholder="No limit"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="max-attempts">Max Attempts</Label>
                <Input
                  id="max-attempts"
                  type="number"
                  min={1}
                  value={maxAttempts}
                  onChange={(e) => setMaxAttempts(e.target.value)}
                  placeholder="Unlimited"
                />
              </div>

              <div className="flex items-center justify-between">
                <Label htmlFor="required">Required</Label>
                <Switch
                  id="required"
                  checked={isRequired}
                  onCheckedChange={setIsRequired}
                />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {error && <p className="text-destructive text-sm">{error}</p>}
              {saved && <p className="text-sm text-green-600">Saved successfully.</p>}

              <Button className="w-full" onClick={handleSave} disabled={isPending}>
                {isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Save className="mr-2 h-4 w-4" />
                )}
                Save Changes
              </Button>
              <Button variant="outline" className="w-full" onClick={handleBack}>
                Cancel
              </Button>

              <Separator />

              <Button
                variant="destructive"
                className="w-full"
                onClick={handleDelete}
                disabled={isDeleting}
              >
                {isDeleting ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Trash2 className="mr-2 h-4 w-4" />
                )}
                Delete Assessment
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Info</CardTitle>
            </CardHeader>
            <CardContent className="text-muted-foreground space-y-1 text-sm">
              <p>Type: {typeLabel}</p>
              <p>Order: {assessment.order}</p>
              <p>
                Available:{' '}
                <Badge variant={assessment.isAvailable ? 'default' : 'secondary'} className="text-xs">
                  {assessment.isAvailable ? 'Yes' : 'No'}
                </Badge>
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
