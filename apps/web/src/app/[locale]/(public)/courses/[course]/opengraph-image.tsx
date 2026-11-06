import { ImageResponse } from 'next/og';

import { getCachedPublicCourseBySlug } from '@/lib/courses/cached-public-course';
import { brandedOgImage, OG_IMAGE_CONTENT_TYPE, OG_IMAGE_SIZE } from '@/lib/seo/branded-og';

export const alt = 'GameGuild course';
export const size = OG_IMAGE_SIZE;
export const contentType = OG_IMAGE_CONTENT_TYPE;

/**
 * Course OG image: the course thumbnail (embedded via Satori's <img> support)
 * over the branded dark template when present, else the branded fallback with
 * the course title.
 */
export default async function Image({ params }: { params: Promise<{ course: string }> }) {
  const { course: slug } = await params;
  // Same cached lookup as the page metadata; a failed lookup degrades to the
  // branded fallback instead of throwing (social crawlers must still get an image).
  const result = await getCachedPublicCourseBySlug(slug).catch(() => undefined);
  const course = result?.success ? result.data : undefined;
  const title = course?.title ?? 'Course';
  // Satori only resolves absolute http(s) image sources.
  const raw = typeof course?.thumbnail === 'string' ? course.thumbnail.trim() : '';
  const thumbnail = /^https?:\/\//i.test(raw) ? raw : null;

  if (thumbnail) {
    return new ImageResponse(
      (
        <picture style={{ width: '100%', height: '100%', display: 'flex', backgroundColor: '#070a12' }}>
          <img src={thumbnail} alt={title} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        </picture>
      ),
      { ...OG_IMAGE_SIZE },
    );
  }

  return brandedOgImage({ title, subtitle: 'GameGuild course' });
}
