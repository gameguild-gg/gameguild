'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ArrowLeft, Save, Eye } from 'lucide-react';
import Link from 'next/link';
import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { GeneralDetailsSection } from './sections/general-details-section';
import { ThumbnailMediaSection } from './sections/thumbnail-media-section';
import { SalesShowcaseSection } from './sections/sales-showcase-section';

interface CourseEditorProps {
  courseSlug?: string;
  isCreating?: boolean;
}

export function CourseEditor({ courseSlug, isCreating = false }: CourseEditorProps) {
  const { state, validate } = useCourseEditor();

  const handleSave = async () => {
    validate();
    
    if (!state.isValid) {
      // Scroll to first error or show toast
      return;
    }

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
    window.open(`/courses/${state.slug}`, '_blank');
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
                <h1 className="text-2xl font-bold text-foreground">
                  {isCreating ? 'Create New Course' : 'Edit Course'}
                </h1>
                {state.title && (
                  <p className="text-sm text-muted-foreground">
                    {state.title} {state.slug && `• /${state.slug}`}
                  </p>
                )}
              </div>
            </div>

            <div className="flex items-center gap-3">
              {/* Status Badge */}
              <div className={`px-3 py-1 rounded-full text-xs font-medium ${
                state.status === 'published' 
                  ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                  : state.status === 'draft'
                  ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400'
                  : 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400'
              }`}>
                {state.status.charAt(0).toUpperCase() + state.status.slice(1)}
              </div>

              {/* Action Buttons */}
              <Button variant="outline" onClick={handlePreview} disabled={!state.slug}>
                <Eye className="h-4 w-4 mr-2" />
                Preview
              </Button>
              
              <Button 
                onClick={handleSave}
                disabled={!state.isValid}
                className="bg-gradient-to-r from-primary to-chart-2 hover:from-primary/90 hover:to-chart-2/90"
              >
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
                <CardTitle className="flex items-center gap-2">
                  📝 General Details
                </CardTitle>
              </CardHeader>
              <CardContent>
                <GeneralDetailsSection />
              </CardContent>
            </Card>

            {/* Thumbnail & Media */}
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  🎨 Thumbnail & Media
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ThumbnailMediaSection />
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-8">
            {/* Sales & Showcase */}
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  💰 Sales & Showcase
                </CardTitle>
              </CardHeader>
              <CardContent>
                <SalesShowcaseSection />
              </CardContent>
            </Card>

            {/* Course Preview */}
            {state.title && (
              <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    👁️ Preview
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="aspect-video bg-muted rounded-lg flex items-center justify-center">
                    {state.media.thumbnail?.url ? (
                      <img 
                        src={state.media.thumbnail.url} 
                        alt={state.media.thumbnail.alt}
                        className="w-full h-full object-cover rounded-lg"
                      />
                    ) : (
                      <div className="text-muted-foreground text-sm">No thumbnail</div>
                    )}
                  </div>
                  
                  <div>
                    <h3 className="font-semibold text-foreground line-clamp-2">
                      {state.title || 'Untitled Course'}
                    </h3>
                    <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                      {state.summary || 'No summary provided'}
                    </p>
                  </div>
                  
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">
                      {state.estimatedHours}h • {state.category}
                    </span>
                    <div className="flex gap-1">
                      {Array.from({ length: state.difficulty }, (_, i) => (
                        <div key={i} className="w-2 h-2 bg-primary rounded-full" />
                      ))}
                      {Array.from({ length: 4 - state.difficulty }, (_, i) => (
                        <div key={i} className="w-2 h-2 bg-muted rounded-full" />
                      ))}
                    </div>
                  </div>
                  
                  {state.tags.length > 0 && (
                    <div className="flex flex-wrap gap-1">
                      {state.tags.slice(0, 3).map((tag) => (
                        <span key={tag} className="px-2 py-1 bg-muted text-xs rounded">
                          {tag}
                        </span>
                      ))}
                      {state.tags.length > 3 && (
                        <span className="px-2 py-1 bg-muted text-xs rounded">
                          +{state.tags.length - 3} more
                        </span>
                      )}
                    </div>
                  )}
                </CardContent>
              </Card>
            )}

            {/* Validation Errors */}
            {Object.keys(state.errors).length > 0 && (
              <Card className="shadow-lg border-red-200 bg-red-50/50 backdrop-blur-sm">
                <CardHeader>
                  <CardTitle className="text-red-800 flex items-center gap-2">
                    ⚠️ Validation Errors
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-1 text-sm text-red-700">
                    {Object.entries(state.errors).map(([field, error]) => (
                      <li key={field}>• {error}</li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
