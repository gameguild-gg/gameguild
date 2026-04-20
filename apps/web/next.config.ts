import fs from 'fs';
import { NextConfig } from 'next';
import createNextIntlPlugin from 'next-intl/plugin';
import path from 'path';
import webpack from 'webpack';

const withNextIntl = createNextIntlPlugin('./src/i18n/request.ts');

const nextConfig: NextConfig = {
  /* config options here */
  transpilePackages: ['mermaid', '@mermaid-js/parser', 'langium', 'vscode-jsonrpc', 'chevrotain'],
  output: 'standalone',
  // Force the app to use the correct base URL
  assetPrefix: process.env.NODE_ENV === 'production' ? undefined : '',
  typescript: {
    ignoreBuildErrors: false,
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
      // .NET managed runtime files are served directly (not gzipped)
    ];
  },
  // Set headers for compressed files
  async headers() {
    return [
      // Enable SharedArrayBuffer for @runno/runtime (required for WASM threads)
      // Only on gglexical routes where code-studio actually needs it
      // Applying globally breaks cross-origin iframes (YouTube, Spotify, etc.) in Firefox
      // Matches both /gglexical/... (default locale hidden) and /pt-BR/gglexical/...
      {
        source: '/gglexical/:path*',
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
        source: '/:locale/gglexical/:path*',
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
        source: '/:path*',
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
      // .NET WASM runtime files (served directly, not gzipped)
      {
        source: '/managed/:path*',
        headers: [
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

    // Resolve vscode-jsonrpc, vscode-languageserver-types, and
    // vscode-languageserver-protocol for langium.  langium imports these
    // directly but doesn't list them as dependencies.  Depending on the
    // install layout (monorepo hoisting vs Docker flat) they may not be
    // resolvable from langium's location, so we alias them explicitly.
    // require.resolve works in every layout because these are direct deps
    // in package.json.
    const resolvePackageDir = (pkg: string): string => {
      try {
        return path.dirname(require.resolve(`${pkg}/package.json`));
      } catch {
        // Fallback: probe nested location inside vscode-languageserver
        const nested = [
          path.resolve(__dirname, 'node_modules/vscode-languageserver/node_modules', pkg),
          path.resolve(__dirname, '../../node_modules/vscode-languageserver/node_modules', pkg),
        ];
        return nested.find((p) => fs.existsSync(p)) ?? pkg;
      }
    };

    config.resolve.alias = {
      ...config.resolve.alias,
      'vscode-jsonrpc': resolvePackageDir('vscode-jsonrpc'),
      'vscode-languageserver-protocol': resolvePackageDir('vscode-languageserver-protocol'),
      'vscode-languageserver-types': resolvePackageDir('vscode-languageserver-types'),
    };

    // Add fallbacks for Node.js modules used by wasmoon and other packages
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

    // Handle markdown imports as raw text (webpack build path)
    config.module.rules.push({
      test: /\.md$/,
      type: 'asset/source',
    });

    // Fix for "self is not defined" error in server-side rendering
    if (isServer) {
      // Define self for server-side to prevent ReferenceError
      config.plugins ??= [];
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
export default withNextIntl(nextConfig);

