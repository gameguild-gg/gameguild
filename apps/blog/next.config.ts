import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone',
  assetPrefix: '/blog/static',
  basePath: '/blog',
  generateBuildId: async () => {
    return process.env.GIT_HASH ?? null;
  },
  rewrites: async () => {
    return {
      beforeFiles: [
        {
          source: '/blog/static/_next/:path*',
          destination: '/_next/:path*',
        },
      ],
    };
  },
  transpilePackages: ['@gameguild/ui'],
};

export default nextConfig;
