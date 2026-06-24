'use client';

import { Link, useRouter } from '@/i18n/navigation';
import { createCourse, updateCourse } from '@/lib/learning/actions';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import {
  CONTENT_VISIBILITIES,
  ENROLLMENT_STATUSES,
  formatEnumLabel,
  PROGRAM_CATEGORIES,
  PROGRAM_DIFFICULTIES,
} from '@/lib/learning/enums';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Textarea } from '@game-guild/ui/components/textarea';
import { ArrowLeft, ArrowRight, Check, Loader2 } from 'lucide-react';
import React, { useState, useTransition } from 'react';

function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .trim();
}

const STEPS = ['Basics', 'Details', 'Settings'] as const;

function parseEnrollmentCap(value: string): number | null {
  if (!value.trim()) return null;

  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

export default function CreateCoursePage({ params }: PageProps<'/[locale]/dashboard/learning/courses/new'>) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [step, setStep] = useState(0);
  const [error, setError] = useState<string | null>(null);

  // params is unused now (locale handled by next-intl router)
  void params;

  // Step 1: Basics
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [slug, setSlug] = useState('');
  const [autoSlug, setAutoSlug] = useState(true);

  // Step 2: Details
  const [category, setCategory] = useState('General');
  const [difficulty, setDifficulty] = useState('Beginner');
  const [estimatedHours, setEstimatedHours] = useState('');
  const [thumbnail, setThumbnail] = useState('');
  const [videoShowcaseUrl, setVideoShowcaseUrl] = useState('');

  // Step 3: Settings
  const [visibility, setVisibility] = useState('Public');
  const [enrollmentStatus, setEnrollmentStatus] = useState('Open');
  const [maxEnrollments, setMaxEnrollments] = useState('');
  const [skillsRequired, setSkillsRequired] = useState('');
  const [skillsProvided, setSkillsProvided] = useState('');

  React.useEffect(() => {
    // no-op
  }, []);

  function handleTitleChange(value: string) {
    setTitle(value);
    if (autoSlug) {
      setSlug(slugify(value));
    }
  }

  function handleSlugChange(value: string) {
    setAutoSlug(false);
    setSlug(slugify(value));
  }

  function canAdvance(): boolean {
    if (step === 0) {
      return title.trim().length >= 3 && description.trim().length >= 10 && slug.trim().length >= 1;
    }
    return true;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (step < STEPS.length - 1) {
      setStep(step + 1);
      return;
    }

    startTransition(async () => {
      // Step 1: Create the course with basics
      const createResult = await createCourse({ title, description, slug });
      if (!createResult.success) {
        setError(createResult.error);
        return;
      }

      const courseId = createResult.data.id;
      const courseRouteParam = createResult.data.routeParam || getCourseRouteParam({ id: courseId, slug: createResult.data.slug });

      // Step 2: Update with extended fields
      const updateResult = await updateCourse({
        courseId,
        category,
        difficulty,
        visibility,
        enrollmentStatus,
        estimatedHours: estimatedHours ? parseInt(estimatedHours, 10) : undefined,
        thumbnail: thumbnail || undefined,
        videoShowcaseUrl: videoShowcaseUrl || undefined,
        maxEnrollments: parseEnrollmentCap(maxEnrollments),
        skillsRequired: skillsRequired.trim() || undefined,
        skillsProvided: skillsProvided.trim() || undefined,
      });
      if (!updateResult.success) {
        // Course was created but update failed — still redirect, user can fix in settings
        console.warn('[CreateCourse] Update failed after creation:', updateResult.error);
      }

      router.push(`/dashboard/learning/courses/${courseRouteParam}`);
    });
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6 p-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link href="/dashboard/learning/courses">
            <ArrowLeft className="size-5" />
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Create Course</h1>
          <p className="text-muted-foreground">
            Step {step + 1} of {STEPS.length}: {STEPS[step]}
          </p>
        </div>
      </div>

      {/* Stepper */}
      <div className="flex items-center gap-2">
        {STEPS.map((label, i) => (
          <React.Fragment key={label}>
            <button
              type="button"
              onClick={() => i < step && setStep(i)}
              disabled={i > step}
              className={`flex size-8 items-center justify-center rounded-full text-sm font-medium transition-colors ${i < step
                ? 'bg-primary text-primary-foreground cursor-pointer'
                : i === step
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground'
                }`}
            >
              {i < step ? <Check className="size-4" /> : i + 1}
            </button>
            {i < STEPS.length - 1 && <div className={`h-0.5 flex-1 ${i < step ? 'bg-primary' : 'bg-muted'}`} />}
          </React.Fragment>
        ))}
      </div>

      <form onSubmit={handleSubmit}>
        {/* Step 1: Basics */}
        {step === 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Course Basics</CardTitle>
              <CardDescription>Enter the core information for your course.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-5">
              <div className="flex flex-col gap-2">
                <Label htmlFor="title">Title *</Label>
                <Input
                  id="title"
                  placeholder="e.g. Introduction to Game Development"
                  value={title}
                  onChange={(e) => handleTitleChange(e.target.value)}
                  required
                  minLength={3}
                  maxLength={255}
                />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="slug">URL Slug</Label>
                <Input id="slug" placeholder="introduction-to-game-development" value={slug} onChange={(e) => handleSlugChange(e.target.value)} required />
                <p className="text-muted-foreground text-xs">Auto-generated from title. Edit to customize.</p>
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="description">Description *</Label>
                <Textarea
                  id="description"
                  placeholder="Describe what students will learn..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  required
                  minLength={10}
                  maxLength={2000}
                  rows={5}
                />
                <p className="text-muted-foreground text-xs">{description.length}/2000 characters</p>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Step 2: Details */}
        {step === 1 && (
          <Card>
            <CardHeader>
              <CardTitle>Course Details</CardTitle>
              <CardDescription>Categorize your course and add media.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-5">
              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-2">
                  <Label>Category</Label>
                  <Select value={category} onValueChange={setCategory}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {PROGRAM_CATEGORIES.map((value) => (
                        <SelectItem key={value} value={value}>
                          {formatEnumLabel(value)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="flex flex-col gap-2">
                  <Label>Difficulty</Label>
                  <Select value={difficulty} onValueChange={setDifficulty}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {PROGRAM_DIFFICULTIES.map((value) => (
                        <SelectItem key={value} value={value}>
                          {formatEnumLabel(value)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="estimatedHours">Estimated Hours</Label>
                <Input
                  id="estimatedHours"
                  type="number"
                  min="1"
                  max="1000"
                  placeholder="e.g. 40"
                  value={estimatedHours}
                  onChange={(e) => setEstimatedHours(e.target.value)}
                />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="thumbnail">Thumbnail URL</Label>
                <Input id="thumbnail" type="url" placeholder="https://example.com/image.jpg" value={thumbnail} onChange={(e) => setThumbnail(e.target.value)} />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="videoShowcaseUrl">Video Showcase URL</Label>
                <Input
                  id="videoShowcaseUrl"
                  type="url"
                  placeholder="https://youtube.com/watch?v=..."
                  value={videoShowcaseUrl}
                  onChange={(e) => setVideoShowcaseUrl(e.target.value)}
                />
                <p className="text-muted-foreground text-xs">Optional promotional video for the course landing page.</p>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Step 3: Settings */}
        {step === 2 && (
          <Card>
            <CardHeader>
              <CardTitle>Course Settings</CardTitle>
              <CardDescription>Configure visibility, enrollment, and skills.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-5">
              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-2">
                  <Label>Visibility</Label>
                  <Select value={visibility} onValueChange={setVisibility}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {CONTENT_VISIBILITIES.map((value) => (
                        <SelectItem key={value} value={value}>
                          {formatEnumLabel(value)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="flex flex-col gap-2">
                  <Label>Enrollment Status</Label>
                  <Select value={enrollmentStatus} onValueChange={setEnrollmentStatus}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {ENROLLMENT_STATUSES.map((value) => (
                        <SelectItem key={value} value={value}>
                          {formatEnumLabel(value)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="maxEnrollments">Max Enrollments</Label>
                <Input
                  id="maxEnrollments"
                  type="number"
                  min="0"
                  placeholder="0 for unlimited"
                  value={maxEnrollments}
                  onChange={(e) => setMaxEnrollments(e.target.value)}
                />
                <p className="text-muted-foreground text-xs">Use 0 or leave blank for unlimited seats.</p>
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="skillsRequired">Skills Required</Label>
                <Textarea
                  id="skillsRequired"
                  placeholder="e.g. Basic programming knowledge, familiarity with C#"
                  value={skillsRequired}
                  onChange={(e) => setSkillsRequired(e.target.value)}
                  rows={2}
                />
                <p className="text-muted-foreground text-xs">Prerequisites students should have before enrolling.</p>
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="skillsProvided">Skills Provided</Label>
                <Textarea
                  id="skillsProvided"
                  placeholder="e.g. Game development basics, Unity fundamentals, 2D/3D concepts"
                  value={skillsProvided}
                  onChange={(e) => setSkillsProvided(e.target.value)}
                  rows={2}
                />
                <p className="text-muted-foreground text-xs">What students will learn from this course.</p>
              </div>
            </CardContent>
          </Card>
        )}

        {error && (
          <div className="mt-4 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
            {error}
          </div>
        )}

        <div className="mt-4 flex gap-3">
          {step > 0 && (
            <Button type="button" variant="outline" onClick={() => setStep(step - 1)}>
              <ArrowLeft className="mr-2 size-4" />
              Back
            </Button>
          )}

          {step < STEPS.length - 1 ? (
            <Button type="submit" disabled={!canAdvance()}>
              Next
              <ArrowRight className="ml-2 size-4" />
            </Button>
          ) : (
            <Button type="submit" disabled={isPending}>
              {isPending ? (
                <>
                  <Loader2 className="mr-2 size-4 animate-spin" />
                  Creating...
                </>
              ) : (
                'Create Course'
              )}
            </Button>
          )}

          <Button type="button" variant="ghost" asChild>
            <Link href="/dashboard/learning/courses">Cancel</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
