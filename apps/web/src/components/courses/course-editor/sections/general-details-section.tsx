'use client';

import React from 'react';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useCourseEditor } from '@/components/courses/editor/context/course-editor-provider';
import { RichTextEditor } from '../rich-text-editor';
import { CourseLevel } from '@/components/legacy/types/courses';

const CATEGORIES = ['Web Development', 'Mobile Development', 'Data Science', 'Machine Learning', 'DevOps', 'Design', 'Business', 'Marketing', 'Other'];

const DIFFICULTY_LEVELS = [
  { value: 1, label: 'Beginner', description: 'No prior experience needed' },
  { value: 2, label: 'Intermediate', description: 'Some experience helpful' },
  { value: 3, label: 'Advanced', description: 'Significant experience required' },
  { value: 4, label: 'Expert', description: 'Professional level expertise' },
];

export function GeneralDetailsSection() {
  const { state, updateTitle, updateSlug, updateSummary, updateDescription, updateCategory, updateDifficulty } = useCourseEditor();

  const handleTitleChange = (value: string) => {
    updateTitle(value);

    // Auto-generate slug from title if it hasn't been manually set
    if (!state.manualSlugEdit) {
      const autoSlug = value
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .trim();
      updateSlug(autoSlug);
    }
  };

  const handleSlugChange = (value: string) => {
    // Mark that slug has been manually edited
    state.manualSlugEdit = true;
    updateSlug(value);
  };

  return (
    <div className="space-y-6">
      {/* Title */}
      <div className="space-y-2">
        <Label htmlFor="title" className="text-sm font-medium">
          Course Title *
        </Label>
        <Input id="title" value={state.title} onChange={(e) => handleTitleChange(e.target.value)} placeholder="Enter a compelling course title..." className={state.errors.title ? 'border-red-500' : ''} />
        {state.errors.title && <p className="text-sm text-red-600">{state.errors.title}</p>}
      </div>

      {/* Slug */}
      <div className="space-y-2">
        <Label htmlFor="slug" className="text-sm font-medium">
          Course URL Slug *
        </Label>
        <div className="flex items-center space-x-2">
          <span className="text-sm text-muted-foreground">/courses/</span>
          <Input id="slug" value={state.slug} onChange={(e) => handleSlugChange(e.target.value)} placeholder="course-url-slug" className={`flex-1 ${state.errors.slug ? 'border-red-500' : ''}`} />
        </div>
        {state.errors.slug && <p className="text-sm text-red-600">{state.errors.slug}</p>}
        <p className="text-xs text-muted-foreground">This will be the URL for your course. Use lowercase letters, numbers, and hyphens only.</p>
      </div>

      {/* Summary */}
      <div className="space-y-2">
        <Label htmlFor="summary" className="text-sm font-medium">
          Short Summary *
        </Label>
        <Textarea
          id="summary"
          value={state.summary}
          onChange={(e) => updateSummary(e.target.value)}
          placeholder="Write a brief, compelling summary that appears in course listings..."
          rows={3}
          className={state.errors.summary ? 'border-red-500' : ''}
        />
        {state.errors.summary && <p className="text-sm text-red-600">{state.errors.summary}</p>}
        <p className="text-xs text-muted-foreground">{state.summary.length}/200 characters</p>
      </div>

      {/* Category & Difficulty Row */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Category */}
        <div className="space-y-2">
          <Label className="text-sm font-medium">Category *</Label>
          <Select value={state.category} onValueChange={updateCategory}>
            <SelectTrigger className={state.errors.category ? 'border-red-500' : ''}>
              <SelectValue placeholder="Select category" />
            </SelectTrigger>
            <SelectContent>
              {CATEGORIES.map((category) => (
                <SelectItem key={category} value={category}>
                  {category}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {state.errors.category && <p className="text-sm text-red-600">{state.errors.category}</p>}
        </div>

        {/* Difficulty */}
        <div className="space-y-2">
          <Label className="text-sm font-medium">Difficulty Level *</Label>
          <Select value={state.difficulty.toString()} onValueChange={(value) => updateDifficulty(parseInt(value) as CourseLevel)}>
            <SelectTrigger className={state.errors.difficulty ? 'border-red-500' : ''}>
              <SelectValue placeholder="Select difficulty" />
            </SelectTrigger>
            <SelectContent>
              {DIFFICULTY_LEVELS.map((level) => (
                <SelectItem key={level.value} value={level.value.toString()}>
                  <div className="flex items-center gap-2">
                    <div className="flex gap-1">
                      {Array.from({ length: level.value }, (_, i) => (
                        <div key={i} className="w-2 h-2 bg-primary rounded-full" />
                      ))}
                      {Array.from({ length: 4 - level.value }, (_, i) => (
                        <div key={i} className="w-2 h-2 bg-muted rounded-full" />
                      ))}
                    </div>
                    <span>{level.label}</span>
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {state.errors.difficulty && <p className="text-sm text-red-600">{state.errors.difficulty}</p>}
        </div>
      </div>

      {/* Description */}
      <div className="space-y-2">
        <Label className="text-sm font-medium">Detailed Description</Label>
        <div className={`border rounded-md ${state.errors.description ? 'border-red-500' : 'border-input'}`}>
          <RichTextEditor content={state.description} onChange={updateDescription} placeholder="Write a detailed description of your course, including what students will learn, prerequisites, and outcomes..." />
        </div>
        {state.errors.description && <p className="text-sm text-red-600">{state.errors.description}</p>}
        <p className="text-xs text-muted-foreground">Use this space to provide comprehensive information about your course content, learning objectives, and what makes it valuable.</p>
      </div>
    </div>
  );
}
