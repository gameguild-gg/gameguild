/*
 * ATTENTION: An "eval-source-map" devtool has been used.
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file with attached SourceMaps in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
exports.id = "vendor-chunks/add-px-to-style";
exports.ids = ["vendor-chunks/add-px-to-style"];
exports.modules = {

/***/ "(ssr)/../../node_modules/add-px-to-style/index.js":
/*!***************************************************!*\
  !*** ../../node_modules/add-px-to-style/index.js ***!
  \***************************************************/
/***/ ((module) => {

eval("/* The following list is defined in React's core */\nvar IS_UNITLESS = {\n  animationIterationCount: true,\n  boxFlex: true,\n  boxFlexGroup: true,\n  boxOrdinalGroup: true,\n  columnCount: true,\n  flex: true,\n  flexGrow: true,\n  flexPositive: true,\n  flexShrink: true,\n  flexNegative: true,\n  flexOrder: true,\n  gridRow: true,\n  gridColumn: true,\n  fontWeight: true,\n  lineClamp: true,\n  lineHeight: true,\n  opacity: true,\n  order: true,\n  orphans: true,\n  tabSize: true,\n  widows: true,\n  zIndex: true,\n  zoom: true,\n\n  // SVG-related properties\n  fillOpacity: true,\n  stopOpacity: true,\n  strokeDashoffset: true,\n  strokeOpacity: true,\n  strokeWidth: true\n};\n\nmodule.exports = function(name, value) {\n  if(typeof value === 'number' && !IS_UNITLESS[ name ]) {\n    return value + 'px';\n  } else {\n    return value;\n  }\n};//# sourceURL=[module]\n//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiKHNzcikvLi4vLi4vbm9kZV9tb2R1bGVzL2FkZC1weC10by1zdHlsZS9pbmRleC5qcyIsIm1hcHBpbmdzIjoiQUFBQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQSxJQUFJO0FBQ0o7QUFDQTtBQUNBIiwic291cmNlcyI6WyJ3ZWJwYWNrOi8vQGdhbWVkZXYtZ3VpbGQvd2ViLy4uLy4uL25vZGVfbW9kdWxlcy9hZGQtcHgtdG8tc3R5bGUvaW5kZXguanM/NWFiYSJdLCJzb3VyY2VzQ29udGVudCI6WyIvKiBUaGUgZm9sbG93aW5nIGxpc3QgaXMgZGVmaW5lZCBpbiBSZWFjdCdzIGNvcmUgKi9cbnZhciBJU19VTklUTEVTUyA9IHtcbiAgYW5pbWF0aW9uSXRlcmF0aW9uQ291bnQ6IHRydWUsXG4gIGJveEZsZXg6IHRydWUsXG4gIGJveEZsZXhHcm91cDogdHJ1ZSxcbiAgYm94T3JkaW5hbEdyb3VwOiB0cnVlLFxuICBjb2x1bW5Db3VudDogdHJ1ZSxcbiAgZmxleDogdHJ1ZSxcbiAgZmxleEdyb3c6IHRydWUsXG4gIGZsZXhQb3NpdGl2ZTogdHJ1ZSxcbiAgZmxleFNocmluazogdHJ1ZSxcbiAgZmxleE5lZ2F0aXZlOiB0cnVlLFxuICBmbGV4T3JkZXI6IHRydWUsXG4gIGdyaWRSb3c6IHRydWUsXG4gIGdyaWRDb2x1bW46IHRydWUsXG4gIGZvbnRXZWlnaHQ6IHRydWUsXG4gIGxpbmVDbGFtcDogdHJ1ZSxcbiAgbGluZUhlaWdodDogdHJ1ZSxcbiAgb3BhY2l0eTogdHJ1ZSxcbiAgb3JkZXI6IHRydWUsXG4gIG9ycGhhbnM6IHRydWUsXG4gIHRhYlNpemU6IHRydWUsXG4gIHdpZG93czogdHJ1ZSxcbiAgekluZGV4OiB0cnVlLFxuICB6b29tOiB0cnVlLFxuXG4gIC8vIFNWRy1yZWxhdGVkIHByb3BlcnRpZXNcbiAgZmlsbE9wYWNpdHk6IHRydWUsXG4gIHN0b3BPcGFjaXR5OiB0cnVlLFxuICBzdHJva2VEYXNob2Zmc2V0OiB0cnVlLFxuICBzdHJva2VPcGFjaXR5OiB0cnVlLFxuICBzdHJva2VXaWR0aDogdHJ1ZVxufTtcblxubW9kdWxlLmV4cG9ydHMgPSBmdW5jdGlvbihuYW1lLCB2YWx1ZSkge1xuICBpZih0eXBlb2YgdmFsdWUgPT09ICdudW1iZXInICYmICFJU19VTklUTEVTU1sgbmFtZSBdKSB7XG4gICAgcmV0dXJuIHZhbHVlICsgJ3B4JztcbiAgfSBlbHNlIHtcbiAgICByZXR1cm4gdmFsdWU7XG4gIH1cbn07Il0sIm5hbWVzIjpbXSwic291cmNlUm9vdCI6IiJ9\n//# sourceURL=webpack-internal:///(ssr)/../../node_modules/add-px-to-style/index.js\n");

/***/ })

};
;