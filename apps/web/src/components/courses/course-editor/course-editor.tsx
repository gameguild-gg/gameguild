'use client';

import { useCourseEditor } from '@/components/courses/editor/context/course-editor-provider';
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
  const { state } = useCourseEditor();
  const [activeSection, setActiveSection] = useState<string>('general');

  const handleSave = async () => {
    // TODO: Add validation logic

    try {
      // TODO: Implement API call to save course
      console.log('Saving course:', state);

      // If creating, redirect to edit page with new slug
      if (isCreating) {
        // router.push(`/dashboard/courses/${state.slug}/edit`);
      }
    } catch (error) {
      console.error('Failed to save course:', error);
      // Show error toast
    }
  };

  const handlePreview = () => {
    // TODO: Open preview in new tab
    if (slug) {
      window.open(`/courses/${slug}`, '_blank');
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

              <Button onClick={handleSave} className="bg-gradient-to-r from-primary to-chart-2 hover:from-primary/90 hover:to-chart-2/90">
                <Save className="h-4 w-4 mr-2" />
                {isCreating ? 'Create Course' : 'Save Changes'}
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

            {/* Course Preview - TODO: Implement preview with proper state management */}
            {false && (
              <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">👁️ Preview</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="aspect-video bg-muted rounded-lg flex items-center justify-center">
                    <div className="text-muted-foreground text-sm">No thumbnail</div>
                  </div>

                  <div>
                    <h3 className="font-semibold text-foreground line-clamp-2">Untitled Course</h3>
                    <p className="text-sm text-muted-foreground mt-1 line-clamp-2">No summary provided</p>
                  </div>

                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">
                      0h • No category
                    </span>
                    <div className="flex gap-1">
                      {Array.from({ length: 1 }, (_, i) => (
                        <div key={i} className="w-2 h-2 bg-primary rounded-full" />
                      ))}
                      {Array.from({ length: 3 }, (_, i) => (
                        <div key={i} className="w-2 h-2 bg-muted rounded-full" />
                      ))}
                    </div>
                  </div>

                  <div className="flex flex-wrap gap-1">
                    {[].map((tag: string) => (
                      <span key={tag} className="px-2 py-1 bg-muted text-xs rounded">
                        {tag}
                      </span>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Validation Errors - TODO: Implement validation */}
          </div>
        </div>
      </div>
    </div>
  );
}
