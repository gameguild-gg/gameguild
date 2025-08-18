import type { CodegenConfig } from '@graphql-codegen/cli';

// GraphQL codegen configuration
// Note: This configuration is now wrapped with a fail-safe script
// that prevents build failures when the schema is unavailable
const config: CodegenConfig = {
  schema: process.env.NEXT_PUBLIC_GRAPHQL_URL || 'http://localhost:5000/graphql',
  documents: ['src/**/*.{ts,tsx}', '!src/lib/graphql/generated/**/*'],
  generates: {
    './src/lib/graphql/generated/': {
      preset: 'client',
      plugins: [],
    },
  },
};

export default config;
