import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone',
  generateBuildId: async () => {
    return process.env.GIT_HASH ?? null;
  },
  rewrites: async () => [
    {
      source: '/blog',
      destination: `${process.env.BLOG_DOMAIN || 'http://localhost:3001'}/blog`,
    },
    {
      source: '/blog/:path*',
      destination: `${process.env.BLOG_DOMAIN || 'http://localhost:3001'}/blog/:path*`,
    },
    {
      source: '/blog/static/:path*',
      destination: `${process.env.BLOG_DOMAIN || 'http://localhost:3001'}/blog/static/:path*`,
    },
  ],
  transpilePackages: ['@gameguild/ui'],
};

export default nextConfig;
