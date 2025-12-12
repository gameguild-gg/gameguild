/**
 * @see https://jamiemason.github.io/syncpack/config/syncpackrc/
 * @type {import('syncpack').RcFile}
 */
const config = {
  sortFirst: [
    'name',
    'version',
    'type',
    'main',
    'description',
    'author',
    'homepage',
    'license',
    'private',
    'workspaces',
    'exports',
    'files',
    'scripts',
    'packageManager',
    'engines',
    'dependencies',
    'devDependencies',
  ],
  sortPackages: true,
};

export default config;
