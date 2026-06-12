import React from 'react';
import { getCourseTestimonials } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Star } from 'lucide-react';

/**
 * Listing Testimonials Page
 *
 * Route: /courses/[course]/listing/testimonials
 *
 * Manage student reviews and testimonials.
 * Feature/unfeature testimonials for the public listing.
 */
export default async function ListingTestimonialsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/listing/testimonials'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const testimonials = await getCourseTestimonials(courseId);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2"><Star className="size-5" />Testimonials &amp; Reviews</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex flex-wrap gap-2">
          <Badge variant="outline">{testimonials.total} reviews</Badge>
          <Badge variant="outline">{testimonials.averageRating.toFixed(1)} average rating</Badge>
        </div>
        {testimonials.testimonials.length === 0 ? (
          <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No course reviews have been submitted yet.</div>
        ) : (
          testimonials.testimonials.map((testimonial) => (
            <div key={testimonial.id} className="rounded-lg border p-4">
              <div className="mb-2 flex items-center justify-between">
                <p className="font-medium">{testimonial.title}</p>
                <Badge>{testimonial.rating}/5</Badge>
              </div>
              <p className="text-sm text-muted-foreground">{testimonial.content || 'No written review.'}</p>
              <p className="mt-2 text-xs text-muted-foreground">{testimonial.studentName}</p>
            </div>
          ))
        )}
      </CardContent>
    </Card>
  );
}
