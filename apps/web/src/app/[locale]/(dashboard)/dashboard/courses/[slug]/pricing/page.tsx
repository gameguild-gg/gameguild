'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { SalesShowcaseSection } from '@/components/courses/course-editor/sections/sales-showcase-section';

export default function CoursePricingPage() {
  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Header */}
      <div className="border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Pricing & Products</h1>
              <p className="text-sm text-muted-foreground">Configure course pricing, products, and sales showcase settings</p>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto">
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">💰 Sales & Pricing</CardTitle>
            </CardHeader>
            <CardContent>
              <SalesShowcaseSection />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
