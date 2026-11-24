'use client';

import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from 'react';
import { BookOpen, Code2, FlaskConical, Settings2 } from 'lucide-react';
import {
  codingDraftOperations,
  parseCodingAuthoringDraft,
  type CodingAuthoringDraft,
  type CodingDraftChangePlan,
  type CodingAuthoringFile,
  type CodingDraftFunctionCase,
  type CodingDraftFunctionTest,
  type CodingDraftTest,
  type CodingFunctionSignature,
} from '@game-guild/coding';
import { CodingAuthoringEnvironment } from './authoring-environment';
import { AuthoringSelect } from './authoring-fields';
import { authoringIssues, blankDraftTest, blankFunctionCase, starterSource } from './authoring-model';
import { CodingAuthoringProblem } from './authoring-problem';
import { CodingAuthoringStarter } from './authoring-starter';
import { CodingAuthoringTests } from './authoring-tests';
import { CODING_AUTHORING_TASKS, type CodingAuthoringSurfaceProps, type CodingAuthoringTask } from './authoring-types';
import './authoring.css';

const TASKS = {
  problem: { label: 'Problem', icon: BookOpen },
  environment: { label: 'Environment', icon: Settings2 },
  starter: { label: 'Starter code', icon: Code2 },
  tests: { label: 'Tests', icon: FlaskConical },
};
interface ChangeReview {
  baseline: string;
  candidate: CodingAuthoringDraft;
  title: string;
  impacts: string[];
}
const emptySignature = (): CodingFunctionSignature => ({ name: '', parameters: [], returnType: 'integer' });

export function CodingAuthoringSurface(props: CodingAuthoringSurfaceProps) {
  if (!props.contextKey.trim()) throw new TypeError('Authoring requires an explicit context key.');
  return <ReadyAuthoringSurface key={props.contextKey} {...props} />;
}

function ReadyAuthoringSurface(props: CodingAuthoringSurfaceProps) {
  const draft = useMemo(() => parseCodingAuthoringDraft(props.draft), [props.draft]);
  const [task, setTask] = useState<CodingAuthoringTask>(props.initialTask ?? 'problem');
  const [selectedFileId, selectFile] = useState<string | null>(draft.files[0]?.id ?? null);
  const [selectedTestId, selectTest] = useState<string | null>(draft.tests[0]?.id ?? null);
  const [selectedCases, selectCases] = useState<Record<string, string>>({});
  const signatureDigest = JSON.stringify(draft.functionSignature);
  const [signatureBuffer, setSignatureBuffer] = useState({ baseline: signatureDigest, value: draft.functionSignature ?? emptySignature() });
  if (signatureBuffer.baseline !== signatureDigest) setSignatureBuffer({ baseline: signatureDigest, value: draft.functionSignature ?? emptySignature() });
  const [pending, setPending] = useState<ChangeReview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showIssues, setShowIssues] = useState(false);
  const issues = useMemo(() => authoringIssues(draft), [draft]);
  const id = useId();
  const disabled = props.disabled ?? false;
  const selectedFile = draft.files.find((file) => file.id === selectedFileId) ?? draft.files[0];
  const selectedTest = draft.tests.find((test) => test.id === selectedTestId) ?? draft.tests[0];
  const selectedCase =
    selectedTest?.kind === 'function' ? (selectedTest.cases.find((testCase) => testCase.id === selectedCases[selectedTest.id]) ?? selectedTest.cases[0]) : null;

  function attempt(operation: () => void): boolean {
    if (disabled) return false;
    try {
      operation();
      setError(null);
      return true;
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'The edit could not be applied.');
      return false;
    }
  }
  function edit(operation: (draft: CodingAuthoringDraft) => void): boolean {
    return attempt(() => {
      const next = structuredClone(draft);
      operation(next);
      props.onChange(parseCodingAuthoringDraft(next));
    });
  }
  function review(plan: CodingDraftChangePlan, title: string) {
    if (disabled) return;
    setPending({
      baseline: plan.baseline,
      candidate: plan.candidate,
      title,
      impacts: [
        `${plan.impact.removedTestIds.length} tests removed`,
        `${plan.impact.changedCaseIds.length} cases reconciled`,
        `${plan.impact.filesToReview.length} files to review`,
      ],
    });
  }
  function reviewCandidate(candidate: CodingAuthoringDraft, title: string, impacts: string[]) {
    if (!disabled) setPending({ baseline: JSON.stringify(draft), candidate: parseCodingAuthoringDraft(candidate), title, impacts });
  }
  function confirm() {
    const change = pending;
    if (!change) return;
    attempt(() => {
      if (JSON.stringify(draft) !== change.baseline) throw new TypeError('The exercise changed. Review the change against the current draft.');
      props.onChange(parseCodingAuthoringDraft(change.candidate));
    });
    setPending(null);
  }
  function addFile() {
    let identity: string | null = null;
    const applied = edit((next) => {
      identity = props.createId('file');
      const extension = next.environment.language === 'c' ? 'c' : 'cpp';
      let counter = next.files.length + 1;
      while (next.files.some((file) => file.path === `/home/user/source-${counter}.${extension}`)) counter++;
      const path = `/home/user/source-${counter}.${extension}`;
      next.files.push({ id: identity, path, encoding: 'text', content: '', visibility: 'public', mutable: true });
      if (!next.entryPoint.path) next.entryPoint.path = path;
    });
    if (applied && identity) selectFile(identity);
  }
  function generate() {
    attempt(() => {
      const candidate = structuredClone(draft);
      const source = starterSource(candidate);
      const file = candidate.files.find((entry) => entry.id === selectedFile?.id);
      if (file) {
        if (file.encoding !== 'text') throw new TypeError('Starter generation requires a text file.');
        file.content = source;
        reviewCandidate(candidate, 'Replace starter code', [`Replace source in ${file.path || '(unnamed file)'}`, 'Protected solutions remain unchanged']);
      } else {
        const identity = props.createId('file');
        const path = `/home/user/main.${draft.environment.language === 'c' ? 'c' : 'cpp'}`;
        candidate.files.push({ id: identity, path, encoding: 'text', content: source, visibility: 'public', mutable: true });
        candidate.entryPoint.path = path;
        props.onChange(parseCodingAuthoringDraft(candidate));
        selectFile(identity);
      }
    });
  }
  function deleteFile(file: CodingAuthoringFile) {
    const candidate = structuredClone(draft);
    candidate.files = candidate.files.filter((entry) => entry.id !== file.id);
    if (candidate.entryPoint.path === file.path) candidate.entryPoint.path = '';
    reviewCandidate(candidate, 'Delete file', [`Delete ${file.path || '(unnamed file)'}`, 'Linked solution data is retained and must be reviewed']);
  }
  function addTest() {
    let identity: string | null = null;
    const applied = edit((next) => {
      const test = blankDraftTest(next, props.createId);
      identity = test.id;
      next.tests.push(test);
    });
    if (applied && identity) selectTest(identity);
  }
  function duplicateTest(test: CodingDraftTest) {
    attempt(() => {
      const identity = props.createId('test');
      const candidate = codingDraftOperations.duplicateTest(
        draft,
        test.id,
        identity,
        test.kind === 'function' ? test.cases.map(() => props.createId('case')) : [],
      );
      props.onChange(candidate);
      selectTest(identity);
    });
  }
  function deleteTest(test: CodingDraftTest) {
    const candidate = structuredClone(draft);
    candidate.tests = candidate.tests.filter((entry) => entry.id !== test.id);
    reviewCandidate(candidate, 'Delete test', [
      `Delete ${test.name || 'unnamed test'}`,
      `${test.kind === 'function' ? test.cases.length : 1} value cases removed`,
    ]);
  }
  function addFunctionCase(test: CodingDraftFunctionTest) {
    let identity: string | null = null;
    const applied = edit((next) => {
      const current = next.tests.find((entry) => entry.id === test.id);
      if (current?.kind !== 'function') throw new TypeError('The selected test changed.');
      const testCase = blankFunctionCase(next.functionSignature, props.createId);
      identity = testCase.id;
      current.cases.push(testCase);
    });
    if (applied && identity) {
      const nextId = identity;
      selectCases((current) => ({ ...current, [test.id]: nextId }));
    }
  }
  function deleteFunctionCase(test: CodingDraftFunctionTest, testCase: CodingDraftFunctionCase) {
    const candidate = structuredClone(draft);
    const current = candidate.tests.find((entry) => entry.id === test.id);
    if (current?.kind === 'function') current.cases = current.cases.filter((entry) => entry.id !== testCase.id);
    reviewCandidate(candidate, 'Delete value case', ['Remove the selected function arguments and expected value']);
  }
  function navigate(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    const next =
      event.key === 'Home'
        ? 0
        : event.key === 'End'
          ? CODING_AUTHORING_TASKS.length - 1
          : event.key === 'ArrowRight'
            ? (index + 1) % CODING_AUTHORING_TASKS.length
            : event.key === 'ArrowLeft'
              ? (index + CODING_AUTHORING_TASKS.length - 1) % CODING_AUTHORING_TASKS.length
              : null;
    if (next === null) return;
    event.preventDefault();
    setTask(CODING_AUTHORING_TASKS[next]!);
    event.currentTarget.parentElement?.querySelectorAll<HTMLButtonElement>('[role="tab"]')[next]?.focus();
  }
  const view = { draft, edit, issues, disabled };
  return (
    <section className="cs-author" aria-label="Coding exercise authoring" data-authoring-context={props.contextKey}>
      <header className="cs-author-header">
        <strong>Coding exercise</strong>
        <AuthoringSelect
          label="Exercise type"
          value={draft.exerciseType}
          disabled={disabled}
          options={[
            { value: 'function', label: 'Implement a function' },
            { value: 'standard-io', label: 'Input and output' },
          ]}
          onChange={(exerciseType) => attempt(() => review(codingDraftOperations.planChange(draft, { kind: 'family', exerciseType }), 'Change exercise type'))}
        />
        <button type="button" className="cs-author-action" onClick={() => setShowIssues(true)}>
          Check fields
        </button>
      </header>
      <div role="tablist" className="cs-author-tasks" aria-label="Exercise authoring tasks">
        {CODING_AUTHORING_TASKS.map((key, index) => {
          const Icon = TASKS[key].icon;
          return (
            <button
              type="button"
              key={key}
              role="tab"
              id={`${id}-${key}`}
              aria-controls={`${id}-panel`}
              aria-selected={task === key}
              tabIndex={task === key ? 0 : -1}
              onClick={() => setTask(key)}
              onKeyDown={(event) => navigate(event, index)}
            >
              <Icon aria-hidden="true" size={16} />
              {TASKS[key].label}
            </button>
          );
        })}
      </div>
      <div className="cs-author-notices" aria-live="polite">
        {error ? <p role="alert">{error}</p> : null}
        {showIssues ? (
          <div role={issues.length ? 'alert' : 'status'}>
            {issues.length ? (
              <>
                <strong>Exercise fields need attention.</strong>
                <ul>
                  {issues.map((issue, index) => (
                    <li key={`${issue.path}:${index}`}>
                      {issue.path}: {issue.message}
                    </li>
                  ))}
                </ul>
              </>
            ) : (
              'Exercise fields are complete.'
            )}
          </div>
        ) : null}
      </div>
      <div role="tabpanel" id={`${id}-panel`} aria-labelledby={`${id}-${task}`} className="cs-author-panel">
        {task === 'problem' ? <CodingAuthoringProblem {...view} createId={props.createId} /> : null}
        {task === 'environment' ? <CodingAuthoringEnvironment {...view} review={review} /> : null}
        {task === 'starter' ? (
          <CodingAuthoringStarter
            {...view}
            contextKey={props.contextKey}
            selectedFileId={selectedFile?.id ?? null}
            selectFile={selectFile}
            signature={signatureBuffer.value}
            setSignature={(value) => {
              if (!disabled) setSignatureBuffer({ baseline: signatureDigest, value });
            }}
            applySignature={() =>
              attempt(() =>
                review(codingDraftOperations.planChange(draft, { kind: 'signature', signature: signatureBuffer.value }), 'Apply function signature'),
              )
            }
            addFile={addFile}
            generate={generate}
            deleteFile={deleteFile}
          />
        ) : null}
        {task === 'tests' ? (
          <CodingAuthoringTests
            {...view}
            selectedTestId={selectedTest?.id ?? null}
            selectedCaseId={selectedCase?.id ?? null}
            selectTest={selectTest}
            selectCase={(caseId) => {
              if (selectedTest) selectCases((current) => ({ ...current, [selectedTest.id]: caseId }));
            }}
            addTest={addTest}
            duplicateTest={duplicateTest}
            deleteTest={deleteTest}
            addFunctionCase={addFunctionCase}
            deleteFunctionCase={deleteFunctionCase}
          />
        ) : null}
      </div>
      {pending ? <ChangeReviewDialog change={pending} disabled={disabled} confirm={confirm} cancel={() => setPending(null)} /> : null}
    </section>
  );
}

function ChangeReviewDialog({
  change,
  disabled,
  confirm,
  cancel,
}: {
  readonly change: ChangeReview;
  readonly disabled: boolean;
  readonly confirm: () => void;
  readonly cancel: () => void;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const title = useId();
  useEffect(() => {
    const previous = document.activeElement;
    ref.current?.querySelector<HTMLButtonElement>('button')?.focus();
    return () => {
      if (!(previous instanceof HTMLElement) || !previous.isConnected) return;
      const disclosure = previous.closest('details');
      if (disclosure instanceof HTMLDetailsElement && !disclosure.open) disclosure.querySelector('summary')?.focus();
      else previous.focus();
    };
  }, []);
  return (
    <div className="cs-author-modal-backdrop">
      <div
        className="cs-author-modal"
        ref={ref}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title}
        onKeyDown={(event) => {
          if (event.key === 'Escape') {
            event.preventDefault();
            cancel();
          }
          if (event.key === 'Tab') {
            const buttons = ref.current?.querySelectorAll<HTMLButtonElement>('button:not(:disabled)');
            const first = buttons?.[0];
            const last = buttons?.[buttons.length - 1];
            if (event.shiftKey && document.activeElement === first) {
              event.preventDefault();
              last?.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
              event.preventDefault();
              first?.focus();
            }
          }
        }}
      >
        <h2 id={title}>{change.title}</h2>
        <ul>
          {change.impacts.map((impact) => (
            <li key={impact}>{impact}</li>
          ))}
        </ul>
        <div className="cs-author-actions">
          <button type="button" className="cs-author-action" onClick={cancel}>
            Cancel
          </button>
          <button type="button" className="cs-author-action cs-author-primary" disabled={disabled} onClick={confirm}>
            Confirm change
          </button>
        </div>
      </div>
    </div>
  );
}
