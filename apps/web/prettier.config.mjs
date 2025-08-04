/**
 * @see https://prettier.io/docs/en/configuration.html
 * @type {import('prettier').Config}
 */
const config = {
  printWidth: 160,
  singleQuote: true,
  tabWidth: 2,
  trailingComma: 'all',
  useTabs: false,
  // Note: File ignoring is handled by .prettierignore
  // We intentionally don't ignore generated files so they get formatted
};

export default config;
