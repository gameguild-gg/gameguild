'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { createProgram } from '@/lib/content-management/programs/programs.actions';
import { AlertCircle, ArrowLeft, Loader2, Save } from 'lucide-react';
import { useRouter } from 'next/navigation';
import React, { useState } from 'react';
import speakingurl from 'speakingurl';

interface CreateCourseFormData {
  title: string;
  description: string;
  slug: string;
}

export const CreateCourseForm = (): React.JSX.Element => {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateCourseFormData>({
    title: '',
    description: '',
    slug: '',
  });
  const [error, setError] = useState<string | null>(null);

  const handleInputChange = (field: keyof CreateCourseFormData, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error when user starts typing
    if (error) setError(null);
  };

  const handleTitleChange = (value: string) => {
    handleInputChange('title', value);
    // Auto-generate slug from title
    const slug = speakingurl(value);
    setFormData(prev => ({ ...prev, slug }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      // Validate form data
      if (!formData.title.trim()) {
        throw new Error('Title is required');
      }
      if (!formData.description.trim()) {
        throw new Error('Description is required');
      }
      if (!formData.slug.trim()) {
        throw new Error('Slug is required');
      }

      const result = await createProgram({
        body: {
          title: formData.title.trim(),
          description: formData.description.trim(),
          slug: formData.slug.trim(),
        },
        url: '/api/program',
      });

      console.log('Form received result:', {
        hasData: !!result.data,
        hasError: !!result.error,
        dataKeys: result.data ? Object.keys(result.data) : null,
        errorType: result.error ? typeof result.error : null,
        error: result.error,
        fullResult: result
      });

      // Check if the result has an error
      if (result.error) {
        const errorMessage = (result.error as any)?.message || 'An error occurred while creating the course';
        throw new Error(errorMessage);
      }

      if (result.data) {
        // Success! Redirect to the created course using slug
        const slug = result.data.slug || formData.slug;
        router.push(`/dashboard/courses/${slug}`);
      } else {
        throw new Error('Failed to create course');
      }
    } catch (err) {
      console.error('Error creating course:', err);

      // Handle different types of errors
      if (err instanceof Error) {
        if (err.message.includes('Authentication required') || err.message.includes('401')) {
          setError('Please sign in to create a course. Redirecting to sign-in page...');
          setTimeout(() => {
            router.push('/sign-in');
          }, 2000);
        } else if (err.message.includes('403') || err.message.includes('Forbidden')) {
          setError('You do not have permission to create courses. Please contact an administrator.');
        } else if (err.message.includes('409') || err.message.includes('Conflict')) {
          setError('A course with this title or slug already exists. Please choose a different title.');
        } else {
          setError(err.message || 'An unexpected error occurred while creating the course.');
        }
      } else {
        setError('An unexpected error occurred while creating the course.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-6">
      <Card className="shadow-lg border-0">
        <CardHeader>
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => router.back()}
              className="flex items-center gap-2"
            >
              <ArrowLeft className="h-4 w-4" />
              Back
            </Button>
            <CardTitle className="text-2xl font-bold">Create New Course</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-4">
                <div className="flex items-start gap-3">
                  <AlertCircle className="h-5 w-5 text-destructive mt-0.5 flex-shrink-0" />
                  <div>
                    <p className="text-destructive font-medium">Error</p>
                    <p className="text-destructive/80 text-sm mt-1">{error}</p>
                  </div>
                </div>
              </div>
            )}

            <div className="space-y-4">
              <div>
                <Label htmlFor="title" className="text-sm font-medium">
                  Course Title *
                </Label>
                <Input
                  id="title"
                  type="text"
                  value={formData.title}
                  onChange={(e) => handleTitleChange(e.target.value)}
                  placeholder="Enter course title"
                  required
                  className="mt-1 !border-border focus:!border-ring focus:!ring-ring/50"
                />
              </div>

              <div>
                <Label htmlFor="description" className="text-sm font-medium">
                  Description *
                </Label>
                <Textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => handleInputChange('description', e.target.value)}
                  placeholder="Enter course description"
                  rows={4}
                  required
                  className="mt-1 border-0 !border-border focus:!border-ring focus:!ring-ring/50 resize-none overflow-hidden [&_.grammarly-desktop-integration]:!hidden [&_.grammarly-extension]:!hidden [&_.grammarly-extension__editing-area]:!border-none [&_.grammarly-extension__editing-area]:!outline-none [&_.grammarly-extension__editing-area]:!resize-none [&_.grammarly-extension__editing-area]:!overflow-hidden"
                />
              </div>

              <div>
                <Label htmlFor="slug" className="text-sm font-medium">
                  URL Slug *
                </Label>
                <Input
                  id="slug"
                  type="text"
                  value={formData.slug}
                  onChange={(e) => handleInputChange('slug', e.target.value)}
                  placeholder="course-url-slug"
                  required
                  className="mt-1 !border-border focus:!border-ring focus:!ring-ring/50"
                />
                <p className="text-xs text-muted-foreground mt-1">
                  This will be used in the course URL. Use only lowercase letters, numbers, and hyphens.
                </p>
              </div>
            </div>

            <div className="flex gap-4 pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => router.back()}
                disabled={isSubmitting}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={isSubmitting || !formData.title.trim() || !formData.description.trim() || !formData.slug.trim()}
                className="flex-1"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Creating...
                  </>
                ) : (
                  <>
                    <Save className="mr-2 h-4 w-4" />
                    Create Course
                  </>
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
};
