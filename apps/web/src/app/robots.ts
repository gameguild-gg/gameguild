import type {MetadataRoute} from 'next';

export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: '*',
      allow: '/',
      disallow: '/dashboard/',
    },
    // TODO: Update the sitemap URL to match your production environment.
    sitemap: 'https://acme.com/sitemap.xml',
  };
}
