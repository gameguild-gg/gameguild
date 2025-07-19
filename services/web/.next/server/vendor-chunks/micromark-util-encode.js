"use strict";
/*
 * ATTENTION: An "eval-source-map" devtool has been used.
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file with attached SourceMaps in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
exports.id = "vendor-chunks/micromark-util-encode";
exports.ids = ["vendor-chunks/micromark-util-encode"];
exports.modules = {

/***/ "(ssr)/../../node_modules/micromark-util-encode/index.js":
/*!*********************************************************!*\
  !*** ../../node_modules/micromark-util-encode/index.js ***!
  \*********************************************************/
/***/ ((__unused_webpack___webpack_module__, __webpack_exports__, __webpack_require__) => {

eval("__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   encode: () => (/* binding */ encode)\n/* harmony export */ });\nconst characterReferences = {'\"': 'quot', '&': 'amp', '<': 'lt', '>': 'gt'}\n\n/**\n * Encode only the dangerous HTML characters.\n *\n * This ensures that certain characters which have special meaning in HTML are\n * dealt with.\n * Technically, we can skip `>` and `\"` in many cases, but CM includes them.\n *\n * @param {string} value\n *   Value to encode.\n * @returns {string}\n *   Encoded value.\n */\nfunction encode(value) {\n  return value.replace(/[\"&<>]/g, replace)\n\n  /**\n   * @param {string} value\n   *   Value to replace.\n   * @returns {string}\n   *   Encoded value.\n   */\n  function replace(value) {\n    return (\n      '&' +\n      characterReferences[\n        /** @type {keyof typeof characterReferences} */ (value)\n      ] +\n      ';'\n    )\n  }\n}\n//# sourceURL=[module]\n//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiKHNzcikvLi4vLi4vbm9kZV9tb2R1bGVzL21pY3JvbWFyay11dGlsLWVuY29kZS9pbmRleC5qcyIsIm1hcHBpbmdzIjoiOzs7O0FBQUEsNkJBQTZCOztBQUU3QjtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLFdBQVcsUUFBUTtBQUNuQjtBQUNBLGFBQWE7QUFDYjtBQUNBO0FBQ087QUFDUDs7QUFFQTtBQUNBLGFBQWEsUUFBUTtBQUNyQjtBQUNBLGVBQWU7QUFDZjtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQSxtQkFBbUIsa0NBQWtDO0FBQ3JEO0FBQ0EsUUFBUTtBQUNSO0FBQ0E7QUFDQSIsInNvdXJjZXMiOlsid2VicGFjazovL0BnYW1lZGV2LWd1aWxkL3dlYi8uLi8uLi9ub2RlX21vZHVsZXMvbWljcm9tYXJrLXV0aWwtZW5jb2RlL2luZGV4LmpzPzNlODEiXSwic291cmNlc0NvbnRlbnQiOlsiY29uc3QgY2hhcmFjdGVyUmVmZXJlbmNlcyA9IHsnXCInOiAncXVvdCcsICcmJzogJ2FtcCcsICc8JzogJ2x0JywgJz4nOiAnZ3QnfVxuXG4vKipcbiAqIEVuY29kZSBvbmx5IHRoZSBkYW5nZXJvdXMgSFRNTCBjaGFyYWN0ZXJzLlxuICpcbiAqIFRoaXMgZW5zdXJlcyB0aGF0IGNlcnRhaW4gY2hhcmFjdGVycyB3aGljaCBoYXZlIHNwZWNpYWwgbWVhbmluZyBpbiBIVE1MIGFyZVxuICogZGVhbHQgd2l0aC5cbiAqIFRlY2huaWNhbGx5LCB3ZSBjYW4gc2tpcCBgPmAgYW5kIGBcImAgaW4gbWFueSBjYXNlcywgYnV0IENNIGluY2x1ZGVzIHRoZW0uXG4gKlxuICogQHBhcmFtIHtzdHJpbmd9IHZhbHVlXG4gKiAgIFZhbHVlIHRvIGVuY29kZS5cbiAqIEByZXR1cm5zIHtzdHJpbmd9XG4gKiAgIEVuY29kZWQgdmFsdWUuXG4gKi9cbmV4cG9ydCBmdW5jdGlvbiBlbmNvZGUodmFsdWUpIHtcbiAgcmV0dXJuIHZhbHVlLnJlcGxhY2UoL1tcIiY8Pl0vZywgcmVwbGFjZSlcblxuICAvKipcbiAgICogQHBhcmFtIHtzdHJpbmd9IHZhbHVlXG4gICAqICAgVmFsdWUgdG8gcmVwbGFjZS5cbiAgICogQHJldHVybnMge3N0cmluZ31cbiAgICogICBFbmNvZGVkIHZhbHVlLlxuICAgKi9cbiAgZnVuY3Rpb24gcmVwbGFjZSh2YWx1ZSkge1xuICAgIHJldHVybiAoXG4gICAgICAnJicgK1xuICAgICAgY2hhcmFjdGVyUmVmZXJlbmNlc1tcbiAgICAgICAgLyoqIEB0eXBlIHtrZXlvZiB0eXBlb2YgY2hhcmFjdGVyUmVmZXJlbmNlc30gKi8gKHZhbHVlKVxuICAgICAgXSArXG4gICAgICAnOydcbiAgICApXG4gIH1cbn1cbiJdLCJuYW1lcyI6W10sInNvdXJjZVJvb3QiOiIifQ==\n//# sourceURL=webpack-internal:///(ssr)/../../node_modules/micromark-util-encode/index.js\n");

/***/ })

};
;