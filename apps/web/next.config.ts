import { NextConfig } from 'next';
import createNextIntlPlugin from 'next-intl/plugin';

const withNextIntl = createNextIntlPlugin('./src/i18n/request.ts');

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone',
  // Force the app to use the correct base URL
  assetPrefix: process.env.NODE_ENV === 'production' ? undefined : '',
  typescript: {
    ignoreBuildErrors: true,
  },
  eslint: {
    ignoreDuringBuilds: true,
  },
  // Explicitly disable automatic redirects that might interfere with auth
  async redirects() {
    return [];
  },
  // Disable automatic rewrites that might cause issues
  async rewrites() {
    return [];
  },
  // Configure experimental features for better subdomain handling
  experimental: {
    // Disable middleware matcher optimizations that might cause issues
    optimizeServerReact: false,
  },
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'github.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'avatars.githubusercontent.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'lh3.googleusercontent.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'lh4.googleusercontent.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'lh5.googleusercontent.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'lh6.googleusercontent.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'images.unsplash.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'sample-videos.com',
        port: '',
        pathname: '/**',
      },
    ],
  },
  webpack: (config) => {
    // Allow importing .js files from TypeScript files
    // the api client generation requires this
    config.resolve.extensionAlias = {
      '.js': ['.js', '.ts'],
      '.jsx': ['.jsx', '.tsx'],
    };

    // Add rule to handle markdown files as raw text
    config.module.rules.push({
      test: /\.md$/,
      type: 'asset/source',
    });

    return config;
  },
};

// Export config with next-intl plugin
export default withNextIntl(nextConfig);
