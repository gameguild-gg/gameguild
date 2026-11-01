'use client';

import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { cn } from '@game-guild/ui/lib/utils';
import { usePathname } from 'next/navigation';
import React, { type FormEvent, useState, useTransition } from 'react';
import {
    submitContentCreatorApplicationAction,
    validateContentCreatorApplicationStep,
    type ContentCreatorApplicationInput,
} from '@/lib/content-creators/actions';

const focusAreas = [
    'Game design',
    'Game programming',
    'Game art and animation',
    'Audio and music',
    'Production and business',
    'Community and marketing',
] as const;

const contentFormats = ['Written courses', 'Video lessons', 'Live workshops', 'Assignments and critiques'] as const;

const initialInput: ContentCreatorApplicationInput = {
    name: '',
    email: '',
    focusArea: '',
    portfolioUrl: '',
    motivation: '',
    creatorHandle: '',
    bio: '',
    links: '',
    preferredFormats: [],
    courseTitle: '',
    courseAudience: '',
    courseOutline: '',
};

const steps = [
    {
        index: 1,
        title: 'Application',
        description: 'Introduce yourself and tell us why you want to create content with GameGuild.',
    },
    {
        index: 2,
        title: 'Creator profile',
        description: 'Set up the public profile your audience will see once you are approved.',
    },
    {
        index: 3,
        title: 'First course setup',
        description: 'Sketch the first course you want to publish so reviewers can plan your onboarding.',
    },
] as const;

/**
 * Multi-step "be a content creator" application wizard (issue #105): application,
 * creator profile, and first course setup, submitted as one course proposal.
 */
export function ContentCreatorApplicationWizard(): React.JSX.Element {
    const pathname = usePathname();
    const [step, setStep] = useState<1 | 2 | 3>(1);
    const [input, setInput] = useState<ContentCreatorApplicationInput>(initialInput);
    const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
    const [isPending, startTransition] = useTransition();

    function updateField<K extends keyof ContentCreatorApplicationInput>(
        field: K,
        value: ContentCreatorApplicationInput[K],
    ) {
        setInput((current) => ({ ...current, [field]: value }));
    }

    function toggleFormat(format: string) {
        setInput((current) => ({
            ...current,
            preferredFormats: current.preferredFormats.includes(format)
                ? current.preferredFormats.filter((candidate) => candidate !== format)
                : [...current.preferredFormats, format],
        }));
    }

    function continueToNextStep() {
        const error = validateContentCreatorApplicationStep(step, input);
        if (error) {
            setFeedback({ type: 'error', message: error });
            return;
        }
        setFeedback(null);
        setStep((current) => (current < 3 ? ((current + 1) as 1 | 2 | 3) : current));
    }

    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setFeedback(null);

        startTransition(async () => {
            const result = await submitContentCreatorApplicationAction({ ...input, pagePath: pathname });

            if (!result.success) {
                setFeedback({ type: 'error', message: result.error });
                return;
            }

            setFeedback({ type: 'success', message: result.message });
            setInput(initialInput);
            setStep(1);
        });
    }

    const activeStep = steps[step - 1];

    return (
        <Card className="mx-auto w-full max-w-3xl" data-testid="content-creator-application">
            <CardHeader>
                <div className="mb-4 flex items-center gap-2" aria-label={`Application step ${step} of 3`}>
                    {steps.map((candidate) => (
                        <div
                            key={candidate.index}
                            className={cn(
                                'h-1.5 flex-1 rounded-full',
                                candidate.index <= step ? 'bg-primary' : 'bg-muted',
                            )}
                        />
                    ))}
                </div>
                <CardTitle className="text-2xl">
                    Step {activeStep.index} of 3 — {activeStep.title}
                </CardTitle>
                <CardDescription>{activeStep.description}</CardDescription>
            </CardHeader>

            <CardContent>
                <form onSubmit={handleSubmit} className="space-y-5" noValidate>
                    {step === 1 ? (
                        <div className="space-y-4">
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div className="space-y-2">
                                    <Label htmlFor="creator-name">Full name</Label>
                                    <Input
                                        id="creator-name"
                                        value={input.name}
                                        onChange={(event) => updateField('name', event.target.value)}
                                        placeholder="Ada Lovelace"
                                        autoComplete="name"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="creator-email">Email</Label>
                                    <Input
                                        id="creator-email"
                                        type="email"
                                        value={input.email}
                                        onChange={(event) => updateField('email', event.target.value)}
                                        placeholder="you@example.com"
                                        autoComplete="email"
                                    />
                                </div>
                            </div>
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div className="space-y-2">
                                    <Label htmlFor="creator-focus">Content area</Label>
                                    <select
                                        id="creator-focus"
                                        value={input.focusArea}
                                        onChange={(event) => updateField('focusArea', event.target.value)}
                                        className="h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                                    >
                                        <option value="">Select an area…</option>
                                        {focusAreas.map((area) => (
                                            <option key={area} value={area}>
                                                {area}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="creator-portfolio">Portfolio or channel URL (optional)</Label>
                                    <Input
                                        id="creator-portfolio"
                                        value={input.portfolioUrl ?? ''}
                                        onChange={(event) => updateField('portfolioUrl', event.target.value)}
                                        placeholder="https://"
                                    />
                                </div>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="creator-motivation">Why do you want to create with us?</Label>
                                <Textarea
                                    id="creator-motivation"
                                    value={input.motivation}
                                    onChange={(event) => updateField('motivation', event.target.value)}
                                    placeholder="Share your motivation, experience, and the kind of content you love making."
                                    rows={5}
                                />
                            </div>
                        </div>
                    ) : null}

                    {step === 2 ? (
                        <div className="space-y-4">
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div className="space-y-2">
                                    <Label htmlFor="creator-handle">Creator handle</Label>
                                    <Input
                                        id="creator-handle"
                                        value={input.creatorHandle}
                                        onChange={(event) => updateField('creatorHandle', event.target.value)}
                                        placeholder="shaderwizard"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="creator-links">Site or social links (optional)</Label>
                                    <Input
                                        id="creator-links"
                                        value={input.links ?? ''}
                                        onChange={(event) => updateField('links', event.target.value)}
                                        placeholder="https://"
                                    />
                                </div>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="creator-bio">Creator bio</Label>
                                <Textarea
                                    id="creator-bio"
                                    value={input.bio}
                                    onChange={(event) => updateField('bio', event.target.value)}
                                    placeholder="What you teach, what you have shipped, and how you like to work with learners."
                                    rows={5}
                                />
                            </div>
                            <fieldset className="space-y-2">
                                <legend className="text-sm font-medium">Preferred formats</legend>
                                <div className="flex flex-wrap gap-2">
                                    {contentFormats.map((format) => (
                                        <label
                                            key={format}
                                            className={cn(
                                                'cursor-pointer rounded-full border px-3 py-1.5 text-sm transition',
                                                input.preferredFormats.includes(format)
                                                    ? 'border-primary bg-primary/10 text-primary'
                                                    : 'border-input text-muted-foreground hover:bg-accent',
                                            )}
                                        >
                                            <input
                                                type="checkbox"
                                                className="sr-only"
                                                checked={input.preferredFormats.includes(format)}
                                                onChange={() => toggleFormat(format)}
                                            />
                                            {format}
                                        </label>
                                    ))}
                                </div>
                            </fieldset>
                        </div>
                    ) : null}

                    {step === 3 ? (
                        <div className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="creator-course-title">First course working title</Label>
                                <Input
                                    id="creator-course-title"
                                    value={input.courseTitle}
                                    onChange={(event) => updateField('courseTitle', event.target.value)}
                                    placeholder="Shader Basics for Game Developers"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="creator-course-audience">Who is it for?</Label>
                                <Input
                                    id="creator-course-audience"
                                    value={input.courseAudience}
                                    onChange={(event) => updateField('courseAudience', event.target.value)}
                                    placeholder="Intermediate developers comfortable with Unity but new to shaders"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="creator-course-outline">Course outline sketch</Label>
                                <Textarea
                                    id="creator-course-outline"
                                    value={input.courseOutline}
                                    onChange={(event) => updateField('courseOutline', event.target.value)}
                                    placeholder="Week 1: rendering pipeline basics; Week 2: first shader; Week 3: lighting models; final project: stylized water."
                                    rows={6}
                                />
                            </div>
                        </div>
                    ) : null}

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

                    <div className="flex items-center justify-between">
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => setStep((current) => (current > 1 ? ((current - 1) as 1 | 2 | 3) : current))}
                            disabled={step === 1 || isPending}
                        >
                            Back
                        </Button>
                        {step < 3 ? (
                            <Button type="button" onClick={continueToNextStep} disabled={isPending}>
                                Continue
                            </Button>
                        ) : (
                            <Button type="submit" disabled={isPending}>
                                {isPending ? 'Submitting…' : 'Submit application'}
                            </Button>
                        )}
                    </div>
                </form>
            </CardContent>
        </Card>
    );
}
