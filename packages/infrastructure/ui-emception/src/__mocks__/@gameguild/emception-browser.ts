// Manual mock for @gameguild/emception-browser
const stubClient = {
  writeFile: jest.fn().mockResolvedValue(undefined),
  getFile: jest.fn().mockResolvedValue(null),
  listDir: jest.fn().mockResolvedValue([]),
  resetVfs: jest.fn().mockResolvedValue(undefined),
  terminate: jest.fn(),
  run: jest.fn().mockResolvedValue({ exitCode: 0, stdout: '', stderr: '' }),
};
const stubTty = {
  clear: jest.fn(),
  writeLine: jest.fn(),
  write: jest.fn(),
  writeError: jest.fn(),
  readByteExclusive: jest.fn().mockReturnValue(null),
  isExclusiveStdin: false,
};

module.exports = {
  DEFAULT_MANIFEST_URL: 'https://cdn.jsdelivr.net/npm/emception/cdn/manifest.json',
  bootInWorker: jest.fn().mockResolvedValue({ client: stubClient, tty: stubTty }),
  wrapWorkerClient: jest.fn().mockReturnValue({
    compileAndRun: jest.fn().mockResolvedValue({ exitCode: 0, stdout: '', stderr: '' }),
    runTests: jest.fn().mockResolvedValue({ passed: 0, failed: 0, totalDurationMs: 0, cases: [] }),
    workspace: { readFile: jest.fn(), writeFile: jest.fn(), listFiles: jest.fn().mockResolvedValue([]), reset: jest.fn() },
    run: jest.fn().mockResolvedValue({ exitCode: 0, stdout: '', stderr: '' }),
    on: jest.fn().mockReturnValue(() => {}),
    dispose: jest.fn(),
  }),
  stubClient,
  stubTty,
};
