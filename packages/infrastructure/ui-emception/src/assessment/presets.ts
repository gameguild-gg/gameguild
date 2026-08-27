import type { ToolchainPreset, WorkspaceConfig } from 'emception';

import { ASSIGNMENT_SAMPLES, type CodingLanguage } from './samples';
import { normalizeAssessmentWorkspacePath } from './paths';

export { ASSIGNMENT_SAMPLES };
export type { AssignmentSample, CodingLanguage } from './samples';

const TOOLCHAIN_BY_LANGUAGE: Record<CodingLanguage, ToolchainPreset> = {
  cpp: 'cpp' as ToolchainPreset,
  c: 'c' as ToolchainPreset,
  'sdl-cpp': 'sdl-cpp' as ToolchainPreset,
  'raylib-cpp': 'raylib-cpp' as ToolchainPreset,
  'allegro-cpp': 'allegro-cpp' as ToolchainPreset,
};

/**
 * Applies a GameGuild language template to the public vanilla IDE workspace
 * contract. This is a data-only boundary: it owns no editor, worker, VFS,
 * compile, or test-execution behaviour.
 */
export function createAssessmentWorkspaceConfig(
  language: CodingLanguage,
  files: WorkspaceConfig['files'],
): WorkspaceConfig {
  const template = (ASSIGNMENT_SAMPLES[language] ?? ASSIGNMENT_SAMPLES.cpp).workspaceConfig;
  const sourceDetect = template.compile.sourceDetect;
  const normalizedFiles = Object.fromEntries(
    Object.entries(files).map(([path, file]) => [normalizeAssessmentWorkspacePath(path), file]),
  );

  return {
    id: template.id,
    label: template.label,
    description: template.description,
    version: template.version,
    compile: {
      ...template.compile,
      toolchain: TOOLCHAIN_BY_LANGUAGE[language],
      ...(sourceDetect?.entryPoint
        ? {
            sourceDetect: {
              ...sourceDetect,
              entryPoint: normalizeAssessmentWorkspacePath(sourceDetect.entryPoint),
            },
          }
        : {}),
    },
    run: {
      ...template.run,
    },
    test: template.test,
    features: template.features,
    files: normalizedFiles,
  };
}
