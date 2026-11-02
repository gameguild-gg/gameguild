'use client';

import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { cn } from '@game-guild/ui/lib/utils';
import React, { type FormEvent, useState, useTransition } from 'react';
import {
    bookMentorshipSessionAction,
    type MentorAvailabilitySlotView,
} from '@/lib/mentorship/actions';

function formatSlot(slot: MentorAvailabilitySlotView): string {
    const start = new Date(slot.startsAt);
    const end = new Date(slot.endsAt);
    const sameDay = start.toDateString() === end.toDateString();
    const formatter = new Intl.DateTimeFormat(undefined, {
        weekday: 'short',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
    });
    return sameDay
        ? `${formatter.format(start)} – ${new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' }).format(end)}`
        : `${formatter.format(start)} – ${formatter.format(end)}`;
}

/**
 * Booking form on the public mentor profile (issue #102): the advisee picks an
 * open availability slot and requests the introductory meeting.
 */
export function MentorBookingForm({
    mentorName,
    slots,
    signedIn,
}: {
    mentorName: string;
    slots: MentorAvailabilitySlotView[];
    signedIn: boolean;
}): React.JSX.Element {
    const [selectedSlotId, setSelectedSlotId] = useState<string | null>(slots[0]?.id ?? null);
    const [topic, setTopic] = useState('');
    const [notes, setNotes] = useState('');
    const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
    const [isPending, startTransition] = useTransition();

    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setFeedback(null);

        if (!selectedSlotId) {
            setFeedback({ type: 'error', message: 'Choose an availability slot first.' });
            return;
        }

        startTransition(async () => {
            const result = await bookMentorshipSessionAction({ slotId: selectedSlotId, topic, notes });

            if (!result.success) {
                setFeedback({ type: 'error', message: result.error });
                return;
            }

            setFeedback({ type: 'success', message: result.message });
            setTopic('');
            setNotes('');
        });
    }

    if (slots.length === 0) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Book a session with {mentorName}</CardTitle>
                    <CardDescription>
                        No open availability right now. Check back soon or browse other mentors.
                    </CardDescription>
                </CardHeader>
            </Card>
        );
    }

    return (
        <Card id="book">
            <CardHeader>
                <CardTitle className="text-lg">Book a session with {mentorName}</CardTitle>
                <CardDescription>
                    {signedIn
                        ? 'Pick an open slot and describe what you want to cover. The mentor confirms by keeping the slot.'
                        : 'Sign in to book one of the open slots below.'}
                </CardDescription>
            </CardHeader>
            <CardContent>
                <form onSubmit={handleSubmit} className="space-y-5" noValidate>
                    <fieldset className="space-y-2">
                        <legend className="text-sm font-medium">Open availability</legend>
                        <div className="grid gap-2 sm:grid-cols-2">
                            {slots.map((slot) => (
                                <label
                                    key={slot.id}
                                    className={cn(
                                        'cursor-pointer rounded-lg border px-3 py-2 text-sm transition',
                                        selectedSlotId === slot.id
                                            ? 'border-primary bg-primary/10 text-primary'
                                            : 'border-input text-muted-foreground hover:bg-accent',
                                    )}
                                >
                                    <input
                                        type="radio"
                                        name="slot"
                                        value={slot.id}
                                        checked={selectedSlotId === slot.id}
                                        onChange={() => setSelectedSlotId(slot.id)}
                                        className="sr-only"
                                    />
                                    {formatSlot(slot)}
                                </label>
                            ))}
                        </div>
                    </fieldset>

                    <div className="space-y-2">
                        <Label htmlFor="booking-topic">Topic</Label>
                        <Textarea
                            id="booking-topic"
                            value={topic}
                            onChange={(event) => setTopic(event.target.value)}
                            placeholder="What do you want to talk about?"
                            rows={2}
                        />
                    </div>
                    <div className="space-y-2">
                        <Label htmlFor="booking-notes">Notes for the mentor (optional)</Label>
                        <Textarea
                            id="booking-notes"
                            value={notes}
                            onChange={(event) => setNotes(event.target.value)}
                            placeholder="Context, links, or specific questions"
                            rows={3}
                        />
                    </div>

                    {feedback ? (
                        <p
                            role="status"
                            className={cn(
                                'rounded-md px-3 py-2 text-sm',
                                feedback.type === 'success'
                                    ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400'
                                    : 'bg-destructive/10 text-destructive',
                            )}
                        >
                            {feedback.message}
                        </p>
                    ) : null}

                    <Button type="submit" disabled={isPending || !signedIn}>
                        {isPending ? 'Booking…' : 'Request session'}
                    </Button>
                </form>
            </CardContent>
        </Card>
    );
}
