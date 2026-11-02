'use client';

import React, { useState, useTransition } from 'react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Loader2, Save, Image as ImageIcon, Video } from 'lucide-react';
import { updateCourse } from '@/lib/learning/actions';
import type { CourseDetails } from '@/lib/learning/types';

function safePreviewUrl(value: string): string {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : '';
  } catch {
    return '';
  }
}

/**
 * Listing media editor (issue #106 phase 1). The course record is fetched by
 * the server page and arrives as a prop; only the edit and save interactions
 * stay on the client.
 */
export function ListingMediaForm({ course }: { course: CourseDetails }): React.JSX.Element {
  const [isPending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  // Form state, initialized from the server-fetched course.
  const [thumbnail, setThumbnail] = useState(course.thumbnail ?? '');
  const [videoShowcaseUrl, setVideoShowcaseUrl] = useState(course.videoShowcaseUrl ?? '');

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    startTransition(async () => {
      const result = await updateCourse({
        courseId: course.id,
        thumbnail: thumbnail.trim(),
        videoShowcaseUrl: videoShowcaseUrl.trim(),
      });
      if (result.success) {
        setSuccess(true);
      } else {
        setError(result.error);
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ImageIcon className="size-5" />
            Cover Image
          </CardTitle>
          <CardDescription>The thumbnail shown in course listings and search results.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="thumbnail">Thumbnail URL</Label>
            <Input id="thumbnail" type="url" placeholder="https://example.com/cover.jpg" value={thumbnail} onChange={(e) => setThumbnail(e.target.value)} />
          </div>
          {thumbnail && (
            <div className="overflow-hidden rounded-lg border">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={safePreviewUrl(thumbnail)}
                alt="Course thumbnail preview"
                className="h-48 w-full object-cover"
                onError={(e) => (e.currentTarget.style.display = 'none')}
              />
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Video className="size-5" />
            Promotional Video
          </CardTitle>
          <CardDescription>An optional video shown on the course landing page to attract students.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="videoShowcaseUrl">Video URL</Label>
            <Input
              id="videoShowcaseUrl"
              type="url"
              placeholder="https://youtube.com/watch?v=..."
              value={videoShowcaseUrl}
              onChange={(e) => setVideoShowcaseUrl(e.target.value)}
            />
            <p className="text-muted-foreground text-xs">YouTube, Vimeo, or direct video URLs are supported.</p>
          </div>
        </CardContent>
      </Card>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>
      )}
      {success && (
        <div className="rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-300">
          Media updated successfully.
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
              <Save className="mr-2 size-4" /> Save Media
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
