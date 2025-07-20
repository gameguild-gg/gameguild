'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ThumbnailMediaSection } from '@/components/courses/course-editor/sections/thumbnail-media-section';

export default function CourseMediaPage() {
  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Header */}
      <div className="border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Media & Assets</h1>
              <p className="text-sm text-muted-foreground">Upload and manage course thumbnails, showcase videos, and other media assets</p>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto">
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">🎬 Course Media</CardTitle>
            </CardHeader>
            <CardContent>
              <ThumbnailMediaSection />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
