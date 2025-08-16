import type { CodegenConfig } from '@graphql-codegen/cli';

const config: CodegenConfig = {
  schema: process.env.NEXT_PUBLIC_GRAPHQL_URL,
  documents: ['src/app/**/*.tsx', 'src/components/**/*.tsx', 'src/lib/**/*.ts', 'src/lib/**/*.tsx'],
  generates: {
    './src/lib/graphql/generated/graphql.ts': {
      plugins: ['typescript', 'typescript-operations', 'typescript-react-apollo'],
      config: { withHooks: true, reactApolloVersion: 3 },
    },
  },
};

export default config;
