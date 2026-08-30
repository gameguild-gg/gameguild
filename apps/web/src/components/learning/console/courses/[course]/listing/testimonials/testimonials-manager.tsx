'use client';

import { updateCourseReviewModeration } from '@/lib/learning/actions';
import type { CourseTestimonials } from '@/lib/learning/queries/listing';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Label } from '@game-guild/ui/components/label';
import { Switch } from '@game-guild/ui/components/switch';
import { Star } from 'lucide-react';
import { useState, useTransition } from 'react';

export function TestimonialsManager({ courseId, testimonials }: { courseId: string; testimonials: CourseTestimonials }) {
  const [items, setItems] = useState(testimonials.testimonials);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function updateReview(reviewId: string, isApproved: boolean, isFeatured: boolean) {
    setError(null);
    startTransition(async () => {
      const result = await updateCourseReviewModeration(courseId, reviewId, isApproved, isFeatured);
      if (!result.success) {
        setError(result.error);
        return;
      }
      setItems((current) => current.map((item) => item.id === reviewId
        ? { ...item, approved: isApproved, featured: isFeatured }
        : item));
    });
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2"><Star className="size-5" />Testimonials &amp; reviews</CardTitle>
        <CardDescription>Approve student reviews and select the strongest testimonials for the public course page.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex flex-wrap gap-2">
          <Badge variant="outline">{testimonials.total} reviews</Badge>
          <Badge variant="outline">{testimonials.averageRating.toFixed(1)} average rating</Badge>
          <Badge variant="outline">{items.filter((item) => item.featured).length} featured</Badge>
        </div>
        {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
        {items.length === 0 ? (
          <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No course reviews have been submitted yet.</div>
        ) : items.map((testimonial) => (
          <article key={testimonial.id} className="space-y-4 rounded-lg border p-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
              <div><div className="flex flex-wrap items-center gap-2"><h2 className="font-medium">{testimonial.title}</h2><Badge>{testimonial.rating}/5</Badge>{testimonial.verified ? <Badge variant="outline">Verified learner</Badge> : null}</div><p className="mt-2 text-sm text-muted-foreground">{testimonial.content || 'No written review.'}</p><p className="mt-2 text-xs text-muted-foreground">{testimonial.studentName} · {testimonial.helpful} helpful</p></div>
              <div className="flex flex-wrap gap-2">{testimonial.approved ? <Badge variant="secondary">Approved for storefront</Badge> : <Badge variant="outline">Pending moderation</Badge>}{testimonial.featured ? <Badge>Featured</Badge> : null}</div>
            </div>
            <div className="flex flex-wrap gap-6 border-t pt-3">
              <div className="flex items-center gap-2"><Switch id={`approve-${testimonial.id}`} disabled={isPending} checked={testimonial.approved} onCheckedChange={(checked) => updateReview(testimonial.id, checked, checked ? testimonial.featured : false)} /><Label htmlFor={`approve-${testimonial.id}`} aria-label={`Approve review ${testimonial.title}`}>Approved</Label></div>
              <div className="flex items-center gap-2"><Switch id={`feature-${testimonial.id}`} disabled={isPending || !testimonial.approved} checked={testimonial.featured} onCheckedChange={(checked) => updateReview(testimonial.id, testimonial.approved, checked)} /><Label htmlFor={`feature-${testimonial.id}`} aria-label={`Feature review ${testimonial.title}`}>Featured</Label></div>
            </div>
          </article>
        ))}
      </CardContent>
    </Card>
  );
}
