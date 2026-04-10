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
    ignoreBuildErrors: false,
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
        source: '/langs/:file*.wasm',
        destination: '/langs/:file*.wasm.gz',
      },
      // WASM directory JS files (pyodide.asm.js)
      {
        source: '/langs/:file*.js',
        destination: '/langs/:file*.js.gz',
      },
      // WASM directory JSON files (pyodide-lock.json)
      {
        source: '/langs/:file*.json',
        destination: '/langs/:file*.json.gz',
      },
      // Pyodide loader JS (kept in /pyodide/)
      {
        source: '/pyodide/:file*.js',
        destination: '/pyodide/:file*.js.gz',
      },
      // .NET managed runtime - rewrite .wasm to .wasm.gz
      {
        source: '/managed/:path*.wasm',
        destination: '/managed/:path*.wasm.gz',
      },
      // .NET managed runtime - rewrite .dll to .dll.gz
      {
        source: '/managed/:path*.dll',
        destination: '/managed/:path*.dll.gz',
      },
      // .NET managed runtime - rewrite .dat to .dat.gz
      {
        source: '/managed/:path*.dat',
        destination: '/managed/:path*.dat.gz',
      },
    ];
  },
  // Set headers for compressed files
  async headers() {
    return [
      // Enable SharedArrayBuffer for @runno/runtime (required for WASM threads)
      // Only on the editor route where code-studio actually needs it
      // Applying globally breaks cross-origin iframes (YouTube, Spotify, etc.) in Firefox
      {
        source: '/:locale/gglexical',
        headers: [
          {
            key: 'Cross-Origin-Opener-Policy',
            value: 'same-origin',
          },
          {
            key: 'Cross-Origin-Embedder-Policy',
            value: 'credentialless',
          },
        ],
      },
      {
        source: '/:path*editor/:file*',
        headers: [
          {
            key: 'Cross-Origin-Opener-Policy',
            value: 'same-origin',
          },
          {
            key: 'Cross-Origin-Embedder-Policy',
            value: 'credentialless',
          },
        ],
      },
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
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/langs/:path*.wasm',
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
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/langs/:path*.js',
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
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/langs/:path*.json',
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
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/langs/:path*.zip',
        headers: [
          {
            key: 'Content-Type',
            value: 'application/zip',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
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
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      // .NET WASM runtime files
      {
        source: '/managed/:path*.js',
        headers: [
          {
            key: 'Content-Type',
            value: 'application/javascript',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/managed/:path*.wasm',
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
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/managed/:path*.dll',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'gzip',
          },
          {
            key: 'Content-Type',
            value: 'application/octet-stream',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/managed/:path*.json',
        headers: [
          {
            key: 'Content-Type',
            value: 'application/json',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
        ],
      },
      {
        source: '/managed/:path*.dat',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'gzip',
          },
          {
            key: 'Content-Type',
            value: 'application/octet-stream',
          },
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
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
      canvas: false, // vega-canvas uses canvas which is Node.js only
      crypto: false,
      stream: false,
      buffer: false,
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
