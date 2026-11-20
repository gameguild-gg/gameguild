(() => {
  /** @typedef {import('./contracts').PrototypeSchemas} PrototypeSchemas */
  /** @param {unknown} value @returns {value is Record<string, unknown>} */
  const record = (value) => typeof value === 'object' && value !== null && !Array.isArray(value);
  /** @param {unknown} value @returns {value is string} */
  const text = (value) => typeof value === 'string';
  /** @param {unknown} value @returns {value is string} */
  const identity = (value) => text(value) && /^[A-Za-z0-9][A-Za-z0-9:._-]*$/.test(value);
  /** @param {unknown} value @returns {value is number} */
  const natural = (value) => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0;
  /** @param {unknown} value */
  const kind = (value) => ['integer', 'float', 'boolean', 'string'].includes(String(value));
  /** @param {unknown} value */
  const visibility = (value) => value === 'public' || value === 'private';
  /** @param {unknown[]} values */
  const unique = (values) => new Set(values).size === values.length;
  /** @type {PrototypeSchemas['environments']} */
  const environments = [
    { id: 'cpp-wasi', name: 'C++ / Clang + WASI', language: 'cpp', families: ['function', 'standard-io'], graphics: false, terminal: true },
    { id: 'c-wasi', name: 'C / Clang + WASI', language: 'c', families: ['standard-io'], graphics: false, terminal: true },
    { id: 'cpp-sdl3', name: 'C++ / SDL3', language: 'cpp', families: ['standard-io'], graphics: true, terminal: true },
    { id: 'cpp-raylib', name: 'C++ / raylib', language: 'cpp', families: ['standard-io'], graphics: true, terminal: true },
  ];

  /** @param {unknown} value */
  function file(value) {
    return (
      record(value) &&
      identity(value.id) &&
      text(value.path) &&
      text(value.content) &&
      typeof value.editable === 'boolean' &&
      ['starter', 'support'].includes(String(value.role))
    );
  }

  /** @type {PrototypeSchemas['isDraft']} */
  function isDraft(value) {
    if (!record(value) || !identity(value.id) || !natural(value.revision) || !natural(value.nextId)) return false;
    if (![value.title, value.statement, value.constraints].every(text)) return false;
    if (!record(value.protected) || !text(value.protected.reference) || !text(value.protected.incorrect)) return false;
    const environment = environments.find((entry) => entry.id === value.environment);
    if (!environment || (value.family !== 'function' && value.family !== 'standard-io')) return false;
    if (!environment.families.includes(value.family) || typeof value.graphics !== 'boolean' || typeof value.terminal !== 'boolean') return false;
    if ((value.graphics && !environment.graphics) || (value.terminal && !environment.terminal)) return false;
    if (!Array.isArray(value.files) || !value.files.every(file) || !unique(value.files.map((entry) => entry.id))) return false;
    if (!Array.isArray(value.cases) || !unique(value.cases.map((entry) => (record(entry) ? entry.id : null)))) return false;
    if (value.family === 'function') {
      const signature = value.signature;
      if (!record(signature) || !text(signature.name) || !kind(signature.returns) || !Array.isArray(signature.parameters)) return false;
      if (!signature.parameters.every((entry) => record(entry) && identity(entry.id) && text(entry.name) && kind(entry.type))) return false;
      if (!unique(signature.parameters.map((entry) => entry.id))) return false;
      const ids = signature.parameters.map((entry) => entry.id);
      return value.cases.every(
        (entry) =>
          record(entry) &&
          identity(entry.id) &&
          text(entry.name) &&
          visibility(entry.visibility) &&
          entry.kind === 'function' &&
          text(entry.expected) &&
          record(entry.inputs) &&
          Object.keys(entry.inputs).length === ids.length &&
          ids.every((id) => record(entry.inputs) && text(entry.inputs[id])),
      );
    }
    return value.cases.every(
      (entry) =>
        record(entry) &&
        identity(entry.id) &&
        text(entry.name) &&
        visibility(entry.visibility) &&
        entry.kind === 'stdio' &&
        text(entry.stdin) &&
        record(entry.expected) &&
        ['none', 'value'].includes(String(entry.expected.mode)) &&
        text(entry.expected.stdout),
    );
  }

  /** @type {PrototypeSchemas['isRun']} */
  function isRun(value) {
    if (!record(value)) return false;
    if (value.status === 'idle') return true;
    if (!identity(value.candidate)) return false;
    if (value.status === 'running') return natural(value.token);
    if (value.status === 'stale' || value.status === 'cancelled') return true;
    if (value.status === 'error') return ['compile', 'timeout', 'executor', 'unrecorded', 'input'].includes(String(value.reason)) && text(value.message);
    return (
      value.status === 'completed' &&
      Array.isArray(value.results) &&
      value.results.length > 0 &&
      value.results.every(
        (entry) =>
          record(entry) &&
          identity(entry.caseId) &&
          text(entry.name) &&
          text(entry.input) &&
          (entry.expected === null || text(entry.expected)) &&
          text(entry.actual) &&
          ['passed', 'mismatch', 'observed'].includes(String(entry.status)) &&
          (entry.expected !== null || entry.status === 'observed'),
      )
    );
  }

  /** @type {PrototypeSchemas['isSave']} */
  function isSave(value) {
    if (!record(value)) return false;
    if (value.status === 'unsaved') return true;
    if (value.status === 'saving' || value.status === 'saved') return natural(value.revision);
    return (value.status === 'failed' || value.status === 'conflict') && text(value.message) && value.message.length > 0;
  }

  /** @type {PrototypeSchemas['isEvent']} */
  function isEvent(value) {
    if (!record(value)) return false;
    switch (value.type) {
      case 'select-task':
        return ['problem', 'environment', 'starter', 'tests', 'validate'].includes(String(value.task));
      case 'select-region':
        return ['problem', 'code', 'cases', 'results', 'graphics', 'terminal'].includes(String(value.region));
      case 'edit-draft':
        return isDraft(value.draft);
      case 'edit-source':
        return text(value.source);
      case 'open-file':
      case 'close-file':
        return identity(value.fileId);
      case 'toggle-explorer':
        return true;
      case 'run':
        return ['samples', 'scratch', 'reference', 'incorrect'].includes(String(value.target));
      case 'request-change': {
        if (!record(value.change)) return false;
        const change = value.change;
        return (
          (change.kind === 'family' && ['function', 'standard-io'].includes(String(change.value))) ||
          (change.kind === 'environment' && environments.some((entry) => entry.id === change.value)) ||
          (change.kind === 'signature' &&
            record(change.value) &&
            text(change.value.name) &&
            kind(change.value.returns) &&
            Array.isArray(change.value.parameters) &&
            change.value.parameters.every((entry) => record(entry) && identity(entry.id) && text(entry.name) && kind(entry.type))) ||
          (change.kind === 'starter' && identity(change.value)) ||
          (change.kind === 'delete-file' && identity(change.value)) ||
          (change.kind === 'example' && ['maximum', 'echo', 'stress', 'blank'].includes(String(change.value))) ||
          (change.kind === 'reset' && change.value === 'learner')
        );
      }
      default:
        return ['confirm-change', 'cancel-change', 'preview', 'close-preview', 'save', 'recover', 'cancel-run', 'submit'].includes(String(value.type));
    }
  }

  /** @type {PrototypeSchemas['parseDraft']} */
  function parseDraft(value) {
    if (!isDraft(value)) throw new TypeError('Invalid prototype draft; recovery was not applied.');
    return structuredClone(value);
  }

  window.codingPrototypeSchemas = { environments, isDraft, isRun, isSave, isEvent, parseDraft };
})();
