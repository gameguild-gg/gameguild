"use client";

import React, { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Checkbox } from "@game-guild/ui/components/checkbox";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Textarea } from "@game-guild/ui/components/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Switch } from "@game-guild/ui/components/switch";
import { Separator } from "@game-guild/ui/components/separator";
import { ArrowLeft, ClipboardCheck, Clock, Code, Loader2, Plus, Save, Trash2, X } from "lucide-react";
import type {
  Assessment,
  AssessmentGroup,
  AssessmentPresentationMode,
  AssessmentType,
} from "@/lib/learning/queries/assessments";
import {
  ASSESSMENT_GRADING_METHOD_FLAGS,
  parseGradingMethods,
  serializeGradingMethods,
  type AssessmentGradingMethodFlag,
} from "@/lib/learning/assessment-grading-methods";
import type { CourseContentItemViewModel } from "@/lib/learning/queries/course";
import { Link } from "@/i18n/navigation";
import {
  deleteAssessment,
  deleteRubric,
  saveRubric,
  updateAssessment,
} from "@/lib/learning/actions";
import { useLearningBase } from '@/lib/learning/use-learning-base';

const ASSESSMENT_TYPE_OPTIONS: { value: AssessmentType; label: string }[] = [
  { value: "Quiz", label: "Quiz" },
  { value: "Assignment", label: "Assignment" },
  { value: "Project", label: "Project" },
  { value: "PeerReview", label: "Peer Review" },
  { value: "SelfAssessment", label: "Self Assessment" },
];

const LINKED_CONTENT_NONE = "none";

const GROUP_SET_NONE = "none";

const RUBRIC_LOCK_MESSAGE = "Rubric locked after grading started";

const DEFAULT_PEER_REVIEWS = 3;

export interface GroupSetOption {
  id: string;
  name: string;
}

export interface RubricCriterionRow {
  description: string;
  points: number | null;
}

export interface RubricViewModel {
  title: string;
  criteria: { description: string; points: number; order: number }[];
}

interface AssessmentEditorProps {
  courseId: string;
  assessment: Assessment;
  assessmentGroups?: AssessmentGroup[];
  courseContent?: CourseContentItemViewModel[];
  groupSets?: GroupSetOption[];
  rubric?: RubricViewModel | null;
  rubricLocked?: boolean;
  canManage?: boolean;
}

function formatWeight(weightPercent: number) {
  return `${Number.isInteger(weightPercent) ? weightPercent : weightPercent.toFixed(1)}% of Total`;
}

export function AssessmentEditor({
  courseId,
  assessment,
  assessmentGroups = [],
  courseContent = [],
  groupSets = [],
  rubric = null,
  rubricLocked = false,
  canManage = false,
}: AssessmentEditorProps) {
  const learningBase = useLearningBase();
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [isDeleting, startDeleteTransition] = useTransition();
  const [isLinkPending, startLinkTransition] = useTransition();
  const [isGradingPending, startGradingTransition] = useTransition();
  const [isPolicyPending, startPolicyTransition] = useTransition();
  const [isRubricPending, startRubricTransition] = useTransition();
  const isQuiz = assessment.type === "Quiz";

  const [title, setTitle] = useState(assessment.title);
  const [description, setDescription] = useState(assessment.description ?? "");
  const [maxScore, setMaxScore] = useState(String(assessment.maxScore));
  const [passingScore, setPassingScore] = useState(
    String(assessment.passingScore),
  );
  const [timeLimitMinutes, setTimeLimitMinutes] = useState(
    assessment.timeLimitMinutes != null
      ? String(assessment.timeLimitMinutes)
      : "",
  );
  const [maxAttempts, setMaxAttempts] = useState(
    assessment.maxAttempts != null ? String(assessment.maxAttempts) : "",
  );
  const [isRequired, setIsRequired] = useState(assessment.isRequired);
  const [assessmentGroupId, setAssessmentGroupId] = useState(
    assessment.assessmentGroupId ?? "none",
  );
  const [presentationMode, setPresentationMode] =
    useState<AssessmentPresentationMode>(assessment.presentationMode);
  const [availableFrom, setAvailableFrom] = useState(
    assessment.availableFrom ? assessment.availableFrom.slice(0, 16) : "",
  );
  const [availableUntil, setAvailableUntil] = useState(
    assessment.availableUntil ? assessment.availableUntil.slice(0, 16) : "",
  );
  const [linkedContentId, setLinkedContentId] = useState(
    assessment.contentId ?? LINKED_CONTENT_NONE,
  );
  const [gradingMethods, setGradingMethods] = useState<Set<AssessmentGradingMethodFlag>>(
    () => parseGradingMethods(assessment.gradingMethods),
  );
  const [groupSetId, setGroupSetId] = useState(
    assessment.groupSetId ?? GROUP_SET_NONE,
  );
  const [groupAssignmentOn, setGroupAssignmentOn] = useState(
    assessment.groupSetId != null,
  );
  const [peerReviewsRequired, setPeerReviewsRequired] = useState(
    String(assessment.peerReviewsRequiredCount || DEFAULT_PEER_REVIEWS),
  );
  const [rubricOn, setRubricOn] = useState(rubric != null);
  const [rubricTitle] = useState(rubric?.title ?? "Rubric");
  const [criteria, setCriteria] = useState<RubricCriterionRow[]>(() =>
    rubric != null && rubric.criteria.length > 0
      ? rubric.criteria.map((criterion) => ({
          description: criterion.description,
          points: criterion.points,
        }))
      : [{ description: "", points: null }],
  );
  const [rubricLockedLocal, setRubricLockedLocal] = useState(rubricLocked);
  const [rubricSaved, setRubricSaved] = useState(false);

  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  function handleSave() {
    if (!title.trim()) {
      setError("Title is required.");
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
        assessmentGroupId:
          assessmentGroupId === "none" ? null : assessmentGroupId,
        clearAssessmentGroupId: assessmentGroupId === "none",
        presentationMode,
      });

      if (!result.success) {
        setError(result.error);
        return;
      }

      setSaved(true);
      router.refresh();
    });
  }

  function handleDelete() {
    if (!confirm("Are you sure you want to delete this assessment?")) return;

    startDeleteTransition(async () => {
      const result = await deleteAssessment(courseId, assessment.id);
      if (result.success) {
        router.push(
          `${learningBase}/courses/${encodeURIComponent(courseId)}/assessments`,
        );
      } else {
        setError(result.error);
      }
    });
  }

  function handleBack() {
    router.push(
      `${learningBase}/courses/${encodeURIComponent(courseId)}/assessments`,
    );
  }

  function handleLinkedContentChange(value: string) {
    const previous = linkedContentId;
    const next = value === LINKED_CONTENT_NONE ? LINKED_CONTENT_NONE : value;
    setLinkedContentId(next);
    setError(null);

    startLinkTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId: assessment.id,
        contentId: next === LINKED_CONTENT_NONE ? null : next,
        clearContentId: next === LINKED_CONTENT_NONE,
      });

      if (!result.success) {
        setLinkedContentId(previous);
        setError(result.error);
        return;
      }

      router.refresh();
    });
  }

  function handleGradingMethodToggle(
    flag: AssessmentGradingMethodFlag,
    checked: boolean,
  ) {
    const next = new Set(gradingMethods);
    if (checked) next.add(flag);
    else next.delete(flag);

    const previous = gradingMethods;
    setGradingMethods(next);
    setError(null);

    startGradingTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId: assessment.id,
        gradingMethods: serializeGradingMethods(next),
      });

      if (!result.success) {
        setGradingMethods(previous);
        setError(result.error);
        return;
      }

      router.refresh();
    });
  }

  function handleGroupAssignmentToggle(checked: boolean) {
    setGroupAssignmentOn(checked);
    setError(null);

    if (checked || groupSetId === GROUP_SET_NONE) {
      return;
    }

    const previous = groupSetId;
    setGroupSetId(GROUP_SET_NONE);

    startPolicyTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId: assessment.id,
        groupSetId: null,
        clearGroupSetId: true,
      });

      if (!result.success) {
        setGroupSetId(previous);
        setGroupAssignmentOn(true);
        setError(result.error);
        return;
      }

      router.refresh();
    });
  }

  function handleGroupSetChange(value: string) {
    const previous = groupSetId;
    setGroupSetId(value);
    setError(null);

    startPolicyTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId: assessment.id,
        groupSetId: value === GROUP_SET_NONE ? null : value,
        clearGroupSetId: value === GROUP_SET_NONE,
      });

      if (!result.success) {
        setGroupSetId(previous);
        setError(result.error);
        return;
      }

      router.refresh();
    });
  }

  function handlePeerReviewToggle(checked: boolean) {
    const next = new Set(gradingMethods);
    if (checked) next.add("PeerReview");
    else next.delete("PeerReview");

    const previous = gradingMethods;
    setGradingMethods(next);
    setError(null);

    startPolicyTransition(async () => {
      const result = await updateAssessment(
        checked
          ? {
              courseId,
              assessmentId: assessment.id,
              gradingMethods: serializeGradingMethods(next),
              peerReviewsRequiredCount: Math.max(
                1,
                Number(peerReviewsRequired) || DEFAULT_PEER_REVIEWS,
              ),
            }
          : {
              courseId,
              assessmentId: assessment.id,
              gradingMethods: serializeGradingMethods(next),
            },
      );

      if (!result.success) {
        setGradingMethods(previous);
        setError(result.error);
        return;
      }

      router.refresh();
    });
  }

  function handlePeerReviewsBlur() {
    const count = Number(peerReviewsRequired);
    if (!Number.isInteger(count) || count < 1) {
      return;
    }
    if (count === assessment.peerReviewsRequiredCount) {
      return;
    }

    startPolicyTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId: assessment.id,
        peerReviewsRequiredCount: count,
      });

      if (!result.success) {
        setError(result.error);
        return;
      }

      router.refresh();
    });
  }

  const rubricLockedNow = rubricLockedLocal;
  const criteriaPointsSum = criteria.reduce(
    (sum, row) => sum + (row.points ?? 0),
    0,
  );
  const criteriaValid =
    criteria.length > 0 &&
    criteria.every(
      (row) => row.description.trim() !== "" && row.points != null && row.points > 0,
    );
  const rubricSumMatches = criteriaPointsSum === assessment.maxScore;
  const canSaveRubric = criteriaValid && rubricSumMatches && !rubricLockedNow;

  function addCriterionRow() {
    setCriteria((rows) => [...rows, { description: "", points: null }]);
  }

  function removeCriterionRow(index: number) {
    setCriteria((rows) =>
      rows.length > 1
        ? rows.filter((_, rowIndex) => rowIndex !== index)
        : [{ description: "", points: null }],
    );
  }

  function handleSaveRubric() {
    setError(null);
    setRubricSaved(false);

    startRubricTransition(async () => {
      const result = await saveRubric({
        assessmentId: assessment.id,
        title: rubricTitle,
        criteria: criteria.map((row, index) => ({
          description: row.description.trim(),
          points: row.points ?? 0,
          order: index,
        })),
      });

      if (!result.success) {
        if (result.error.includes(RUBRIC_LOCK_MESSAGE)) {
          setRubricLockedLocal(true);
          setError(null);
          return;
        }
        setError(result.error);
        return;
      }

      setRubricSaved(true);
      router.refresh();
    });
  }

  function handleDeleteRubric() {
    if (!confirm("Are you sure you want to remove the rubric from this assessment?")) {
      return;
    }

    startRubricTransition(async () => {
      const result = await deleteRubric(assessment.id);

      if (!result.success) {
        if (result.error.includes(RUBRIC_LOCK_MESSAGE)) {
          setRubricLockedLocal(true);
          setError(null);
          return;
        }
        setError(result.error);
        return;
      }

      setRubricOn(false);
      setCriteria([{ description: "", points: null }]);
      router.refresh();
    });
  }

  const typeLabel =
    ASSESSMENT_TYPE_OPTIONS.find((o) => o.value === assessment.type)?.label ??
    assessment.type;

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
        {canManage && (
          <Button variant="outline" size="sm" asChild>
            <Link
              href={`${learningBase}/courses/${encodeURIComponent(courseId)}/assessments/${assessment.id}/submissions`}
              data-testid="grade-submissions-button"
            >
              <ClipboardCheck className="mr-2 h-4 w-4" />
              Grade submissions
            </Link>
          </Button>
        )}
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
                Students need at least {passingScore || 0} out of{" "}
                {maxScore || 0} points to pass (
                {maxScore && Number(maxScore) > 0
                  ? Math.round(
                      (Number(passingScore || 0) / Number(maxScore)) * 100,
                    )
                  : 0}
                %).
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Rubric</CardTitle>
                <div className="flex items-center gap-2">
                  <Label
                    htmlFor="grade-by-rubric"
                    className="text-sm font-normal text-muted-foreground"
                  >
                    Grade by rubric
                  </Label>
                  <Switch
                    id="grade-by-rubric"
                    checked={rubricOn}
                    onCheckedChange={(checked) => setRubricOn(checked === true)}
                    disabled={rubricLockedNow}
                  />
                </div>
              </div>
            </CardHeader>
            {rubricOn && (
              <CardContent className="space-y-4">
                {rubricLockedNow && (
                  <p className="text-destructive text-sm" role="alert">
                    {RUBRIC_LOCK_MESSAGE}
                  </p>
                )}
                <div className="space-y-3">
                  {criteria.map((row, index) => (
                    <div
                      key={index}
                      className="flex items-end gap-2"
                    >
                          <div className="flex-1 space-y-1">
                            <Label htmlFor={`criterion-${index + 1}-description`}>
                              Criterion {index + 1} description
                            </Label>
                            <Input
                              id={`criterion-${index + 1}-description`}
                              value={row.description}
                              onChange={(e) =>
                                setCriteria((rows) =>
                                  rows.map((current, rowIndex) =>
                                    rowIndex === index
                                      ? { ...current, description: e.target.value }
                                      : current,
                                  ),
                                )
                              }
                              placeholder="What this criterion assesses"
                              disabled={isRubricPending || rubricLockedNow}
                            />
                          </div>
                          <div className="w-24 space-y-1">
                            <Label htmlFor={`criterion-${index + 1}-points`}>
                              Criterion {index + 1} points
                            </Label>
                            <Input
                              id={`criterion-${index + 1}-points`}
                              type="number"
                              min={1}
                              value={row.points ?? ""}
                              onChange={(e) =>
                                setCriteria((rows) =>
                                  rows.map((current, rowIndex) =>
                                    rowIndex === index
                                      ? {
                                          ...current,
                                          points: e.target.value
                                            ? Number(e.target.value)
                                            : null,
                                        }
                                      : current,
                                  ),
                                )
                              }
                              disabled={isRubricPending || rubricLockedNow}
                            />
                          </div>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            aria-label={`Remove criterion ${index + 1}`}
                            onClick={() => removeCriterionRow(index)}
                            disabled={isRubricPending}
                          >
                            <X className="size-4" />
                          </Button>
                        </div>
                      ))}
                    </div>

                    <div className="flex items-center justify-between">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={addCriterionRow}
                        disabled={isRubricPending}
                      >
                        <Plus className="mr-2 size-4" />
                        Add criterion
                      </Button>
                      <p
                        data-testid="rubric-sum"
                        className={`text-sm font-semibold ${
                          rubricSumMatches
                            ? "text-green-600"
                            : "text-destructive"
                        }`}
                      >
                        Σ {criteriaPointsSum} / {assessment.maxScore}
                        {!rubricSumMatches && (
                          <span>
                            {" "}
                            ({criteriaPointsSum > assessment.maxScore ? "+" : ""}
                            {criteriaPointsSum - assessment.maxScore})
                          </span>
                        )}
                      </p>
                    </div>

                    <div className="flex items-center gap-2">
                      <Button
                        type="button"
                        onClick={handleSaveRubric}
                        disabled={!canSaveRubric || isRubricPending}
                      >
                        {isRubricPending ? (
                          <Loader2 className="mr-2 size-4 animate-spin" />
                        ) : (
                          <Save className="mr-2 size-4" />
                        )}
                        Save rubric
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={handleDeleteRubric}
                        disabled={isRubricPending}
                      >
                        <Trash2 className="mr-2 size-4" />
                        Delete rubric
                      </Button>
                      {rubricSaved && (
                        <span className="text-sm text-green-600">
                          Rubric saved.
                        </span>
                      )}
                    </div>
                    <p className="text-muted-foreground text-xs">
                      Criterion points must sum to the assessment max score.
                      Locked after grading starts.
                    </p>
              </CardContent>
            )}
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
                <Select
                  value={assessmentGroupId}
                  onValueChange={setAssessmentGroupId}
                >
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
                <Label htmlFor="linked-content">Linked content</Label>
                <Select
                  value={linkedContentId}
                  onValueChange={handleLinkedContentChange}
                  disabled={isLinkPending}
                >
                  <SelectTrigger id="linked-content">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={LINKED_CONTENT_NONE}>
                      None (standalone assessment)
                    </SelectItem>
                    {courseContent.map((item) => (
                      <SelectItem key={item.id} value={item.id}>
                        {item.title}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-muted-foreground text-xs">
                  Link this assessment to a content item, or leave it
                  standalone.
                </p>
              </div>

              <Separator />

              <div className="space-y-2">
                <Label>Grading methods</Label>
                <div className="space-y-2">
                  {ASSESSMENT_GRADING_METHOD_FLAGS.map((flag) => (
                    <label
                      key={flag}
                      htmlFor={`grading-method-${flag}`}
                      className="flex items-center gap-2 text-sm"
                    >
                      <Checkbox
                        id={`grading-method-${flag}`}
                        checked={gradingMethods.has(flag)}
                        onCheckedChange={(checked: boolean) =>
                          handleGradingMethodToggle(flag, checked === true)
                        }
                        disabled={isGradingPending}
                      />
                      {flag}
                    </label>
                  ))}
                </div>
                <p className="text-muted-foreground text-xs">
                  How this assessment can be graded. Multiple allowed.
                </p>
              </div>

              <Separator />

              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="group-assignment">Group assignment</Label>
                  <Switch
                    id="group-assignment"
                    checked={groupAssignmentOn}
                    onCheckedChange={handleGroupAssignmentToggle}
                    disabled={isPolicyPending}
                  />
                </div>
                {groupAssignmentOn && (
                  <div className="space-y-2">
                    <Label htmlFor="group-set">Group set</Label>
                    <Select
                      value={groupSetId}
                      onValueChange={handleGroupSetChange}
                      disabled={isPolicyPending}
                    >
                      <SelectTrigger id="group-set">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value={GROUP_SET_NONE}>
                          No group set
                        </SelectItem>
                        {groupSets.map((set) => (
                          <SelectItem key={set.id} value={set.id}>
                            {set.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <p className="text-muted-foreground text-xs">
                      Students submit once per group; the grade applies to every
                      member.
                    </p>
                  </div>
                )}
              </div>

              <Separator />

              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="peer-review">Peer review</Label>
                  <Switch
                    id="peer-review"
                    checked={gradingMethods.has("PeerReview")}
                    onCheckedChange={(checked) =>
                      handlePeerReviewToggle(checked === true)
                    }
                    disabled={isPolicyPending}
                  />
                </div>
                {gradingMethods.has("PeerReview") && (
                  <div className="space-y-2">
                    <Label htmlFor="required-reviews">Required reviews</Label>
                    <Input
                      id="required-reviews"
                      type="number"
                      min={1}
                      value={peerReviewsRequired}
                      onChange={(e) => setPeerReviewsRequired(e.target.value)}
                      onBlur={handlePeerReviewsBlur}
                      disabled={isPolicyPending}
                    />
                    <p className="text-muted-foreground text-xs">
                      Each student reviews this many peers before the assessment
                      closes.
                    </p>
                  </div>
                )}
              </div>

              <Separator />

              {isQuiz && (
                <>
                  <div className="space-y-2">
                    <Label htmlFor="presentation-mode">Presentation</Label>
                    <Select
                      value={presentationMode}
                      onValueChange={(value) =>
                        setPresentationMode(value as AssessmentPresentationMode)
                      }
                    >
                      <SelectTrigger id="presentation-mode">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Continuous">
                          Continuous list
                        </SelectItem>
                        <SelectItem value="SingleStep">
                          One at a time
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <Separator />
                </>
              )}

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
              <CardTitle>Coding Assignment</CardTitle>
            </CardHeader>
            <CardContent>
              <Button
                className="w-full"
                onClick={() =>
                  router.push(
                    `${learningBase}/courses/${encodeURIComponent(courseId)}/assessments/${assessment.id}/coding-definition`,
                  )
                }
              >
                <Code className="mr-2 h-4 w-4" />
                Edit Coding Definition
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {error && <p className="text-destructive text-sm">{error}</p>}
              {saved && (
                <p className="text-sm text-green-600">Saved successfully.</p>
              )}

              <Button
                className="w-full"
                onClick={handleSave}
                disabled={isPending}
              >
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
                Available:{" "}
                <Badge
                  variant={assessment.isAvailable ? "default" : "secondary"}
                  className="text-xs"
                >
                  {assessment.isAvailable ? "Yes" : "No"}
                </Badge>
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
