import type { NextConfig } from "next";
import path from "path";

const nextConfig: NextConfig = {
    output: "export",
    // @wasmer/wasi is an optional runtime dependency whose npm package is
    // incomplete (WASM binary missing). Alias it to a stub so Turbopack/webpack
    // doesn't fail trying to resolve wasmer_wasi_js_bg.wasm.
    // WasmerRustAdapter catches the resulting runtime error and falls back to
    // the built-in WASI runtime automatically.
    turbopack: {
        resolveAlias: {
            "@wasmer/wasi": path.resolve(__dirname, "src/wasmer-wasi-stub.ts"),
        },
    },
    webpack: (config, { webpack }) => {
        config.plugins.push(
            new webpack.IgnorePlugin({ resourceRegExp: /^@wasmer\/wasi$/ }),
        );
        return config;
    },
    headers: async () => {
        return [
            {
                source: "/:path*",
                headers: [
                    {
                        key: "Cross-Origin-Opener-Policy",
                        value: "same-origin",
                    },
                    {
                        key: "Cross-Origin-Embedder-Policy",
                        value: "require-corp",
                    },
                ],
            },
        ];
    },
};

export default nextConfig;
