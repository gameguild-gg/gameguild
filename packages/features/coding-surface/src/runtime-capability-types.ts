import type { CodingExecutionConfiguration, CodingExerciseType } from '@game-guild/coding';
import type { CodingSurfaceMode } from './types';

export type CodingCapabilityUnavailableReason =
  | 'authoring-only'
  | 'read-only-context'
  | 'assignment-disallows-files'
  | 'incompatible-toolchain'
  | 'unsupported-environment'
  | 'graphics-tests-unavailable'
  | 'graphics-not-required'
  | 'interactive-input-unverified'
  | 'runtime-unverified';

export type CodingLocalCapability =
  | { readonly status: 'available' }
  | { readonly status: 'unavailable'; readonly reason: CodingCapabilityUnavailableReason };

export type CodingLocalTestCapability =
  | { readonly status: 'available'; readonly scope: 'public' | 'full' }
  | { readonly status: 'unavailable'; readonly reason: CodingCapabilityUnavailableReason };

/** Eligibility only: no boot, case execution, grade or authorization decision. */
export interface CodingRuntimeCapabilityRequest {
  readonly exerciseType: CodingExerciseType;
  readonly execution: Pick<CodingExecutionConfiguration, 'environment' | 'toolchain'>;
  /** The host has already authorized this mode and its data projection. */
  readonly mode: CodingSurfaceMode;
  /** Existing host presentation restriction; it never widens permissions. */
  readonly readOnly: boolean;
  readonly allowLearnerCreatedFiles: boolean;
  readonly graphicsPanelVisible: boolean;
}

export interface CodingRuntimeCapabilities {
  /** A program run never claims to evaluate a function contract. */
  readonly program: CodingLocalCapability;
  readonly tests: CodingLocalTestCapability;
  readonly interactiveInput: CodingLocalCapability;
  readonly graphics: CodingLocalCapability;
  readonly createFiles: CodingLocalCapability;
  /** Required when executing, not permission to boot on form navigation. */
  readonly graphicsTarget: { readonly required: boolean; readonly visible: boolean };
}

export interface CodingRuntimeCapabilityResolver {
  (request: CodingRuntimeCapabilityRequest): CodingRuntimeCapabilities;
}
