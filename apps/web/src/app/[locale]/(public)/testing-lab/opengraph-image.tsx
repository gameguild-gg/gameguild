import { brandedOgAlt, brandedOgImage, OG_IMAGE_CONTENT_TYPE, OG_IMAGE_SIZE } from '@/lib/seo/branded-og';

export const alt = brandedOgAlt('Testing Lab');
export const size = OG_IMAGE_SIZE;
export const contentType = OG_IMAGE_CONTENT_TYPE;

export default function Image() {
  return brandedOgImage({ title: 'Testing Lab', subtitle: 'Playtest games' });
}
