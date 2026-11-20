(() => {
  /** @typedef {import('./contracts').PrototypeCodeView} PrototypeCodeView */
  const h = window.codingPrototypeHtml;

  /** @type {PrototypeCodeView['session']} */
  function session(files) {
    return {
      openFiles: files[0] ? [files[0].id] : [],
      positions: {},
      baseline: Object.fromEntries(files.map((file) => [file.id, file.content])),
      explorer: false,
    };
  }

  /** @type {PrototypeCodeView['pathError']} */
  function pathError(path, files, exceptId) {
    const parts = path.split('/');
    if (!path || parts.some((part) => !/^[A-Za-z0-9_.-]+$/.test(part) || part === '.' || part === '..'))
      return 'Use a relative workspace path, such as include/helper.hpp.';
    return files.some((file) => file.id !== exceptId && file.path === path) ? 'A file with this path already exists.' : null;
  }

  /** @param {string} source */
  function colorize(source) {
    // Presentation-only token coloring; this never parses or executes a program.
    const tokens =
      /\/\/[^\n]*|\/\*[\s\S]*?\*\/|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|#[^\n]*|\b(?:int|double|bool|void|char|return|if|else|for|while|const|constexpr|auto|true|false|namespace|class|struct|using)\b|\b\d+(?:\.\d+)?\b/g;
    let from = 0;
    let output = '';
    for (const match of source.matchAll(tokens)) {
      const token = match[0];
      const kind = token.startsWith('/')
        ? 'comment'
        : /^["']/.test(token)
          ? 'string'
          : token.startsWith('#')
            ? 'directive'
            : /^\d/.test(token)
              ? 'number'
              : 'keyword';
      output += h.escape(source.slice(from, match.index)) + `<span class="syntax-${kind}">${h.escape(token)}</span>`;
      from = match.index + token.length;
    }
    return output + h.escape(source.slice(from)) + ' ';
  }

  /** @type {PrototypeCodeView['editor']} */
  function editor(options) {
    const { field, label, content, path, fileId, scope, readOnly, language } = options;
    return `<div class="source-editor" data-source-editor="${h.escape(field)}">
      <label class="sr-only" for="${h.escape(field)}">${h.escape(label)}</label>
      <div class="editor-breadcrumb"><span>${h.escape(path)}</span><span>${readOnly ? 'Read-only' : 'Editable'}</span></div>
      <div class="code-editor">
        <div class="line-gutter" aria-hidden="true"><pre class="line-numbers">${content
          .split('\n')
          .map((_, index) => index + 1)
          .join('\n')}</pre></div>
        <div class="editor-viewport"><pre class="syntax-layer" aria-hidden="true">${colorize(content)}</pre><textarea id="${h.escape(field)}" data-field="${h.escape(field)}" data-code-scope="${scope}" data-code-file="${h.escape(fileId)}" aria-description="Press Escape followed by Tab to leave the code editor." spellcheck="false" autocapitalize="off" autocomplete="off" autocorrect="off" wrap="off" ${readOnly ? 'readonly' : ''}>${h.escape(content)}</textarea></div>
      </div>
      <div class="editor-status"><span>${readOnly ? 'Read-only' : 'Editable'}</span><span data-cursor>Ln 1, Col 1</span><span>${language === 'cpp' ? 'C++' : 'C'}</span><span>UTF-8</span></div>
    </div>`;
  }

  /** @type {PrototypeCodeView['render']} */
  function render(options) {
    const { files, selectedFile, session: state, scope, language } = options;
    const file = files.find((entry) => entry.id === selectedFile);
    const author = scope === 'author';
    const folders = new Set();
    const explorer = files
      .map((entry) => {
        const parts = entry.path.split('/');
        const name = parts.pop() ?? entry.path;
        let parents = '';
        const headings = parts
          .map((part, depth) => {
            parents += `/${part}`;
            if (folders.has(parents)) return '';
            folders.add(parents);
            return `<div class="explorer-folder" style="--depth:${depth}">${h.escape(part)}/</div>`;
          })
          .join('');
        return `${headings}<button type="button" class="explorer-file" data-editor-open="${h.escape(entry.id)}" aria-label="${h.escape(entry.path)}" aria-pressed="${entry.id === selectedFile}" title="${h.escape(entry.path)}${!entry.editable ? ' - Read-only for learner' : ''}" style="--depth:${parts.length}"><span class="file-kind" aria-hidden="true">${entry.path.endsWith('.hpp') || entry.path.endsWith('.h') ? 'H' : entry.path.endsWith('.cpp') ? 'C++' : 'T'}</span><span>${h.escape(name)}</span>${!author && !entry.editable ? '<span class="readonly-mark" aria-hidden="true">RO</span>' : ''}</button>`;
      })
      .join('');
    return `<div class="code-workbench ${state.explorer ? 'explorer-open' : ''}" data-workbench-scope="${scope}">
      <div class="workbench-toolbar"><button type="button" id="command-toggle-explorer" data-action="toggle-explorer" aria-pressed="${state.explorer}" title="Show or hide files">Files</button><span>${author ? 'Starter workspace' : 'Solution workspace'}</span>${author ? `${h.button('New file', 'add-file')}${h.button('Rename file', 'rename-file', !file)}${h.button('Delete file', 'delete-file', !file || files.length <= 1)}` : ''}</div>
      <div class="workbench-body"><nav class="source-explorer" aria-label="${author ? 'Starter files' : 'Solution files'}" ${state.explorer ? '' : 'hidden'}><div class="explorer-heading">WORKSPACE</div>${explorer}</nav>
      <div class="workbench-editor"><div class="source-tabs" role="tablist" aria-label="Open source files">${state.openFiles
        .map((id) => {
          const entry = files.find((item) => item.id === id);
          if (!entry) return '';
          const modified = state.baseline[id] !== entry.content;
          return `<div class="editor-tab ${modified ? 'is-modified' : ''}"><button type="button" role="tab" id="source-tab-${h.escape(id)}" data-editor-open="${h.escape(id)}" aria-label="${h.escape(entry.path)}" aria-controls="source-buffer" aria-selected="${id === selectedFile}" tabindex="${id === selectedFile ? 0 : -1}" title="${h.escape(entry.path)}${modified ? (author ? ' - Unsaved edits' : ' - Modified from starter') : ''}">${h.escape(entry.path.split('/').at(-1) ?? entry.path)}${modified ? '<span class="modified-dot" aria-hidden="true"></span>' : ''}</button><button type="button" class="close-tab" data-editor-close="${h.escape(id)}" aria-label="Close ${h.escape(entry.path)}" title="Close ${h.escape(entry.path)}">&times;</button></div>`;
        })
        .join('')}</div>
      <div id="source-buffer" role="tabpanel" ${file ? `aria-labelledby="source-tab-${h.escape(file.id)}"` : 'aria-label="No open file"'}>${file ? editor({ label: author ? 'Source code' : 'Your code', field: author ? 'source' : 'learner-source', content: file.content, path: file.path, fileId: file.id, scope, readOnly: !author && !file.editable, language }) : '<div class="editor-empty">No file open</div>'}</div>
      </div></div>
    </div>`;
  }

  /** @param {string | undefined} scope @param {import('./contracts').PresentationState} state */
  const stateFor = (scope, state) =>
    scope === 'author' ? state.authorEditor : scope === 'preview' ? state.preview?.editor : scope === 'learner' ? state.learner?.editor : undefined;

  /** @type {PrototypeCodeView['capture']} */
  function capture(root, state) {
    for (const input of root.querySelectorAll('textarea[data-code-file]')) {
      if (!(input instanceof HTMLTextAreaElement)) continue;
      const session = stateFor(input.dataset.codeScope, state);
      const id = input.dataset.codeFile;
      if (session && id) session.positions[id] = { start: input.selectionStart, end: input.selectionEnd, top: input.scrollTop, left: input.scrollLeft };
    }
  }

  /** @type {PrototypeCodeView['sync']} */
  function sync(input) {
    const root = input.closest('.source-editor');
    const code = root?.querySelector('.syntax-layer');
    const gutter = root?.querySelector('.line-numbers');
    if (code instanceof HTMLElement) code.style.transform = `translate(${-input.scrollLeft}px, ${-input.scrollTop}px)`;
    if (gutter instanceof HTMLElement) gutter.style.transform = `translateY(${-input.scrollTop}px)`;
    const prefix = input.value.slice(0, input.selectionStart).split('\n');
    const cursor = root?.querySelector('[data-cursor]');
    if (cursor) cursor.textContent = `Ln ${prefix.length}, Col ${(prefix.at(-1)?.length ?? 0) + 1}`;
  }

  /** @type {PrototypeCodeView['restore']} */
  function restore(root, state) {
    for (const input of root.querySelectorAll('textarea[data-code-file]')) {
      if (!(input instanceof HTMLTextAreaElement)) continue;
      const position = stateFor(input.dataset.codeScope, state)?.positions[input.dataset.codeFile ?? ''];
      if (position) {
        input.setSelectionRange(position.start, position.end);
        input.scrollTop = position.top;
        input.scrollLeft = position.left;
      }
      sync(input);
    }
    const tab = root.querySelector('.source-tabs [aria-selected="true"]');
    const list = root.querySelector('.source-tabs');
    if (tab instanceof HTMLElement && list instanceof HTMLElement) {
      const a = tab.getBoundingClientRect();
      const b = list.getBoundingClientRect();
      if (a.left < b.left) list.scrollLeft -= b.left - a.left;
      else if (a.right > b.right) list.scrollLeft += a.right - b.right;
    }
  }

  window.codingPrototypeCodeView = { session, pathError, editor, render, capture, restore, sync };
})();
