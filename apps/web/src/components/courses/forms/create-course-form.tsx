'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Loader2, Save, ArrowLeft } from 'lucide-react';
import { createProgram } from '@/lib/content-management/programs/programs.actions';

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

  // Auto-generate slug from title
  const handleTitleChange = (title: string) => {
    const slug = title
      .toLowerCase()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .trim();

    setFormData({ ...formData, title, slug });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      const response = await createProgram({
        url: '/api/program',
        body: {
          title: formData.title,
          description: formData.description || null,
          slug: formData.slug || null,
        },
      });

      if (response.data) {
        // Redirect to the courses list or the newly created course
        router.push('/dashboard/courses');
      } else {
        console.error('Failed to create course:', response.error);
      }
    } catch (error) {
      console.error('Error creating course:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = () => {
    router.push('/dashboard/courses');
  };

  return (
    <div className="max-w-2xl mx-auto">
      <Card>
        <CardHeader>
          <CardTitle>Course Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Title */}
            <div className="space-y-2">
              <Label htmlFor="title">Course Title *</Label>
              <Input id="title" type="text" value={formData.title} onChange={(e) => handleTitleChange(e.target.value)} placeholder="Enter course title" required />
            </div>

            {/* Slug */}
            <div className="space-y-2">
              <Label htmlFor="slug">Course Slug</Label>
              <Input id="slug" type="text" value={formData.slug} onChange={(e) => setFormData({ ...formData, slug: e.target.value })} placeholder="course-url-slug" />
              <p className="text-sm text-slate-500">This will be used in the course URL</p>
            </div>

            {/* Description */}
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea id="description" value={formData.description} onChange={(e) => setFormData({ ...formData, description: e.target.value })} placeholder="Describe what students will learn in this course" rows={4} />
            </div>

            {/* Action Buttons */}
            <div className="flex items-center justify-between pt-6">
              <Button type="button" variant="outline" onClick={handleCancel} className="flex items-center gap-2">
                <ArrowLeft className="h-4 w-4" />
                Cancel
              </Button>

              <Button type="submit" disabled={isSubmitting || !formData.title.trim()} className="flex items-center gap-2">
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                {isSubmitting ? 'Creating...' : 'Create Course'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
};
