import { NextConfig } from 'next';
import createNextIntlPlugin from 'next-intl/plugin';
import webpack from 'webpack';

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
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: '**',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'http',
        hostname: '**',
        port: '',
        pathname: '/**',
      },
    ],
    unoptimized: true,
    dangerouslyAllowSVG: true,
    contentDispositionType: 'attachment',
    contentSecurityPolicy: "default-src 'self'; script-src 'none'; sandbox;",
  },
  // Rewrite requests for non-.gz files to .gz versions
  async rewrites() {
    return [
      // WASM files
      {
        source: '/wasm/:file*.wasm',
        destination: '/wasm/:file*.wasm.gz',
      },
      // WASM directory JS files (pyodide.asm.js)
      {
        source: '/wasm/:file*.js',
        destination: '/wasm/:file*.js.gz',
      },
      // WASM directory JSON files (pyodide-lock.json)
      {
        source: '/wasm/:file*.json',
        destination: '/wasm/:file*.json.gz',
      },
      // Pyodide loader JS (kept in /pyodide/)
      {
        source: '/pyodide/:file*.js',
        destination: '/pyodide/:file*.js.gz',
      },
    ];
  },
  // Set headers for compressed files
  async headers() {
    return [
      {
        source: '/wasm/:path*.wasm',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'gzip',
          },
          {
            key: 'Content-Type',
            value: 'application/wasm',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
      {
        source: '/wasm/:path*.js',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'gzip',
          },
          {
            key: 'Content-Type',
            value: 'application/javascript',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
      {
        source: '/wasm/:path*.json',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'gzip',
          },
          {
            key: 'Content-Type',
            value: 'application/json',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
      {
        source: '/wasm/:path*.zip',
        headers: [
          {
            key: 'Content-Type',
            value: 'application/zip',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
      {
        source: '/pyodide/:path*.js',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'gzip',
          },
          {
            key: 'Content-Type',
            value: 'application/javascript',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
    ];
  },
  webpack: (config, { isServer }) => {
    // Allow importing .js files from TypeScript files
    // the api client generation requires this
    config.resolve.extensionAlias = {
      '.js': ['.js', '.ts'],
      '.jsx': ['.jsx', '.tsx'],
    };

    // Add fallbacks for Node.js modules used by wasmoon
    config.resolve.fallback = {
      ...config.resolve.fallback,
      module: false,
      fs: false,
      path: false,
    };

    // Add rule to handle markdown files as raw text
    config.module.rules.push({
      test: /\.md$/,
      type: 'asset/source',
    });

    // Fix for "self is not defined" error in server-side rendering
    if (typeof isServer !== 'undefined' && isServer) {
      // Define self for server-side to prevent ReferenceError
      config.plugins = config.plugins || [];
      config.plugins.push(
        new webpack.DefinePlugin({
          self: 'globalThis',
        })
      );
    }

    return config;
  },
};

// Export config with next-intl plugin
export default withNextIntl(nextConfig as any);
