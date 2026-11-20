(() => {
  /** @type {import('./contracts').PrototypeRecovery['create']} */
  function create() {
    /** @type {Map<string, import('./contracts').AuthorRecovery>} */
    const entries = new Map();
    /** @param {import('./contracts').RecoveryScope} scope */
    function key(scope) {
      const parts = [scope.tenant, scope.actor, scope.exercise];
      if (!parts.every((part) => typeof part === 'string' && part.length > 0)) throw new Error('Recovery requires its full synthetic scope.');
      return JSON.stringify(parts);
    }
    /** @param {import('./contracts').RecoveryScope} scope @param {import('./contracts').AuthorRecovery} value */
    function put(scope, value) {
      const draft = window.codingPrototypeSchemas.parseDraft(value.draft);
      if (draft.id !== scope.exercise) throw new Error('Recovery exercise does not match its scope.');
      if (
        value.signatureEditor !== null &&
        !window.codingPrototypeSchemas.isEvent({ type: 'request-change', change: { kind: 'signature', value: value.signatureEditor } })
      )
        throw new Error('Invalid recovered signature editor.');
      entries.set(key(scope), { draft, signatureEditor: structuredClone(value.signatureEditor) });
    }
    /** @param {import('./contracts').RecoveryScope} scope */
    function get(scope) {
      const value = entries.get(key(scope));
      return value ? structuredClone(value) : null;
    }
    return { put, get };
  }
  window.codingPrototypeRecovery = { create };
})();
