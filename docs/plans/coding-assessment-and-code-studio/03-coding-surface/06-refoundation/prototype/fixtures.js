(() => {
  /** @typedef {import('./contracts').Fixture} Fixture */
  /** @type {import('./contracts').PrototypeFixtures['blank']} */
  function blank(family) {
    if (family !== 'function' && family !== 'standard-io') throw new TypeError('Unknown exercise family.');
    /** @type {import('./contracts').DraftBase} */
    const common = {
      id: 'design-exercise',
      revision: 0,
      nextId: 20,
      title: '',
      statement: '',
      constraints: '',
      environment: 'cpp-wasi',
      graphics: false,
      terminal: false,
      files: [{ id: 'file-1', path: 'solution.cpp', content: '', editable: true, role: 'starter' }],
      protected: { reference: '', incorrect: '' },
    };
    return family === 'function'
      ? { ...common, family, signature: { name: '', parameters: [], returns: 'integer' }, cases: [] }
      : { ...common, family, cases: [] };
  }

  /** @type {import('./contracts').PrototypeFixtures['example']} */
  function example(id) {
    if (id !== 'maximum' && id !== 'echo' && id !== 'stress') throw new TypeError('Unknown prototype fixture.');
    const family = id === 'echo' ? 'standard-io' : 'function';
    const draft = blank(family);
    const sourceFile = draft.files[0];
    if (!sourceFile) throw new Error('Example requires its starter file.');
    const correct =
      id === 'echo'
        ? '#include <iostream>\n#include <string>\nint main() {\n  std::string line;\n  std::getline(std::cin, line);\n  std::cout << line;\n}\n'
        : 'int maximum(int a, int b) {\n  return a > b ? a : b;\n}\n';
    const incorrect =
      id === 'echo' ? '#include <iostream>\nint main() { std::cout << "wrong"; }\n' : 'int maximum(int a, int b) {\n  return a < b ? a : b;\n}\n';
    draft.title = id === 'echo' ? 'Echo a line' : 'Maximum of two integers';
    draft.statement =
      id === 'echo'
        ? 'Read one line and print it unchanged. An empty line produces empty output.'
        : 'Implement maximum(a, b). Return the greater of the two integers.';
    draft.constraints = id === 'echo' ? 'Input contains at most 100 characters.' : 'Both integers are between -100 and 100.';
    draft.protected = { reference: correct, incorrect };
    draft.files.push({
      id: 'file-support',
      path: 'include/limits.hpp',
      content: '#pragma once\nconstexpr int MIN_INPUT = -100;\nconstexpr int MAX_INPUT = 100;\n',
      editable: false,
      role: 'support',
    });
    if (draft.family === 'function') {
      draft.signature = {
        name: 'maximum',
        parameters: [
          { id: 'param-a', name: 'a', type: 'integer' },
          { id: 'param-b', name: 'b', type: 'integer' },
        ],
        returns: 'integer',
      };
      sourceFile.content = 'int maximum(int a, int b) {\n  return 0;\n}\n';
      draft.cases = [
        { kind: 'function', id: 'case-1', name: 'Larger second value', visibility: 'public', inputs: { 'param-a': '2', 'param-b': '7' }, expected: '7' },
        { kind: 'function', id: 'case-2', name: 'Larger first value', visibility: 'public', inputs: { 'param-a': '9', 'param-b': '3' }, expected: '9' },
        {
          kind: 'function',
          id: 'case-3',
          name: 'PRIVATE-CANARY negative values',
          visibility: 'private',
          inputs: { 'param-a': '-4', 'param-b': '-9' },
          expected: '-4',
        },
      ];
    } else {
      sourceFile.content = '#include <iostream>\nint main() {\n  // Read and echo a line.\n}\n';
      draft.cases = [
        { kind: 'stdio', id: 'case-1', name: 'Greeting', visibility: 'public', stdin: 'hello', expected: { mode: 'value', stdout: 'hello' } },
        { kind: 'stdio', id: 'case-2', name: 'Intentional empty output', visibility: 'public', stdin: '', expected: { mode: 'value', stdout: '' } },
        {
          kind: 'stdio',
          id: 'case-3',
          name: 'PRIVATE-CANARY multiline',
          visibility: 'private',
          stdin: 'private-line',
          expected: { mode: 'value', stdout: 'private-line' },
        },
      ];
    }
    if (id === 'stress' && draft.family === 'function') {
      draft.statement = `${draft.statement}\n\n${'This paragraph is synthetic long-form exercise content. '.repeat(60)}`;
      const first = draft.cases[0];
      if (!first) throw new Error('Stress fixture requires a case.');
      draft.cases = Array.from({ length: 20 }, (_, index) => ({
        ...structuredClone(first),
        id: `case-${index + 1}`,
        name: `Case ${index + 1}: a long descriptive name for a boundary condition`,
        visibility: index < 10 ? 'public' : 'private',
      }));
      draft.nextId = 100;
    }
    /** @type {Fixture['outcomes']} */
    const outcomes = draft.cases.map((entry) =>
      entry.kind === 'function'
        ? {
            caseId: entry.id,
            input: Object.values(entry.inputs).join(', '),
            expected: entry.expected,
            correct: entry.expected,
            incorrect: entry.id === 'case-2' ? '3' : '2',
          }
        : { caseId: entry.id, input: entry.stdin, expected: entry.expected.stdout, correct: entry.expected.stdout, incorrect: 'wrong' },
    );
    return { id, draft, correct, incorrect, outcomes };
  }

  window.codingPrototypeFixtures = { blank, example };
})();
