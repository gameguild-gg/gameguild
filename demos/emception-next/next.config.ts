import type { NextConfig } from 'next';

const isProduction = process.env.NODE_ENV === 'production';

const nextConfig: NextConfig = {
    output: isProduction ? 'export' : undefined,
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
