// JSDOM does not implement PointerEvent, but Base UI dispatches one for switches.
if (typeof globalThis.PointerEvent === "undefined") {
  Object.defineProperty(globalThis, "PointerEvent", {
    configurable: true,
    value: MouseEvent,
    writable: true,
  });
}
