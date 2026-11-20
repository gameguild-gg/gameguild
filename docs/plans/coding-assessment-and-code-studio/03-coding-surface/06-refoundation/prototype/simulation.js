(() => {
  /** @typedef {import('./contracts').PrototypeSimulation} PrototypeSimulation */
  /** @type {PrototypeSimulation['input']} */
  const input = (entry) => (entry.kind === 'function' ? Object.values(entry.inputs).join(', ') : entry.stdin);
  /** @type {PrototypeSimulation['project']} */
  function project(draft) {
    return {
      candidate: `${draft.id}:r${draft.revision}`,
      family: draft.family,
      title: draft.title,
      statement: draft.statement,
      constraints: draft.constraints,
      environment: draft.environment,
      graphics: draft.graphics,
      terminal: draft.terminal,
      signature: draft.family === 'function' ? window.codingPrototypeModel.signature(draft.signature) : '',
      parameters: draft.family === 'function' ? structuredClone(draft.signature.parameters) : [],
      returns: draft.family === 'function' ? draft.signature.returns : null,
      files: structuredClone(draft.files),
      samples: structuredClone(draft.cases.filter((entry) => entry.visibility === 'public')),
    };
  }
  /** @type {PrototypeSimulation['workspace']} */
  function workspace(delivery) {
    return {
      delivery: structuredClone(delivery),
      files: structuredClone(delivery.files),
      selectedFile: delivery.files[0]?.id ?? '',
      editor: window.codingPrototypeCodeView.session(delivery.files),
      selectedSample: delivery.samples[0]?.id ?? null,
      scratch: '',
      scratchInputs: Object.fromEntries(delivery.parameters.map((entry) => [entry.id, ''])),
      scratchExpected: null,
      version: 0,
      inputVersion: 0,
      run: { status: 'idle' },
      submission: null,
      feedback: 'pending',
    };
  }
  /** @type {PrototypeSimulation['outcome']} */
  function outcome(delivery, source, mode, cases, oracle = true) {
    const candidate = delivery.candidate;
    if (mode === 'compile') return { status: 'error', candidate, reason: 'compile', message: 'Compilation failed: expected expression at solution.cpp:2.' };
    if (mode === 'timeout') return { status: 'error', candidate, reason: 'timeout', message: 'Execution time limit reached. No correctness verdict.' };
    if (mode === 'executor') return { status: 'error', candidate, reason: 'executor', message: 'Execution service unavailable. No correctness verdict.' };
    const fixture = ['maximum', 'echo', 'stress']
      .map((id) =>
        id === 'echo'
          ? window.codingPrototypeFixtures.example('echo')
          : id === 'stress'
            ? window.codingPrototypeFixtures.example('stress')
            : window.codingPrototypeFixtures.example('maximum'),
      )
      .find((entry) => (source.trim() === entry.correct.trim() || source.trim() === entry.incorrect.trim()) && entry.draft.family === delivery.family);
    /** @returns {import('./contracts').RunState} */
    const unrecorded = () => ({
      status: 'error',
      candidate,
      reason: 'unrecorded',
      message: 'No recorded response for this source or case. Choose an example and its recorded solution in the study controls.',
    });
    if (!fixture || cases.length === 0) return unrecorded();
    if (
      delivery.files.length !== fixture.draft.files.length ||
      fixture.draft.files.some((expectedFile) => {
        const actual = delivery.files.find((file) => file.id === expectedFile.id);
        return (
          !actual ||
          actual.path !== expectedFile.path ||
          actual.role !== expectedFile.role ||
          (actual.role !== 'starter' && actual.content !== expectedFile.content)
        );
      })
    )
      return unrecorded();
    const correct = source.trim() === fixture.correct.trim();
    /** @type {import('./contracts').Diagnostic[]} */
    const results = [];
    for (const entry of cases) {
      const expected = !oracle ? null : entry.kind === 'function' ? entry.expected : entry.expected.mode === 'value' ? entry.expected.stdout : null;
      const recorded = fixture.outcomes.find((item) => item.input === input(entry) && (expected === null || item.expected === expected));
      if (!recorded) return unrecorded();
      const actual = correct ? recorded.correct : recorded.incorrect;
      results.push({
        caseId: entry.id,
        name: entry.name,
        input: input(entry),
        expected,
        actual,
        status: expected === null ? 'observed' : actual === expected ? 'passed' : 'mismatch',
      });
    }
    return { status: 'completed', candidate, results };
  }
  window.codingPrototypeSimulation = { project, workspace, outcome, input };
})();
