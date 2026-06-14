'use client';

import { beginCourseContent, completeCourseContent } from '@/lib/course-progress-actions';
import type { CourseAttendanceData, CourseAttendanceItem } from '@/lib/courses';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CheckCircle2, Clock3, Lock, PlayCircle, Send, Star } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import MarkdownRenderer from './markdown-renderer';

interface PeerReviewCriterion {
    name: string;
    description?: string;
}

function getStatusLabel(status: CourseAttendanceItem['status']) {
    switch (status) {
        case 'completed':
            return 'Completed';
        case 'in-progress':
            return 'In Progress';
        case 'available':
            return 'Available';
        case 'locked':
        default:
            return 'Locked';
    }
}

function getStatusClasses(status: CourseAttendanceItem['status']) {
    switch (status) {
        case 'completed':
            return 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300';
        case 'in-progress':
            return 'border-sky-500/40 bg-sky-500/10 text-sky-300';
        case 'available':
            return 'border-violet-500/40 bg-violet-500/10 text-violet-300';
        case 'locked':
        default:
            return 'border-slate-700 bg-slate-900 text-slate-400';
    }
}

function contentToMarkdown(content: unknown): string | null {
    if (typeof content === 'string' && content.trim()) {
        return content;
    }

    if (content && typeof content === 'object') {
        const record = content as Record<string, unknown>;

        if (typeof record.markdown === 'string' && record.markdown.trim()) {
            return record.markdown;
        }

        if (typeof record.content === 'string' && record.content.trim()) {
            return record.content;
        }

        return `\`\`\`json\n${JSON.stringify(record, null, 2)}\n\`\`\``;
    }

    return null;
}

function parseObjectContent(content: unknown): Record<string, unknown> | null {
    if (content && typeof content === 'object' && !Array.isArray(content)) {
        return content as Record<string, unknown>;
    }

    if (typeof content === 'string' && content.trim()) {
        try {
            const parsed = JSON.parse(content) as unknown;

            return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
                ? parsed as Record<string, unknown>
                : null;
        } catch {
            return null;
        }
    }

    return null;
}

function normalizePeerReviewCriterion(value: unknown, index: number): PeerReviewCriterion {
    if (typeof value === 'string' && value.trim()) {
        return { name: value.trim() };
    }

    if (value && typeof value === 'object') {
        const record = value as Record<string, unknown>;
        const name = typeof record.name === 'string' && record.name.trim()
            ? record.name.trim()
            : typeof record.title === 'string' && record.title.trim()
                ? record.title.trim()
                : `Criterion ${index + 1}`;
        const description = typeof record.description === 'string' && record.description.trim()
            ? record.description.trim()
            : undefined;

        return { name, description };
    }

    return { name: `Criterion ${index + 1}` };
}

function getPeerReviewContent(item: CourseAttendanceItem): { prompt: string; criteria: PeerReviewCriterion[] } {
    const content = item.content;
    const record = parseObjectContent(content);

    if (record) {
        const prompt =
            (typeof record.prompt === 'string' && record.prompt.trim()) ||
            (typeof record.instructions === 'string' && record.instructions.trim()) ||
            (typeof record.content === 'string' && record.content.trim()) ||
            item.description ||
            'Review the peer submission, rate the criteria, and leave actionable feedback.';
        const criteriaSource = Array.isArray(record.criteria) ? record.criteria : [];
        const criteria = criteriaSource.map(normalizePeerReviewCriterion).filter((criterion) => criterion.name.trim().length > 0);

        return {
            prompt,
            criteria: criteria.length > 0
                ? criteria
                : [
                    { name: 'clarity', description: 'Is the work easy to understand?' },
                    { name: 'usefulness', description: 'Will the feedback help the creator improve?' },
                    { name: 'production readiness', description: 'Is the submission ready for portfolio or launch review?' },
                ],
        };
    }

    if (typeof content === 'string' && content.trim()) {
        return {
            prompt: content.trim(),
            criteria: [
                { name: 'clarity', description: 'Is the work easy to understand?' },
                { name: 'usefulness', description: 'Will the feedback help the creator improve?' },
                { name: 'production readiness', description: 'Is the submission ready for portfolio or launch review?' },
            ],
        };
    }

    return {
        prompt: item.description || 'Review the peer submission, rate the criteria, and leave actionable feedback.',
        criteria: [
            { name: 'clarity', description: 'Is the work easy to understand?' },
            { name: 'usefulness', description: 'Will the feedback help the creator improve?' },
            { name: 'production readiness', description: 'Is the submission ready for portfolio or launch review?' },
        ],
    };
}

export function CourseAttendanceShell({ course }: { course: CourseAttendanceData }) {
    const router = useRouter();
    const [selectedItemId, setSelectedItemId] = useState<string | null>(course.currentItem?.id ?? course.modules[0]?.items[0]?.id ?? null);
    const [isMutating, setIsMutating] = useState(false);
    const [actionError, setActionError] = useState<string | null>(null);
    const [peerRatings, setPeerRatings] = useState<Record<string, number>>({});
    const [peerFeedback, setPeerFeedback] = useState('');

    const selectedItem = course.modules.flatMap((module) => module.items).find((item) => item.id === selectedItemId) ?? null;
    const contentMarkdown = selectedItem ? contentToMarkdown(selectedItem.content) : null;
    const peerReviewContent = selectedItem?.type === 'peer-review' ? getPeerReviewContent(selectedItem) : null;
    const allPeerCriteriaRated = peerReviewContent ? peerReviewContent.criteria.every((criterion) => (peerRatings[criterion.name] ?? 0) > 0) : false;
    const canSubmitPeerReview = Boolean(selectedItem && peerReviewContent && allPeerCriteriaRated && peerFeedback.trim().length > 0 && !isMutating);
    const hoursRemaining = course.remainingMinutes > 0 ? (course.remainingMinutes / 60).toFixed(1) : '0.0';

    function handleSelectItem(itemId: string) {
        setSelectedItemId(itemId);
        setActionError(null);
        setPeerRatings({});
        setPeerFeedback('');
    }

    async function handleBeginSelectedItem() {
        if (!selectedItem || selectedItem.status !== 'available') {
            return;
        }

        setIsMutating(true);
        setActionError(null);

        const result = await beginCourseContent(course.id, selectedItem.id);

        setIsMutating(false);

        if (!result.success) {
            setActionError(result.error);
            return;
        }

        router.refresh();
    }

    async function handleCompleteSelectedItem() {
        if (!selectedItem || selectedItem.status === 'locked' || selectedItem.status === 'completed') {
            return;
        }

        setIsMutating(true);
        setActionError(null);

        const result = await completeCourseContent(course.id, selectedItem.id);

        setIsMutating(false);

        if (!result.success) {
            setActionError(result.error);
            return;
        }

        router.refresh();
    }

    async function handleSubmitPeerReview() {
        if (!selectedItem || selectedItem.type !== 'peer-review' || selectedItem.status === 'locked' || selectedItem.status === 'completed') {
            return;
        }

        if (!canSubmitPeerReview) {
            setActionError('Rate every criterion and write feedback before submitting.');
            return;
        }

        setIsMutating(true);
        setActionError(null);

        const result = await completeCourseContent(course.id, selectedItem.id);

        setIsMutating(false);

        if (!result.success) {
            setActionError(result.error);
            return;
        }

        router.refresh();
    }

    return (
        <div className="mx-auto flex min-h-screen max-w-7xl flex-col gap-6 px-4 py-8 lg:px-6">
            <div className="flex flex-col gap-4 rounded-3xl border border-slate-800 bg-slate-900/80 p-6 shadow-2xl shadow-slate-950/40">
                <div className="flex flex-wrap items-center justify-between gap-4">
                    <div>
                        <p className="text-sm uppercase tracking-[0.2em] text-sky-300">Learning</p>
                        <h1 className="mt-2 text-3xl font-semibold text-white">{course.title}</h1>
                        <p className="mt-2 max-w-3xl text-sm text-slate-300">{course.description || 'This learner surface is now separated from the public website and focused on course attendance.'}</p>
                    </div>
                    <Button asChild variant="outline" className="border-slate-700 bg-slate-950 text-slate-100 hover:bg-slate-900">
                        <Link href={`/courses/${course.slug}`}>Back to overview</Link>
                    </Button>
                </div>

                <div className="flex flex-wrap gap-3">
                    <Badge className="border border-sky-500/40 bg-sky-500/10 text-sky-200">{course.overallProgress}% complete</Badge>
                    <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{course.completedItems}/{course.totalItems} items done</Badge>
                    <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{hoursRemaining}h remaining</Badge>
                    <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{course.modules.length} modules</Badge>
                </div>
            </div>

            <div className="grid gap-6 lg:grid-cols-[320px_minmax(0,1fr)]">
                <Card className="border-slate-800 bg-slate-900/80 text-slate-100 shadow-xl shadow-slate-950/30">
                    <CardHeader>
                        <CardTitle className="text-base">Course Navigation</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        {course.modules.map((module) => (
                            <div key={module.id} className="space-y-3 rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                <div>
                                    <p className="text-sm font-medium text-white">{module.title}</p>
                                    <p className="mt-1 text-xs text-slate-400">{module.progress}% complete</p>
                                </div>

                                <div className="space-y-2">
                                    {module.items.map((item) => {
                                        const isActive = item.id === selectedItemId;
                                        const disabled = item.status === 'locked';

                                        return (
                                            <button
                                                key={item.id}
                                                type="button"
                                                disabled={disabled}
                                                onClick={() => handleSelectItem(item.id)}
                                                className={`flex w-full items-start gap-3 rounded-2xl border px-3 py-3 text-left transition ${isActive ? 'border-sky-500/50 bg-sky-500/10' : 'border-slate-800 bg-slate-900 hover:border-slate-700'} ${disabled ? 'cursor-not-allowed opacity-60' : ''}`}
                                            >
                                                <span className="mt-0.5 text-slate-300">
                                                    {item.status === 'completed' ? <CheckCircle2 className="size-4 text-emerald-300" /> : item.status === 'locked' ? <Lock className="size-4" /> : <PlayCircle className="size-4 text-sky-300" />}
                                                </span>
                                                <span className="min-w-0 flex-1">
                                                    <span className="block text-sm font-medium text-white">{item.title}</span>
                                                    <span className="mt-1 block text-xs text-slate-400">{getStatusLabel(item.status)}{item.duration ? ` • ${item.duration} min` : ''}</span>
                                                </span>
                                            </button>
                                        );
                                    })}
                                </div>
                            </div>
                        ))}
                    </CardContent>
                </Card>

                <Card className="border-slate-800 bg-slate-900/80 text-slate-100 shadow-xl shadow-slate-950/30">
                    <CardHeader className="space-y-4">
                        {selectedItem ? (
                            <>
                                <div className="flex flex-wrap items-center gap-3">
                                    <Badge className={getStatusClasses(selectedItem.status)}>{getStatusLabel(selectedItem.status)}</Badge>
                                    <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{selectedItem.type}</Badge>
                                    {selectedItem.duration ? (
                                        <Badge className="border border-slate-700 bg-slate-950 text-slate-200">
                                            <Clock3 className="mr-1 size-3.5" />
                                            {selectedItem.duration} min
                                        </Badge>
                                    ) : null}
                                </div>
                                <div>
                                    <CardTitle className="text-2xl">{selectedItem.title}</CardTitle>
                                    {selectedItem.description ? <p className="mt-2 text-sm text-slate-300">{selectedItem.description}</p> : null}
                                </div>
                                <div className="flex flex-wrap items-center gap-3">
                                    {selectedItem.status === 'available' ? (
                                        <Button
                                            type="button"
                                            onClick={handleBeginSelectedItem}
                                            disabled={isMutating}
                                            className="bg-sky-600 text-white hover:bg-sky-500"
                                        >
                                            {isMutating ? 'Updating progress...' : 'Start this item'}
                                        </Button>
                                    ) : null}
                                    {selectedItem.status !== 'locked' && selectedItem.status !== 'completed' ? (
                                        selectedItem.type === 'peer-review' ? null : (
                                            <Button
                                                type="button"
                                                variant="outline"
                                                onClick={handleCompleteSelectedItem}
                                                disabled={isMutating}
                                                className="border-emerald-500/40 bg-emerald-500/10 text-emerald-100 hover:bg-emerald-500/20"
                                            >
                                                {isMutating ? 'Updating progress...' : 'Mark completed'}
                                            </Button>
                                        )
                                    ) : null}
                                </div>
                                {actionError ? <p className="text-sm text-rose-300">{actionError}</p> : null}
                            </>
                        ) : (
                            <CardTitle className="text-2xl">No lesson selected</CardTitle>
                        )}
                    </CardHeader>
                    <CardContent>
                        {selectedItem ? (
                            selectedItem.status === 'locked' ? (
                                <div className="rounded-2xl border border-dashed border-slate-700 bg-slate-950/60 p-6 text-sm text-slate-300">
                                    Complete the unlocked item before this lesson becomes available.
                                </div>
                            ) : selectedItem.type === 'peer-review' && peerReviewContent ? (
                                <div className="space-y-6">
                                    <div className="rounded-2xl border border-sky-500/30 bg-sky-500/10 p-5">
                                        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200">Peer review brief</p>
                                        <h2 className="mt-3 text-xl font-semibold text-white">{selectedItem.title}</h2>
                                        <p className="mt-3 text-sm leading-6 text-slate-200">{peerReviewContent.prompt}</p>
                                    </div>

                                    <div className="space-y-4">
                                        {peerReviewContent.criteria.map((criterion) => {
                                            const rating = peerRatings[criterion.name] ?? 0;

                                            return (
                                                <div key={criterion.name} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                                    <div className="flex flex-wrap items-start justify-between gap-3">
                                                        <div>
                                                            <p className="text-sm font-medium capitalize text-white">{criterion.name}</p>
                                                            {criterion.description ? <p className="mt-1 text-xs text-slate-400">{criterion.description}</p> : null}
                                                        </div>
                                                        <span className="text-xs font-medium text-slate-400">{rating > 0 ? `${rating}/5` : 'Not rated'}</span>
                                                    </div>
                                                    <div className="mt-4 flex flex-wrap gap-2" aria-label={`Rate ${criterion.name}`}>
                                                        {[1, 2, 3, 4, 5].map((value) => (
                                                            <button
                                                                key={value}
                                                                type="button"
                                                                aria-label={`Rate ${criterion.name} ${value}`}
                                                                aria-pressed={rating === value}
                                                                onClick={() => {
                                                                    setPeerRatings((current) => ({ ...current, [criterion.name]: value }));
                                                                    setActionError(null);
                                                                }}
                                                                className={`inline-flex size-9 items-center justify-center rounded-full border transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-300 ${
                                                                    value <= rating
                                                                        ? 'border-amber-300/70 bg-amber-300/15 text-amber-200'
                                                                        : 'border-slate-700 bg-slate-900 text-slate-400 hover:border-slate-500 hover:text-slate-200'
                                                                }`}
                                                            >
                                                                <Star className="size-4" fill={value <= rating ? 'currentColor' : 'none'} />
                                                            </button>
                                                        ))}
                                                    </div>
                                                </div>
                                            );
                                        })}
                                    </div>

                                    <div className="space-y-2">
                                        <label htmlFor="peer-review-feedback" className="text-sm font-medium text-white">
                                            Written feedback
                                        </label>
                                        <Textarea
                                            id="peer-review-feedback"
                                            value={peerFeedback}
                                            onChange={(event) => {
                                                setPeerFeedback(event.target.value);
                                                setActionError(null);
                                            }}
                                            rows={6}
                                            className="border-slate-700 bg-slate-950 text-slate-100 placeholder:text-slate-500"
                                            placeholder="Explain what is working, what should improve, and one concrete next step for the creator."
                                        />
                                    </div>

                                    <Button
                                        type="button"
                                        onClick={handleSubmitPeerReview}
                                        disabled={!canSubmitPeerReview}
                                        className="bg-sky-600 text-white hover:bg-sky-500 disabled:opacity-50"
                                    >
                                        <Send className="size-4" />
                                        {isMutating ? 'Submitting peer review...' : 'Submit peer review'}
                                    </Button>
                                </div>
                            ) : contentMarkdown ? (
                                <MarkdownRenderer content={contentMarkdown} />
                            ) : (
                                <div className="rounded-2xl border border-dashed border-slate-700 bg-slate-950/60 p-6 text-sm text-slate-300">
                                    This content item has no authored body yet. Use the course outline, assessment status, and progress actions while the instructor finishes the lesson material.
                                </div>
                            )
                        ) : (
                            <div className="rounded-2xl border border-dashed border-slate-700 bg-slate-950/60 p-6 text-sm text-slate-300">
                                Select an item from the left to start attending the course.
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
