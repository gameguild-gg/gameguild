import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const nextConfig: NextConfig = {
  reactCompiler: true,
  output: "standalone",
  transpilePackages: ["@game-guild/ui"],
};

const withNextIntl = createNextIntlPlugin();
export default withNextIntl(nextConfig);
