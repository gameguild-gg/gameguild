/*
 * ATTENTION: An "eval-source-map" devtool has been used.
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file with attached SourceMaps in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
exports.id = "vendor-chunks/prefix-style";
exports.ids = ["vendor-chunks/prefix-style"];
exports.modules = {

/***/ "(ssr)/../../node_modules/prefix-style/index.js":
/*!************************************************!*\
  !*** ../../node_modules/prefix-style/index.js ***!
  \************************************************/
/***/ ((module) => {

eval("var div = null\nvar prefixes = [ 'Webkit', 'Moz', 'O', 'ms' ]\n\nmodule.exports = function prefixStyle (prop) {\n  // re-use a dummy div\n  if (!div) {\n    div = document.createElement('div')\n  }\n\n  var style = div.style\n\n  // prop exists without prefix\n  if (prop in style) {\n    return prop\n  }\n\n  // borderRadius -> BorderRadius\n  var titleCase = prop.charAt(0).toUpperCase() + prop.slice(1)\n\n  // find the vendor-prefixed prop\n  for (var i = prefixes.length; i >= 0; i--) {\n    var name = prefixes[i] + titleCase\n    // e.g. WebkitBorderRadius or webkitBorderRadius\n    if (name in style) {\n      return name\n    }\n  }\n\n  return false\n}\n//# sourceURL=[module]\n//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiKHNzcikvLi4vLi4vbm9kZV9tb2R1bGVzL3ByZWZpeC1zdHlsZS9pbmRleC5qcyIsIm1hcHBpbmdzIjoiQUFBQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBLGdDQUFnQyxRQUFRO0FBQ3hDO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBIiwic291cmNlcyI6WyJ3ZWJwYWNrOi8vQGdhbWVkZXYtZ3VpbGQvd2ViLy4uLy4uL25vZGVfbW9kdWxlcy9wcmVmaXgtc3R5bGUvaW5kZXguanM/OTgzZSJdLCJzb3VyY2VzQ29udGVudCI6WyJ2YXIgZGl2ID0gbnVsbFxudmFyIHByZWZpeGVzID0gWyAnV2Via2l0JywgJ01veicsICdPJywgJ21zJyBdXG5cbm1vZHVsZS5leHBvcnRzID0gZnVuY3Rpb24gcHJlZml4U3R5bGUgKHByb3ApIHtcbiAgLy8gcmUtdXNlIGEgZHVtbXkgZGl2XG4gIGlmICghZGl2KSB7XG4gICAgZGl2ID0gZG9jdW1lbnQuY3JlYXRlRWxlbWVudCgnZGl2JylcbiAgfVxuXG4gIHZhciBzdHlsZSA9IGRpdi5zdHlsZVxuXG4gIC8vIHByb3AgZXhpc3RzIHdpdGhvdXQgcHJlZml4XG4gIGlmIChwcm9wIGluIHN0eWxlKSB7XG4gICAgcmV0dXJuIHByb3BcbiAgfVxuXG4gIC8vIGJvcmRlclJhZGl1cyAtPiBCb3JkZXJSYWRpdXNcbiAgdmFyIHRpdGxlQ2FzZSA9IHByb3AuY2hhckF0KDApLnRvVXBwZXJDYXNlKCkgKyBwcm9wLnNsaWNlKDEpXG5cbiAgLy8gZmluZCB0aGUgdmVuZG9yLXByZWZpeGVkIHByb3BcbiAgZm9yICh2YXIgaSA9IHByZWZpeGVzLmxlbmd0aDsgaSA+PSAwOyBpLS0pIHtcbiAgICB2YXIgbmFtZSA9IHByZWZpeGVzW2ldICsgdGl0bGVDYXNlXG4gICAgLy8gZS5nLiBXZWJraXRCb3JkZXJSYWRpdXMgb3Igd2Via2l0Qm9yZGVyUmFkaXVzXG4gICAgaWYgKG5hbWUgaW4gc3R5bGUpIHtcbiAgICAgIHJldHVybiBuYW1lXG4gICAgfVxuICB9XG5cbiAgcmV0dXJuIGZhbHNlXG59XG4iXSwibmFtZXMiOltdLCJzb3VyY2VSb290IjoiIn0=\n//# sourceURL=webpack-internal:///(ssr)/../../node_modules/prefix-style/index.js\n");

/***/ })

};
;