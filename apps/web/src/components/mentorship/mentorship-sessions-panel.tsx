'use client';

import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@game-guild/ui/lib/utils';
import { CheckCircle2, XCircle } from 'lucide-react';
import { useRouter } from 'next/navigation';
import React, { useState, useTransition } from 'react';
import {
    cancelMentorshipSessionAction,
    completeMentorshipSessionAction,
    type MentorshipSessionView,
} from '@/lib/mentorship/actions';

const statusLabels: Record<number, string> = {
    0: 'Booked',
    1: 'Cancelled',
    2: 'Completed',
};

function formatSessionDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
        weekday: 'short',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
    }).format(new Date(value));
}

/**
 * The signed-in member's mentorship sessions (issue #102): upcoming and past
 * sessions as mentor or advisee, with cancel/complete actions.
 */
export function MentorshipSessionsPanel({
    sessions,
    currentUserId,
}: {
    sessions: MentorshipSessionView[];
    currentUserId: string;
}): React.JSX.Element {
    const router = useRouter();
    const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
    const [isPending, startTransition] = useTransition();

    function cancel(sessionId: string) {
        setFeedback(null);
        startTransition(async () => {
            const result = await cancelMentorshipSessionAction(sessionId);
            if (result.success) {
                setFeedback({ type: 'success', message: result.message });
                router.refresh();
            } else {
                setFeedback({ type: 'error', message: result.error });
            }
        });
    }

    function complete(sessionId: string) {
        setFeedback(null);
        startTransition(async () => {
            const result = await completeMentorshipSessionAction(sessionId);
            if (result.success) {
                setFeedback({ type: 'success', message: result.message });
                router.refresh();
            } else {
                setFeedback({ type: 'error', message: result.error });
            }
        });
    }

    const sorted = [...sessions].sort((left, right) => left.startsAt.localeCompare(right.startsAt));

    return (
        <Card>
            <CardHeader>
                <CardTitle className="text-xl">My mentorship sessions</CardTitle>
                <CardDescription>
                    Sessions where you are the mentor or the advisee. Cancelled slots return to the mentor's calendar.
                </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
                {sorted.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                        No sessions yet. Book an introductory call from the{' '}
                        <a href="/mentorship" className="font-medium text-primary hover:underline">
                            mentorship directory
                        </a>
                        .
                    </p>
                ) : (
                    <ul className="space-y-3">
                        {sorted.map((session) => {
                            const isMentor = session.mentorUserId === currentUserId;
                            return (
                                <li
                                    key={session.id}
                                    className="flex flex-col gap-3 rounded-lg border p-4 sm:flex-row sm:items-center sm:justify-between"
                                >
                                    <div className="space-y-1">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <span className="text-sm font-semibold text-foreground">{session.topic}</span>
                                            <span
                                                className={cn(
                                                    'rounded-full px-2 py-0.5 text-xs font-semibold',
                                                    session.status === 0
                                                        ? 'bg-sky-500/10 text-sky-600 dark:text-sky-300'
                                                        : session.status === 1
                                                          ? 'bg-destructive/10 text-destructive'
                                                          : 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-300',
                                                )}
                                            >
                                                {statusLabels[session.status] ?? 'Booked'}
                                            </span>
                                            {session.isIntroductory ? (
                                                <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-semibold text-muted-foreground">
                                                    Intro call
                                                </span>
                                            ) : null}
                                            <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                                                {isMentor ? 'as mentor' : 'as advisee'}
                                            </span>
                                        </div>
                                        <p className="text-sm text-muted-foreground">
                                            {formatSessionDate(session.startsAt)} –{' '}
                                            {new Intl.DateTimeFormat(undefined, {
                                                hour: 'numeric',
                                                minute: '2-digit',
                                            }).format(new Date(session.endsAt))}
                                        </p>
                                        {session.notes ? (
                                            <p className="text-xs text-muted-foreground">{session.notes}</p>
                                        ) : null}
                                    </div>
                                    {session.status === 0 ? (
                                        <div className="flex shrink-0 gap-2">
                                            {isMentor ? (
                                                <Button
                                                    type="button"
                                                    variant="outline"
                                                    size="sm"
                                                    disabled={isPending}
                                                    onClick={() => complete(session.id)}
                                                >
                                                    <CheckCircle2 className="mr-1.5 size-4" aria-hidden="true" />
                                                    Complete
                                                </Button>
                                            ) : null}
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                disabled={isPending}
                                                onClick={() => cancel(session.id)}
                                            >
                                                <XCircle className="mr-1.5 size-4" aria-hidden="true" />
                                                Cancel
                                            </Button>
                                        </div>
                                    ) : null}
                                </li>
                            );
                        })}
                    </ul>
                )}

                {feedback ? (
                    <p
                        role="status"
                        className={
                            feedback.type === 'success'
                                ? 'rounded-md bg-emerald-500/10 px-3 py-2 text-sm text-emerald-600 dark:text-emerald-400'
                                : 'rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive'
                        }
                    >
                        {feedback.message}
                    </p>
                ) : null}
            </CardContent>
        </Card>
    );
}
