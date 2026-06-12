'use client';

import { beginCourseContent, completeCourseContent } from '@/lib/course-progress-actions';
import type { CourseAttendanceData, CourseAttendanceItem } from '@/lib/courses';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CheckCircle2, Clock3, Lock, PlayCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import MarkdownRenderer from './markdown-renderer';

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

export function CourseAttendanceShell({ course }: { course: CourseAttendanceData }) {
    const router = useRouter();
    const [selectedItemId, setSelectedItemId] = useState<string | null>(course.currentItem?.id ?? course.modules[0]?.items[0]?.id ?? null);
    const [isMutating, setIsMutating] = useState(false);
    const [actionError, setActionError] = useState<string | null>(null);

    const selectedItem = course.modules.flatMap((module) => module.items).find((item) => item.id === selectedItemId) ?? null;
    const contentMarkdown = selectedItem ? contentToMarkdown(selectedItem.content) : null;
    const hoursRemaining = course.remainingMinutes > 0 ? (course.remainingMinutes / 60).toFixed(1) : '0.0';

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
                                                onClick={() => setSelectedItemId(item.id)}
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
                                        <Button
                                            type="button"
                                            variant="outline"
                                            onClick={handleCompleteSelectedItem}
                                            disabled={isMutating}
                                            className="border-emerald-500/40 bg-emerald-500/10 text-emerald-100 hover:bg-emerald-500/20"
                                        >
                                            {isMutating ? 'Updating progress...' : 'Mark completed'}
                                        </Button>
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
