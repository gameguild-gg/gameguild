export { default as Ide } from './components/Ide';
export type { IdeProps, IdeHandle } from './components/Ide';
export { default as TestResultsPanel } from './components/TestResultsPanel';
export type { TestResultsPanelProps, TestReport, TestCaseResult } from './components/TestResultsPanel';
export { parseWorkspaceBundle, resolveArgs, workspaceConfigToState } from './components/ide-types';
export type {
  BundleFile,
  CompileConfig,
  LayoutConfig,
  LayoutTabConfig,
  RunConfig,
  RunType,
  TestConfig,
  WorkspaceConfig,
  WorkspaceFeatures,
} from './components/ide-types';
export { CMAKE_PRESET, CPP_SDL3_PRESET, CPP_TERMINAL_PRESET, DEFAULT_PRESET, PRESETS, PRESET_IDS, PYTHON_PRESET } from './components/workspace-presets';
export { ASSIGNMENT_SAMPLES } from './components/assignment-samples';
export type { AssignmentSample, CodingLanguage } from './components/assignment-samples';
export type { GradingCase, GradingPlan } from './components/ide-types';
