import type { CodingAuthoringDefinition, CodingLearnerDelivery } from '@game-guild/coding';
import { PRESETS, type WorkspaceConfig } from '@gameguild/emception-ide';

export type CodingWorkspaceLanguage = 'cpp' | 'c' | 'sdl-cpp' | 'raylib-cpp' | 'allegro-cpp';

const presetByLanguage: Record<CodingWorkspaceLanguage, string> = {
  cpp: 'cpp-terminal',
  c: 'c-terminal',
  'sdl-cpp': 'cpp-sdl3',
  'raylib-cpp': 'cpp-raylib',
  'allegro-cpp': 'cpp-allegro',
};

export interface CodingWorkspaceSample {
  readonly workspaceConfig: WorkspaceConfig;
}

export const CODING_WORKSPACE_SAMPLES: Readonly<Record<CodingWorkspaceLanguage, CodingWorkspaceSample>> = Object.freeze(
  Object.fromEntries(
    (Object.keys(presetByLanguage) as CodingWorkspaceLanguage[]).map((language) => {
      const preset = PRESETS[presetByLanguage[language]];
      if (!preset) throw new TypeError(`No public Emception preset is available for ${language}.`);
      return [language, Object.freeze({ workspaceConfig: structuredClone(preset) })];
    }),
  ) as Record<CodingWorkspaceLanguage, CodingWorkspaceSample>,
);

export function createCodingWorkspaceConfig(
  language: CodingWorkspaceLanguage,
  files: WorkspaceConfig['files'],
): WorkspaceConfig {
  const preset = PRESETS[presetByLanguage[language]] ?? PRESETS['cpp-terminal'];
  if (!preset) throw new TypeError(`No public Emception preset is available for ${language}.`);
  return {
    ...preset,
    id: `gameguild-coding-${language}`,
    label: `${language.toUpperCase()} coding exercise`,
    compile: {
      ...preset.compile,
      sourceDetect: preset.compile.sourceDetect ? { ...preset.compile.sourceDetect } : undefined,
    },
    run: { ...preset.run },
    features: { ...preset.features },
    files: structuredClone(files),
  };
}

export function workspaceConfigFromDefinition(
  definition: CodingAuthoringDefinition | CodingLearnerDelivery,
): WorkspaceConfig {
  const files = Object.fromEntries(
    definition.files.map((file) => [file.path, { encoding: file.encoding, content: file.content }]),
  );
  return createCodingWorkspaceConfig(languageFromToolchain(definition.toolchain.key), files);
}

function languageFromToolchain(key: string): CodingWorkspaceLanguage {
  if (key.includes('sdl')) return 'sdl-cpp';
  if (key.includes('raylib')) return 'raylib-cpp';
  if (key.includes('allegro')) return 'allegro-cpp';
  if (key === 'c' || key.startsWith('c-') || key.includes('c-clang')) return 'c';
  return 'cpp';
}
