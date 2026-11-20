(() => {
  /** @typedef {import('./contracts').PrototypeModel} PrototypeModel */
  /** @type {Record<import('./contracts').ScalarKind, string>} */
  const cpp = { integer: 'int', float: 'double', boolean: 'bool', string: 'std::string' };
  /** @type {PrototypeModel['signature']} */
  function signature(value) {
    return `${cpp[value.returns]} ${value.name}(${value.parameters.map((parameter) => `${cpp[parameter.type]} ${parameter.name}`).join(', ')})`;
  }
  /** @type {PrototypeModel['scalarError']} */
  function scalarError(kind, raw) {
    if (kind === 'string') return null;
    if (kind === 'boolean') return raw === 'true' || raw === 'false' ? null : 'must be true or false';
    if (kind === 'integer') return /^-?\d+$/.test(raw) && Number.isSafeInteger(Number(raw)) ? null : 'must be an integer';
    return /^-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?$/.test(raw) && Number.isFinite(Number(raw)) ? null : 'must be a finite decimal';
  }
  /** @type {PrototypeModel['issues']} */
  function issues(draft) {
    /** @type {import('./contracts').DraftIssue[]} */
    const found = [];
    if (!draft.title.trim()) found.push({ task: 'problem', field: 'title', message: 'Add an exercise title.' });
    if (!draft.statement.trim()) found.push({ task: 'problem', field: 'statement', message: 'Write the problem statement.' });
    if (!draft.files.some((file) => file.role === 'starter' && file.content.trim()))
      found.push({ task: 'starter', field: 'source', message: 'Prepare a starter file.' });
    if (draft.family === 'function') {
      if (!/^[A-Za-z_]\w*$/.test(draft.signature.name)) found.push({ task: 'starter', field: 'function-name', message: 'Enter a valid function name.' });
      const names = draft.signature.parameters.map((entry) => entry.name);
      if (names.some((name) => !/^[A-Za-z_]\w*$/.test(name)) || new Set(names).size !== names.length)
        found.push({ task: 'starter', field: 'function-name', message: 'Parameters need unique valid names.' });
      for (const entry of draft.cases) {
        for (const parameter of draft.signature.parameters) {
          const error = scalarError(parameter.type, entry.inputs[parameter.id] ?? '');
          if (error) found.push({ task: 'tests', field: entry.id, message: `${parameter.name || 'Parameter'} ${error}.` });
        }
        const error = scalarError(draft.signature.returns, entry.expected);
        if (error) found.push({ task: 'tests', field: entry.id, message: `Expected result ${error}.` });
      }
    } else {
      for (const entry of draft.cases)
        if (entry.expected.mode === 'none') found.push({ task: 'tests', field: entry.id, message: 'This case has no output oracle.' });
    }
    if (!draft.cases.length) found.push({ task: 'tests', field: 'add-case', message: 'Add a test case.' });
    return found;
  }
  /** @type {PrototypeModel['addCase']} */
  function addCase(draft) {
    const id = `case-${draft.nextId++}`;
    const common = { id, name: `Case ${draft.cases.length + 1}`, visibility: /** @type {const} */ ('public') };
    if (draft.family === 'function')
      draft.cases.push({ ...common, kind: 'function', inputs: Object.fromEntries(draft.signature.parameters.map((entry) => [entry.id, ''])), expected: '' });
    else draft.cases.push({ ...common, kind: 'stdio', stdin: '', expected: { mode: 'value', stdout: '' } });
    return id;
  }
  /** @type {PrototypeModel['duplicateCase']} */
  function duplicateCase(draft, id) {
    const next = `case-${draft.nextId++}`;
    if (draft.family === 'function') {
      const entry = draft.cases.find((item) => item.id === id);
      if (!entry) throw new Error('Select a case before duplicating.');
      draft.cases.push({ ...structuredClone(entry), id: next, name: `${entry.name} (copy)` });
    } else {
      const entry = draft.cases.find((item) => item.id === id);
      if (!entry) throw new Error('Select a case before duplicating.');
      draft.cases.push({ ...structuredClone(entry), id: next, name: `${entry.name} (copy)` });
    }
    return next;
  }
  /** @type {PrototypeModel['applySignature']} */
  function applySignature(draft, signature) {
    if (draft.family !== 'function') throw new Error('Only function exercises have signatures.');
    const old = draft.signature;
    for (const entry of draft.cases) {
      entry.inputs = Object.fromEntries(
        signature.parameters.map((parameter) => [
          parameter.id,
          old.parameters.find((item) => item.id === parameter.id)?.type === parameter.type ? (entry.inputs[parameter.id] ?? '') : '',
        ]),
      );
      if (signature.returns !== old.returns) entry.expected = '';
    }
    draft.signature = structuredClone(signature);
  }
  window.codingPrototypeModel = { signature, scalarError, issues, addCase, duplicateCase, applySignature };
})();
