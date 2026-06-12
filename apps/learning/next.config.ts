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
    async redirects() {
        const webUrl = process.env.NEXT_PUBLIC_WEB_URL || process.env.WEB_PUBLIC_URL;
        if (!webUrl) {
            return [];
        }

        const destination = webUrl.replace(/\/$/, "");

        return [
            {
                source: "/dashboard/:path*",
                destination: `${destination}/dashboard/:path*`,
                permanent: false,
            },
            {
                source: "/:locale(en-US|pt-BR)/dashboard/:path*",
                destination: `${destination}/:locale/dashboard/:path*`,
                permanent: false,
            },
            {
                source: "/courses",
                destination: `${destination}/courses`,
                permanent: false,
            },
            {
                source: "/:locale(en-US|pt-BR)/courses",
                destination: `${destination}/:locale/courses`,
                permanent: false,
            },
            {
                source: "/tracks/:path*",
                destination: `${destination}/tracks/:path*`,
                permanent: false,
            },
            {
                source: "/:locale(en-US|pt-BR)/tracks/:path*",
                destination: `${destination}/:locale/tracks/:path*`,
                permanent: false,
            },
        ];
    },
};

export default nextConfig;
