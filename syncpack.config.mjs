/**
 * @see https://jamiemason.github.io/syncpack/config/syncpackrc/
 * @type {import('syncpack').RcFile}
 */
const config = {
  sortFirst: ['name', 'description', 'type', 'version', 'author', 'license', 'private', 'workspaces', 'scripts', 'dependencies', 'devDependencies'],
  sortPackages: true,
};

export default config;
