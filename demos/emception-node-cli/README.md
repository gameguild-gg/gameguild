# `@gameguild/emception-demo-node-cli`

Minimal Node-side demo of the emception toolchain. Wires together
[`@emception/node`](../../tools/emception/packages/node) and
[`@emception/core`](../../tools/emception/packages/core) to:

1. Load the bundled `@emception/sysroot` manifest from disk.
2. Open a workspace under `os.tmpdir()/emception-demo-node-cli`.
3. Seed it with a student submission + a hidden grader stub.
4. List workspace contents (visible vs. hidden).

`createEmception()` for Node is pending Phase 7.2 of `@emception/node`;
once it lands, the same workspace + manifest can be handed to a
`worker_threads` runtime to actually compile and run submissions.

## Usage

```bash
# from repo root
npm install --ignore-scripts
node demos/emception-node-cli/bin/grade.mjs demos/emception-node-cli/fixtures/hello.c

# or as a workspace bin
npx -w @gameguild/emception-demo-node-cli emception-grade fixtures/hello.c

# the npm test script runs the same flow against the bundled fixture
npm test -w @gameguild/emception-demo-node-cli
```
