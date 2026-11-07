import { ImageResponse } from 'next/og';

import { getPublicProgramPackage } from '@/lib/courses/public-programs';
import { brandedOgImage, OG_IMAGE_CONTENT_TYPE, OG_IMAGE_SIZE } from '@/lib/seo/branded-og';

export const alt = 'GameGuild program';
export const size = OG_IMAGE_SIZE;
export const contentType = OG_IMAGE_CONTENT_TYPE;

/**
 * Program OG image: the program cover image over the branded dark background
 * when present, else the branded fallback with the program title.
 */
export default async function Image({ params }: { params: Promise<{ program: string }> }) {
  const { program: slug } = await params;
  const program = getPublicProgramPackage(slug);
  const title = program?.title ?? 'Program';
  // Satori only resolves absolute http(s) image sources.
  const rawImage = typeof program?.image === 'string' ? program.image.trim() : '';
  const image = /^https?:\/\//i.test(rawImage) ? rawImage : null;

  if (image) {
    return new ImageResponse(
      (
        <picture style={{ width: '100%', height: '100%', display: 'flex', backgroundColor: '#070a12' }}>
          <img src={image} alt={title} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        </picture>
      ),
      { ...OG_IMAGE_SIZE },
    );
  }

  return brandedOgImage({ title, subtitle: 'GameGuild program' });
}
