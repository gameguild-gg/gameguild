import type { NextConfig } from 'next';
import createNextIntlPlugin from 'next-intl/plugin';

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone',
  // assetPrefix: '/console/static',
  generateBuildId: async () => {
    return process.env.GIT_HASH ?? null;
  },
  // rewrites: async () => {
  //   return {
  //     beforeFiles: [
  //       {
  //         source: '/console/static/_next/:path*',
  //         destination: '/_next/:path*',
  //       },
  //     ],
  //   };
  // },
  transpilePackages: ['@gameguild/ui'],
};

const withNextIntl = createNextIntlPlugin();

export default withNextIntl(nextConfig);
