import { fixupConfigRules } from "@eslint/compat";
import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const baselineRules = new Set([
  "@next/next/no-before-interactive-script-outside-document",
  "@next/next/no-img-element",
  "@next/next/no-location-assign-relative-destination",
  "@typescript-eslint/no-unused-expressions",
  "@typescript-eslint/no-unused-vars",
  "import/no-anonymous-default-export",
  "jsx-a11y/alt-text",
  "react-hooks/exhaustive-deps",
]);

function promoteBaselineRules(configs) {
  return configs.map((config) => ({
    ...config,
    rules: Object.fromEntries(
      Object.entries(config.rules ?? {}).map(([rule, setting]) => [
        rule,
        baselineRules.has(rule)
          ? Array.isArray(setting)
            ? ["error", ...setting.slice(1)]
            : "error"
          : setting,
      ]),
    ),
  }));
}

const eslintConfig = defineConfig([
  ...fixupConfigRules(promoteBaselineRules(nextVitals)),
  ...fixupConfigRules(promoteBaselineRules(nextTs)),
  {
    // ESLint suppressions only baseline errors. Promote warning-only rules so
    // legacy debt is recorded explicitly and every new violation still fails CI.
    linterOptions: {
      reportUnusedDisableDirectives: "error",
    },
  },
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    ".next-*/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    // Versioned browser assets are generated or vendored and linted at source.
    "public/**",
    "test-results/**",
  ]),
]);

export default eslintConfig;
