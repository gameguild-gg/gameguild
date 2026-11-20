(() => {
  const h = window.codingPrototypeHtml;
  const simulation = window.codingPrototypeSimulation;
  /** @param {import('./contracts').RunState} run */
  function results(run) {
    if (run.status === 'idle') return '<p class="muted">No execution yet.</p>';
    const candidate = `<small>Executed ${h.escape(run.candidate)}</small>`;
    if (run.status === 'running') return `${candidate}<p role="status">Running...</p>`;
    if (run.status === 'cancelled') return `${candidate}<p>Cancelled. No verdict.</p>`;
    if (run.status === 'stale') return `${candidate}<p class="warning">Out of date. Code or input changed.</p>`;
    if (run.status === 'error') return `${candidate}<p class="errors">${h.escape(run.message)}</p>`;
    const status = run.results.every((entry) => entry.status === 'observed')
      ? 'Output observed'
      : run.results.some((entry) => entry.status === 'mismatch')
        ? 'Wrong answer'
        : 'All sample cases passed';
    return `${candidate}<h3 class="${status === 'Wrong answer' ? 'warning' : 'success'}">${status}</h3>${run.results.map((entry) => `<section class="diagnostic" data-diagnostic="${entry.status}"><strong>${h.escape(entry.name)}</strong><dl><dt>Input</dt><dd><pre>${h.escape(entry.input || '(empty input)')}</pre></dd>${entry.expected === null ? '' : `<dt>Expected</dt><dd><pre>${h.escape(entry.expected || '(empty output)')}</pre></dd>`}<dt>Actual</dt><dd><pre>${h.escape(entry.actual || '(empty output)')}</pre></dd></dl></section>`).join('')}`;
  }
  /** @param {import('./contracts').Workspace} workspace */
  function cases(workspace) {
    const selected = workspace.delivery.samples.find((entry) => entry.id === workspace.selectedSample);
    const parameters = workspace.delivery.parameters;
    const custom =
      workspace.delivery.family === 'standard-io'
        ? h.area('Custom standard input', 'scratch', workspace.scratch)
        : parameters
            .map(
              (entry) =>
                `<div class="field">${
                  entry.type === 'boolean'
                    ? h.select(`Custom ${entry.name} (${entry.type})`, `scratch-${entry.id}`, workspace.scratchInputs[entry.id] ?? '', [
                        { value: '', label: 'Select a value' },
                        { value: 'true', label: 'true' },
                        { value: 'false', label: 'false' },
                      ])
                    : h.field(
                        `Custom ${entry.name} (${entry.type})`,
                        `scratch-${entry.id}`,
                        workspace.scratchInputs[entry.id] ?? '',
                        entry.type === 'string' ? 'text' : 'number',
                      )
                }</div>`,
            )
            .join('');
    return `<h3>Sample cases</h3><div class="sample-navigation">${workspace.delivery.samples.map((entry) => `<button type="button" data-learner-case="${entry.id}" aria-pressed="${entry.id === workspace.selectedSample}">${h.escape(entry.name)}</button>`).join('')}</div>${selected ? `<pre class="sample-detail">Input: ${h.escape(simulation.input(selected) || '(empty input)')}\nExpected: ${h.escape((selected.kind === 'function' ? selected.expected : selected.expected.stdout) || '(empty output)')}</pre>` : '<p>No public samples.</p>'}<section class="custom-case" aria-label="Personal custom case"><h3>Custom case</h3><div class="field">${custom}</div>${h.check('Compare an expected value', 'scratch-oracle', workspace.scratchExpected !== null)}${workspace.scratchExpected === null ? '' : `<div class="field">${h.area('Custom expected value', 'scratch-expected', workspace.scratchExpected)}</div>`}${h.button('Run custom case', 'run-scratch', workspace.run.status === 'running')}</section>`;
  }
  /** @type {import('./contracts').PrototypeLearnerView['render']} */
  function render(state, workspace) {
    const delivery = workspace.delivery;
    const submitted = workspace.submission;
    const currentCandidate = `${delivery.candidate}:edit${workspace.version}`;
    const running = workspace.run.status === 'running';
    const region = state.layout.region;
    const regions = [
      { id: 'problem', label: 'Problem' },
      { id: 'code', label: 'Code' },
      { id: 'cases', label: 'Cases' },
      { id: 'results', label: 'Results' },
    ];
    if (delivery.graphics) regions.push({ id: 'graphics', label: 'Canvas' });
    if (delivery.terminal) regions.push({ id: 'terminal', label: 'Terminal' });
    return `<section aria-label="Learner workspace" class="solver-shell">
      <div class="workspace-bar">${h.button('Run tests', 'run-samples', running || !delivery.samples.length)}${h.button('Cancel run', 'cancel-run', !running)}${h.button('Submit solution', 'submit', state.persona === 'preview' || running)}${h.button('Reset code', 'reset-learner')}<span class="muted">${state.persona === 'preview' ? 'Preview' : 'Learner'}</span></div>
      <nav class="region-tabs" aria-label="Workspace views">${regions.map((entry) => `<button type="button" data-region="${entry.id}" aria-pressed="${region === entry.id}">${entry.label}</button>`).join('')}<button type="button" data-action="focus-view" aria-pressed="${state.layout.focused}">Focus selected view</button></nav>
      <div class="layout-controls"><label for="problem-width">Problem width</label><input type="range" id="problem-width" min="25" max="65" value="${state.layout.split}"></div>
      <div class="solver ${state.layout.focused ? 'focused' : ''}" data-region="${region}" style="--problem-share:${state.layout.split}%">
        <article class="learner-problem" data-scroll="learner-problem" role="region" aria-label="Learner problem"><h2>${h.escape(delivery.title || 'Untitled exercise')}</h2><p class="statement">${h.escape(delivery.statement)}</p>${delivery.signature ? `<pre>${h.escape(delivery.signature)}</pre>` : ''}<h3>Examples</h3>${delivery.samples.map((entry) => `<div class="sample"><strong>${h.escape(entry.name)}</strong><pre>Input: ${h.escape(simulation.input(entry) || '(empty input)')}\nExpected: ${h.escape((entry.kind === 'function' ? entry.expected : entry.expected.stdout) || '(empty output)')}</pre></div>`).join('')}<h3>Constraints</h3><p class="statement">${h.escape(delivery.constraints)}</p></article>
        <section class="learner-code" aria-label="Code workspace"><section class="code-zone">${window.codingPrototypeCodeView.render({ scope: state.persona === 'preview' ? 'preview' : 'learner', files: workspace.files, selectedFile: workspace.selectedFile, session: workspace.editor, language: delivery.environment === 'c-wasi' ? 'c' : 'cpp' })}</section>
        <section class="output-zone" data-scroll="learner-output-${region}"><div id="learner-cases" ${region === 'cases' ? '' : 'hidden'}>${cases(workspace)}</div><div id="learner-results" aria-live="polite" ${region === 'cases' || region === 'graphics' || region === 'terminal' ? 'hidden' : ''}>${results(workspace.run)}</div>
          <div id="learner-graphics" ${region === 'graphics' && delivery.graphics ? '' : 'hidden'}><h3>Canvas layout specimen</h3><canvas id="graphics-canvas" width="640" height="320" tabindex="0" aria-label="Synthetic graphics canvas"></canvas><div class="actions">${h.button('Move shape', 'move-shape', !state.graphicsSpecimen.playing)}${h.button(state.graphicsSpecimen.playing ? 'Stop canvas' : 'Reopen canvas', 'toggle-canvas')}${h.button('Focus canvas', 'focus-canvas')}</div><p>${state.graphicsSpecimen.playing ? 'Canvas active' : 'Canvas stopped'}</p></div>
          <div id="learner-terminal" ${region === 'terminal' && delivery.terminal ? '' : 'hidden'}><h3>Interactive input specimen</h3><pre aria-label="Terminal transcript">${h.escape(state.terminalSpecimen.lines.join('\n') || 'Recorded echo session')}</pre><div class="field">${h.field('Terminal input', 'terminal-input', state.terminalSpecimen.input)}</div>${h.button('Send input', 'send-terminal')}</div>
        </section></section>
      </div>
      <section id="submission-status" aria-live="polite">${submitted ? `<strong>Submission acknowledged</strong><small>${h.escape(submitted.id)} / ${h.escape(submitted.candidate)}</small><p>${submitted.candidate === currentCandidate ? 'Submitted version matches your current code.' : 'Unsubmitted changes. The submitted version is unchanged.'}</p><p>${workspace.feedback === 'released' ? 'Released feedback - supplied by the host example.' : 'Awaiting released feedback.'}</p>` : '<span class="muted">No submission sent.</span>'}</section>
    </section>`;
  }
  window.codingPrototypeLearnerView = { render };
})();
