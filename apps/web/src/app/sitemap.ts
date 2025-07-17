import type { MetadataRoute } from 'next';

export async function generateSitemaps() {
  // This function can be used to generate dynamic sitemaps if needed.
  // Currently, it returns an empty array as a placeholder.
  return [];
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  return [];
}
