"use strict";

/**
 * Turn a source file into an ECMAScript module exporting its contents.
 * Turbopack loaders must emit JavaScript, so this mirrors webpack's
 * `asset/source` behavior for Emception's Python subprocess shim.
 */
module.exports = function turbopackRawSourceLoader(source) {
  return `export default ${JSON.stringify(String(source))};`;
};
