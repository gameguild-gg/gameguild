import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";
import path from "node:path";

const nextConfig: NextConfig = {
  reactCompiler: false,
  output: "standalone",
  transpilePackages: [
    "@game-guild/ui",
    "@game-guild/community-members",
    "@game-guild/courses",
    "@game-guild/dotnet-wasm",
    "mermaid",
    "@mermaid-js/parser",
    "langium",
    "vscode-jsonrpc",
    "chevrotain",
  ],
  images: {
    remotePatterns: [
      {
        protocol: "https",
        hostname: "placehold.co",
      },
      {
        protocol: "https",
        hostname: "i.imgur.com",
      },
      {
        protocol: "https",
        hostname: "images.unsplash.com",
      },
      {
        protocol: "https",
        hostname: "www.python.org",
      },
    ],
  },
  experimental: {
    authInterrupts: true,
    cpus: 1,
  },
  async rewrites() {
    return [
      {
        source: "/langs/:file*.wasm",
        destination: "/langs/:file*.wasm.gz",
      },
      {
        source: "/langs/:file*.js",
        destination: "/langs/:file*.js.gz",
      },
      {
        source: "/langs/:file*.json",
        destination: "/langs/:file*.json.gz",
      },
      {
        source: "/pyodide/:file*.js",
        destination: "/pyodide/:file*.js.gz",
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/block-content-editor/:path*",
        headers: [
          { key: "Cross-Origin-Opener-Policy", value: "same-origin" },
          { key: "Cross-Origin-Embedder-Policy", value: "credentialless" },
        ],
      },
      {
        source: "/:locale/block-content-editor/:path*",
        headers: [
          { key: "Cross-Origin-Opener-Policy", value: "same-origin" },
          { key: "Cross-Origin-Embedder-Policy", value: "credentialless" },
        ],
      },
      {
        source: "/wasm/:path*.wasm",
        headers: [
          { key: "Content-Encoding", value: "gzip" },
          { key: "Content-Type", value: "application/wasm" },
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
      {
        source: "/langs/:path*.wasm",
        headers: [
          { key: "Content-Encoding", value: "gzip" },
          { key: "Content-Type", value: "application/wasm" },
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
      {
        source: "/langs/:path*.js",
        headers: [
          { key: "Content-Encoding", value: "gzip" },
          { key: "Content-Type", value: "application/javascript" },
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
      {
        source: "/langs/:path*.json",
        headers: [
          { key: "Content-Encoding", value: "gzip" },
          { key: "Content-Type", value: "application/json" },
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
      {
        source: "/langs/:path*.zip",
        headers: [
          { key: "Content-Type", value: "application/zip" },
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
      {
        source: "/pyodide/:path*.js",
        headers: [
          { key: "Content-Encoding", value: "gzip" },
          { key: "Content-Type", value: "application/javascript" },
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
      {
        source: "/managed/:path*",
        headers: [
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
      {
        source: "/mathlive/:path*",
        headers: [
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
        ],
      },
    ];
  },
  webpack: (config, { isServer, webpack }) => {
    const resolvePackageDir = (pkg: string): string => {
      try {
        return path.dirname(require.resolve(`${pkg}/package.json`));
      } catch {
        const nested = [
          path.resolve(__dirname, "node_modules/vscode-languageserver/node_modules", pkg),
          path.resolve(__dirname, "../../node_modules/vscode-languageserver/node_modules", pkg),
        ];

        return nested.find((candidate) => require("node:fs").existsSync(candidate)) ?? pkg;
      }
    };

    config.resolve.alias = {
      ...config.resolve.alias,
      "@game-guild/dotnet-wasm": path.resolve(
        __dirname,
        "../../packages/infrastructure/wasm/dotnet/src/index.ts"
      ),
      "next/dist/server/route-modules/app-page/vendored/contexts/loadable": require.resolve(
        "next/dist/server/route-modules/pages/vendored/contexts/loadable.js"
      ),
      "vscode-jsonrpc": resolvePackageDir("vscode-jsonrpc"),
      "vscode-languageserver-protocol": resolvePackageDir("vscode-languageserver-protocol"),
      "vscode-languageserver-types": resolvePackageDir("vscode-languageserver-types"),
    };

    config.resolve.fallback = {
      ...config.resolve.fallback,
      module: false,
      fs: false,
      path: false,
      canvas: false,
      crypto: false,
      stream: false,
      buffer: false,
    };

    config.module.rules.push({
      test: /\.md$/,
      type: "asset/source",
    });

    if (isServer) {
      config.plugins ??= [];
      config.plugins.push(
        new webpack.DefinePlugin({
          self: "globalThis",
        })
      );
    }

    return config;
  },
};

const withNextIntl = createNextIntlPlugin();
export default withNextIntl(nextConfig);
