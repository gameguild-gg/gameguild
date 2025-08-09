export default {
  client: '@hey-api/client-next',
  plugins: [
    {
      name: '@hey-api/typescript',
      enums: 'typescript', // Generate TypeScript enums instead of union types
    },
    '@hey-api/sdk',
  ],
};