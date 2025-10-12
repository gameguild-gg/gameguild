import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone',
  assetPrefix: '/console/static',
  generateBuildId: async () => {
    return process.env.GIT_HASH ?? null;
  },
  rewrites: async () => {
    return {
      beforeFiles: [
        {
          source: '/academy/static/_next/:path*',
          destination: '/_next/:path*',
        },
      ],
    };
  },
  transpilePackages: ['@gameguild/ui'],
};

export default nextConfig;
