"use strict";
/*
 * ATTENTION: An "eval-source-map" devtool has been used.
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file with attached SourceMaps in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
exports.id = "vendor-chunks/is-plain-obj";
exports.ids = ["vendor-chunks/is-plain-obj"];
exports.modules = {

/***/ "(ssr)/../../node_modules/is-plain-obj/index.js":
/*!************************************************!*\
  !*** ../../node_modules/is-plain-obj/index.js ***!
  \************************************************/
/***/ ((__unused_webpack___webpack_module__, __webpack_exports__, __webpack_require__) => {

eval("__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   \"default\": () => (/* binding */ isPlainObject)\n/* harmony export */ });\nfunction isPlainObject(value) {\n\tif (typeof value !== 'object' || value === null) {\n\t\treturn false;\n\t}\n\n\tconst prototype = Object.getPrototypeOf(value);\n\treturn (prototype === null || prototype === Object.prototype || Object.getPrototypeOf(prototype) === null) && !(Symbol.toStringTag in value) && !(Symbol.iterator in value);\n}\n//# sourceURL=[module]\n//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiKHNzcikvLi4vLi4vbm9kZV9tb2R1bGVzL2lzLXBsYWluLW9iai9pbmRleC5qcyIsIm1hcHBpbmdzIjoiOzs7O0FBQWU7QUFDZjtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBIiwic291cmNlcyI6WyJ3ZWJwYWNrOi8vQGdhbWVkZXYtZ3VpbGQvd2ViLy4uLy4uL25vZGVfbW9kdWxlcy9pcy1wbGFpbi1vYmovaW5kZXguanM/MWU4NyJdLCJzb3VyY2VzQ29udGVudCI6WyJleHBvcnQgZGVmYXVsdCBmdW5jdGlvbiBpc1BsYWluT2JqZWN0KHZhbHVlKSB7XG5cdGlmICh0eXBlb2YgdmFsdWUgIT09ICdvYmplY3QnIHx8IHZhbHVlID09PSBudWxsKSB7XG5cdFx0cmV0dXJuIGZhbHNlO1xuXHR9XG5cblx0Y29uc3QgcHJvdG90eXBlID0gT2JqZWN0LmdldFByb3RvdHlwZU9mKHZhbHVlKTtcblx0cmV0dXJuIChwcm90b3R5cGUgPT09IG51bGwgfHwgcHJvdG90eXBlID09PSBPYmplY3QucHJvdG90eXBlIHx8IE9iamVjdC5nZXRQcm90b3R5cGVPZihwcm90b3R5cGUpID09PSBudWxsKSAmJiAhKFN5bWJvbC50b1N0cmluZ1RhZyBpbiB2YWx1ZSkgJiYgIShTeW1ib2wuaXRlcmF0b3IgaW4gdmFsdWUpO1xufVxuIl0sIm5hbWVzIjpbXSwic291cmNlUm9vdCI6IiJ9\n//# sourceURL=webpack-internal:///(ssr)/../../node_modules/is-plain-obj/index.js\n");

/***/ })

};
;