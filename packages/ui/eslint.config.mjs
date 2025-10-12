import baseConfig from '@gameguild/eslint-config';

/**
 * @see https://eslint.org/docs/latest/use/configure/configuration-files
 * @type {import('eslint').Linter.Config[]}
 */
const config = [...baseConfig];

export default config.flat();
