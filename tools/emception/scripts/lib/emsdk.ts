import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd(); // Assume run from project root
const EMSDK_DIR = path.join(ROOT, 'tools', 'emsdk');

const EMSDK_ARCHITECTURES: Partial<Record<NodeJS.Architecture, string>> = {
  arm: 'arm',
  arm64: 'arm64',
  ia32: 'x86',
  x64: 'x86_64',
};

export function ensureEmsdkArchitecture(environment: NodeJS.ProcessEnv = process.env, architecture: NodeJS.Architecture = process.arch): void {
  if (environment.EMSDK_ARCH) return;
  const emsdkArchitecture = EMSDK_ARCHITECTURES[architecture];
  if (!emsdkArchitecture) throw new Error(`Unsupported Node architecture for emsdk: ${architecture}`);
  environment.EMSDK_ARCH = emsdkArchitecture;
}

export function ensureWindowsEmsdkShell(environment: NodeJS.ProcessEnv = process.env, platform: NodeJS.Platform = process.platform): void {
  if (platform !== 'win32') return;
  delete environment.EMSDK_BASH;
  delete environment.EMSDK_CSH;
  delete environment.EMSDK_FISH;
  delete environment.EMSDK_POWERSHELL;
  delete environment.MSYSTEM;
  delete environment.SHELL;
  environment.EMSDK_CMD = '1';
}

// Ensure shell commands fail on error
shell.config.fatal = true;

/**
 * Acquire a simple advisory lock using O_EXCL (atomic create).
 * Spins up to `timeoutMs` (default 120 s) waiting for the lock.
 * Returns a release function.
 */
function acquireEmsdkLock(timeoutMs = 120_000): () => void {
  const lockFile = path.join(EMSDK_DIR, '.emception-setup.lock');
  const deadline = Date.now() + timeoutMs;
  while (true) {
    try {
      const fd = fs.openSync(lockFile, 'wx'); // O_WRONLY|O_CREAT|O_EXCL — atomic
      fs.writeSync(fd, String(process.pid));
      fs.closeSync(fd);
      return () => { try { fs.unlinkSync(lockFile); } catch { } };
    } catch {
      if (Date.now() > deadline) throw new Error('Timed out waiting for emsdk setup lock');
      Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 250);
    }
  }
}

/**
 * Downloads, installs, activates EMSDK, and sets environment variables.
 * Returns the environment variables map.
 */
export function setupEmsdk(version: string = 'latest'): NodeJS.ProcessEnv {
  console.log(`>>> Setting up Emscripten SDK (${version})...`);
  ensureEmsdkArchitecture();
  ensureWindowsEmsdkShell();

  if (!fs.existsSync(path.join(EMSDK_DIR, 'emsdk'))) {
    console.log(`    Cloning EMSDK to ${EMSDK_DIR}...`);
    shell.mkdir('-p', path.dirname(EMSDK_DIR));
    shell.exec(`git clone --depth 1 https://github.com/emscripten-core/emsdk.git "${EMSDK_DIR}"`);
    // After a fresh clone there's no bundled Python yet — unset stale env vars
    // so the emsdk script falls back to system python3 and system certs.
    delete process.env.EMSDK_PYTHON;
    delete process.env.SSL_CERT_FILE;
    delete process.env.SSL_CERT_DIR;
    delete process.env.CURL_CA_BUNDLE;
    delete process.env.REQUESTS_CA_BUNDLE;
  }

  const originalCwd = process.cwd();
  shell.cd(EMSDK_DIR);

  const emsdkCmd = process.platform === 'win32' ? 'emsdk.bat' : './emsdk';

  // Marker file records that install+activate for this exact version already ran.
  // Multiple parallel scripts (e.g. build:imgui and build:raylib) check this to
  // avoid racing on the emsdk activate step which is not concurrency-safe.
  const markerFile = path.join(EMSDK_DIR, `.emception-activated-${version}`);

  if (!fs.existsSync(markerFile)) {
    // Acquire exclusive lock — other parallel processes will spin until released.
    const release = acquireEmsdkLock();
    try {
      // Double-check inside the lock: another process may have just written the marker.
      if (!fs.existsSync(markerFile)) {
        console.log(`    Installing ${version}...`);
        shell.exec(`${emsdkCmd} install ${version}`);

        console.log(`    Activating ${version}...`);
        shell.exec(`${emsdkCmd} activate ${version}`);

        fs.writeFileSync(markerFile, String(Date.now()));
      } else {
        console.log(`    Already activated by parallel process (skipping).`);
      }
    } finally {
      release();
    }
  } else {
    console.log(`    Already activated (skipping install/activate).`);
  }

  // Capture environment variables
  console.log('    Capturing environment variables...');
  const envVars: Record<string, string> = {};

  if (process.platform === 'win32') {
    // On Windows, run emsdk_env.bat and capture output of set
    // We use a temporary file to avoid parsing issues with pipe
    const tempFile = path.join(EMSDK_DIR, 'env.txt');
    shell.exec(`call emsdk_env.bat > NUL && set > "${tempFile}"`, { shell: 'cmd.exe' });
    const output = fs.readFileSync(tempFile, 'utf8');
    fs.unlinkSync(tempFile);

    output.split('\r\n').forEach(line => {
      const idx = line.indexOf('=');
      if (idx > 0) {
        const key = line.substring(0, idx);
        const val = line.substring(idx + 1);
        envVars[key] = val;
      }
    });
  } else {
    // On Unix, source emsdk_env.sh and print env
    const output = shell.exec(`source ./emsdk_env.sh > /dev/null && env`, { shell: '/bin/bash', silent: true }).stdout;
    output.split('\n').forEach(line => {
      const idx = line.indexOf('=');
      if (idx > 0) {
        const key = line.substring(0, idx);
        const val = line.substring(idx + 1);
        envVars[key] = val;
      }
    });
  }

  shell.cd(originalCwd);

  // Update current process env
  Object.assign(process.env, envVars);

  return process.env;
}

export function getEmsdkDir(): string {
  return EMSDK_DIR;
}
