/*
 * ATTENTION: An "eval-source-map" devtool has been used.
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file with attached SourceMaps in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
exports.id = "vendor-chunks/to-camel-case";
exports.ids = ["vendor-chunks/to-camel-case"];
exports.modules = {

/***/ "(ssr)/../../node_modules/to-camel-case/index.js":
/*!*************************************************!*\
  !*** ../../node_modules/to-camel-case/index.js ***!
  \*************************************************/
/***/ ((module, __unused_webpack_exports, __webpack_require__) => {

eval("\nvar space = __webpack_require__(/*! to-space-case */ \"(ssr)/../../node_modules/to-space-case/index.js\")\n\n/**\n * Export.\n */\n\nmodule.exports = toCamelCase\n\n/**\n * Convert a `string` to camel case.\n *\n * @param {String} string\n * @return {String}\n */\n\nfunction toCamelCase(string) {\n  return space(string).replace(/\\s(\\w)/g, function (matches, letter) {\n    return letter.toUpperCase()\n  })\n}\n//# sourceURL=[module]\n//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiKHNzcikvLi4vLi4vbm9kZV9tb2R1bGVzL3RvLWNhbWVsLWNhc2UvaW5kZXguanMiLCJtYXBwaW5ncyI6IjtBQUNBLFlBQVksbUJBQU8sQ0FBQyxzRUFBZTs7QUFFbkM7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTtBQUNBLFdBQVcsUUFBUTtBQUNuQixZQUFZO0FBQ1o7O0FBRUE7QUFDQTtBQUNBO0FBQ0EsR0FBRztBQUNIIiwic291cmNlcyI6WyJ3ZWJwYWNrOi8vQGdhbWVkZXYtZ3VpbGQvd2ViLy4uLy4uL25vZGVfbW9kdWxlcy90by1jYW1lbC1jYXNlL2luZGV4LmpzPzVmZDciXSwic291cmNlc0NvbnRlbnQiOlsiXG52YXIgc3BhY2UgPSByZXF1aXJlKCd0by1zcGFjZS1jYXNlJylcblxuLyoqXG4gKiBFeHBvcnQuXG4gKi9cblxubW9kdWxlLmV4cG9ydHMgPSB0b0NhbWVsQ2FzZVxuXG4vKipcbiAqIENvbnZlcnQgYSBgc3RyaW5nYCB0byBjYW1lbCBjYXNlLlxuICpcbiAqIEBwYXJhbSB7U3RyaW5nfSBzdHJpbmdcbiAqIEByZXR1cm4ge1N0cmluZ31cbiAqL1xuXG5mdW5jdGlvbiB0b0NhbWVsQ2FzZShzdHJpbmcpIHtcbiAgcmV0dXJuIHNwYWNlKHN0cmluZykucmVwbGFjZSgvXFxzKFxcdykvZywgZnVuY3Rpb24gKG1hdGNoZXMsIGxldHRlcikge1xuICAgIHJldHVybiBsZXR0ZXIudG9VcHBlckNhc2UoKVxuICB9KVxufVxuIl0sIm5hbWVzIjpbXSwic291cmNlUm9vdCI6IiJ9\n//# sourceURL=webpack-internal:///(ssr)/../../node_modules/to-camel-case/index.js\n");

/***/ })

};
;