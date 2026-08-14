import type { NextConfig } from 'next';

const isProduction = process.env.NODE_ENV === 'production';

const nextConfig: NextConfig = {
    output: isProduction ? 'export' : undefined,
    // The `@emception/browser` package contains `import x from './foo.py?raw'`
    // statements (an Emscripten subprocess shim). Turbopack does not know how
    // to handle `.py` files out of the box, so register a raw-text loader so
    // the file is inlined as a string at build time.
    turbopack: {
        rules: {
            '*.py': {
                loaders: ['raw-loader'],
                as: '*.js',
            },
        },
    },
    headers: async () => {
        return [
            {
                source: '/:path*',
                headers: [
                    {
                        key: 'Cross-Origin-Opener-Policy',
                        value: 'same-origin',
                    },
                    {
                        key: 'Cross-Origin-Embedder-Policy',
                        // 'credentialless' enables SharedArrayBuffer (required by Wasmer)
                        // while allowing cross-origin resources (like unpkg CDN) that do
                        // not set Cross-Origin-Resource-Policy headers.
                        // 'require-corp' is stricter but blocks external CDN imports.
                        value: 'credentialless',
                    },
                ],
            },
        ];
    },
};

export default nextConfig;
