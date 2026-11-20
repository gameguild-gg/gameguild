(() => {
  /** @typedef {import('./contracts').PrototypeHtml} PrototypeHtml */
  /** @type {PrototypeHtml['escape']} */
  const escape = (value) => value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#39;');
  /** @type {PrototypeHtml['field']} */
  const field = (label, id, value, type = 'text') =>
    `<label for="${escape(id)}">${escape(label)}</label><input id="${escape(id)}" data-field="${escape(id)}" type="${type}" ${type === 'number' ? 'step="any"' : ''} value="${escape(value)}">`;
  /** @type {PrototypeHtml['area']} */
  const area = (label, id, value, className = '', readOnly = false) =>
    `<label for="${escape(id)}">${escape(label)}</label><textarea id="${escape(id)}" data-field="${escape(id)}" class="${className}" spellcheck="false" ${readOnly ? 'readonly' : ''}>${escape(value)}</textarea>`;
  /** @type {PrototypeHtml['select']} */
  const select = (label, id, value, options) =>
    `<label for="${escape(id)}">${escape(label)}</label><select id="${escape(id)}" data-field="${escape(id)}">${options.map((option) => `<option value="${escape(option.value)}" ${option.value === value ? 'selected' : ''} ${option.disabled ? 'disabled' : ''}>${escape(option.label)}</option>`).join('')}</select>`;
  /** @type {PrototypeHtml['check']} */
  const check = (label, id, checked, disabled = false) =>
    `<label class="check"><input id="${id}" data-field="${id}" type="checkbox" ${checked ? 'checked' : ''} ${disabled ? 'disabled' : ''}>${escape(label)}</label>`;
  /** @type {PrototypeHtml['button']} */
  const button = (label, action, disabled = false) =>
    `<button type="button" id="command-${escape(action)}" data-action="${escape(action)}" ${disabled ? 'disabled' : ''}>${escape(label)}</button>`;
  window.codingPrototypeHtml = { escape, field, area, select, check, button };
})();
