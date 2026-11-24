import { Copy, Plus, Trash2 } from 'lucide-react';
import { useEffect, useId, useRef, useState, type KeyboardEvent, type ReactElement } from 'react';
import { codingLearnerPractice } from './learner-practice';
import type { CodingLearnerCasesProps, CodingLearnerPracticeIntent, CodingPersonalCase } from './learner-practice-types';
import './learner-practice.css';

export function CodingLearnerCases({ state, onChange, createId, disabled = false }: CodingLearnerCasesProps): ReactElement {
  const id = useId();
  const publicTab = useRef<HTMLButtonElement>(null);
  const personalTab = useRef<HTMLButtonElement>(null);
  const addButton = useRef<HTMLButtonElement>(null);
  const deleteButton = useRef<HTMLButtonElement>(null);
  const cancelButton = useRef<HTMLButtonElement>(null);
  const confirmButton = useRef<HTMLButtonElement>(null);
  const restoreFocus = useRef<'add' | 'delete' | null>(null);
  const [error, setError] = useState<{ contextKey: string; message: string } | null>(null);
  const cases = state.view === 'public' ? state.publicCases : state.personalCases;
  const selected = cases.find((value) => state.selection?.source === state.view && value.id === state.selection.id);
  const personal = selected?.source === 'personal' ? selected : null;
  const pending = state.personalCases.find((value) => value.id === state.pendingDeletion);
  const readOnly = disabled || !personal;

  useEffect(() => {
    if (state.pendingDeletion) cancelButton.current?.focus();
    else if (restoreFocus.current) {
      (restoreFocus.current === 'add' ? addButton : deleteButton).current?.focus();
      restoreFocus.current = null;
    }
  }, [state.pendingDeletion]);

  function dispatch(intent: CodingLearnerPracticeIntent): boolean {
    if (disabled && !['view', 'select', 'cancel-delete'].includes(intent.kind)) return false;
    try {
      const next = codingLearnerPractice.reduce(state, intent);
      onChange(next);
      setError(null);
      return true;
    } catch (cause) {
      setError({ contextKey: state.contextKey, message: cause instanceof Error ? cause.message : 'The personal case could not be updated.' });
      return false;
    }
  }

  function withNewId(kind: 'add' | 'duplicate') {
    if (disabled) return;
    try {
      const nextId = createId();
      if (kind === 'add') dispatch({ kind, id: nextId });
      else if (state.selection) dispatch({ kind, id: nextId, selection: state.selection });
    } catch (cause) {
      setError({ contextKey: state.contextKey, message: cause instanceof Error ? cause.message : 'A personal case identity could not be created.' });
    }
  }

  function replace(value: CodingPersonalCase) {
    if (!readOnly) dispatch({ kind: 'replace', value });
  }

  function selectTab(view: 'public' | 'personal', focus = false) {
    dispatch({ kind: 'view', view });
    if (focus) (view === 'public' ? publicTab : personalTab).current?.focus();
  }

  function tabKey(event: KeyboardEvent<HTMLButtonElement>) {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
    event.preventDefault();
    selectTab(event.key === 'Home' ? 'public' : event.key === 'End' ? 'personal' : state.view === 'public' ? 'personal' : 'public', true);
  }

  function cancelDelete() {
    restoreFocus.current = 'delete';
    dispatch({ kind: 'cancel-delete' });
  }

  function dialogKey(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.preventDefault();
      cancelDelete();
    } else if (event.key === 'Tab') {
      event.preventDefault();
      const target = disabled || event.currentTarget.ownerDocument.activeElement !== cancelButton.current ? cancelButton : confirmButton;
      target.current?.focus();
    }
  }

  const canAdd = !disabled && (state.exerciseType !== 'function' || state.signature !== null);
  return (
    <section className="cs-practice" aria-label="Practice cases">
      <div inert={Boolean(pending)}>
        <div className="cs-practice-toolbar">
          <div role="tablist" aria-label="Case sources" className="cs-practice-tabs">
            <button
              ref={publicTab}
              id={`${id}-public`}
              role="tab"
              type="button"
              aria-selected={state.view === 'public'}
              aria-controls={`${id}-panel`}
              tabIndex={state.view === 'public' ? 0 : -1}
              onKeyDown={tabKey}
              onClick={() => selectTab('public')}
            >
              Public tests
            </button>
            <button
              ref={personalTab}
              id={`${id}-personal`}
              role="tab"
              type="button"
              aria-selected={state.view === 'personal'}
              aria-controls={`${id}-panel`}
              tabIndex={state.view === 'personal' ? 0 : -1}
              onKeyDown={tabKey}
              onClick={() => selectTab('personal')}
            >
              My cases
            </button>
          </div>
          <div className="cs-practice-actions">
            <button ref={addButton} type="button" title="Add personal case" aria-label="Add personal case" disabled={!canAdd} onClick={() => withNewId('add')}>
              <Plus size={16} aria-hidden="true" />
            </button>
            {selected ? (
              <button
                type="button"
                title={personal ? 'Duplicate personal case' : 'Copy to my cases'}
                aria-label={personal ? 'Duplicate personal case' : 'Copy to my cases'}
                disabled={disabled}
                onClick={() => withNewId('duplicate')}
              >
                <Copy size={16} aria-hidden="true" />
              </button>
            ) : null}
            {personal ? (
              <button
                ref={deleteButton}
                type="button"
                title="Delete personal case"
                aria-label="Delete personal case"
                disabled={disabled}
                onClick={() => dispatch({ kind: 'request-delete', id: personal.id })}
              >
                <Trash2 size={16} aria-hidden="true" />
              </button>
            ) : null}
          </div>
        </div>
        {error?.contextKey === state.contextKey ? (
          <p role="alert" className="cs-practice-error">
            {error.message}
          </p>
        ) : null}
        <div id={`${id}-panel`} role="tabpanel" aria-labelledby={`${id}-${state.view}`} className="cs-practice-body">
          <nav aria-label={state.view === 'public' ? 'Public cases' : 'Personal cases'} className="cs-practice-list">
            {cases.length ? (
              cases.map((value, index) => (
                <button
                  type="button"
                  key={value.id}
                  aria-pressed={selected?.id === value.id}
                  onClick={() => dispatch({ kind: 'select', selection: { source: value.source, id: value.id } })}
                >
                  {value.name || `Case ${index + 1}`}
                </button>
              ))
            ) : (
              <p>{state.view === 'public' ? 'No public tests' : 'No personal cases'}</p>
            )}
          </nav>
          <section aria-label="Case details" className="cs-practice-details">
            {selected ? (
              <>
                <PracticeField
                  label="Case name"
                  value={selected.name}
                  readOnly={readOnly}
                  onChange={(name) => {
                    if (personal) replace({ ...personal, name });
                  }}
                />
                {selected.kind === 'stdio' ? (
                  <>
                    <PracticeField
                      label="Standard input"
                      value={selected.stdin}
                      multiline
                      readOnly={readOnly}
                      onChange={(stdin) => {
                        if (personal?.kind === 'stdio') replace({ ...personal, stdin });
                      }}
                    />
                    {(['stdout', 'stderr', 'exitCode'] as const).map((channel) => (
                      <div className="cs-practice-expectation" key={channel}>
                        <label className="cs-practice-check">
                          <input
                            type="checkbox"
                            checked={selected.expected[channel] !== null}
                            disabled={readOnly}
                            onChange={(event) => {
                              if (personal?.kind === 'stdio')
                                replace({ ...personal, expected: { ...personal.expected, [channel]: event.target.checked ? '' : null } });
                            }}
                          />
                          Compare {channel === 'exitCode' ? 'exit code' : channel}
                        </label>
                        {selected.expected[channel] !== null ? (
                          <PracticeField
                            label={`Expected ${channel === 'exitCode' ? 'exit code' : channel}`}
                            value={selected.expected[channel]}
                            multiline={channel !== 'exitCode'}
                            readOnly={readOnly}
                            onChange={(text) => {
                              if (personal?.kind === 'stdio') replace({ ...personal, expected: { ...personal.expected, [channel]: text } });
                            }}
                          />
                        ) : null}
                      </div>
                    ))}
                  </>
                ) : (
                  <>
                    {selected.inputs.map((input, index) => (
                      <PracticeField
                        key={index}
                        label={`${state.signature?.parameters[index].name} (${input.type})`}
                        value={input.text}
                        readOnly={readOnly}
                        multiline={input.type === 'string'}
                        onChange={(text) => {
                          if (personal?.kind === 'function')
                            replace({ ...personal, inputs: personal.inputs.map((value, position) => (position === index ? { ...value, text } : value)) });
                        }}
                      />
                    ))}
                    <div className="cs-practice-expectation">
                      <label className="cs-practice-check">
                        <input
                          type="checkbox"
                          checked={selected.expected !== null}
                          disabled={readOnly}
                          onChange={(event) => {
                            if (personal?.kind === 'function' && state.signature)
                              replace({ ...personal, expected: event.target.checked ? { type: state.signature.returnType, text: '' } : null });
                          }}
                        />
                        Compare return value
                      </label>
                      {selected.expected ? (
                        <PracticeField
                          label={`Expected result (${selected.expected.type})`}
                          value={selected.expected.text}
                          readOnly={readOnly}
                          multiline={selected.expected.type === 'string'}
                          onChange={(text) => {
                            if (personal?.kind === 'function' && personal.expected) replace({ ...personal, expected: { ...personal.expected, text } });
                          }}
                        />
                      ) : null}
                    </div>
                  </>
                )}
              </>
            ) : null}
          </section>
        </div>
      </div>
      {pending ? (
        <div className="cs-practice-modal">
          <div
            role="alertdialog"
            aria-modal="true"
            aria-labelledby={`${id}-delete-title`}
            aria-describedby={`${id}-delete-case`}
            className="cs-practice-dialog"
            onKeyDown={dialogKey}
          >
            <h3 id={`${id}-delete-title`}>Delete personal case?</h3>
            <p id={`${id}-delete-case`}>{pending.name}</p>
            <div className="cs-practice-actions">
              <button ref={cancelButton} type="button" onClick={cancelDelete}>
                Cancel
              </button>
              <button
                ref={confirmButton}
                type="button"
                disabled={disabled}
                onClick={() => {
                  restoreFocus.current = state.personalCases.length === 1 ? 'add' : 'delete';
                  if (!dispatch({ kind: 'confirm-delete' })) restoreFocus.current = null;
                }}
              >
                <Trash2 size={16} aria-hidden="true" />
                Delete case
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  );
}

function PracticeField({
  label,
  value,
  onChange,
  readOnly,
  multiline = false,
}: {
  readonly label: string;
  readonly value: string;
  readonly onChange: (value: string) => void;
  readonly readOnly: boolean;
  readonly multiline?: boolean;
}) {
  const id = useId();
  const props = {
    id,
    value,
    readOnly,
    onChange: (event: { target: { value: string } }) => {
      if (!readOnly) onChange(event.target.value);
    },
  };
  return (
    <div className="cs-practice-field">
      <label htmlFor={id}>{label}</label>
      {multiline ? <textarea {...props} rows={3} /> : <input {...props} type="text" />}
    </div>
  );
}
