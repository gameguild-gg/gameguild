import { brandedOgAlt, brandedOgImage, OG_IMAGE_CONTENT_TYPE, OG_IMAGE_SIZE } from '@/lib/seo/branded-og';

export const alt = brandedOgAlt('Launch Pad');
export const size = OG_IMAGE_SIZE;
export const contentType = OG_IMAGE_CONTENT_TYPE;

export default function Image() {
  return brandedOgImage({ title: 'Launch Pad', subtitle: 'Ship your game' });
}
