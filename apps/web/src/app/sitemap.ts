import type { MetadataRoute } from 'next';
import { PUBLIC_PROGRAM_PACKAGES } from '@/lib/courses/public-programs';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { getAllBlogPostsForSitemap, getAllPublishedProjectsForSitemap } from '@/lib/sitemap/public-entries';
import { buildBlogAbsoluteCanonicalUrl, BLOG_SITE_BASE_URL } from '@/lib/blogs/seo';

const baseUrl = process.env.NEXT_PUBLIC_APP_URL ?? 'https://gameguild.gg';
// Auth routes are intentionally excluded: sign-in/sign-up are noindexed utility
// pages and should not be advertised to crawlers.
const staticRoutes = [
  '',
  '/courses',
  '/projects',
  '/community',
  '/testing-lab',
  '/launch-pad',
  '/jobs',
  '/about',
  '/about/roadmap',
  '/about/contributors',
  '/contact',
  '/legal',
  '/terms-of-service',
  '/terms-of-use',
  '/polices/privacy',
  '/polices/cookies',
  '/legal/licenses',
  '/legal/ferpa-waiver',
  '/legal/academic-honesty',
];

// NOTE: This must stay a single sitemap (no `generateSitemaps`). With `generateSitemaps`
// Next.js serves the sitemap at `/sitemap/<id>.xml` and `/sitemap.xml` 404s — which is
// what robots.ts advertises. sitemap.test.ts guards this coupling.
//
// lastmod policy: only emit a real, content-derived timestamp when the source data has
// one (blog posts: publishedAt). Google treats an absent lastmod as better than a fake
// one, and our project/course/program sources expose no updatedAt — so those entries
// omit lastmod entirely. Never emit sitemap generation time as lastmod.
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [projects, catalog, blogPosts] = await Promise.all([
    getAllPublishedProjectsForSitemap().catch(() => []),
    getPublicCourseCatalog().catch(() => null),
    getAllBlogPostsForSitemap().catch(() => []),
  ]);
  const courseRoutes = catalog?.success
    ? catalog.data
        .map((course) => (typeof course.slug === 'string' && course.slug.length > 0 ? course.slug : null))
        .filter((slug): slug is string => Boolean(slug))
        .map((slug) => ({
          url: `${baseUrl}/courses/${slug}`,
        }))
    : [];

  return [
    ...staticRoutes.map((route) => ({
      url: `${baseUrl}${route}`,
    })),
    ...projects.map((project) => ({
      url: `${baseUrl}/projects/${project.slug}`,
    })),
    ...PUBLIC_PROGRAM_PACKAGES.map((program) => ({
      url: `${baseUrl}/programs/${program.slug}`,
    })),
    ...courseRoutes,
    ...blogPosts
      .filter((post) => post.slug && post.primaryAuthorHandle)
      .map((post) => ({
        url: buildBlogAbsoluteCanonicalUrl(BLOG_SITE_BASE_URL, post.primaryAuthorHandle ?? '', post.slug ?? ''),
        lastModified: post.publishedAt ? new Date(post.publishedAt) : undefined,
      })),
  ];
}
