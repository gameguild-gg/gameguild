'use client';

import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { useRouter } from 'next/navigation';
import React, { type FormEvent, useState, useTransition } from 'react';
import {
    publishMentorAvailabilityAction,
    setMentorProfileStatusAction,
    upsertMentorProfileAction,
    type MentorProfileView,
} from '@/lib/mentorship/actions';

const roleOptions = [
    { value: 0, label: 'Mentor — hands-on pairing on craft and code' },
    { value: 1, label: 'Advisor — strategic and career guidance' },
    { value: 2, label: 'Matchmaker — onboarding and pairing advisees' },
] as const;

const bookingModeOptions = [
    { value: 0, label: 'Open scheduling — advisees book my published slots directly' },
    { value: 1, label: 'Matchmaker-managed — only a matchmaker can book my slots' },
] as const;

/**
 * Self-service mentor profile and availability management (issue #102).
 */
export function MentorProfileManager({ profile }: { profile: MentorProfileView | null }): React.JSX.Element {
    const router = useRouter();
    const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
    const [isPending, startTransition] = useTransition();

    const [displayName, setDisplayName] = useState(profile?.displayName ?? '');
    const [headline, setHeadline] = useState(profile?.headline ?? '');
    const [bio, setBio] = useState(profile?.bio ?? '');
    const [role, setRole] = useState(profile?.role ?? 0);
    const [pricingModel, setPricingModel] = useState(profile?.pricingModel ?? 0);
    const [freeContributionReason, setFreeContributionReason] = useState(profile?.freeContributionReason ?? '');
    const [bookingMode, setBookingMode] = useState(profile?.bookingMode ?? 0);
    const [offersIntroductoryCall, setOffersIntroductoryCall] = useState(profile?.offersIntroductoryCall ?? true);
    const [introductoryCallMinutes, setIntroductoryCallMinutes] = useState(profile?.introductoryCallMinutes ?? 15);
    const [timezoneId, setTimezoneId] = useState(profile?.timezoneId ?? 'UTC');
    const [skills, setSkills] = useState((profile?.skills ?? []).map((skill) => skill.skill).join(', '));

    const [slotDate, setSlotDate] = useState('');
    const [slotStart, setSlotStart] = useState('');
    const [slotEnd, setSlotEnd] = useState('');

    function run(result: { success: boolean; message?: string; error?: string }, onDone?: () => void) {
        if (result.success) {
            setFeedback({ type: 'success', message: result.message ?? 'Saved.' });
            onDone?.();
            router.refresh();
        } else {
            setFeedback({ type: 'error', message: result.error ?? 'Something went wrong.' });
        }
    }

    function handleSave(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setFeedback(null);

        startTransition(async () => {
            const result = await upsertMentorProfileAction({
                displayName,
                headline,
                bio,
                role,
                pricingModel,
                freeContributionReason,
                bookingMode,
                offersIntroductoryCall,
                introductoryCallMinutes,
                timezoneId,
                skills: skills.split(',').map((skill) => skill.trim()).filter(Boolean),
            });
            run(result);
        });
    }

    function handleStatus(status: number) {
        setFeedback(null);
        startTransition(async () => {
            const result = await setMentorProfileStatusAction(status);
            run(result);
        });
    }

    function handlePublishSlot(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setFeedback(null);

        if (!slotDate || !slotStart || !slotEnd) {
            setFeedback({ type: 'error', message: 'Pick a date with a start and end time.' });
            return;
        }

        const startsAt = new Date(`${slotDate}T${slotStart}`);
        const endsAt = new Date(`${slotDate}T${slotEnd}`);
        if (Number.isNaN(startsAt.getTime()) || Number.isNaN(endsAt.getTime()) || endsAt <= startsAt) {
            setFeedback({ type: 'error', message: 'The slot end must be after its start.' });
            return;
        }

        startTransition(async () => {
            const result = await publishMentorAvailabilityAction([
                { startsAt: startsAt.toISOString(), endsAt: endsAt.toISOString() },
            ]);
            run(result, () => {
                setSlotDate('');
                setSlotStart('');
                setSlotEnd('');
            });
        });
    }

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle className="text-xl">Mentor profile</CardTitle>
                    <CardDescription>
                        {profile
                            ? `Status: ${['Draft', 'Published', 'Paused'][profile.status] ?? 'Draft'}. ${
                                  profile.status === 1
                                      ? 'Your profile is listed in the public directory.'
                                      : 'Publish to appear in the public directory.'
                              }`
                            : 'Create your profile to start mentoring or advising (issue #102).'}
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSave} className="space-y-4" noValidate>
                        <div className="grid gap-4 sm:grid-cols-2">
                            <div className="space-y-2">
                                <Label htmlFor="mentor-display-name">Display name</Label>
                                <Input
                                    id="mentor-display-name"
                                    value={displayName}
                                    onChange={(event) => setDisplayName(event.target.value)}
                                    placeholder="Your public name"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="mentor-headline">Headline</Label>
                                <Input
                                    id="mentor-headline"
                                    value={headline}
                                    onChange={(event) => setHeadline(event.target.value)}
                                    placeholder="e.g. Gameplay programmer, 10 shipped titles"
                                />
                            </div>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="mentor-bio">Bio</Label>
                            <Textarea
                                id="mentor-bio"
                                value={bio}
                                onChange={(event) => setBio(event.target.value)}
                                placeholder="Your experience and how you like to mentor"
                                rows={4}
                            />
                        </div>

                        <div className="grid gap-4 sm:grid-cols-2">
                            <div className="space-y-2">
                                <Label htmlFor="mentor-role">Program role</Label>
                                <select
                                    id="mentor-role"
                                    value={role}
                                    onChange={(event) => setRole(Number(event.target.value))}
                                    className="h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                                >
                                    {roleOptions.map((option) => (
                                        <option key={option.value} value={option.value}>
                                            {option.label}
                                        </option>
                                    ))}
                                </select>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="mentor-timezone">Timezone (IANA id)</Label>
                                <Input
                                    id="mentor-timezone"
                                    value={timezoneId}
                                    onChange={(event) => setTimezoneId(event.target.value)}
                                    placeholder="America/Sao_Paulo"
                                />
                            </div>
                        </div>

                        <div className="grid gap-4 sm:grid-cols-2">
                            <div className="space-y-2">
                                <Label htmlFor="mentor-pricing">Pricing</Label>
                                <select
                                    id="mentor-pricing"
                                    value={pricingModel}
                                    onChange={(event) => setPricingModel(Number(event.target.value))}
                                    className="h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                                >
                                    <option value={0}>Free</option>
                                    <option value={1}>Paid</option>
                                </select>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="mentor-booking-mode">Scheduling</Label>
                                <select
                                    id="mentor-booking-mode"
                                    value={bookingMode}
                                    onChange={(event) => setBookingMode(Number(event.target.value))}
                                    className="h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                                >
                                    {bookingModeOptions.map((option) => (
                                        <option key={option.value} value={option.value}>
                                            {option.label}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        {pricingModel === 0 ? (
                            <div className="space-y-2">
                                <Label htmlFor="mentor-free-reason">
                                    Why do you mentor for free? (required for free programs)
                                </Label>
                                <Input
                                    id="mentor-free-reason"
                                    value={freeContributionReason}
                                    onChange={(event) => setFreeContributionReason(event.target.value)}
                                    placeholder="e.g. Giving back to the community I learned from"
                                />
                            </div>
                        ) : null}

                        <div className="space-y-2">
                            <Label htmlFor="mentor-skills">Skills (comma-separated)</Label>
                            <Input
                                id="mentor-skills"
                                value={skills}
                                onChange={(event) => setSkills(event.target.value)}
                                placeholder="shaders, level design, project scoping"
                            />
                        </div>

                        <div className="flex flex-wrap items-center gap-4">
                            <label className="inline-flex items-center gap-2 text-sm">
                                <input
                                    type="checkbox"
                                    checked={offersIntroductoryCall}
                                    onChange={(event) => setOffersIntroductoryCall(event.target.checked)}
                                    className="size-4 rounded border-input"
                                />
                                Offer a free introductory call
                            </label>
                            <label className="inline-flex items-center gap-2 text-sm">
                                Intro length (10-15 min)
                                <Input
                                    type="number"
                                    min={10}
                                    max={15}
                                    value={introductoryCallMinutes}
                                    onChange={(event) => setIntroductoryCallMinutes(Number(event.target.value))}
                                    className="h-8 w-20"
                                />
                            </label>
                        </div>

                        <div className="flex flex-wrap gap-2">
                            <Button type="submit" disabled={isPending}>
                                {isPending ? 'Saving…' : 'Save profile'}
                            </Button>
                            <Button type="button" variant="outline" disabled={isPending} onClick={() => handleStatus(1)}>
                                Publish
                            </Button>
                            <Button type="button" variant="outline" disabled={isPending} onClick={() => handleStatus(2)}>
                                Pause
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle className="text-xl">Publish availability</CardTitle>
                    <CardDescription>
                        Slots must start at least an hour from now, last 10 minutes to 8 hours, and not overlap your
                        open slots.
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handlePublishSlot} className="grid gap-4 sm:grid-cols-4" noValidate>
                        <div className="space-y-2">
                            <Label htmlFor="slot-date">Date</Label>
                            <Input
                                id="slot-date"
                                type="date"
                                value={slotDate}
                                onChange={(event) => setSlotDate(event.target.value)}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="slot-start">Start</Label>
                            <Input
                                id="slot-start"
                                type="time"
                                value={slotStart}
                                onChange={(event) => setSlotStart(event.target.value)}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="slot-end">End</Label>
                            <Input
                                id="slot-end"
                                type="time"
                                value={slotEnd}
                                onChange={(event) => setSlotEnd(event.target.value)}
                            />
                        </div>
                        <div className="flex items-end">
                            <Button type="submit" disabled={isPending}>
                                {isPending ? 'Publishing…' : 'Add slot'}
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>

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
        </div>
    );
}
