(() => {
  /** @typedef {import('./contracts').Task} Task */
  /** @typedef {import('./contracts').ScalarKind} ScalarKind */
  const h = window.codingPrototypeHtml;
  const model = window.codingPrototypeModel;
  const schemas = window.codingPrototypeSchemas;
  const fixtures = window.codingPrototypeFixtures;
  const simulation = window.codingPrototypeSimulation;
  const code = window.codingPrototypeCodeView;
  const saved = window.codingPrototypeRecovery.create();
  const recovery = window.codingPrototypeRecovery.create();
  const scope = { tenant: 'design-tenant', actor: 'instructor-1', exercise: 'design-exercise' };
  let returnFocusId = '';
  /** @type {number | undefined} */
  let saveTimer;
  let operation = 0;
  let draftRevision = 0;
  /** @type {number | undefined} */
  let timer;
  /** @returns {import('./contracts').Workspace | null} */
  const currentWorkspace = () => (state.persona === 'preview' ? state.preview : state.persona === 'learner' ? state.learner : null);
  function stopRuns() {
    window.clearTimeout(timer);
    operation++;
    const workspace = currentWorkspace();
    if (workspace?.run.status === 'running') workspace.run = { status: 'cancelled', candidate: workspace.run.candidate };
    for (const target of /** @type {const} */ (['reference', 'incorrect'])) {
      const run = state.validation[target];
      if (run.status === 'running') state.validation[target] = { status: 'cancelled', candidate: run.candidate };
    }
  }
  /** @param {import('./contracts').Workspace} workspace @param {'source' | 'input'} kind */
  function workspaceChanged(workspace, kind = 'source') {
    if (kind === 'source') workspace.version++;
    else workspace.inputVersion++;
    if (workspace.run.status !== 'idle') workspace.run = { status: 'stale', candidate: workspace.run.candidate };
  }
  /** @param {'samples' | 'scratch'} target */
  function runLearner(target) {
    const workspace = currentWorkspace();
    if (!workspace) return;
    stopRuns();
    const candidate = `${workspace.delivery.candidate}:edit${workspace.version}:input${workspace.inputVersion}`;
    /** @type {(import('./contracts').FunctionCase | import('./contracts').IoCase)[]} */
    let selected = workspace.delivery.samples;
    if (target === 'scratch') {
      if (workspace.delivery.family === 'function') {
        for (const parameter of workspace.delivery.parameters) {
          const error = model.scalarError(parameter.type, workspace.scratchInputs[parameter.id] ?? '');
          if (error) {
            workspace.run = { status: 'error', candidate, reason: 'input', message: `${parameter.name} ${error}.` };
            state.layout.region = 'results';
            render();
            return;
          }
        }
        selected = [
          {
            kind: 'function',
            id: 'scratch',
            name: 'Personal custom case',
            visibility: 'public',
            inputs: structuredClone(workspace.scratchInputs),
            expected: workspace.scratchExpected ?? '',
          },
        ];
      } else
        selected = [
          {
            kind: 'stdio',
            id: 'scratch',
            name: 'Personal custom case',
            visibility: 'public',
            stdin: workspace.scratch,
            expected: { mode: workspace.scratchExpected === null ? 'none' : 'value', stdout: workspace.scratchExpected ?? '' },
          },
        ];
    }
    const source = workspace.files.find((entry) => entry.role === 'starter')?.content ?? '';
    const result = simulation.outcome(
      { ...workspace.delivery, files: structuredClone(workspace.files), candidate },
      source,
      state.simulation,
      structuredClone(selected),
      target === 'samples' || workspace.scratchExpected !== null,
    );
    const token = ++operation;
    workspace.run = { status: 'running', candidate, token };
    state.layout.region = 'results';
    timer = window.setTimeout(
      () => {
        if (
          workspace.run.status === 'running' &&
          workspace.run.token === token &&
          `${workspace.delivery.candidate}:edit${workspace.version}:input${workspace.inputVersion}` === candidate
        ) {
          workspace.run = result;
          render();
        }
      },
      state.simulation === 'slow' ? 1500 : 100,
    );
    render();
  }
  /** @type {import('./contracts').PresentationState} */
  const state = {
    draft: fixtures.blank('function'),
    layout: {
      task: 'problem',
      region: 'code',
      selectedCase: null,
      selectedFile: 'file-1',
      curriculum: false,
      settings: false,
      split: 42,
      focused: false,
      studyControls: !window.matchMedia('(max-width: 700px)').matches,
    },
    persona: 'author',
    preview: null,
    learner: null,
    validation: { reference: { status: 'idle' }, incorrect: { status: 'idle' } },
    simulation: 'normal',
    save: { status: 'unsaved' },
    pendingChange: null,
    fileDialog: null,
    authorEditor: { openFiles: ['file-1'], positions: {}, baseline: { 'file-1': '' }, explorer: false },
    signatureEditor: { name: '', parameters: [], returns: 'integer' },
    saveMode: 'normal',
    recoveryAvailable: false,
    graphicsSpecimen: { playing: true, position: 40 },
    terminalSpecimen: { input: '', lines: [] },
    example: 'blank',
  };
  /** @type {readonly { id: Task; label: string }[]} */
  const tasks = [
    { id: 'problem', label: 'Problem' },
    { id: 'environment', label: 'Environment' },
    { id: 'starter', label: 'Starter code' },
    { id: 'tests', label: 'Tests' },
    { id: 'validate', label: 'Validate exercise' },
  ];
  const app = document.getElementById('app');
  if (!app) throw new Error('Prototype entry requires #app.');

  function changed() {
    state.draft.revision = ++draftRevision;
    state.save = { status: 'unsaved' };
    for (const target of ['reference', 'incorrect']) {
      if (target !== 'reference' && target !== 'incorrect') continue;
      const result = state.validation[target];
      if (result.status !== 'idle') state.validation[target] = { status: 'stale', candidate: result.candidate };
    }
    if (!schemas.isDraft(state.draft)) throw new Error('A presentation edit violated the draft contract.');
    recovery.put(scope, { draft: state.draft, signatureEditor: state.signatureEditor });
  }

  function render() {
    if (!app) return;
    code.capture(app, state);
    const focused = document.activeElement;
    const focusId = focused?.id;
    const selection =
      focused instanceof HTMLTextAreaElement || (focused instanceof HTMLInputElement && focused.type === 'text')
        ? [focused.selectionStart, focused.selectionEnd]
        : null;
    const scrolls = [...app.querySelectorAll('[data-scroll]')].map((element) => ({
      key: element.getAttribute('data-scroll'),
      top: element.scrollTop,
      left: element.scrollLeft,
    }));
    const disclosures = [...app.querySelectorAll('details[data-disclosure]')].map((element) => ({
      key: element.getAttribute('data-disclosure'),
      open: element instanceof HTMLDetailsElement && element.open,
    }));
    const workspace = state.persona === 'preview' ? state.preview : state.learner;
    const body = state.persona === 'author' ? authorMarkup() : workspace ? window.codingPrototypeLearnerView.render(state, workspace) : '';
    const panels = `grid-template-columns: ${state.layout.curriculum ? '256px ' : ''}minmax(0, 1fr)${state.layout.settings ? ' 320px' : ''}`;
    app.innerHTML = `${studyControls()}<div class="host-controls">${h.check('Curriculum panel', 'curriculum-toggle', state.layout.curriculum)}${h.check('Settings panel', 'settings-toggle', state.layout.settings)}</div><div class="host" style="${panels}">${state.layout.curriculum ? `<aside class="sidebar curriculum"><h2>Curriculum</h2><p>Programming fundamentals</p><div class="current-lesson">${h.escape(state.draft.title || 'Untitled exercise')}</div></aside>` : ''}<main class="content"><header class="exercise-header"><h1>${h.escape(state.draft.title || 'Untitled exercise')}</h1>${state.persona === 'author' ? `<span class="status" role="status">${saveLabel()}</span>${h.button('Save draft', 'save', state.save.status === 'saving')}` : h.button('Return to authoring', 'return-author')}</header>${body}</main>${state.layout.settings ? '<aside class="sidebar settings"><h2>Lesson settings</h2><p>Enrolled students</p><hr><p>Required for completion</p><p class="muted">Coding assignment</p></aside>' : ''}</div>${pendingMarkup()}`;
    const canvas = document.getElementById('graphics-canvas');
    document.getElementById('study-toggle')?.setAttribute('aria-expanded', String(state.layout.studyControls));
    if (canvas instanceof HTMLCanvasElement) {
      const context = canvas.getContext('2d');
      if (context) {
        context.fillStyle = '#202728';
        context.fillRect(0, 0, 640, 320);
        context.fillStyle = '#67c6a4';
        context.fillRect(state.graphicsSpecimen.position, 100, 72, 72);
        context.fillStyle = '#e5e7e8';
        context.font = '18px monospace';
        context.fillText('Synthetic output', 24, 280);
      }
    }
    for (const scroll of scrolls) {
      const element = [...app.querySelectorAll('[data-scroll]')].find((entry) => entry.getAttribute('data-scroll') === scroll.key);
      if (element) {
        element.scrollTop = scroll.top;
        element.scrollLeft = scroll.left;
      }
    }
    for (const previous of disclosures) {
      const detail = [...app.querySelectorAll('details[data-disclosure]')].find((entry) => entry.getAttribute('data-disclosure') === previous.key);
      if (detail instanceof HTMLDetailsElement) detail.open = previous.open;
    }
    code.restore(app, state);
    const caseList = app.querySelector('.case-list');
    const selectedCase = document.getElementById(`case-nav-${state.layout.selectedCase}`);
    if (caseList instanceof HTMLElement && selectedCase && caseList.clientHeight > 0) {
      const bounds = caseList.getBoundingClientRect();
      const selected = selectedCase.getBoundingClientRect();
      if (selected.top < bounds.top) caseList.scrollTop += selected.top - bounds.top - 4;
      else if (selected.bottom > bounds.bottom) caseList.scrollTop += selected.bottom - bounds.bottom + 4;
    }
    const modal = app.querySelector('dialog');
    if (modal instanceof HTMLDialogElement) {
      modal.showModal();
      const target = focusId ? document.getElementById(focusId) : null;
      if (target && modal.contains(target)) {
        target.focus();
        if (selection && target instanceof HTMLInputElement) target.setSelectionRange(selection[0] ?? 0, selection[1] ?? 0);
      } else if (state.fileDialog) modal.querySelector('input')?.focus();
      else modal.querySelector('button')?.focus();
      return;
    }
    if (focusId) {
      const target = document.getElementById(focusId);
      target?.focus({ preventScroll: true });
      if (selection && (target instanceof HTMLTextAreaElement || (target instanceof HTMLInputElement && target.type === 'text')))
        target.setSelectionRange(selection[0] ?? 0, selection[1] ?? 0);
    }
  }
  function authorMarkup() {
    const issues = model.issues(state.draft);
    if (state.draft.family === 'function' && JSON.stringify(state.signatureEditor) !== JSON.stringify(state.draft.signature))
      issues.push({ task: 'starter', field: 'function-name', message: 'Apply the pending signature changes.' });
    return `<section class="author-shell" aria-label="Exercise authoring"><nav class="tabs" role="tablist" aria-label="Authoring tasks">${tasks.map((task) => `<button type="button" role="tab" id="tab-${task.id}" aria-controls="${task.id}" aria-selected="${task.id === state.layout.task}" tabindex="${task.id === state.layout.task ? 0 : -1}" data-task="${task.id}">${task.label}</button>`).join('')}</nav><details class="readiness"><summary>${issues.length ? `${issues.length} fields need attention` : 'Exercise configuration complete'}</summary>${issues.map((issue) => `<button type="button" data-task="${issue.task}">${h.escape(issue.message)}</button>`).join('')}</details>${state.recoveryAvailable ? `<div class="recovery-notice">A local draft can be recovered. ${h.button('Recover local draft', 'recover')}</div>` : ''}${tasks.map((task) => `<section class="task" data-scroll="task-${task.id}" role="tabpanel" id="${task.id}" aria-labelledby="tab-${task.id}" ${task.id === state.layout.task ? '' : 'hidden'}>${task.id === state.layout.task ? window.codingPrototypeAuthorView.panel(state) : ''}</section>`).join('')}</section>`;
  }
  function saveLabel() {
    switch (state.save.status) {
      case 'unsaved':
        return 'Unsaved';
      case 'saving':
        return 'Saving...';
      case 'saved':
        return 'Saved (simulation)';
      case 'failed':
        return 'Save failed - draft retained';
      case 'conflict':
        return 'Version conflict - draft retained';
    }
  }
  function studyControls() {
    const workspace = currentWorkspace();
    const teacher = `${h.select('Design example', 'example', state.example === 'blank' ? '' : state.example, [
      { value: '', label: 'Blank exercise' },
      { value: 'maximum', label: 'Function example' },
      { value: 'echo', label: 'Input/output example' },
      { value: 'stress', label: '20-case example' },
    ])}${h.button('Learner', 'open-learner')}${h.select('Save response', 'save-response', state.saveMode, [
      { value: 'normal', label: 'Acknowledge save' },
      { value: 'failed', label: 'Save failure' },
      { value: 'conflict', label: 'Version conflict' },
    ])}${h.button('Simulate reload', 'simulate-reload')}${h.button('Reopen saved draft', 'reopen', !saved.get(scope))}`;
    const student = `${h.select('Recorded source', 'recorded-source', '', [
      { value: '', label: 'Current code' },
      { value: 'correct', label: 'Recorded correct code' },
      { value: 'incorrect', label: 'Recorded incorrect code' },
    ])}${
      workspace
        ? h.select('Host feedback example', 'host-feedback', workspace.feedback, [
            { value: 'pending', label: 'Pending' },
            { value: 'released', label: 'Released' },
          ])
        : ''
    }`;
    return `<div class="study-controls" ${state.layout.studyControls ? '' : 'hidden'}>${state.persona === 'author' ? teacher : student}${h.select(
      'Simulation mode',
      'simulation-mode',
      state.simulation,
      [
        { value: 'normal', label: 'Normal recording' },
        { value: 'compile', label: 'Compilation error' },
        { value: 'timeout', label: 'Time limit' },
        { value: 'executor', label: 'Execution failure' },
        { value: 'slow', label: 'Delayed response' },
      ],
    )}</div>`;
  }
  function pendingMarkup() {
    const dialog = state.fileDialog;
    if (dialog)
      return `<dialog aria-labelledby="file-heading"><h2 id="file-heading">${dialog.kind === 'create' ? 'New file' : 'Rename file'}</h2><div class="field">${h.field('File path', 'dialog-file-path', dialog.path)}</div>${dialog.error ? `<p role="alert" class="errors">${h.escape(dialog.error)}</p>` : ''}<div class="actions">${h.button('Cancel', 'cancel-file')}${h.button(dialog.kind === 'create' ? 'Create file' : 'Rename', 'confirm-file')}</div></dialog>`;
    const change = state.pendingChange;
    if (!change) return '';
    const message =
      change.kind === 'family'
        ? `Changing exercise type removes ${state.draft.cases.length} cases and the signature. Problem text and source files are preserved.`
        : change.kind === 'signature'
          ? 'Apply the signature to every case. Removed or differently typed arguments and return values will need new values. Source files are preserved.'
          : change.kind === 'starter'
            ? 'Replace the selected source with a generated starter?'
            : change.kind === 'delete-file'
              ? `Delete ${state.draft.files.find((file) => file.id === change.value)?.path ?? 'this file'} from the exercise? This removes its current source, not just the open tab.`
              : change.kind === 'example'
                ? 'Replace the current draft with a different design example? Your current local recovery copy remains available.'
                : change.kind === 'reset'
                  ? 'Restore supplied starter files? Custom cases and the submitted snapshot are preserved.'
                  : 'Switch execution environment and reset incompatible output capabilities. Source files and cases are preserved.';
    return `<dialog aria-labelledby="change-heading"><h2 id="change-heading">Confirm change</h2><p>${message}</p><div class="actions">${h.button('Cancel change', 'cancel-change')}${h.button('Confirm change', 'confirm-change')}</div></dialog>`;
  }
  function saveDraft() {
    window.clearTimeout(saveTimer);
    const record = { draft: structuredClone(state.draft), signatureEditor: structuredClone(state.signatureEditor) };
    const response = state.saveMode;
    state.save = { status: 'saving', revision: record.draft.revision };
    saveTimer = window.setTimeout(() => {
      if (response !== 'normal')
        state.save = { status: response, message: response === 'failed' ? 'Save failed; local draft retained.' : 'Version conflict; local draft retained.' };
      else {
        saved.put(scope, record);
        state.authorEditor.baseline = Object.fromEntries(record.draft.files.map((file) => [file.id, file.content]));
        state.save = state.draft.revision === record.draft.revision ? { status: 'saved', revision: record.draft.revision } : { status: 'unsaved' };
      }
      render();
    }, 100);
    render();
  }
  /** @param {import('./contracts').AuthorRecovery} record */
  function restoreAuthor(record) {
    stopRuns();
    window.clearTimeout(saveTimer);
    state.draft = schemas.parseDraft(record.draft);
    draftRevision = Math.max(draftRevision, state.draft.revision);
    state.signatureEditor = structuredClone(record.signatureEditor);
    state.layout.selectedCase = state.draft.cases[0]?.id ?? null;
    state.layout.selectedFile = state.draft.files[0]?.id ?? '';
    state.authorEditor = code.session(state.draft.files);
    state.fileDialog = null;
    state.layout.task = 'problem';
    state.validation = { reference: { status: 'idle' }, incorrect: { status: 'idle' } };
    state.recoveryAvailable = false;
    state.save = { status: 'unsaved' };
  }
  /** @param {'reference' | 'incorrect'} target */
  function runPreparation(target) {
    if (state.persona !== 'author') throw new Error('Exercise preparation unavailable in learner context.');
    if (
      model.issues(state.draft).length ||
      (state.draft.family === 'function' && JSON.stringify(state.signatureEditor) !== JSON.stringify(state.draft.signature))
    )
      throw new Error('Exercise preparation requires a coherent candidate.');
    stopRuns();
    const delivery = simulation.project(state.draft);
    const token = ++operation;
    const result = simulation.outcome(delivery, state.draft.protected[target], state.simulation, structuredClone(state.draft.cases));
    state.validation[target] = { status: 'running', candidate: delivery.candidate, token };
    timer = window.setTimeout(
      () => {
        const current = state.validation[target];
        if (current.status === 'running' && current.token === token && simulation.project(state.draft).candidate === delivery.candidate) {
          state.validation[target] = result;
          render();
        }
      },
      state.simulation === 'slow' ? 1500 : 100,
    );
    render();
  }

  /** @param {import('./contracts').PendingChange} change */
  function requestChange(change) {
    returnFocusId = document.activeElement?.id ?? '';
    state.pendingChange = structuredClone(change);
  }
  function confirmChange() {
    const change = state.pendingChange;
    if (!change) return;
    if (change.kind === 'family') {
      const previous = state.draft;
      const next = fixtures.blank(change.value);
      state.draft = {
        ...next,
        id: previous.id,
        revision: previous.revision,
        nextId: previous.nextId,
        title: previous.title,
        statement: previous.statement,
        constraints: previous.constraints,
        files: previous.files,
        protected: previous.protected,
      };
      state.signatureEditor = state.draft.family === 'function' ? structuredClone(state.draft.signature) : null;
      state.layout.selectedCase = null;
    } else if (change.kind === 'environment') {
      const environment = schemas.environments.find((entry) => entry.id === change.value && entry.families.includes(state.draft.family));
      if (!environment) throw new Error('Unsupported exercise/environment combination.');
      state.draft.environment = environment.id;
      state.draft.graphics = environment.graphics;
      state.draft.terminal = false;
    } else if (change.kind === 'signature') model.applySignature(state.draft, change.value);
    else if (change.kind === 'delete-file') {
      if (state.persona !== 'author' || state.draft.files.length <= 1 || !state.draft.files.some((file) => file.id === change.value))
        throw new Error('File deletion unavailable.');
      state.draft.files = state.draft.files.filter((file) => file.id !== change.value);
      state.authorEditor.openFiles = state.authorEditor.openFiles.filter((id) => id !== change.value);
      delete state.authorEditor.positions[change.value];
      delete state.authorEditor.baseline[change.value];
      const selected = state.authorEditor.openFiles.at(-1) ?? state.draft.files[0]?.id ?? '';
      if (!state.authorEditor.openFiles.includes(selected)) state.authorEditor.openFiles.push(selected);
      state.layout.selectedFile = selected;
    } else if (change.kind === 'starter') generateStarter();
    else if (change.kind === 'example') loadExample(change.value);
    else if (change.kind === 'reset') {
      const workspace = currentWorkspace();
      if (workspace) {
        workspace.files = structuredClone(workspace.delivery.files);
        workspace.editor = code.session(workspace.files);
        workspace.selectedFile = workspace.files[0]?.id ?? '';
        workspaceChanged(workspace);
      }
    }
    state.pendingChange = null;
    if (change.kind !== 'reset' && change.kind !== 'example') changed();
  }
  /** @param {'maximum' | 'echo' | 'stress' | 'blank'} value */
  function loadExample(value) {
    stopRuns();
    const draft = value === 'blank' ? fixtures.blank('function') : fixtures.example(value).draft;
    draft.revision = ++draftRevision;
    restoreAuthor({ draft, signatureEditor: draft.family === 'function' ? draft.signature : null });
    state.example = value;
  }
  function generateStarter() {
    const file = state.draft.files.find((entry) => entry.id === state.layout.selectedFile);
    if (!file) return;
    file.content =
      state.draft.family === 'function'
        ? `${state.draft.signature.returns === 'string' ? '#include <string>\n' : ''}${model.signature(state.draft.signature)} {\n  // Your solution\n}\n`
        : '#include <iostream>\nint main() {\n  // Read standard input and write standard output.\n}\n';
  }
  /** @type {import('./contracts').PrototypeApp['dispatch']} */
  function dispatch(event) {
    if (!schemas.isEvent(event)) throw new Error('Invalid presentation event.');
    if (app) code.capture(app, state);
    if (event.type === 'select-task') state.layout.task = event.task;
    else if (event.type === 'open-file' || event.type === 'close-file' || event.type === 'toggle-explorer') {
      const workspace = currentWorkspace();
      const editor = workspace?.editor ?? state.authorEditor;
      const files = workspace?.files ?? state.draft.files;
      if (event.type === 'toggle-explorer') editor.explorer = !editor.explorer;
      else {
        if (!files.some((file) => file.id === event.fileId)) throw new Error('Unknown or unavailable file.');
        if (event.type === 'open-file') {
          if (!editor.openFiles.includes(event.fileId)) editor.openFiles.push(event.fileId);
          if (workspace) workspace.selectedFile = event.fileId;
          else state.layout.selectedFile = event.fileId;
        } else {
          if (!editor.openFiles.includes(event.fileId)) throw new Error('File tab is not open.');
          const index = editor.openFiles.indexOf(event.fileId);
          editor.openFiles.splice(index, 1);
          const selected = workspace?.selectedFile ?? state.layout.selectedFile;
          if (selected === event.fileId) {
            const next = editor.openFiles[Math.min(index, editor.openFiles.length - 1)] ?? '';
            if (workspace) workspace.selectedFile = next;
            else state.layout.selectedFile = next;
          }
        }
      }
    } else if (event.type === 'request-change') requestChange(event.change);
    else if (event.type === 'cancel-change') state.pendingChange = null;
    else if (event.type === 'confirm-change') confirmChange();
    else if (event.type === 'edit-draft') {
      if (state.persona !== 'author' || event.draft.id !== scope.exercise) throw new Error('Authoring command unavailable in this context.');
      state.draft = structuredClone(event.draft);
      changed();
    } else if (event.type === 'preview') {
      stopRuns();
      state.preview = simulation.workspace(simulation.project(state.draft));
      state.persona = 'preview';
      if (window.matchMedia('(max-width: 700px)').matches) state.layout.studyControls = false;
    } else if (event.type === 'close-preview') {
      stopRuns();
      state.persona = 'author';
    } else if (event.type === 'select-region') {
      const workspace = currentWorkspace();
      if ((event.region === 'graphics' && !workspace?.delivery.graphics) || (event.region === 'terminal' && !workspace?.delivery.terminal))
        throw new Error('Selected capability is unavailable.');
      state.layout.region = event.region;
    } else if (event.type === 'run') {
      if (event.target === 'reference' || event.target === 'incorrect') runPreparation(event.target);
      else runLearner(event.target);
      return;
    } else if (event.type === 'save') {
      if (state.persona !== 'author') throw new Error('Authoring command unavailable.');
      saveDraft();
      return;
    } else if (event.type === 'recover') {
      if (state.persona !== 'author') throw new Error('Authoring command unavailable.');
      const record = recovery.get(scope);
      if (record) restoreAuthor(record);
    } else if (event.type === 'edit-source') {
      const workspace = currentWorkspace();
      const file = workspace?.files.find((entry) => entry.id === workspace.selectedFile);
      if (!file?.editable || !workspace) throw new Error('Selected file is read-only or unavailable.');
      file.content = event.source;
      workspaceChanged(workspace);
    } else if (event.type === 'cancel-run') stopRuns();
    else if (event.type === 'submit') {
      const workspace = currentWorkspace();
      if (workspace && state.persona === 'learner' && workspace.run.status !== 'running') {
        const candidate = `${workspace.delivery.candidate}:edit${workspace.version}`;
        if (workspace.submission?.candidate !== candidate) {
          workspace.submission = Object.freeze({
            id: `simulated-submission-${++operation}`,
            candidate,
            files: Object.freeze(workspace.files.map((file) => Object.freeze(structuredClone(file)))),
            status: 'acknowledged',
          });
          workspace.feedback = 'pending';
        }
      }
    }
    render();
    if (event.type === 'open-file' || event.type === 'close-file') {
      const selected = currentWorkspace()?.selectedFile ?? state.layout.selectedFile;
      (document.getElementById(`source-tab-${selected}`) ?? document.getElementById('command-toggle-explorer'))?.focus({ preventScroll: true });
    }
    if ((event.type === 'confirm-change' || event.type === 'cancel-change') && returnFocusId)
      document.getElementById(returnFocusId)?.focus({ preventScroll: true });
  }
  /** @param {string} value @returns {value is ScalarKind} */
  const scalarKind = (value) => value === 'integer' || value === 'float' || value === 'boolean' || value === 'string';

  /** @param {string} id @param {string} value @param {boolean} checked */
  function editField(id, value, checked) {
    const draft = state.draft;
    const file = draft.files.find((entry) => entry.id === state.layout.selectedFile);
    const selected = draft.cases.find((entry) => entry.id === state.layout.selectedCase);
    const workspace = currentWorkspace();
    if (id === 'dialog-file-path' && state.fileDialog) {
      state.fileDialog.path = value;
      state.fileDialog.error = '';
      render();
      return;
    }
    if (id === 'curriculum-toggle' || id === 'settings-toggle') {
      state.layout[id === 'curriculum-toggle' ? 'curriculum' : 'settings'] = checked;
      render();
      return;
    }
    if (id === 'problem-width') {
      state.layout.split = Math.max(25, Math.min(65, Number(value)));
      render();
      return;
    }
    if (id === 'selected-case') {
      state.layout.selectedCase = value;
      render();
      return;
    }
    if (id === 'save-response' && (value === 'normal' || value === 'failed' || value === 'conflict')) {
      state.saveMode = value;
      render();
      return;
    }
    if (id === 'simulation-mode' && (value === 'normal' || value === 'compile' || value === 'timeout' || value === 'executor' || value === 'slow')) {
      state.simulation = value;
      render();
      return;
    }
    if (workspace) {
      if (id === 'terminal-input') {
        state.terminalSpecimen.input = value;
        render();
        return;
      }
      const source = workspace.files.find((entry) => entry.id === workspace.selectedFile);
      if (id === 'host-feedback' && (value === 'pending' || value === 'released')) {
        workspace.feedback = value;
        render();
        return;
      }
      const sourceChange = id === 'learner-source' || id === 'recorded-source';
      if (sourceChange && !source?.editable) return;
      if (id === 'learner-source' && source) source.content = value;
      else if (id === 'recorded-source' && source && (value === 'correct' || value === 'incorrect'))
        source.content = fixtures.example(workspace.delivery.family === 'function' ? 'maximum' : 'echo')[value];
      else if (id === 'scratch') workspace.scratch = value;
      else if (id === 'scratch-oracle') workspace.scratchExpected = checked ? '' : null;
      else if (id === 'scratch-expected') workspace.scratchExpected = value;
      else if (id.startsWith('scratch-')) workspace.scratchInputs[id.slice('scratch-'.length)] = value;
      else return;
      workspaceChanged(workspace, sourceChange ? 'source' : 'input');
      render();
      return;
    }
    if (id === 'example') {
      const example = value === 'maximum' || value === 'echo' || value === 'stress' ? value : 'blank';
      if (state.draft.title || state.draft.cases.length || state.draft.files.some((entry) => entry.content)) requestChange({ kind: 'example', value: example });
      else loadExample(example);
      render();
      return;
    } else if (id === 'family' && (value === 'function' || value === 'standard-io')) requestChange({ kind: 'family', value });
    else if (id === 'execution-environment') {
      const environment = schemas.environments.find((entry) => entry.id === value);
      if (environment) requestChange({ kind: 'environment', value: environment.id });
    } else if (id === 'language') requestChange({ kind: 'environment', value: value === 'c' ? 'c-wasi' : 'cpp-wasi' });
    else if (id === 'title' || id === 'statement' || id === 'constraints') draft[id] = value;
    else if (id === 'graphics' || id === 'terminal') draft[id] = checked;
    else if (id === 'function-name' && state.signatureEditor) state.signatureEditor.name = value;
    else if (id === 'return-type' && state.signatureEditor && scalarKind(value)) state.signatureEditor.returns = value;
    else if (id.startsWith('param-name-') && state.signatureEditor) {
      const parameter = state.signatureEditor.parameters.find((entry) => entry.id === id.slice('param-name-'.length));
      if (parameter) parameter.name = value;
    } else if (id.startsWith('param-type-') && state.signatureEditor && scalarKind(value)) {
      const parameter = state.signatureEditor.parameters.find((entry) => entry.id === id.slice('param-type-'.length));
      if (parameter) parameter.type = value;
    } else if (id === 'file-role' && file && (value === 'starter' || value === 'support')) file.role = value;
    else if (id === 'file-editable' && file) file.editable = checked;
    else if (id === 'source' && file) file.content = value;
    else if (id === 'case-name' && selected) selected.name = value;
    else if (id === 'case-visibility' && selected && (value === 'public' || value === 'private')) selected.visibility = value;
    else if (id.startsWith('argument-') && selected?.kind === 'function') selected.inputs[id.slice('argument-'.length)] = value;
    else if (id === 'expected' && selected?.kind === 'function') selected.expected = value;
    else if (id === 'stdin' && selected?.kind === 'stdio') selected.stdin = value;
    else if (id === 'expectation' && selected?.kind === 'stdio' && (value === 'value' || value === 'none')) selected.expected.mode = value;
    else if (id === 'stdout' && selected?.kind === 'stdio') selected.expected.stdout = value;
    else if (id === 'reference-source') draft.protected.reference = value;
    else if (id === 'incorrect-source') draft.protected.incorrect = value;
    if (state.pendingChange) returnFocusId = id;
    else changed();
    render();
  }

  /** @param {string} action */
  function action(action) {
    const draft = state.draft;
    if (action === 'toggle-explorer') {
      dispatch({ type: 'toggle-explorer' });
      return;
    }
    if (action === 'add-file' || action === 'rename-file') {
      if (state.persona !== 'author') throw new Error('File changes require author context.');
      const file = draft.files.find((entry) => entry.id === state.layout.selectedFile);
      if (action === 'rename-file' && !file) throw new Error('Select a file to rename.');
      returnFocusId = document.activeElement?.id ?? '';
      state.fileDialog = {
        kind: action === 'add-file' ? 'create' : 'rename',
        fileId: action === 'rename-file' ? (file?.id ?? null) : null,
        path: action === 'rename-file' ? (file?.path ?? '') : '',
        error: '',
      };
      render();
      return;
    }
    if (action === 'cancel-file') {
      state.fileDialog = null;
      render();
      document.getElementById(returnFocusId)?.focus({ preventScroll: true });
      return;
    }
    if (action === 'confirm-file' && state.fileDialog) {
      if (state.persona !== 'author') throw new Error('File changes require author context.');
      const dialog = state.fileDialog;
      const error = code.pathError(dialog.path, draft.files, dialog.fileId ?? undefined);
      if (error) {
        dialog.error = error;
        render();
        return;
      }
      let id = dialog.fileId;
      if (dialog.kind === 'create') {
        id = `file-${draft.nextId++}`;
        draft.files.push({ id, path: dialog.path, content: '', editable: true, role: 'support' });
      } else {
        const file = draft.files.find((entry) => entry.id === id);
        if (!file) throw new Error('Selected file is unavailable.');
        file.path = dialog.path;
      }
      if (!id) throw new Error('File operation requires an identity.');
      state.layout.selectedFile = id;
      if (!state.authorEditor.openFiles.includes(id)) state.authorEditor.openFiles.push(id);
      state.fileDialog = null;
      changed();
      render();
      document.getElementById('source')?.focus();
      return;
    }
    if (action === 'run-reference' || action === 'run-incorrect') {
      runPreparation(action === 'run-reference' ? 'reference' : 'incorrect');
      return;
    }
    if (action === 'open-preview') {
      dispatch({ type: 'preview' });
      return;
    }
    if (action === 'return-author') {
      dispatch({ type: 'close-preview' });
      return;
    }
    if (action === 'save') {
      saveDraft();
      return;
    }
    if (action === 'simulate-reload') {
      stopRuns();
      window.clearTimeout(saveTimer);
      const record = saved.get(scope);
      restoreAuthor(
        record ?? {
          draft: fixtures.blank(state.draft.family),
          signatureEditor: state.draft.family === 'function' ? { name: '', parameters: [], returns: 'integer' } : null,
        },
      );
      state.recoveryAvailable = recovery.get(scope) !== null;
      render();
      return;
    }
    if (action === 'recover' || action === 'reopen') {
      const record = (action === 'recover' ? recovery : saved).get(scope);
      if (record) restoreAuthor(record);
      if (action === 'reopen' && record) state.save = { status: 'saved', revision: record.draft.revision };
      render();
      return;
    }
    if (action === 'focus-view') {
      state.layout.focused = !state.layout.focused;
      render();
      return;
    }
    if (action === 'move-shape') {
      if (state.graphicsSpecimen.playing) state.graphicsSpecimen.position = (state.graphicsSpecimen.position + 40) % 500;
      render();
      return;
    }
    if (action === 'toggle-canvas') {
      state.graphicsSpecimen.playing = !state.graphicsSpecimen.playing;
      render();
      return;
    }
    if (action === 'focus-canvas') {
      state.layout.focused = true;
      state.layout.region = 'graphics';
      render();
      document.querySelector('.output-zone')?.scrollTo(0, 0);
      document.getElementById('graphics-canvas')?.focus({ preventScroll: true });
      return;
    }
    if (action === 'send-terminal') {
      const input = state.terminalSpecimen.input.slice(0, 4096);
      state.terminalSpecimen.lines.push(`> ${input}`, input);
      state.terminalSpecimen.lines = state.terminalSpecimen.lines.slice(-20);
      state.terminalSpecimen.input = '';
      render();
      return;
    }
    if (action === 'reset-learner') {
      requestChange({ kind: 'reset', value: 'learner' });
      render();
      return;
    }
    if (action === 'open-learner') {
      stopRuns();
      const delivery = simulation.project(state.draft);
      if (state.learner?.delivery.candidate !== delivery.candidate) state.learner = simulation.workspace(delivery);
      state.persona = 'learner';
      if (window.matchMedia('(max-width: 700px)').matches) state.layout.studyControls = false;
      render();
      return;
    }
    if (action === 'run-samples' || action === 'run-scratch') {
      runLearner(action === 'run-samples' ? 'samples' : 'scratch');
      return;
    }
    if (action === 'cancel-run' || action === 'submit') {
      dispatch({ type: action });
      return;
    }
    if (action === 'add-param' && state.signatureEditor) state.signatureEditor.parameters.push({ id: `param-${draft.nextId++}`, name: '', type: 'integer' });
    else if (action.startsWith('remove-param:') && state.signatureEditor)
      state.signatureEditor.parameters = state.signatureEditor.parameters.filter((entry) => entry.id !== action.slice('remove-param:'.length));
    else if (action === 'apply-signature' && state.signatureEditor) {
      if (draft.cases.length) requestChange({ kind: 'signature', value: state.signatureEditor });
      else model.applySignature(draft, state.signatureEditor);
    } else if (action === 'generate-starter') {
      const file = draft.files.find((entry) => entry.id === state.layout.selectedFile);
      if (file?.content) requestChange({ kind: 'starter', value: file.id });
      else generateStarter();
    } else if (action === 'delete-file') {
      requestChange({ kind: 'delete-file', value: state.layout.selectedFile });
    } else if (action === 'add-case') state.layout.selectedCase = model.addCase(draft);
    else if (action === 'duplicate-case' && state.layout.selectedCase) state.layout.selectedCase = model.duplicateCase(draft, state.layout.selectedCase);
    else if (action === 'delete-case') {
      if (draft.family === 'function') draft.cases = draft.cases.filter((entry) => entry.id !== state.layout.selectedCase);
      else draft.cases = draft.cases.filter((entry) => entry.id !== state.layout.selectedCase);
      state.layout.selectedCase = draft.cases[0]?.id ?? null;
    } else if (action === 'confirm-change' || action === 'cancel-change') {
      dispatch({ type: action });
      return;
    }
    if (!state.pendingChange) changed();
    render();
  }

  app.addEventListener('click', (event) => {
    if (!(event.target instanceof Element)) return;
    const button = event.target.closest('button');
    const task = tasks.find((entry) => entry.id === button?.getAttribute('data-task'));
    if (task) {
      dispatch({ type: 'select-task', task: task.id });
      return;
    }
    const caseId = button?.getAttribute('data-case') ?? button?.getAttribute('data-sample');
    if (caseId) {
      state.layout.task = 'tests';
      state.layout.selectedCase = caseId;
      render();
      return;
    }
    const fileId = button?.getAttribute('data-editor-open');
    if (fileId) {
      const explorer = button?.closest('.source-explorer');
      if (explorer && getComputedStyle(explorer).position === 'absolute') (currentWorkspace()?.editor ?? state.authorEditor).explorer = false;
      dispatch({ type: 'open-file', fileId });
      return;
    }
    const closedFile = button?.getAttribute('data-editor-close');
    if (closedFile) {
      dispatch({ type: 'close-file', fileId: closedFile });
      return;
    }
    const command = button?.getAttribute('data-action');
    const region = button?.getAttribute('data-region');
    if (region === 'problem' || region === 'code' || region === 'cases' || region === 'results' || region === 'graphics' || region === 'terminal') {
      dispatch({ type: 'select-region', region });
      return;
    }
    const workspace = currentWorkspace();
    const sample = button?.getAttribute('data-learner-case');
    if (sample && workspace) {
      workspace.selectedSample = sample;
      render();
      return;
    }
    const learnerFile = button?.getAttribute('data-learner-file');
    if (learnerFile && workspace) {
      workspace.selectedFile = learnerFile;
      render();
      return;
    }
    if (command) action(command);
  });
  app.addEventListener('input', (event) => {
    const target = event.target;
    if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement)
      editField(target.id, target.value, target instanceof HTMLInputElement && target.checked);
  });
  app.addEventListener('keydown', (event) => {
    if (state.fileDialog && event.key === 'Enter' && event.target instanceof HTMLInputElement) {
      event.preventDefault();
      action('confirm-file');
      return;
    }
    if (state.fileDialog && event.key === 'Escape') {
      event.preventDefault();
      action('cancel-file');
      return;
    }
    if (state.pendingChange && event.key === 'Escape') {
      event.preventDefault();
      dispatch({ type: 'cancel-change' });
      return;
    }
    const target = event.target;
    if (target instanceof HTMLTextAreaElement && target.hasAttribute('data-code-file')) {
      if (event.key === 'Escape') target.dataset.tabExit = 'true';
      else if (event.key === 'Tab' && !event.shiftKey && !target.readOnly && target.dataset.tabExit !== 'true') {
        event.preventDefault();
        target.setRangeText('  ', target.selectionStart, target.selectionEnd, 'end');
        editField(target.id, target.value, false);
      } else if (event.key === 'Tab') delete target.dataset.tabExit;
      return;
    }
    if (!(target instanceof HTMLElement) || target.getAttribute('role') !== 'tab') return;
    if (!['Home', 'End', 'ArrowLeft', 'ArrowRight'].includes(event.key)) return;
    if (target.hasAttribute('data-editor-open')) {
      const workspace = currentWorkspace();
      const tabs = (workspace?.editor ?? state.authorEditor).openFiles;
      const index = tabs.indexOf(target.getAttribute('data-editor-open') ?? '');
      const next =
        event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
      const fileId = tabs[next];
      if (fileId) {
        event.preventDefault();
        dispatch({ type: 'open-file', fileId });
        document.getElementById(`source-tab-${fileId}`)?.focus();
      }
      return;
    }
    const index = tasks.findIndex((entry) => entry.id === target.getAttribute('data-task'));
    const next =
      event.key === 'Home' ? 0 : event.key === 'End' ? tasks.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tasks.length) % tasks.length;
    const task = tasks[next];
    if (!task) return;
    event.preventDefault();
    dispatch({ type: 'select-task', task: task.id });
    document.getElementById(`tab-${task.id}`)?.focus();
  });
  for (const name of ['scroll', 'click', 'keyup'])
    app.addEventListener(
      name,
      (event) => {
        if (event.target instanceof HTMLTextAreaElement && event.target.hasAttribute('data-code-file')) code.sync(event.target);
      },
      true,
    );
  window.codingPrototypeApp = { state, dispatch };
  document.getElementById('study-toggle')?.addEventListener('click', () => {
    state.layout.studyControls = !state.layout.studyControls;
    render();
  });
  window.matchMedia('(max-width: 700px)').addEventListener('change', (event) => {
    state.layout.studyControls = !event.matches;
    render();
  });
  window.addEventListener('pagehide', () => {
    window.clearTimeout(timer);
    window.clearTimeout(saveTimer);
  });
  render();
})();
