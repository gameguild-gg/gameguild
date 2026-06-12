'use client';

import { useCourseEditor } from '@/components/courses/editor/context/course-editor-provider';
import { createCourse, saveCourse } from '@/components/courses/editor/actions';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ArrowLeft, BookOpen, DollarSign, Eye, FileText, Image, Save, Settings } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';
import { ContentStructureSection } from './sections/content-structure-section';
import { GeneralDetailsSection } from './sections/general-details-section';
import { SalesShowcaseSection } from './sections/sales-showcase-section';
import { ThumbnailMediaSection } from './sections/thumbnail-media-section';

interface CourseEditorProps {
  slug?: string;
  isCreating?: boolean;
}

const SECTIONS = [
  { id: 'general', label: 'General Details', icon: FileText, description: 'Title, description, category' },
  { id: 'media', label: 'Media & Assets', icon: Image, description: 'Thumbnail, videos, images' },
  { id: 'content', label: 'Course Content', icon: BookOpen, description: 'Lessons, modules, materials' },
  { id: 'pricing', label: 'Pricing & Sales', icon: DollarSign, description: 'Products, pricing, enrollment' },
  { id: 'settings', label: 'Settings', icon: Settings, description: 'Publishing, access, advanced' },
] as const;

export function CourseEditor({ slug, isCreating = false }: CourseEditorProps) {
  const { state, validate } = useCourseEditor();
  const [activeSection, setActiveSection] = useState<string>('general');
  const [isSaving, setIsSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  const level: 'Beginner' | 'Intermediate' | 'Advanced' = state.difficulty >= 3 ? 'Advanced' : state.difficulty === 2 ? 'Intermediate' : 'Beginner';

  const handleSave = async () => {
    const validation = validate();
    setSaveMessage(null);

    if (!validation.isValid) {
      return;
    }

    try {
      setIsSaving(true);
      const payload = {
        id: slug ?? state.slug,
        title: state.title,
        slug: state.slug,
        description: state.description || state.summary,
        area: state.category,
        level,
        status: state.status,
        tags: state.tags,
        tools: [],
        isPublic: state.status === 'published',
      };

      const result = isCreating ? await createCourse(payload) : await saveCourse(payload);

      setSaveMessage(result ? `Course ${isCreating ? 'created' : 'saved'}.` : 'Course could not be saved.');
    } catch (error) {
      console.error('Failed to save course:', error);
      setSaveMessage('Course could not be saved.');
    } finally {
      setIsSaving(false);
    }
  };

  const handlePreview = () => {
    const previewSlug = slug ?? state.slug;
    if (previewSlug) {
      window.open(`/courses/${previewSlug}`, '_blank', 'noopener,noreferrer');
    }
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Sticky Header */}
      <div className="sticky top-0 z-50 bg-background/95 backdrop-blur-sm border-b border-border shadow-sm">
        <div className="container mx-auto max-w-7xl px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Link href="/dashboard/courses">
                <Button variant="ghost" size="sm">
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  Back to Courses
                </Button>
              </Link>

              <div>
                <h1 className="text-2xl font-bold text-foreground">{isCreating ? 'Create New Course' : 'Edit Course'}</h1>
                {slug && (
                  <p className="text-sm text-muted-foreground">
                    /{slug}
                  </p>
                )}
              </div>
            </div>

            <div className="flex items-center gap-3">
              {/* Status Badge */}
              <div className="px-3 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400">
                Draft
              </div>

              {/* Action Buttons */}
              <Button variant="outline" onClick={handlePreview} disabled={!slug}>
                <Eye className="h-4 w-4 mr-2" />
                Preview
              </Button>

              <Button onClick={handleSave} disabled={isSaving} className="bg-gradient-to-r from-primary to-chart-2 hover:from-primary/90 hover:to-chart-2/90">
                <Save className="h-4 w-4 mr-2" />
                {isSaving ? 'Saving...' : isCreating ? 'Create Course' : 'Save Changes'}
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto max-w-7xl p-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Main Content Column */}
          <div className="lg:col-span-2 space-y-8">
            {/* General Details */}
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">📝 General Details</CardTitle>
              </CardHeader>
              <CardContent>
                {Object.keys(state.errors).length > 0 && (
                  <div role="alert" className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
                    <p className="font-medium">Fix course details before saving.</p>
                    <ul className="mt-2 list-disc space-y-1 pl-5">
                      {Object.entries(state.errors).map(([field, error]) => (
                        <li key={field}>{error}</li>
                      ))}
                    </ul>
                  </div>
                )}
                {saveMessage && (
                  <div role="status" className="mb-4 rounded-md border border-border bg-muted p-3 text-sm text-foreground">
                    {saveMessage}
                  </div>
                )}
                <GeneralDetailsSection />
              </CardContent>
            </Card>

            {/* Thumbnail & Media */}
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">🎨 Thumbnail & Media</CardTitle>
              </CardHeader>
              <CardContent>
                <ThumbnailMediaSection />
              </CardContent>
            </Card>

            {/* Course Content */}
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">📚 Course Content</CardTitle>
              </CardHeader>
              <CardContent>
                <ContentStructureSection />
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-8">
            {/* Sales & Showcase */}
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">💰 Sales & Showcase</CardTitle>
              </CardHeader>
              <CardContent>
                <SalesShowcaseSection />
              </CardContent>
            </Card>

            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">Preview</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="aspect-video overflow-hidden rounded-lg bg-muted">
                  {state.media.thumbnail?.url ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={state.media.thumbnail.url} alt={state.media.thumbnail.alt || state.title || 'Course thumbnail'} className="h-full w-full object-cover" />
                  ) : (
                    <div className="flex h-full items-center justify-center text-sm text-muted-foreground">No thumbnail</div>
                  )}
                </div>

                <div>
                  <h3 className="line-clamp-2 font-semibold text-foreground">{state.title || 'Untitled Course'}</h3>
                  <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{state.summary || 'No summary provided'}</p>
                </div>

                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">
                    {state.estimatedHours}h - {state.category || 'No category'}
                  </span>
                  <span className="rounded-full bg-muted px-2 py-1 text-xs text-muted-foreground">{level}</span>
                </div>

                {state.tags.length > 0 && (
                  <div className="flex flex-wrap gap-1">
                    {state.tags.map((tag) => (
                      <span key={tag} className="rounded bg-muted px-2 py-1 text-xs">
                        {tag}
                      </span>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
