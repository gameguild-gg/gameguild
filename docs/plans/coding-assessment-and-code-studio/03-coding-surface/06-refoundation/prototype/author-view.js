(() => {
  const h = window.codingPrototypeHtml;
  const model = window.codingPrototypeModel;
  const code = window.codingPrototypeCodeView;
  /** @type {readonly import('./contracts').ScalarKind[]} */
  const kinds = ['integer', 'float', 'boolean', 'string'];
  const typeOptions = kinds.map((value) => ({ value, label: value }));
  /** @param {string} label @param {string} id @param {import('./contracts').ScalarKind} kind @param {string} value */
  function scalar(label, id, kind, value) {
    return kind === 'boolean'
      ? h.select(label, id, value, [
          { value: '', label: 'Select a value' },
          { value: 'true', label: 'true' },
          { value: 'false', label: 'false' },
        ])
      : h.field(label, id, value, kind === 'integer' || kind === 'float' ? 'number' : 'text');
  }
  /** @param {import('./contracts').PresentationState} state */
  function problem(state) {
    const draft = state.draft;
    const samples = draft.cases.filter((entry) => entry.visibility === 'public');
    return `<div class="task-heading"><h2>Problem</h2><div class="compact">${h.select('Exercise type', 'family', draft.family, [
      { value: 'function', label: 'Implement a function' },
      { value: 'standard-io', label: 'Input and output' },
    ])}</div></div><div class="form-wide"><div class="field">${h.field('Exercise title', 'title', draft.title)}</div><div class="field">${h.area('Statement', 'statement', draft.statement)}</div><div class="field">${h.area('Constraints', 'constraints', draft.constraints)}</div><h3>Examples</h3><div class="sample-list">${samples.length ? samples.map((entry) => `<button type="button" data-sample="${entry.id}">${h.escape(entry.name)}<span>${h.escape(entry.kind === 'function' ? Object.values(entry.inputs).join(', ') : entry.stdin)} &rarr; ${h.escape(entry.kind === 'function' ? entry.expected : entry.expected.stdout)}</span></button>`).join('') : '<p class="muted">No public cases yet.</p>'}</div></div>`;
  }
  /** @param {import('./contracts').PresentationState} state */
  function environment(state) {
    const draft = state.draft;
    const environments = window.codingPrototypeSchemas.environments;
    const current = environments.find((entry) => entry.id === draft.environment);
    if (!current) throw new Error('Unknown environment.');
    return `<h2>Environment</h2><div class="form-wide fields"><div class="field">${h.select('Language', 'language', current.language, [
      { value: 'cpp', label: 'C++' },
      { value: 'c', label: draft.family === 'function' ? 'C - function harness unavailable' : 'C', disabled: draft.family === 'function' },
    ])}</div><div class="field">${h.select(
      'Execution environment',
      'execution-environment',
      current.id,
      environments
        .filter((entry) => entry.language === current.language)
        .map((entry) => ({ value: entry.id, label: entry.name, disabled: !entry.families.includes(draft.family) })),
    )}</div></div>${h.check('Graphics output', 'graphics', draft.graphics, !current.graphics)}${h.check('Interactive terminal', 'terminal', draft.terminal, !current.terminal)}`;
  }
  /** @param {import('./contracts').PresentationState} state */
  function starter(state) {
    const draft = state.draft;
    const signature = state.signatureEditor;
    const signaturePending = draft.family === 'function' && JSON.stringify(signature) !== JSON.stringify(draft.signature);
    const file = draft.files.find((entry) => entry.id === state.layout.selectedFile);
    const signatureForm =
      draft.family === 'function' && signature
        ? `<details class="signature-form" data-disclosure="signature" ${!signature.name || signaturePending ? 'open' : ''}><summary>Function signature <code>${h.escape(model.signature(draft.signature))}</code></summary><section aria-label="Function signature"><div class="fields"><div class="field">${h.field('Function name', 'function-name', signature.name)}</div><div class="field">${h.select('Return type', 'return-type', signature.returns, typeOptions)}</div></div>${signature.parameters.map((parameter, index) => `<div class="parameter-row"><div class="field">${h.field(`Parameter ${index + 1} name`, `param-name-${parameter.id}`, parameter.name)}</div><div class="field">${h.select(`Parameter ${index + 1} type`, `param-type-${parameter.id}`, parameter.type, typeOptions)}</div>${h.button(`Remove parameter ${index + 1}`, `remove-param:${parameter.id}`)}</div>`).join('')}<div class="actions">${h.button('Add parameter', 'add-param')}${h.button('Apply signature', 'apply-signature')}</div></section></details>`
        : '';
    return `<div class="task-heading"><h2>Starter code</h2>${h.button('Generate starter', 'generate-starter', !file || (draft.family === 'function' && (!draft.signature.name || signaturePending)))}</div>${signatureForm}${signaturePending ? '<p class="warning">Pending signature changes</p>' : ''}${
      file
        ? `<div class="file-policy">${h.select('File role', 'file-role', file.role, [
            { value: 'starter', label: 'Learner starter' },
            { value: 'support', label: 'Supplied support' },
          ])}${h.check('Learner can edit this file', 'file-editable', file.editable)}</div>`
        : ''
    }${code.render({ scope: 'author', files: draft.files, selectedFile: state.layout.selectedFile, session: state.authorEditor, language: draft.environment === 'c-wasi' ? 'c' : 'cpp' })}`;
  }
  /** @param {import('./contracts').PresentationState} state */
  function tests(state) {
    const draft = state.draft;
    const selected = draft.cases.find((entry) => entry.id === state.layout.selectedCase);
    let detail = '<p class="muted">Select a case or add one.</p>';
    if (selected) {
      let values = '';
      if (selected.kind === 'function' && draft.family === 'function') {
        values = `<h3>Arguments</h3><div class="fields">${draft.signature.parameters.map((parameter) => `<div class="field">${scalar(`${parameter.name} (${parameter.type})`, `argument-${parameter.id}`, parameter.type, selected.inputs[parameter.id] ?? '')}</div>`).join('')}</div><div class="field">${scalar(`Expected result (${draft.signature.returns})`, 'expected', draft.signature.returns, selected.expected)}</div>`;
      } else if (selected.kind === 'stdio') {
        values = `<div class="field">${h.area('Standard input', 'stdin', selected.stdin)}</div><div class="field">${h.select(
          'Output expectation',
          'expectation',
          selected.expected.mode,
          [
            { value: 'value', label: 'Compare exact output' },
            { value: 'none', label: 'No expected output' },
          ],
        )}</div>${selected.expected.mode === 'value' ? `<div class="field">${h.area('Expected stdout', 'stdout', selected.expected.stdout)}</div><p class="muted">${selected.expected.stdout === '' ? 'Intentional empty output (0 characters).' : `${selected.expected.stdout.length} expected characters.`}</p>` : '<p class="warning">No output oracle. Add one before validating this exercise.</p>'}`;
      }
      detail = `<div class="fields"><div class="field">${h.field('Case name', 'case-name', selected.name)}</div><div class="field">${h.select(
        'Case visibility',
        'case-visibility',
        selected.visibility,
        [
          { value: 'public', label: 'Public sample' },
          { value: 'private', label: 'Private test' },
        ],
      )}</div></div>${values}<ul class="errors">${model
        .issues(draft)
        .filter((issue) => issue.field === selected.id)
        .map((issue) => `<li>${h.escape(issue.message)}</li>`)
        .join('')}</ul><div class="actions">${h.button('Duplicate case', 'duplicate-case')}${h.button('Delete case', 'delete-case')}</div>`;
    }
    return `<div class="task-heading"><h2>Tests</h2><span class="muted">${draft.cases.filter((entry) => entry.visibility === 'public').length} public / ${draft.cases.filter((entry) => entry.visibility === 'private').length} private</span>${h.button('Add case', 'add-case')}</div><div class="case-selector">${h.select(
      'Selected test case',
      'selected-case',
      state.layout.selectedCase ?? '',
      draft.cases.map((entry, index) => ({ value: entry.id, label: `${index + 1}. ${entry.name}` })),
    )}</div><div class="test-layout"><nav class="case-list" data-scroll="author-cases" aria-label="Test cases">${draft.cases.map((entry, index) => `<button type="button" id="case-nav-${entry.id}" data-case="${entry.id}" aria-pressed="${entry.id === selected?.id}"><span>${index + 1}. ${h.escape(entry.name)}</span><small>${entry.visibility === 'public' ? 'Public sample' : 'Private test'}</small></button>`).join('')}</nav><section id="case-detail" aria-label="Test case details">${detail}</section></div>`;
  }
  /** @type {import('./contracts').PrototypeAuthorView['panel']} */
  function panel(state) {
    switch (state.layout.task) {
      case 'problem':
        return problem(state);
      case 'environment':
        return environment(state);
      case 'starter':
        return starter(state);
      case 'tests':
        return tests(state);
      case 'validate':
        return preparation(state);
    }
  }
  /** @param {import('./contracts').RunState} run @param {boolean} reference */
  function result(run, reference) {
    if (run.status === 'idle') return '<p class="muted">Exercise not validated</p>';
    const prefix = `<small data-candidate="${h.escape(run.candidate)}">Candidate ${h.escape(run.candidate)}</small>`;
    if (run.status === 'running') return `${prefix}<p role="status">Checking cases...</p>`;
    if (run.status === 'stale') return `${prefix}<p class="warning">Out of date. Run against the current exercise.</p>`;
    if (run.status === 'cancelled') return `${prefix}<p>Cancelled</p>`;
    if (run.status === 'error') return `${prefix}<p class="errors">${h.escape(run.message)}</p>`;
    const passed = run.results.every((entry) => entry.status === 'passed');
    return `${prefix}<p class="${passed ? 'success' : 'warning'}">${reference ? (passed ? 'All cases passed' : 'Reference needs attention') : passed ? 'No defect detected' : 'Cases detected the defect'}</p><ul class="diagnostic-list">${run.results.map((entry) => `<li data-diagnostic="${entry.status}"><strong>${h.escape(entry.name)}</strong> / ${entry.status}<pre>Expected: ${h.escape(entry.expected ?? '(no oracle)')}\nActual: ${h.escape(entry.actual)}</pre></li>`).join('')}</ul>`;
  }
  /** @param {import('./contracts').PresentationState} state */
  function preparation(state) {
    const ready =
      model.issues(state.draft).length === 0 &&
      (state.draft.family !== 'function' || JSON.stringify(state.signatureEditor) === JSON.stringify(state.draft.signature));
    const solutionEditor = (/** @type {'reference' | 'incorrect'} */ kind, /** @type {string} */ label) =>
      code.editor({
        label,
        field: `${kind}-source`,
        content: state.draft.protected[kind],
        path: `${kind}.cpp`,
        fileId: kind,
        scope: 'protected',
        language: state.draft.environment === 'c-wasi' ? 'c' : 'cpp',
        readOnly: false,
      });
    return `<h2>Validate exercise</h2><p class="muted">Candidate ${h.escape(state.draft.id)}:r${state.draft.revision}</p><div class="preparation-layout"><section aria-label="Reference preparation"><h3>Reference solution</h3>${solutionEditor('reference', 'Reference solution')}${h.button('Run reference', 'run-reference', !ready || !state.draft.protected.reference.trim())}<div id="reference-result" aria-live="polite">${result(state.validation.reference, true)}</div></section><section aria-label="Incorrect solution preparation"><h3>Incorrect solution</h3>${solutionEditor('incorrect', 'Incorrect solution')}${h.button('Run incorrect solution', 'run-incorrect', !ready || !state.draft.protected.incorrect.trim())}<div id="incorrect-result" aria-live="polite">${result(state.validation.incorrect, false)}</div></section></div><div class="preview-action">${h.button('Open learner preview', 'open-preview')}<span class="muted">Current problem, starter files and public samples</span></div>`;
  }
  window.codingPrototypeAuthorView = { panel };
})();
