import type { MetadataRoute } from 'next';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { PUBLIC_PROGRAM_PACKAGES } from '@/lib/courses/public-programs';

const baseUrl = process.env.NEXT_PUBLIC_APP_URL ?? 'https://gameguild.gg';
const staticRoutes = ['', '/sign-in', '/sign-up', '/courses', '/programs', '/about/roadmap', '/about/contributors', '/licenses', '/ferpa-waiver', '/academic-honesty'];

export async function generateSitemaps() {
  return [{ id: 0 }];
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const now = new Date();
  const catalog = await getPublicCourseCatalog();
  const courseRoutes = catalog.success
    ? catalog.data
        .map((course) => (typeof course.slug === 'string' && course.slug.length > 0 ? course.slug : null))
        .filter((slug): slug is string => Boolean(slug))
        .map((slug) => ({
          url: `${baseUrl}/courses/${slug}`,
          lastModified: now,
          changeFrequency: 'weekly' as const,
          priority: 0.7,
        }))
    : [];

  return [
    ...staticRoutes.map((route) => ({
      url: `${baseUrl}${route}`,
      lastModified: now,
      changeFrequency: route === '' ? ('daily' as const) : ('monthly' as const),
      priority: route === '' ? 1 : 0.6,
    })),
    ...PUBLIC_PROGRAM_PACKAGES.map((program) => ({
      url: `${baseUrl}/programs/${program.slug}`,
      lastModified: now,
      changeFrequency: 'weekly' as const,
      priority: 0.7,
    })),
    ...courseRoutes,
  ];
}
