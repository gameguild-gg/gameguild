/**
 * Build Ninja as a standalone Emscripten WASM module.
 *
 * Ninja has minimal dependencies — just a C++ compiler. We download the
 * source, cross-compile with emcmake/emmake, and produce ninja.wasm + ninja.mjs.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { standaloneFlags } from './lib/emcc-flags.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-ninja');

const ROOT = process.cwd();

shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;
setupEmsdk(EMSDK_VERSION);

const CONCURRENCY = os.cpus().length;

const USERLAND_DIR = path.join(ROOT, 'userland', 'ninja');
const OUTPUT_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const LIBCURL_INC = path.join(ROOT, 'userland', 'libcurl-lite', 'include');
const LIBCURL_A = path.join(OUTPUT_DIR, 'libcurl.a');

// 2 MB stack; 128 KB Asyncify stack — ninja needs a deep unwind for builder→subprocess→system().
const STANDALONE_FLAGS = standaloneFlags({ stackSize: 2 * 1024 * 1024, asyncifyStackSize: 131072 });

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);

const GITHUB_AUTH = process.env.GITHUB_TOKEN
  ? `-H "Authorization: token ${process.env.GITHUB_TOKEN}"`
  : '';

// Detect latest Ninja release from GitHub
function detectNinjaVersion(): string {
  const envVer = process.env.NINJA_VERSION;
  if (envVer) return envVer;

  const FALLBACK_NINJA_VERSION = PINNED.NINJA_VERSION;
  console.log('Detecting latest Ninja release...');
  const authHeader = process.env.GITHUB_TOKEN
    ? `-H "Authorization: token ${process.env.GITHUB_TOKEN}"`
    : '';
  const prevFatal = shell.config.fatal;
  shell.config.fatal = false;
  const result = shell.exec(
    `curl -fsSL ${authHeader} https://api.github.com/repos/ninja-build/ninja/releases/latest`,
    { silent: true },
  );
  shell.config.fatal = prevFatal;
  if (result.code !== 0) {
    console.warn(`  GitHub API unavailable (exit ${result.code}), using fallback ${FALLBACK_NINJA_VERSION}`);
    return FALLBACK_NINJA_VERSION;
  }
  const data = JSON.parse(result.stdout);
  const tag = (data.tag_name as string).replace(/^v/, '');
  console.log(`  Latest Ninja release: ${tag}`);
  return tag;
}

const NINJA_VERSION = detectNinjaVersion();
const SOURCE_DIR = path.join(USERLAND_DIR, `ninja-${NINJA_VERSION}`);
const BUILD_WASM_DIR = path.join(SOURCE_DIR, 'build-wasm');

// 1. Download source
// Validate by checking CMakeLists.txt — missing means incomplete cache entry.
const isNinjaSourceValid = fs.existsSync(path.join(SOURCE_DIR, 'CMakeLists.txt'));
if (!isNinjaSourceValid) {
  if (fs.existsSync(SOURCE_DIR)) {
    console.log(`Removing incomplete Ninja source dir: ${path.basename(SOURCE_DIR)}`);
    shell.rm('-rf', SOURCE_DIR);
  }
  console.log(`Downloading Ninja ${NINJA_VERSION}...`);
  shell.cd(USERLAND_DIR);
  const tarball = `v${NINJA_VERSION}.tar.gz`;
  shell.exec(`curl -fSL ${GITHUB_AUTH} -o "${tarball}" "https://github.com/ninja-build/ninja/archive/refs/tags/${tarball}"`);
  shell.exec(`tar xzf "${tarball}"`);
  shell.rm(tarball);
}

shell.cd(SOURCE_DIR);

// 2. Apply source patches (TS-based, not .patch files)
function patchSource(relPath: string, needle: string, replacement: string, label: string) {
  const filePath = path.join(SOURCE_DIR, relPath);
  const content = fs.readFileSync(filePath, 'utf8');
  if (content.includes(replacement)) {
    console.log(`  [${label}] already applied — skipping`);
    return;
  }
  if (!content.includes(needle)) {
    throw new Error(`[${label}] needle not found in ${relPath} — upstream may have changed`);
  }
  fs.writeFileSync(filePath, content.replace(needle, replacement));
  console.log(`  [${label}] applied`);
}

// Emscripten has no sched_getaffinity — return 1 (single-threaded WASM)
patchSource(
  'src/util.cc',
  'int GetProcessorCount() {\n#ifdef _WIN32',
  'int GetProcessorCount() {\n#ifdef __EMSCRIPTEN__\n  return 1;  // WASM is single-threaded\n#elif defined(_WIN32)',
  'GetProcessorCount-emscripten',
);

// Emscripten: replace posix_spawn with system("__dispatch_subprocess") dispatch.
// In the browser WASM environment, posix_spawn is not available. Instead,
// we write the command as JSON to /tmp/.subprocess_request and call
// system("__dispatch_subprocess"). The JS glue code (patched by patch-glue.ts)
// intercepts this and dispatches the command to the appropriate WASM tool.
const SUBPROCESS_EMSCRIPTEN_CC = `\
// Emscripten/WASM subprocess dispatch via system("__dispatch_subprocess").
//
// Instead of posix_spawn (unavailable in WASM), commands are serialized as JSON
// to /tmp/.subprocess_request and dispatched via system("__dispatch_subprocess").
// The JS glue code intercepts this and runs the actual subprocess tool.
// Results are read back from /tmp/.subprocess_stdout and /tmp/.subprocess_stderr.

#include "subprocess.h"
#include "exit_status.h"
#include "util.h"

#include <cassert>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <signal.h>
#include <unistd.h>
#include <sys/wait.h>

using namespace std;

Subprocess::Subprocess(bool use_console) : fd_(-1), pid_(-1),
                                           exit_status_(ExitSuccess),
                                           use_console_(use_console) {
}

Subprocess::~Subprocess() {
  // No OS resources to clean up in Emscripten — synchronous dispatch
}

static void writeJsonEscaped(FILE* f, const char* s) {
  for (; *s; ++s) {
    switch (*s) {
      case '"':  fputs("\\\\\\"", f); break;
      case '\\\\': fputs("\\\\\\\\", f); break;
      case '\\n': fputs("\\\\n", f); break;
      case '\\r': fputs("\\\\r", f); break;
      case '\\t': fputs("\\\\t", f); break;
      default:   fputc(*s, f); break;
    }
  }
}

bool Subprocess::Start(SubprocessSet* set, const string& command) {
  // 1. Write the command as JSON to /tmp/.subprocess_request
  FILE* f = fopen("/tmp/.subprocess_request", "w");
  if (!f)
    Fatal("Failed to open /tmp/.subprocess_request for writing");

  fputs("{\\"cmd\\":\\"", f);
  writeJsonEscaped(f, command.c_str());

  char cwd_buf[4096];
  const char* cwd = getcwd(cwd_buf, sizeof(cwd_buf));
  if (!cwd) cwd = "/";

  fputs("\\",\\"cwd\\":\\"", f);
  writeJsonEscaped(f, cwd);
  fputs("\\"}", f);
  fclose(f);

  // 2. Dispatch via system() — intercepted by JS glue (systemCallback)
  int ret = system("__dispatch_subprocess");

  // 3. Read stdout from /tmp/.subprocess_stdout
  FILE* fout = fopen("/tmp/.subprocess_stdout", "r");
  if (fout) {
    char buf[4096];
    size_t n;
    while ((n = fread(buf, 1, sizeof(buf), fout)) > 0)
      buf_.append(buf, n);
    fclose(fout);
  }

  // 4. Read stderr and append (ninja shows combined output)
  FILE* ferr = fopen("/tmp/.subprocess_stderr", "r");
  if (ferr) {
    char buf[4096];
    size_t n;
    while ((n = fread(buf, 1, sizeof(buf), ferr)) > 0)
      buf_.append(buf, n);
    fclose(ferr);
  }

  // 5. Store exit status and mark fd_ closed so Done() returns true
  if (WIFEXITED(ret)) {
    exit_status_ = WEXITSTATUS(ret) == 0 ? ExitSuccess : ExitFailure;
  } else {
    exit_status_ = ExitFailure;
  }
  fd_ = -1;  // marks Done() = true

  return true;
}

void Subprocess::OnPipeReady() {
  // No-op — output is collected synchronously in Start()
}

bool Subprocess::TryFinish(int waitpid_options) {
  return true;  // Always done (synchronous)
}

ExitStatus Subprocess::Finish() {
  return exit_status_;
}

bool Subprocess::Done() const {
  return fd_ == -1;
}

const string& Subprocess::GetOutput() const {
  return buf_;
}

// Static member definitions (required by subprocess.h)
volatile sig_atomic_t SubprocessSet::interrupted_ = 0;
volatile sig_atomic_t SubprocessSet::s_sigchld_received = 0;

void SubprocessSet::SetInterruptedFlag(int signum) {
  interrupted_ = signum;
}

void SubprocessSet::SigChldHandler(int signo, siginfo_t* info, void* context) {
  (void)signo; (void)info; (void)context;
  s_sigchld_received = 1;
}

void SubprocessSet::HandlePendingInterruption() {}

void SubprocessSet::CheckConsoleProcessTerminated() {}

SubprocessSet::SubprocessSet() {
  memset(&old_int_act_, 0, sizeof(old_int_act_));
  memset(&old_term_act_, 0, sizeof(old_term_act_));
  memset(&old_hup_act_, 0, sizeof(old_hup_act_));
  memset(&old_chld_act_, 0, sizeof(old_chld_act_));
  sigemptyset(&old_mask_);
}

SubprocessSet::~SubprocessSet() {
  Clear();
}

Subprocess* SubprocessSet::Add(const string& command, bool use_console) {
  Subprocess* subprocess = new Subprocess(use_console);
  if (!subprocess->Start(this, command)) {
    delete subprocess;
    return 0;
  }
  // Subprocess completes synchronously — put directly in finished queue
  finished_.push(subprocess);
  return subprocess;
}

bool SubprocessSet::DoWork() {
  // All subprocesses complete synchronously in Add() — nothing to wait for
  return IsInterrupted();
}

Subprocess* SubprocessSet::NextFinished() {
  if (finished_.empty())
    return NULL;
  Subprocess* subproc = finished_.front();
  finished_.pop();
  return subproc;
}

void SubprocessSet::Clear() {
  for (vector<Subprocess*>::iterator i = running_.begin();
       i != running_.end(); ++i)
    delete *i;
  running_.clear();
}
`;

const emscriptenSubprocessPath = path.join(SOURCE_DIR, 'src', 'subprocess-emscripten.cc');
const existingCC = fs.existsSync(emscriptenSubprocessPath) ? fs.readFileSync(emscriptenSubprocessPath, 'utf8') : '';
if (!existingCC.includes('__dispatch_subprocess') || !existingCC.includes('TryFinish')) {
  console.log('Writing subprocess-emscripten.cc...');
  fs.writeFileSync(emscriptenSubprocessPath, SUBPROCESS_EMSCRIPTEN_CC);
  console.log('  [subprocess-emscripten.cc] created');
} else {
  console.log('  [subprocess-emscripten.cc] already exists — skipping');
}

// Patch CMakeLists.txt to use subprocess-emscripten.cc for Emscripten builds
patchSource(
  'CMakeLists.txt',
  `else()\n\ttarget_sources(libninja PRIVATE\n\t\tsrc/jobserver-posix.cc\n\t\tsrc/subprocess-posix.cc\n\t)`,
  `else()\n\ttarget_sources(libninja PRIVATE\n\t\tsrc/jobserver-posix.cc\n\t)\n\tif(EMSCRIPTEN)\n\t\ttarget_sources(libninja PRIVATE src/subprocess-emscripten.cc)\n\telse()\n\t\ttarget_sources(libninja PRIVATE src/subprocess-posix.cc)\n\tendif()`,
  'emscripten-subprocess-cmakelists',
);

// 3. Build with emcmake
console.log('Cross-compiling Ninja for WASM...');
if (fs.existsSync(BUILD_WASM_DIR)) shell.rm('-rf', BUILD_WASM_DIR);
shell.mkdir('-p', BUILD_WASM_DIR);

const cmakeCmd = [
  'emcmake cmake',
  `-S "${SOURCE_DIR}"`,
  `-B "${BUILD_WASM_DIR}"`,
  '-DCMAKE_BUILD_TYPE=MinSizeRel',
  '-DBUILD_TESTING=OFF',
].join(' ');
console.log(cmakeCmd);
shell.exec(cmakeCmd);

shell.exec(`emmake make -C "${BUILD_WASM_DIR}" -j${CONCURRENCY} ninja`);

// 4. Re-link as standalone module with libcurl-lite
console.log('Linking Ninja as standalone WASM module...');

// Find all .o files from the ninja build, excluding CMake internal test artifacts
// (e.g. _CMakeLTOTest-CXX which contains its own main() returning 0x42=66)
const ninjaObjs = shell.find(BUILD_WASM_DIR)
  .filter(f => f.endsWith('.o') && !f.includes('CMakeFiles/CMakeTmp') && !f.includes('_CMakeLTOTest'));

const toolWasm = path.join(OUTPUT_DIR, 'ninja.wasm');
const toolMjs = path.join(OUTPUT_DIR, 'ninja.mjs');

const linkCmd = [
  'em++',
  ...ninjaObjs.map(o => `"${o}"`),
  fs.existsSync(LIBCURL_A) ? `"${LIBCURL_A}"` : '',
  fs.existsSync(LIBCURL_A) ? `-I "${LIBCURL_INC}"` : '',
  STANDALONE_FLAGS,
  '-Os',
  `-o "${toolMjs}"`,
].filter(Boolean).join(' \\\n    ');

console.log(linkCmd);
shell.exec(linkCmd);

if (!fs.existsSync(toolWasm)) {
  console.error('ERROR: ninja.wasm not generated');
  process.exit(1);
}
console.log(`Created ${toolWasm} + ${toolMjs}`);

// 5. Deploy to sysroot
console.log('Deploying to sysroot...');
for (const ext of ['.wasm', '.mjs']) {
  const src = path.join(OUTPUT_DIR, `ninja${ext}`);
  if (fs.existsSync(src)) shell.cp('-f', src, SYSROOT_LIB);
}

console.log('>>> Ninja build complete.');
