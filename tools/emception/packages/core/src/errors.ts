// Typed error hierarchy.

/** Base class for all emception runtime errors. Catch this to handle any emception fault. */
export class EmceptionError extends Error {
  constructor(message: string, public readonly cause?: unknown) {
    super(message);
    this.name = this.constructor.name;
  }
}

/** Thrown when a tool or compilation exceeds its configured `timeoutMs`. */
export class TimeoutError extends EmceptionError { }

/** Thrown when two concurrent requests attempt to create the same named workspace. */
export class WorkspaceConflictError extends EmceptionError { }

/** Thrown by the testing engine when one or more test cases fail. */
export class TestFailureError extends EmceptionError { }

/** Thrown when the workspace build configuration is invalid or inconsistent. */
export class BuildConfigError extends EmceptionError { }

/** Base for errors that indicate a required runtime feature is unavailable in the current context. */
export class RuntimeFeatureUnavailableError extends EmceptionError { }

/**
 * Thrown when SharedArrayBuffer / cross-origin isolation is required but the
 * page is not served with the `Cross-Origin-Opener-Policy: same-origin` and
 * `Cross-Origin-Embedder-Policy: require-corp` headers.
 *
 * Fix: in browser apps, self-host a root-level `/coi-serviceworker.js` or configure
 * your server to emit these headers.
 */
export class CrossOriginIsolationError extends EmceptionError { }

/**
 * Thrown when an SDL / WebGL canvas output path is requested but
 * `OffscreenCanvas` is not available in the current browser context.
 *
 * Fix: ensure the canvas worker is served from a cross-origin-isolated context
 * and that the browser supports `OffscreenCanvas` (all modern browsers do).
 */
export class CanvasUnavailableError extends RuntimeFeatureUnavailableError { }
