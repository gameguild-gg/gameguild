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
import { setupEmsdk } from './lib/emsdk.ts';

const ROOT = process.cwd();

shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';
setupEmsdk(EMSDK_VERSION);

const CONCURRENCY = os.cpus().length;

const USERLAND_DIR = path.join(ROOT, 'userland', 'ninja');
const OUTPUT_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const LIBCURL_INC = path.join(ROOT, 'userland', 'libcurl-lite', 'include');
const LIBCURL_A = path.join(OUTPUT_DIR, 'libcurl.a');

/** Common Emscripten flags for standalone tool modules */
const STANDALONE_FLAGS = [
    '-sALLOW_MEMORY_GROWTH=1',
    '-sSTACK_SIZE=2097152',    // 2 MB stack
    '-sFORCE_FILESYSTEM=1',
    '-sMODULARIZE=1',
    '-sEXPORT_ES6=1',
    '-sEXIT_RUNTIME=1',
    '-sINVOKE_RUN=0',
    '-sEXPORTED_FUNCTIONS=_main',
    '-sEXPORTED_RUNTIME_METHODS=FS,callMain',
    // Emscripten JS-based exception handling — compatible with Asyncify.
    '-sDISABLE_EXCEPTION_CATCHING=0',
    // Asyncify: transparent async suspension for FS hooks + subprocess dispatch.
    '-sASYNCIFY',
    '-sASYNCIFY_STACK_SIZE=131072',   // 128 KB — ninja needs a deep stack for builder→subprocess→system() unwind
    `-sASYNCIFY_IMPORTS=${JSON.stringify([
        '__syscall_openat', '__syscall_stat64', '__syscall_lstat64',
        '__syscall_faccessat', '__syscall_readlinkat', '__syscall_newfstatat',
        '__emscripten_system',
    ])}`,
    '-mno-reference-types',
].join(' ');

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);

// Detect latest Ninja release from GitHub
function detectNinjaVersion(): string {
    const envVer = process.env.NINJA_VERSION;
    if (envVer) return envVer;

    console.log('Detecting latest Ninja release...');
    const result = shell.exec(
        'curl -fsSL https://api.github.com/repos/ninja-build/ninja/releases/latest',
        { silent: true },
    );
    if (result.code !== 0) {
        throw new Error('Failed to query GitHub for latest Ninja release');
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
if (!fs.existsSync(SOURCE_DIR)) {
    console.log(`Downloading Ninja ${NINJA_VERSION}...`);
    shell.cd(USERLAND_DIR);
    const tarball = `v${NINJA_VERSION}.tar.gz`;
    shell.exec(`curl -fSL -o "${tarball}" "https://github.com/ninja-build/ninja/archive/refs/tags/${tarball}"`);
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

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <unistd.h>
#include <sys/wait.h>

using namespace std;

Subprocess::Subprocess(bool use_console) : fd_(-1), pid_(-1),
                                           use_console_(use_console) {
}

Subprocess::~Subprocess() {
  // No OS resources to clean up in Emscripten
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

  // 3. Parse exit code from system() return value
  if (WIFEXITED(ret)) {
    exit_status_ = static_cast<ExitStatus>(WEXITSTATUS(ret));
  } else {
    exit_status_ = ExitFailure;
  }

  // 4. Read stdout from /tmp/.subprocess_stdout
  FILE* fout = fopen("/tmp/.subprocess_stdout", "r");
  if (fout) {
    char buf[4096];
    size_t n;
    while ((n = fread(buf, 1, sizeof(buf), fout)) > 0)
      buf_.append(buf, n);
    fclose(fout);
  }

  // 5. Read stderr and append (ninja shows combined output)
  FILE* ferr = fopen("/tmp/.subprocess_stderr", "r");
  if (ferr) {
    char buf[4096];
    size_t n;
    while ((n = fread(buf, 1, sizeof(buf), ferr)) > 0)
      buf_.append(buf, n);
    fclose(ferr);
  }

  // Mark as done immediately — synchronous in WASM
  pid_ = -1;
  fd_ = -1;

  return true;
}

void Subprocess::OnPipeReady() {
  // No-op — output is collected synchronously in Start()
}

bool Subprocess::TryFinish(int /*waitpid_options*/) {
  return true;  // Already done after Start()
}

ExitStatus Subprocess::Finish() {
  return exit_status_;
}

bool Subprocess::Done() const {
  return true;  // Always done after Start()
}

const string& Subprocess::GetOutput() const {
  return buf_;
}

// Static members (required by header, but unused in Emscripten)
volatile sig_atomic_t SubprocessSet::interrupted_ = 0;
volatile sig_atomic_t SubprocessSet::s_sigchld_received = 0;

void SubprocessSet::SetInterruptedFlag(int) {}
void SubprocessSet::SigChldHandler(int, siginfo_t*, void*) {}
void SubprocessSet::HandlePendingInterruption() {}
void SubprocessSet::CheckConsoleProcessTerminated() {}

SubprocessSet::SubprocessSet() {
  // No signal handling needed in Emscripten
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
if (!fs.existsSync(emscriptenSubprocessPath) || !fs.readFileSync(emscriptenSubprocessPath, 'utf8').includes('__dispatch_subprocess')) {
    console.log('Writing subprocess-emscripten.cc...');
    fs.writeFileSync(emscriptenSubprocessPath, SUBPROCESS_EMSCRIPTEN_CC);
    console.log('  [subprocess-emscripten.cc] created');
} else {
    console.log('  [subprocess-emscripten.cc] already exists — skipping');
}

// Patch CMakeLists.txt to use subprocess-emscripten.cc for Emscripten builds
patchSource(
    'CMakeLists.txt',
    `\ttarget_sources(libninja PRIVATE
\t\tsrc/jobserver-posix.cc
\t\tsrc/subprocess-posix.cc
\t)`,
    `\ttarget_sources(libninja PRIVATE
\t\tsrc/jobserver-posix.cc
\t)
\tif(EMSCRIPTEN)
\t\ttarget_sources(libninja PRIVATE src/subprocess-emscripten.cc)
\telse()
\t\ttarget_sources(libninja PRIVATE src/subprocess-posix.cc)
\tendif()`,
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
