'use client';

import React, { useState, useTransition, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Badge } from '@game-guild/ui/components/badge';
import { Loader2, Save } from 'lucide-react';
import { updateCourse, fetchCourse } from '@/lib/learning/actions';
import { buildDashboardCoursePath } from '@/lib/learning/course-route';
import type { CourseDetails } from '@/lib/learning/types';
import {
  PROGRAM_CATEGORIES,
  PROGRAM_DIFFICULTIES,
  formatEnumLabel,
} from '@/lib/learning/enums';

export default function ListingInfoPage({ params }: { params: Promise<{ locale: string; course: string }> }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [course, setCourse] = useState<CourseDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [courseId, setCourseId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  // Form state
  const [title, setTitle] = useState('');
  const [slug, setSlug] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('');
  const [difficulty, setDifficulty] = useState('');
  const [estimatedHours, setEstimatedHours] = useState('');
  const [passingScore, setPassingScore] = useState('60');
  const [skillsRequired, setSkillsRequired] = useState('');
  const [skillsProvided, setSkillsProvided] = useState('');

  useEffect(() => {
    let active = true;

    params.then(async (p) => {
      try {
        const data = await fetchCourse(p.course);
        if (active && data) {
          setCourseId(data.id);
          setCourse(data);
          setTitle(data.title);
          setSlug(data.slug);
          setDescription(data.description);
          setCategory(data.category);
          setDifficulty(data.difficulty);
          setEstimatedHours(data.estimatedHours?.toString() ?? '');
          setPassingScore(
            typeof data.passingScore === 'number' ? data.passingScore.toString() : '60',
          );
          setSkillsRequired(data.skillsRequired ?? '');
          setSkillsProvided(data.skillsProvided ?? '');
        }
      } catch {
        // ignore
      }
      if (active) setLoading(false);
    });

    return () => {
      active = false;
    };
  }, [params]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    startTransition(async () => {
      const result = await updateCourse({
        courseId,
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
        if (course) {
          router.replace(buildDashboardCoursePath({ ...course, slug: slug.trim() }, 'listing/info', 'console'));
        }
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!course) {
    return <div className="text-muted-foreground p-6">Course not found.</div>;
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
