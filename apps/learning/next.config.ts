import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    reactCompiler: true,
    output: "standalone",
    transpilePackages: ["@game-guild/ui"],
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
    },
};

export default nextConfig;
