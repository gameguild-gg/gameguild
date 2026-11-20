export type Family = 'function' | 'standard-io';
export type ScalarKind = 'integer' | 'float' | 'boolean' | 'string';
export type Task = 'problem' | 'environment' | 'starter' | 'tests' | 'validate';
export type Region = 'problem' | 'code' | 'cases' | 'results' | 'graphics' | 'terminal';
export type EnvironmentId = 'cpp-wasi' | 'c-wasi' | 'cpp-sdl3' | 'cpp-raylib';
export type Visibility = 'public' | 'private';
export type SimulationMode = 'normal' | 'compile' | 'timeout' | 'executor' | 'slow';
export type SaveMode = 'normal' | 'failed' | 'conflict';

export interface Parameter {
  id: string;
  name: string;
  type: ScalarKind;
}

export interface FunctionSignature {
  name: string;
  parameters: Parameter[];
  returns: ScalarKind;
}

export interface SourceFile {
  id: string;
  path: string;
  content: string;
  editable: boolean;
  role: 'starter' | 'support';
}

export interface EditorPosition {
  start: number;
  end: number;
  top: number;
  left: number;
}

export interface EditorSession {
  openFiles: string[];
  positions: Record<string, EditorPosition>;
  baseline: Record<string, string>;
  explorer: boolean;
}

export interface FileDialog {
  kind: 'create' | 'rename';
  fileId: string | null;
  path: string;
  error: string;
}

export interface CaseBase {
  id: string;
  name: string;
  visibility: Visibility;
}

export interface FunctionCase extends CaseBase {
  kind: 'function';
  inputs: Record<string, string>;
  expected: string;
}

export interface IoCase extends CaseBase {
  kind: 'stdio';
  stdin: string;
  expected: { mode: 'none' | 'value'; stdout: string };
}

export interface DraftBase {
  id: string;
  revision: number;
  nextId: number;
  title: string;
  statement: string;
  constraints: string;
  environment: EnvironmentId;
  graphics: boolean;
  terminal: boolean;
  files: SourceFile[];
  protected: { reference: string; incorrect: string };
}

export type AuthorDraft = DraftBase &
  ({ family: 'function'; signature: FunctionSignature; cases: FunctionCase[] } | { family: 'standard-io'; cases: IoCase[] });

export interface Delivery {
  candidate: string;
  family: Family;
  title: string;
  statement: string;
  constraints: string;
  environment: EnvironmentId;
  graphics: boolean;
  terminal: boolean;
  signature: string;
  parameters: Parameter[];
  returns: ScalarKind | null;
  files: SourceFile[];
  samples: (FunctionCase | IoCase)[];
}

export interface Diagnostic {
  caseId: string;
  name: string;
  input: string;
  expected: string | null;
  actual: string;
  status: 'passed' | 'mismatch' | 'observed';
}

export type RunState =
  | { status: 'idle' }
  | { status: 'running'; token: number; candidate: string }
  | { status: 'completed'; candidate: string; results: Diagnostic[] }
  | { status: 'cancelled'; candidate: string }
  | { status: 'stale'; candidate: string }
  | { status: 'error'; candidate: string; reason: 'compile' | 'timeout' | 'executor' | 'unrecorded' | 'input'; message: string };

export interface Workspace {
  delivery: Delivery;
  files: SourceFile[];
  selectedFile: string;
  editor: EditorSession;
  selectedSample: string | null;
  scratch: string;
  scratchInputs: Record<string, string>;
  scratchExpected: string | null;
  version: number;
  inputVersion: number;
  run: RunState;
  submission: null | { readonly id: string; readonly candidate: string; readonly files: readonly Readonly<SourceFile>[]; readonly status: 'acknowledged' };
  feedback: 'pending' | 'released';
}

export type SaveState =
  { status: 'unsaved' } | { status: 'saving'; revision: number } | { status: 'saved'; revision: number } | { status: 'failed' | 'conflict'; message: string };

export type PendingChange =
  | { kind: 'family'; value: Family }
  | { kind: 'environment'; value: EnvironmentId }
  | { kind: 'signature'; value: FunctionSignature }
  | { kind: 'starter'; value: string }
  | { kind: 'example'; value: 'maximum' | 'echo' | 'stress' | 'blank' }
  | { kind: 'delete-file'; value: string }
  | { kind: 'reset'; value: 'learner' };

export interface Layout {
  task: Task;
  region: Region;
  selectedCase: string | null;
  selectedFile: string;
  curriculum: boolean;
  settings: boolean;
  split: number;
  focused: boolean;
  studyControls: boolean;
}

export interface PresentationState {
  draft: AuthorDraft;
  layout: Layout;
  persona: 'author' | 'learner' | 'preview';
  preview: Workspace | null;
  learner: Workspace | null;
  validation: { reference: RunState; incorrect: RunState };
  simulation: SimulationMode;
  save: SaveState;
  pendingChange: PendingChange | null;
  fileDialog: FileDialog | null;
  authorEditor: EditorSession;
  signatureEditor: FunctionSignature | null;
  saveMode: SaveMode;
  recoveryAvailable: boolean;
  graphicsSpecimen: { playing: boolean; position: number };
  terminalSpecimen: { input: string; lines: string[] };
  example: 'maximum' | 'echo' | 'stress' | 'blank';
}

export type PresentationEvent =
  | { type: 'select-task'; task: Task }
  | { type: 'select-region'; region: Region }
  | { type: 'edit-draft'; draft: AuthorDraft }
  | { type: 'request-change'; change: PendingChange }
  | { type: 'confirm-change' | 'cancel-change' | 'preview' | 'close-preview' | 'save' | 'recover' | 'cancel-run' | 'submit' }
  | { type: 'edit-source'; source: string }
  | { type: 'open-file' | 'close-file'; fileId: string }
  | { type: 'toggle-explorer' }
  | { type: 'run'; target: 'samples' | 'scratch' | 'reference' | 'incorrect' };

export interface Environment {
  id: EnvironmentId;
  name: string;
  language: 'cpp' | 'c';
  families: readonly Family[];
  graphics: boolean;
  terminal: boolean;
}

export interface FixtureOutcome {
  caseId: string;
  input: string;
  expected: string;
  correct: string;
  incorrect: string;
}

export interface Fixture {
  id: 'maximum' | 'echo' | 'stress';
  draft: AuthorDraft;
  correct: string;
  incorrect: string;
  outcomes: FixtureOutcome[];
}

export interface PrototypeSchemas {
  environments: readonly Environment[];
  isDraft(value: unknown): value is AuthorDraft;
  isRun(value: unknown): value is RunState;
  isSave(value: unknown): value is SaveState;
  isEvent(value: unknown): value is PresentationEvent;
  parseDraft(value: unknown): AuthorDraft;
}

export interface PrototypeFixtures {
  blank(family: Family): AuthorDraft;
  example(id: Fixture['id']): Fixture;
}

export interface PrototypeApp {
  state: PresentationState;
  dispatch(event: PresentationEvent): void;
}

export interface DraftIssue {
  task: Task;
  field: string;
  message: string;
}

export interface PrototypeModel {
  signature(value: FunctionSignature): string;
  scalarError(kind: ScalarKind, raw: string): string | null;
  issues(draft: AuthorDraft): DraftIssue[];
  addCase(draft: AuthorDraft): string;
  duplicateCase(draft: AuthorDraft, id: string): string;
  applySignature(draft: AuthorDraft, signature: FunctionSignature): void;
}

export interface PrototypeHtml {
  escape(value: string): string;
  field(label: string, id: string, value: string, type?: string): string;
  area(label: string, id: string, value: string, className?: string, readOnly?: boolean): string;
  select(label: string, id: string, value: string, options: readonly { value: string; label: string; disabled?: boolean }[]): string;
  check(label: string, id: string, checked: boolean, disabled?: boolean): string;
  button(label: string, action: string, disabled?: boolean): string;
}

export interface PrototypeAuthorView {
  panel(state: PresentationState): string;
}

export interface PrototypeSimulation {
  project(draft: AuthorDraft): Delivery;
  workspace(delivery: Delivery): Workspace;
  outcome(delivery: Delivery, source: string, mode: SimulationMode, cases: readonly (FunctionCase | IoCase)[], oracle?: boolean): RunState;
  input(testCase: FunctionCase | IoCase): string;
}

export interface PrototypeLearnerView {
  render(state: PresentationState, workspace: Workspace): string;
}

export interface RecoveryScope {
  tenant: string;
  actor: string;
  exercise: string;
}

export interface AuthorRecovery {
  draft: AuthorDraft;
  signatureEditor: FunctionSignature | null;
}

export interface PrototypeRecovery {
  create(): {
    put(scope: RecoveryScope, value: AuthorRecovery): void;
    get(scope: RecoveryScope): AuthorRecovery | null;
  };
}

export interface CodeEditorOptions {
  label: string;
  field: string;
  content: string;
  path: string;
  readOnly: boolean;
  fileId: string;
  scope: 'author' | 'learner' | 'preview' | 'protected';
  language: 'c' | 'cpp';
}

export interface CodeWorkbenchOptions {
  scope: 'author' | 'learner' | 'preview';
  files: SourceFile[];
  selectedFile: string;
  session: EditorSession;
  language: 'c' | 'cpp';
}

export interface PrototypeCodeView {
  session(files: readonly SourceFile[]): EditorSession;
  pathError(path: string, files: readonly SourceFile[], exceptId?: string): string | null;
  editor(options: CodeEditorOptions): string;
  render(options: CodeWorkbenchOptions): string;
  capture(root: HTMLElement, state: PresentationState): void;
  restore(root: HTMLElement, state: PresentationState): void;
  sync(input: HTMLTextAreaElement): void;
}

declare global {
  interface Window {
    codingPrototypeSchemas: PrototypeSchemas;
    codingPrototypeFixtures: PrototypeFixtures;
    codingPrototypeApp: PrototypeApp;
    codingPrototypeModel: PrototypeModel;
    codingPrototypeHtml: PrototypeHtml;
    codingPrototypeAuthorView: PrototypeAuthorView;
    codingPrototypeSimulation: PrototypeSimulation;
    codingPrototypeLearnerView: PrototypeLearnerView;
    codingPrototypeRecovery: PrototypeRecovery;
    codingPrototypeCodeView: PrototypeCodeView;
  }
}
