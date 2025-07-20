'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { GeneralDetailsSection } from '@/components/courses/course-editor/sections/general-details-section';

export default function CourseGeneralDetailsPage() {
  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Sticky Header */}
      <div className="sticky top-0 z-10 border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">General Details</h1>
              <p className="text-sm text-muted-foreground">Configure basic course information, title, description, and categorization</p>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto">
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">📝 Course Information</CardTitle>
            </CardHeader>
            <CardContent>
              <GeneralDetailsSection />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
