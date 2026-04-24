// Typed error hierarchy. Phase 1 will expand with stack-preserving converters.

export class EmceptionError extends Error {
  constructor(message: string, public readonly cause?: unknown) {
    super(message);
    this.name = this.constructor.name;
  }
}

export class TimeoutError extends EmceptionError {}
export class WorkspaceConflictError extends EmceptionError {}
export class TestFailureError extends EmceptionError {}
export class BuildConfigError extends EmceptionError {}
export class RuntimeFeatureUnavailableError extends EmceptionError {}
export class CrossOriginIsolationError extends EmceptionError {}
export class CanvasUnavailableError extends RuntimeFeatureUnavailableError {}
