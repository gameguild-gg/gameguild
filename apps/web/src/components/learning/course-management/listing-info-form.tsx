'use client';

import React, { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Badge } from '@game-guild/ui/components/badge';
import { Loader2, Save } from 'lucide-react';
import { updateCourse } from '@/lib/learning/actions';
import { buildDashboardCoursePath } from '@/lib/learning/course-route';
import type { CourseDetails } from '@/lib/learning/types';
import {
  PROGRAM_CATEGORIES,
  PROGRAM_DIFFICULTIES,
  formatEnumLabel,
} from '@/lib/learning/enums';

/**
 * Storefront identity editor (issue #106 phase 1). The course record is
 * fetched by the server page and arrives as a prop, so the form renders its
 * initial values server-side with no client fetch waterfall; only the edit
 * and save interactions stay on the client.
 */
export function ListingInfoForm({
  course,
  surface,
}: {
  course: CourseDetails;
  surface: 'workspace' | 'console';
}): React.JSX.Element {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  // Form state, initialized from the server-fetched course.
  const [title, setTitle] = useState(course.title);
  const [slug, setSlug] = useState(course.slug);
  const [description, setDescription] = useState(course.description);
  const [category, setCategory] = useState(course.category);
  const [difficulty, setDifficulty] = useState(course.difficulty);
  const [estimatedHours, setEstimatedHours] = useState(course.estimatedHours?.toString() ?? '');
  const [passingScore, setPassingScore] = useState(
    typeof course.passingScore === 'number' ? course.passingScore.toString() : '60',
  );
  const [skillsRequired, setSkillsRequired] = useState(course.skillsRequired ?? '');
  const [skillsProvided, setSkillsProvided] = useState(course.skillsProvided ?? '');

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    startTransition(async () => {
      const result = await updateCourse({
        courseId: course.id,
        title: title.trim(),
        slug: slug.trim(),
        description: description.trim(),
        category,
        difficulty,
        estimatedHours: estimatedHours ? parseInt(estimatedHours, 10) : 0,
        passingScore: passingScore ? parseInt(passingScore, 10) : undefined,
        skillsRequired: skillsRequired.trim(),
        skillsProvided: skillsProvided.trim(),
      });
      if (result.success) {
        setSuccess(true);
        router.replace(buildDashboardCoursePath({ ...course, slug: slug.trim() }, 'listing/info', surface));
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      {/* Two-column layout: identity + classification */}
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="flex flex-col gap-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle>Course Identity</CardTitle>
              <CardDescription>
                This information is shown to prospective students on the course landing page.
              </CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-5">
              <div className="flex flex-col gap-2">
                <Label htmlFor="title">Course Title</Label>
                <Input id="title" value={title} onChange={(e) => setTitle(e.target.value)} required minLength={3} maxLength={255} />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="description">Description</Label>
                <Textarea id="description" value={description} onChange={(e) => setDescription(e.target.value)} required minLength={10} maxLength={2000} rows={6} />
                <p className="text-muted-foreground text-xs">{description.length}/2000 characters</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Skills &amp; Outcomes</CardTitle>
              <CardDescription>Define what students will learn and what they need to know first.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-5">
              <div className="flex flex-col gap-2">
                <Label htmlFor="skillsProvided">Skills Students Will Learn</Label>
                <Textarea
                  id="skillsProvided"
                  placeholder="e.g. Game development basics, Unity fundamentals, 2D/3D concepts"
                  value={skillsProvided}
                  onChange={(e) => setSkillsProvided(e.target.value)}
                  rows={3}
                />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="skillsRequired">Prerequisites</Label>
                <Textarea
                  id="skillsRequired"
                  placeholder="e.g. Basic programming knowledge, familiarity with C#"
                  value={skillsRequired}
                  onChange={(e) => setSkillsRequired(e.target.value)}
                  rows={3}
                />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right column: classification */}
        <div className="flex flex-col gap-6">
          <Card>
            <CardHeader>
              <CardTitle>Classification</CardTitle>
              <CardDescription>Help students find your course.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-5">
              <div className="flex flex-col gap-2">
                <Label htmlFor="slug">URL Slug</Label>
                <Input id="slug" value={slug} onChange={(e) => setSlug(e.target.value)} required />
              </div>

              <div className="flex flex-col gap-2">
                <Label>Category</Label>
                <Select value={category} onValueChange={(value) => setCategory(value ?? '')}>
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
                <Select value={difficulty} onValueChange={(value) => setDifficulty(value ?? '')}>
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
                <Label htmlFor="passingScore">Passing score (%)</Label>
                <Input
                  id="passingScore"
                  type="number"
                  min={0}
                  max={100}
                  step={1}
                  value={passingScore}
                  onChange={(e) => setPassingScore(e.target.value)}
                />
                <p className="text-muted-foreground text-xs">
                  Minimum percentage (0-100) required to pass this course. Applied to all assessments.
                </p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Status</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              <Badge variant="outline">{course.category}</Badge>
              <Badge variant="outline">{course.difficulty}</Badge>
              {course.estimatedHours && <Badge variant="secondary">{course.estimatedHours}h estimated</Badge>}
              <Badge variant={course.status === 'published' ? 'default' : 'secondary'}>{course.status}</Badge>
            </CardContent>
          </Card>
        </div>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>
      )}
      {success && (
        <div className="rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-300">
          Course info updated successfully.
        </div>
      )}

      <div className="flex gap-3">
        <Button type="submit" disabled={isPending}>
          {isPending ? (
            <>
              <Loader2 className="mr-2 size-4 animate-spin" /> Saving...
            </>
          ) : (
            <>
              <Save className="mr-2 size-4" /> Save Changes
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
