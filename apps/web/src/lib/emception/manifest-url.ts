/**
 * The web app publishes the pinned Emception toolchain under `/emception`
 * during both local development and production builds. An explicit public URL
 * may still be supplied by deployments that host the same artifacts elsewhere.
 */
export const EMCEPTION_MANIFEST_URL =
  process.env.NEXT_PUBLIC_EMCEPTION_MANIFEST_URL?.trim() ||
  "/emception/manifest.json";
