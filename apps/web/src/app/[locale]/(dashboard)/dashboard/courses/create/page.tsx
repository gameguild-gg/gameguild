'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { ArrowLeft, Save } from 'lucide-react';
import Link from 'next/link';

interface CourseFormData {
  title: string;
  description: string;
  area: string;
  level: string;
  estimatedHours: string;
  thumbnail: string;
  tools: string;
}

export default function CreateCoursePage() {
  const router = useRouter();
  const [formData, setFormData] = useState<CourseFormData>({
    title: '',
    description: '',
    area: 'programming',
    level: '1',
    estimatedHours: '',
    thumbnail: '',
    tools: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      // Here you would typically call an API to create the course
      // For now, we'll simulate a delay and show success
      await new Promise((resolve) => setTimeout(resolve, 1000));

      // Redirect to courses list with success message
      router.push('/dashboard/courses?created=true');
    } catch (err) {
      console.error('Error creating course:', err);
      setError('Failed to create course. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  const handleInputChange = (field: keyof CourseFormData, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  return (
    <div className="flex flex-col flex-1 p-6 max-w-4xl mx-auto w-full">
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <Link href="/dashboard/courses">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Courses
          </Button>
        </Link>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Create New Course</h1>
          <p className="text-muted-foreground">Add a new course to your educational content</p>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>}

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Course Information</CardTitle>
            <CardDescription>Basic details about your course</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="title">Course Title *</Label>
              <Input id="title" value={formData.title} onChange={(e) => handleInputChange('title', e.target.value)} placeholder="Enter course title" required />
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description *</Label>
              <Textarea
                id="description"
                value={formData.description}
                onChange={(e) => handleInputChange('description', e.target.value)}
                placeholder="Describe what students will learn in this course"
                rows={4}
                required
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="area">Course Area *</Label>
                <Select value={formData.area} onValueChange={(value) => handleInputChange('area', value)}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select area" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="programming">Programming</SelectItem>
                    <SelectItem value="art">Art</SelectItem>
                    <SelectItem value="design">Design</SelectItem>
                    <SelectItem value="audio">Audio</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="level">Difficulty Level *</Label>
                <Select value={formData.level} onValueChange={(value) => handleInputChange('level', value)}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select level" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">Beginner</SelectItem>
                    <SelectItem value="2">Intermediate</SelectItem>
                    <SelectItem value="3">Advanced</SelectItem>
                    <SelectItem value="4">Arcane</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="estimatedHours">Estimated Hours</Label>
              <Input
                id="estimatedHours"
                type="number"
                value={formData.estimatedHours}
                onChange={(e) => handleInputChange('estimatedHours', e.target.value)}
                placeholder="How many hours to complete"
                min="1"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="thumbnail">Thumbnail URL</Label>
              <Input
                id="thumbnail"
                value={formData.thumbnail}
                onChange={(e) => handleInputChange('thumbnail', e.target.value)}
                placeholder="URL to course thumbnail image"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="tools">Tools & Technologies</Label>
              <Input
                id="tools"
                value={formData.tools}
                onChange={(e) => handleInputChange('tools', e.target.value)}
                placeholder="Comma-separated list (e.g., Unity, C#, Blender)"
              />
              <p className="text-sm text-muted-foreground">Enter tools and technologies used in this course, separated by commas</p>
            </div>
          </CardContent>
        </Card>

        {/* Course Preview */}
        <Card>
          <CardHeader>
            <CardTitle>Preview</CardTitle>
            <CardDescription>How your course will appear to students</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="border rounded-lg p-4 bg-muted/30">
              <h3 className="font-semibold text-lg mb-2">{formData.title || 'Course Title'}</h3>
              <p className="text-muted-foreground mb-3">{formData.description || 'Course description will appear here...'}</p>
              <div className="flex gap-2 mb-2">
                <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">{formData.area.charAt(0).toUpperCase() + formData.area.slice(1)}</span>
                <span className="px-2 py-1 bg-green-100 text-green-800 text-xs rounded">
                  {formData.level === '1'
                    ? 'Beginner'
                    : formData.level === '2'
                      ? 'Intermediate'
                      : formData.level === '3'
                        ? 'Advanced'
                        : formData.level === '4'
                          ? 'Arcane'
                          : 'Unknown'}
                </span>
                {formData.estimatedHours && <span className="px-2 py-1 bg-orange-100 text-orange-800 text-xs rounded">{formData.estimatedHours}h</span>}
              </div>
              {formData.tools && (
                <div className="flex flex-wrap gap-1">
                  {formData.tools.split(',').map((tool, index) => (
                    <span key={index} className="px-2 py-1 bg-gray-100 text-gray-800 text-xs rounded">
                      {tool.trim()}
                    </span>
                  ))}
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Actions */}
        <div className="flex justify-end gap-4">
          <Link href="/dashboard/courses">
            <Button variant="outline" type="button">
              Cancel
            </Button>
          </Link>
          <Button type="submit" disabled={saving || !formData.title || !formData.description}>
            <Save className="w-4 h-4 mr-2" />
            {saving ? 'Creating...' : 'Create Course'}
          </Button>
        </div>
      </form>
    </div>
  );
}
