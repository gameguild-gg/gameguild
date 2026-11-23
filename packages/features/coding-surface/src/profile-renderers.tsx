import type {
  CodingAuthoringDefinition,
  CodingLearnerTest,
  CodingTest,
} from '@game-guild/coding';
import type {
  CodingFullProfileRendererProps,
  CodingLearnerProfileRendererProps,
} from './types';

export function FunctionAuthorProfile({ definition }: CodingFullProfileRendererProps) {
  return (
    <ProfileBand
      label="author function profile"
      title="Function contract"
      primary={functionSignature(definition.tests, definition.entryPoint.symbol)}
      secondary={fullTestSummary(definition, 'function')}
    />
  );
}

export function FunctionAuthorTestProfile({ definition }: CodingFullProfileRendererProps) {
  return (
    <ProfileBand
      label="author-test function profile"
      title="Function preview"
      primary={functionSignature(definition.tests, definition.entryPoint.symbol)}
      secondary={fullTestSummary(definition, 'function')}
    />
  );
}

export function FunctionLearnerProfile({ definition }: CodingLearnerProfileRendererProps) {
  return (
    <ProfileBand
      label="learner function profile"
      title="Function task"
      primary={functionSignature(definition.publicTests, definition.entryPoint.symbol)}
      secondary={`${countTests(definition.publicTests, 'function')} public function test group(s)`}
    />
  );
}

export function FunctionGraderProfile({ definition }: CodingFullProfileRendererProps) {
  return (
    <ProfileBand
      label="grader function profile"
      title="Function evidence"
      primary={functionSignature(definition.tests, definition.entryPoint.symbol)}
      secondary={fullTestSummary(definition, 'function')}
    />
  );
}

export function StdioAuthorProfile({ definition }: CodingFullProfileRendererProps) {
  return (
    <ProfileBand
      label="author standard-io profile"
      title="Input and output contract"
      primary={`Initial file: ${definition.entryPoint.path}`}
      secondary={fullTestSummary(definition, 'stdio')}
    />
  );
}

export function StdioAuthorTestProfile({ definition }: CodingFullProfileRendererProps) {
  return (
    <ProfileBand
      label="author-test standard-io profile"
      title="Input and output preview"
      primary={`Initial file: ${definition.entryPoint.path}`}
      secondary={fullTestSummary(definition, 'stdio')}
    />
  );
}

export function StdioLearnerProfile({ definition }: CodingLearnerProfileRendererProps) {
  return (
    <ProfileBand
      label="learner standard-io profile"
      title="Input and output task"
      primary={`Initial file: ${definition.entryPoint.path}`}
      secondary={`${countTests(definition.publicTests, 'stdio')} public input/output test(s)`}
    />
  );
}

export function StdioGraderProfile({ definition }: CodingFullProfileRendererProps) {
  return (
    <ProfileBand
      label="grader standard-io profile"
      title="Input and output evidence"
      primary={`Initial file: ${definition.entryPoint.path}`}
      secondary={fullTestSummary(definition, 'stdio')}
    />
  );
}

function ProfileBand({
  label,
  title,
  primary,
  secondary,
}: {
  readonly label: string;
  readonly title: string;
  readonly primary: string;
  readonly secondary: string;
}) {
  return (
    <aside
      aria-label={label}
      style={{
        display: 'grid',
        gap: 3,
        borderBottom: '1px solid rgb(63 63 70)',
        padding: '8px 12px',
      }}
    >
      <strong style={{ fontSize: 13 }}>{title}</strong>
      <code style={{ overflowWrap: 'anywhere', whiteSpace: 'normal' }}>{primary}</code>
      <span style={{ fontSize: 12, opacity: 0.75 }}>{secondary}</span>
    </aside>
  );
}

function functionSignature(
  tests: readonly (CodingTest | CodingLearnerTest)[],
  fallbackSymbol: string | undefined,
): string {
  const test = tests.find((candidate) => candidate.kind === 'function');
  if (!test) return fallbackSymbol ? `Function: ${fallbackSymbol}` : 'Function signature is not available in the visible cases.';
  const parameters = test.function.parameters
    .map((parameter) => `${parameter.name}: ${parameter.type}`)
    .join(', ');
  return `${test.function.name}(${parameters}): ${test.function.returnType}`;
}

function fullTestSummary(
  definition: CodingAuthoringDefinition,
  kind: 'function' | 'stdio',
): string {
  const matching = definition.tests.filter((test) => test.kind === kind);
  const publicCount = matching.filter((test) => test.visibility === 'public').length;
  const privateCount = matching.length - publicCount;
  return `${publicCount} public and ${privateCount} private test group(s)`;
}

function countTests(
  tests: readonly CodingLearnerTest[],
  kind: 'function' | 'stdio',
): number {
  return tests.filter((test) => test.kind === kind).length;
}
