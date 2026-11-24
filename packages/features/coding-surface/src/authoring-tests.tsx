import { Copy, Plus, Trash2 } from 'lucide-react';
import type { CodingDraftFunctionCase, CodingDraftFunctionTest, CodingDraftScalar, CodingDraftTest } from '@game-guild/coding';
import { AuthoringAction, AuthoringCheck, AuthoringField, AuthoringHeading, AuthoringSelect, type AuthoringViewProps } from './authoring-fields';

interface AuthoringTestsProps extends AuthoringViewProps {
  readonly selectedTestId: string | null;
  readonly selectedCaseId: string | null;
  readonly selectTest: (id: string) => void;
  readonly selectCase: (id: string) => void;
  readonly addTest: () => void;
  readonly duplicateTest: (test: CodingDraftTest) => void;
  readonly deleteTest: (test: CodingDraftTest) => void;
  readonly addFunctionCase: (test: CodingDraftFunctionTest) => void;
  readonly deleteFunctionCase: (test: CodingDraftFunctionTest, testCase: CodingDraftFunctionCase) => void;
}

export function CodingAuthoringTests(props: AuthoringTestsProps) {
  const { draft, issues, edit, disabled } = props;
  const index = draft.tests.findIndex((test) => test.id === props.selectedTestId);
  const test = draft.tests[index];
  const change = (operation: (test: CodingDraftTest) => void) =>
    edit((next) => {
      const current = next.tests[index];
      if (current) operation(current);
    });
  return (
    <>
      <AuthoringHeading title="Tests">
        <span className="cs-author-muted">
          {draft.tests.filter((entry) => entry.visibility === 'public').length} public / {draft.tests.filter((entry) => entry.visibility === 'private').length}{' '}
          private
        </span>
        <AuthoringAction label="Add test" icon={Plus} disabled={disabled || draft.tests.length >= 100} onClick={props.addTest} />
      </AuthoringHeading>
      <div className="cs-author-test-layout">
        <nav className="cs-author-test-list" aria-label="Test cases">
          {draft.tests.map((entry, position) => (
            <button type="button" key={entry.id} aria-pressed={entry.id === test?.id} onClick={() => props.selectTest(entry.id)}>
              <span>{entry.name || `Test ${position + 1}`}</span>
              <small>
                {entry.visibility === 'public' ? 'Public' : 'Private'}
                {entry.kind === 'function' ? ` / ${entry.cases.length} value cases` : ''}
              </small>
            </button>
          ))}
        </nav>
        <section className="cs-author-case-inspector" aria-label="Test case details">
          {test ? (
            <>
              <div className="cs-author-grid cs-author-test-metadata">
                <AuthoringField
                  label="Test name"
                  value={test.name}
                  disabled={disabled}
                  path={`tests.${index}.name`}
                  issues={issues}
                  onChange={(name) =>
                    change((current) => {
                      current.name = name;
                    })
                  }
                />
                <AuthoringSelect
                  label="Case visibility"
                  value={test.visibility}
                  disabled={disabled}
                  options={[
                    { value: 'public', label: 'Public sample' },
                    { value: 'private', label: 'Private test' },
                  ]}
                  onChange={(visibility) =>
                    change((current) => {
                      current.visibility = visibility;
                    })
                  }
                />
                <AuthoringField
                  label="Weight units"
                  value={test.weightUnits}
                  numeric
                  disabled={disabled}
                  path={`tests.${index}.weightUnits`}
                  issues={issues}
                  onChange={(weightUnits) =>
                    change((current) => {
                      current.weightUnits = weightUnits;
                    })
                  }
                />
              </div>
              {test.kind === 'stdio' ? (
                <>
                  <AuthoringField
                    label="Standard input"
                    value={test.stdin}
                    multiline
                    disabled={disabled}
                    onChange={(stdin) =>
                      change((current) => {
                        if (current.kind === 'stdio') current.stdin = stdin;
                      })
                    }
                  />
                  <AuthoringSelect
                    label="Output expectation"
                    value={test.expected.stdout === null ? 'none' : 'exact'}
                    disabled={disabled}
                    options={[
                      { value: 'exact', label: 'Compare exact output' },
                      { value: 'none', label: 'No expected output' },
                    ]}
                    onChange={(value) =>
                      change((current) => {
                        if (current.kind === 'stdio') current.expected.stdout = value === 'none' ? null : '';
                      })
                    }
                  />
                  {test.expected.stdout === null ? (
                    <p className="cs-author-field-error">A test requires expected stdout; empty output is allowed.</p>
                  ) : (
                    <AuthoringField
                      label="Expected stdout"
                      value={test.expected.stdout}
                      multiline
                      disabled={disabled}
                      path={`tests.${index}.expected.stdout`}
                      issues={issues}
                      onChange={(value) =>
                        change((current) => {
                          if (current.kind === 'stdio') current.expected.stdout = value;
                        })
                      }
                    />
                  )}
                  <AuthoringCheck
                    label="Compare stderr"
                    checked={test.expected.stderr !== null}
                    disabled={disabled}
                    onChange={(checked) =>
                      change((current) => {
                        if (current.kind === 'stdio') current.expected.stderr = checked ? '' : null;
                      })
                    }
                  />
                  {test.expected.stderr === null ? null : (
                    <AuthoringField
                      label="Expected stderr"
                      value={test.expected.stderr}
                      multiline
                      disabled={disabled}
                      onChange={(value) =>
                        change((current) => {
                          if (current.kind === 'stdio') current.expected.stderr = value;
                        })
                      }
                    />
                  )}
                  <AuthoringField
                    label="Expected exit code (optional)"
                    value={test.expected.exitCode}
                    numeric
                    disabled={disabled}
                    path={`tests.${index}.expected.exitCode`}
                    issues={issues}
                    onChange={(value) =>
                      change((current) => {
                        if (current.kind === 'stdio') current.expected.exitCode = value;
                      })
                    }
                  />
                </>
              ) : (
                <FunctionalCases {...props} test={test} testIndex={index} change={change} />
              )}
              <div className="cs-author-actions">
                <AuthoringAction
                  label="Duplicate test"
                  icon={Copy}
                  disabled={disabled || draft.tests.length >= 100}
                  onClick={() => props.duplicateTest(test)}
                />
                <AuthoringAction label="Delete test" icon={Trash2} disabled={disabled} onClick={() => props.deleteTest(test)} />
              </div>
            </>
          ) : (
            <p role="status">No test selected.</p>
          )}
        </section>
      </div>
    </>
  );
}

function FunctionalCases(
  props: AuthoringTestsProps & {
    readonly test: CodingDraftFunctionTest;
    readonly testIndex: number;
    readonly change: (operation: (test: CodingDraftTest) => void) => boolean;
  },
) {
  const { draft, issues, disabled, test, testIndex, change } = props;
  const caseIndex = test.cases.findIndex((entry) => entry.id === props.selectedCaseId);
  const testCase = test.cases[caseIndex];
  const signature = draft.functionSignature;
  const updateCase = (operation: (testCase: CodingDraftFunctionCase) => void) =>
    change((current) => {
      if (current.kind === 'function') {
        const value = current.cases[caseIndex];
        if (value) operation(value);
      }
    });
  return (
    <>
      <div className="cs-author-inline">
        <AuthoringSelect
          label="Function case"
          value={testCase?.id ?? ''}
          disabled={disabled}
          options={test.cases.map((entry, index) => ({ value: entry.id, label: `Value case ${index + 1}` }))}
          onChange={props.selectCase}
        />
        <AuthoringAction
          compact
          label="Add value case"
          icon={Plus}
          disabled={disabled || test.cases.length >= 100}
          onClick={() => props.addFunctionCase(test)}
        />
        {testCase ? (
          <AuthoringAction compact label="Delete value case" icon={Trash2} disabled={disabled} onClick={() => props.deleteFunctionCase(test, testCase)} />
        ) : null}
      </div>
      {signature && testCase ? (
        <>
          <h3>Arguments</h3>
          <div className="cs-author-grid">
            {signature.parameters.map((parameter, index) => {
              const scalar = testCase.inputs[index] ?? { type: parameter.type, text: '' };
              return (
                <ScalarField
                  key={`${index}:${parameter.name}`}
                  label={`${parameter.name || `Parameter ${index + 1}`} (${parameter.type})`}
                  scalar={scalar}
                  path={`tests.${testIndex}.cases.${caseIndex}.inputs.${index}`}
                  issues={issues}
                  disabled={disabled}
                  onChange={(value) =>
                    updateCase((current) => {
                      current.inputs[index] = { type: parameter.type, text: value };
                    })
                  }
                />
              );
            })}
          </div>
          <ScalarField
            label={`Expected result (${signature.returnType})`}
            scalar={testCase.expected ?? { type: signature.returnType, text: '' }}
            path={`tests.${testIndex}.cases.${caseIndex}.expected`}
            issues={issues}
            disabled={disabled}
            onChange={(value) =>
              updateCase((current) => {
                current.expected = { type: signature.returnType, text: value };
              })
            }
          />
        </>
      ) : (
        <p role="status">{signature ? 'No value cases.' : 'Apply a function signature before editing arguments.'}</p>
      )}
    </>
  );
}

function ScalarField({
  scalar,
  onChange,
  ...props
}: {
  readonly label: string;
  readonly scalar: CodingDraftScalar;
  readonly path: string;
  readonly issues: AuthoringViewProps['issues'];
  readonly disabled: boolean;
  readonly onChange: (value: string) => void;
}) {
  return scalar.type === 'boolean' ? (
    <AuthoringSelect
      label={props.label}
      value={scalar.text}
      disabled={props.disabled}
      options={[
        { value: '', label: 'Select a value' },
        { value: 'true', label: 'true' },
        { value: 'false', label: 'false' },
      ]}
      onChange={onChange}
    />
  ) : (
    <AuthoringField
      {...props}
      value={scalar.text}
      numeric={scalar.type === 'integer' || scalar.type === 'float'}
      multiline={scalar.type === 'string'}
      onChange={onChange}
    />
  );
}
