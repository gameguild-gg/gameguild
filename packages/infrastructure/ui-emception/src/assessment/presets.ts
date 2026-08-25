import type { ToolchainPreset, WorkspaceConfig } from 'emception';

import { ASSIGNMENT_SAMPLES, type CodingLanguage } from '../components/assignment-samples';

export { ASSIGNMENT_SAMPLES };
export type { AssignmentSample, CodingLanguage } from '../components/assignment-samples';

const TOOLCHAIN_BY_LANGUAGE: Record<CodingLanguage, ToolchainPreset> = {
  cpp: 'cpp' as ToolchainPreset,
  c: 'c' as ToolchainPreset,
  'sdl-cpp': 'sdl-cpp' as ToolchainPreset,
  'raylib-cpp': 'raylib-cpp' as ToolchainPreset,
  'allegro-cpp': 'allegro-cpp' as ToolchainPreset,
};

/**
 * Convert a legacy GameGuild sample descriptor into the public vanilla IDE
 * workspace contract. This is a data-only boundary: it owns no editor,
 * worker, VFS, compile, or test-execution behaviour.
 */
export function createAssessmentWorkspaceConfig(
  language: CodingLanguage,
  files: WorkspaceConfig['files'],
): WorkspaceConfig {
  const legacyConfig = (ASSIGNMENT_SAMPLES[language] ?? ASSIGNMENT_SAMPLES.cpp).workspaceConfig;
  const runType: WorkspaceConfig['run']['type'] = legacyConfig.run.type === 'sdl3-canvas'
    ? 'canvas'
    : legacyConfig.run.type;

  return {
    id: legacyConfig.id,
    label: legacyConfig.label,
    description: legacyConfig.description,
    version: legacyConfig.version,
    compile: {
      ...legacyConfig.compile,
      toolchain: TOOLCHAIN_BY_LANGUAGE[language],
    },
    run: {
      ...legacyConfig.run,
      type: runType,
    },
    test: legacyConfig.test,
    features: legacyConfig.features,
    files,
  };
}
