/**
 * Build CMake as a standalone Emscripten WASM module.
 *
 * CMake is linked against libcurl-lite so file(DOWNLOAD), FetchContent,
 * and ExternalProject_Add route HTTP through the browser's fetch() API.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { standaloneFlags } from './lib/emcc-flags.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-cmake');

const ROOT = process.cwd();
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;
setupEmsdk(EMSDK_VERSION);

const CONCURRENCY = os.cpus().length;

const USERLAND_DIR = path.join(ROOT, 'userland', 'cmake');
const OUTPUT_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const LIBCURL_INC = path.join(ROOT, 'userland', 'libcurl-lite', 'include');
const LIBCURL_A = path.join(OUTPUT_DIR, 'libcurl.a');

// 4 MB stack — CMake can do deep recursion.
const STANDALONE_FLAGS = standaloneFlags({ stackSize: 4 * 1024 * 1024, asyncifyStackSize: 65536 });

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);

// Detect latest CMake release
const GITHUB_AUTH = process.env.GITHUB_TOKEN
    ? `-H "Authorization: token ${process.env.GITHUB_TOKEN}"`
    : '';

function detectCMakeVersion(): string {
    const envVer = process.env.CMAKE_VERSION;
    if (envVer) return envVer;

    // CMake 4.x changed CMakeBuildUtilities.cmake: when CMAKE_USE_SYSTEM_CURL=ON it
    // forces CMAKE_USE_SYSTEM_ZLIB=ON via a regular set() that overrides our -D flag,
    // and Emscripten's cross-compile sysroot has no system zlib. Cap at 3.x.
    const FALLBACK_CMAKE_VERSION = PINNED.CMAKE_VERSION;
    console.log('Detecting latest CMake 3.x release...');
    const prevFatal = shell.config.fatal;
    shell.config.fatal = false;
    const result = shell.exec(
        `curl -fsSL ${GITHUB_AUTH} "https://api.github.com/repos/Kitware/CMake/releases?per_page=100"`,
        { silent: true },
    );
    shell.config.fatal = prevFatal;
    if (result.code !== 0) {
        console.warn(`  GitHub API unavailable (exit ${result.code}), using fallback ${FALLBACK_CMAKE_VERSION}`);
        return FALLBACK_CMAKE_VERSION;
    }
    const releases = JSON.parse(result.stdout) as Array<{
        tag_name: string;
        prerelease: boolean;
        draft: boolean;
    }>;
    for (const rel of releases) {
        if (rel.prerelease || rel.draft) continue;
        const m = rel.tag_name.match(/^v(3\.\d+\.\d+)$/);
        if (m) {
            console.log(`  Latest CMake 3.x release: ${m[1]}`);
            return m[1];
        }
    }
    throw new Error('No CMake 3.x release found in recent 100 releases');
}

function escapeRegex(input: string): string {
    return input.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function isCMakeSourceDir(dirPath: string): boolean {
    if (!fs.existsSync(path.join(dirPath, 'CMakeLists.txt'))) return false;
    // bundled libarchive version file must exist for cross-compilation configure
    if (!fs.existsSync(path.join(dirPath, 'Utilities', 'cmlibarchive', 'build', 'version'))) return false;
    return true;
}

function findExistingSourceDir(version: string): string | null {
    const candidates = new Set<string>([
        path.join(USERLAND_DIR, `cmake-${version}`),
        path.join(USERLAND_DIR, `CMake-${version}`),
    ]);

    const versionPattern = new RegExp(`^cmake[-_]?${escapeRegex(version)}$`, 'i');
    for (const entry of fs.readdirSync(USERLAND_DIR, { withFileTypes: true })) {
        if (!entry.isDirectory()) continue;
        if (versionPattern.test(entry.name)) {
            candidates.add(path.join(USERLAND_DIR, entry.name));
        }
    }

    for (const candidate of candidates) {
        if (isCMakeSourceDir(candidate)) return candidate;
    }
    return null;
}

function ensureCMakeSource(version: string): string {
    const existing = findExistingSourceDir(version);
    if (existing) {
        console.log(`Using existing CMake source dir: ${path.basename(existing)}`);
        return existing;
    }

    const normalizedSourceDir = path.join(USERLAND_DIR, `cmake-${version}`);
    // Remove any incomplete source directory (e.g. gitignore stripped build/ subdirs)
    if (fs.existsSync(normalizedSourceDir)) {
        console.log(`Removing incomplete cmake source dir: ${path.basename(normalizedSourceDir)}`);
        shell.rm('-rf', normalizedSourceDir);
    }
    const tarball = `v${version}.tar.gz`;

    console.log(`Downloading CMake ${version}...`);
    shell.cd(USERLAND_DIR);
    shell.exec(`curl -fSL ${GITHUB_AUTH} -o "${tarball}" "https://github.com/Kitware/CMake/archive/refs/tags/${tarball}"`);

    shell.rm('-rf', normalizedSourceDir);
    shell.mkdir('-p', normalizedSourceDir);
    shell.exec(`tar xzf "${tarball}" --strip-components=1 -C "${normalizedSourceDir}"`);
    shell.rm('-f', tarball);

    if (!isCMakeSourceDir(normalizedSourceDir)) {
        throw new Error(`Extracted CMake source is invalid: ${normalizedSourceDir}`);
    }

    console.log(`Extracted CMake source to: ${path.basename(normalizedSourceDir)}`);
    return normalizedSourceDir;
}

const CMAKE_VERSION = detectCMakeVersion();
const SOURCE_DIR = ensureCMakeSource(CMAKE_VERSION);
const BUILD_WASM_DIR = path.join(SOURCE_DIR, 'build-wasm');

shell.cd(SOURCE_DIR);

// 2. Apply source patches (TS-based, not .patch files)
function patchSource(relPath: string, needle: string, replacement: string, label: string, marker?: string) {
    const filePath = path.join(SOURCE_DIR, relPath);
    const content = fs.readFileSync(filePath, 'utf8');
    // Use an explicit marker (if provided) or the full replacement to detect "already applied".
    const alreadyApplied = marker ? content.includes(marker) : content.includes(replacement);
    if (alreadyApplied) {
        console.log(`  [${label}] already applied — skipping`);
        return;
    }
    if (!content.includes(needle)) {
        throw new Error(`[${label}] needle not found in ${relPath} — upstream may have changed`);
    }
    fs.writeFileSync(filePath, content.replace(needle, replacement));
    console.log(`  [${label}] applied`);
}

// Emscripten subprocess dispatch: bypass libuv process chain in cmSystemTools.
// On Emscripten, fork/posix_spawn are not available.  Instead, route subprocess
// execution through system() → __emscripten_system → ToolRunner.systemCallback.
// This enables cmake's project() / ninja version / compiler detection to work.
patchSource(
    'Source/cmSystemTools.cxx',
    `bool cmSystemTools::RunSingleCommand(std::vector<std::string> const& command,
                                     std::string* captureStdOut,
                                     std::string* captureStdErr, int* retVal,
                                     const char* dir, OutputOption outputflag,
                                     cmDuration timeout, Encoding encoding)
{
  cmUVProcessChainBuilder builder;`,
    `bool cmSystemTools::RunSingleCommand(std::vector<std::string> const& command,
                                     std::string* captureStdOut,
                                     std::string* captureStdErr, int* retVal,
                                     const char* dir, OutputOption outputflag,
                                     cmDuration timeout, Encoding encoding)
{
#ifdef __EMSCRIPTEN__
  /* On Emscripten, fork/posix_spawn are not available (ENOSYS).
   * Bypass the libuv process chain and dispatch through system(),
   * which routes through __emscripten_system → systemCallback → ToolRunner.
   * The ToolRunner recursively spawns the target tool's WASM module. */
  {
    /* Build a shell-safe command string from the argv vector. */
    std::string cmdStr;
    for (size_t i = 0; i < command.size(); ++i) {
      if (i > 0) cmdStr += ' ';
      cmdStr += '"';
      for (char c : command[i]) {
        if (c == '"' || c == '\\\\') cmdStr += '\\\\';
        cmdStr += c;
      }
      cmdStr += '"';
    }

    /* Build the subprocess-request JSON.
     * The ToolRunner's systemCallback reads this file when it receives
     * the "__dispatch_subprocess" command. */
    std::string cwd;
    if (dir) {
      cwd = dir;
    } else {
      char cwdBuf[4096];
      if (getcwd(cwdBuf, sizeof(cwdBuf))) cwd = cwdBuf;
      else cwd = ".";
    }
    {
      FILE* f = fopen("/tmp/.subprocess_request", "w");
      if (f) {
        /* Simple JSON escaping for cmd and cwd strings. */
        std::string jCmd, jCwd;
        for (char c : cmdStr) {
          if (c == '"' || c == '\\\\') jCmd += '\\\\';
          jCmd += c;
        }
        for (char c : cwd) {
          if (c == '"' || c == '\\\\') jCwd += '\\\\';
          jCwd += c;
        }
        fprintf(f, "{\\"cmd\\":\\"%s\\",\\"cwd\\":\\"%s\\"}", jCmd.c_str(), jCwd.c_str());
        fclose(f);
      }
    }

    /* Dispatch — blocks via Asyncify until the ToolRunner finishes. */
    int rc = std::system("__dispatch_subprocess");
    int exitCode = (rc >> 8) & 0xFF;

    /* Helper to slurp a file into a string. */
    auto readTempFile = [](const char* path) -> std::string {
      FILE* f = fopen(path, "r");
      if (!f) return {};
      std::string result;
      char buf[4096];
      size_t n;
      while ((n = fread(buf, 1, sizeof(buf), f)) > 0) {
        result.append(buf, n);
      }
      fclose(f);
      return result;
    };

    if (captureStdOut) {
      *captureStdOut = readTempFile("/tmp/.subprocess_stdout");
    }
    if (captureStdErr) {
      *captureStdErr = readTempFile("/tmp/.subprocess_stderr");
    }
    if (retVal) {
      *retVal = exitCode;
    }
    if (outputflag == OUTPUT_PASSTHROUGH) {
      std::string out = readTempFile("/tmp/.subprocess_stdout");
      std::string err = readTempFile("/tmp/.subprocess_stderr");
      if (!out.empty()) cmSystemTools::Stdout(out);
      if (!err.empty()) cmSystemTools::Stderr(err);
    }
    return true;
  }
#endif /* __EMSCRIPTEN__ */

  cmUVProcessChainBuilder builder;`,
    'cmSystemTools-emscripten-subprocess',
    '__dispatch_subprocess',  // marker: detect any variant of the patch
);

// TODO: Emscripten subprocess dispatch for execute_process() / libuv.
// The cmSystemTools::RunSingleCommand patch above handles the critical path
// (ninja --version, compiler detection).  A full libuv-level patch for
// execute_process() requires handling the process lifecycle (waitpid, SIGCHLD,
// event loop completion) — and is left for a future iteration.

// Emscripten libuv: add platform-specific stubs (posix-poll, posix-hrtime, no-fsevents, etc.)
// Without this, libuv fails to link because Emscripten doesn't match any platform block.
patchSource(
    'Utilities/cmlibuv/CMakeLists.txt',
    'if(CMAKE_SYSTEM_NAME STREQUAL "AIX")',
    `if(CMAKE_SYSTEM_NAME STREQUAL "Emscripten")
  list(APPEND uv_headers
    include/uv/posix.h
    )
  list(APPEND uv_defines
    _GNU_SOURCE
    )
  list(APPEND uv_sources
    src/unix/no-fsevents.c
    src/unix/no-proctitle.c
    src/unix/posix-hrtime.c
    src/unix/posix-poll.c
    )
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "AIX")`,
    'cmlibuv-emscripten-platform',
);

// Emscripten libuv: include posix.h for uv__loop_s fields (poll_fds etc.)
// Without this, the uv_loop_s struct misses platform-specific fields and posix-poll.c fails.
patchSource(
    'Utilities/cmlibuv/include/uv/unix.h',
    `#elif defined(__CYGWIN__) || \\
      defined(__MSYS__)   || \\
      defined(__HAIKU__)  || \\
      defined(__QNX__)    || \\
      defined(__GNU__)
# include "posix.h"`,
    `#elif defined(__EMSCRIPTEN__)
# include "posix.h"
#elif defined(__CYGWIN__) || \\
      defined(__MSYS__)   || \\
      defined(__HAIKU__)  || \\
      defined(__QNX__)    || \\
      defined(__GNU__)
# include "posix.h"`,
    'cmlibuv-unix-h-emscripten',
);

// Decouple ZLIB from CURL: cmake's CMAKE_DEPENDENT_OPTION forces ZLIB=ON whenever
// CURL=ON (for ABI compat on native builds), but Emscripten's sysroot has no
// pre-built system zlib. Replace the dependent option with a plain option so our
// -DCMAKE_USE_SYSTEM_ZLIB=OFF is honoured even when CMAKE_USE_SYSTEM_CURL=ON.
patchSource(
    'CMakeLists.txt',
    `  CMAKE_DEPENDENT_OPTION(CMAKE_USE_SYSTEM_ZLIB "Use system-installed zlib"
    "\${CMAKE_USE_SYSTEM_LIBRARY_ZLIB}" "NOT CMAKE_USE_SYSTEM_LIBARCHIVE;NOT CMAKE_USE_SYSTEM_CURL" ON)`,
    `  option(CMAKE_USE_SYSTEM_ZLIB "Use system-installed zlib"
    "\${CMAKE_USE_SYSTEM_LIBRARY_ZLIB}")`,
    'cmake-decouple-zlib-from-curl',
);

// 3. Build with emcmake
console.log('Cross-compiling CMake for WASM...');
if (fs.existsSync(BUILD_WASM_DIR)) shell.rm('-rf', BUILD_WASM_DIR);
shell.mkdir('-p', BUILD_WASM_DIR);

// CMake needs to find our libcurl-lite
const curlFlags = fs.existsSync(LIBCURL_A)
    ? [
        `-DCURL_INCLUDE_DIR="${LIBCURL_INC}"`,
        `-DCURL_LIBRARY="${LIBCURL_A}"`,
        '-DCMAKE_USE_SYSTEM_CURL=ON',
    ]
    : ['-DCMAKE_USE_SYSTEM_CURL=OFF'];

const cmakeCmd = [
    'emcmake cmake',
    `-S "${SOURCE_DIR}"`,
    `-B "${BUILD_WASM_DIR}"`,
    '-DCMAKE_BUILD_TYPE=MinSizeRel',
    '-DBUILD_TESTING=OFF',
    '-DCMAKE_USE_OPENSSL=OFF',
    // Use bundled zlib — the Emscripten cross-compile env has no system zlib
    '-DCMAKE_USE_SYSTEM_ZLIB=OFF',
    ...curlFlags,
].join(' ');
console.log(cmakeCmd);
shell.exec(cmakeCmd);

// Build only the cmake executable (not ctest, cpack, etc.)
shell.exec(`emmake make -C "${BUILD_WASM_DIR}" -j${CONCURRENCY} cmake`);

// 4. Re-link as standalone module
console.log('Linking CMake as standalone WASM module...');

const toolWasm = path.join(OUTPUT_DIR, 'cmake.wasm');
const toolMjs = path.join(OUTPUT_DIR, 'cmake.mjs');

// Find the CMake libraries produced by the build.
const cmakeLibs = shell.find(BUILD_WASM_DIR)
    .filter(f => f.endsWith('.a') && !f.includes('CMakeTmp'));

// Find the main object files — these contain main() and are NOT in any .a
const cmakeMainObjs = shell.find(path.join(BUILD_WASM_DIR, 'Source', 'CMakeFiles', 'cmake.dir'))
    .filter(f => f.endsWith('.o'));

// Emscripten's sysroot zlib — CMake uses it instead of bundled cmzlib
const EMSDK_ZLIB = path.join(
    ROOT, 'tools', 'emsdk', 'upstream', 'emscripten', 'cache', 'sysroot',
    'lib', 'wasm32-emscripten', 'libz.a',
);

const linkCmd = [
    'em++',
    // Main object files (contain main())
    ...cmakeMainObjs.map(o => `"${o}"`),
    // Static libraries
    ...cmakeLibs.map(o => `"${o}"`),
    fs.existsSync(LIBCURL_A) ? `"${LIBCURL_A}"` : '',
    // When CMAKE_USE_SYSTEM_ZLIB=OFF, cmake builds its own libcmzlib.a (already
    // included in cmakeLibs above). Only link sysroot libz.a if it exists.
    fs.existsSync(EMSDK_ZLIB) ? `"${EMSDK_ZLIB}"` : '',
    STANDALONE_FLAGS,
    '-Os',
    `-o "${toolMjs}"`,
].filter(Boolean).join(' \\\n    ');

console.log(linkCmd);
shell.exec(linkCmd);

if (!fs.existsSync(toolWasm)) {
    console.error('ERROR: cmake.wasm not generated');
    process.exit(1);
}
console.log(`Created ${toolWasm} + ${toolMjs}`);

// 4b. Patch Emscripten glue: fix PIPEFS.poll to return POLLHUP on closed pipe ends
// Without this, kwsys ProcessUNIX.c's poll() loop spins forever because
// Emscripten's PIPEFS.poll returns 0 (no events) instead of POLLHUP when the
// other end of a pipe is closed, and Emscripten's ___syscall_poll ignores the
// timeout parameter.
{
    const mjsContent = fs.readFileSync(toolMjs, 'utf8');
    const pipefsNeedle = 'poll(stream,timeout,notifyCallback){var pipe=stream.node.pipe;if((stream.flags&2097155)===1){return 256|4}for(var bucket of pipe.buckets){if(bucket.offset-bucket.roffset>0){return 64|1}}return 0}';
    const pipefsReplacement = 'poll(stream,timeout,notifyCallback){var pipe=stream.node.pipe;if((stream.flags&2097155)===1){if(pipe.refcnt<=1)return 4|8;return 256|4}if(pipe.refcnt<=1){for(var bucket of pipe.buckets){if(bucket.offset-bucket.roffset>0){return 64|1|16}}return 16}for(var bucket of pipe.buckets){if(bucket.offset-bucket.roffset>0){return 64|1}}return 0}';
    if (mjsContent.includes(pipefsReplacement)) {
        console.log('  [pipefs-pollhup] already applied — skipping');
    } else if (!mjsContent.includes(pipefsNeedle)) {
        console.warn('  [pipefs-pollhup] needle not found in cmake.mjs — upstream Emscripten may have changed');
    } else {
        fs.writeFileSync(toolMjs, mjsContent.replace(pipefsNeedle, pipefsReplacement));
        console.log('  [pipefs-pollhup] applied');
    }
}

// 4c. Patch ___syscall_poll: return POLLHUP on pipe fds with infinite timeout
// Emscripten's ___syscall_poll ignores the timeout parameter — it returns 0
// immediately even when timeout=-1.  Since Emscripten can never spawn child
// processes (no fork/clone), an infinite-timeout poll on a pipe will never
// succeed.  Inject POLLHUP (read-end) / POLLERR (write-end) so callers like
// kwsys ProcessUNIX.c break out of their retry loops.
{
    const mjsContent = fs.readFileSync(toolMjs, 'utf8');
    const pollNeedle = 'if(stream.stream_ops.poll){flags=stream.stream_ops.poll(stream,-1)}else{flags=5}';
    const pollReplacement = 'if(stream.stream_ops.poll){flags=stream.stream_ops.poll(stream,-1);if(flags===0&&timeout<0&&stream.node&&stream.node.pipe){flags=(stream.flags&2097155)===1?12:16}}else{flags=5}';
    if (mjsContent.includes(pollReplacement)) {
        console.log('  [syscall-poll-pipe-hup] already applied — skipping');
    } else if (!mjsContent.includes(pollNeedle)) {
        console.warn('  [syscall-poll-pipe-hup] needle not found in cmake.mjs — upstream Emscripten may have changed');
    } else {
        fs.writeFileSync(toolMjs, mjsContent.replace(pollNeedle, pollReplacement));
        console.log('  [syscall-poll-pipe-hup] applied');
    }
}

// 5. Deploy to sysroot
console.log('Deploying to sysroot...');
for (const ext of ['.wasm', '.mjs']) {
    const src = path.join(OUTPUT_DIR, `cmake${ext}`);
    if (fs.existsSync(src)) shell.cp('-f', src, SYSROOT_LIB);
}

// 6. Copy CMake data files (Modules/, Templates/) to sysroot.
// Without these, cmake fails at runtime with "Could not find CMAKE_ROOT".
const CMAKE_MAJOR_MINOR = CMAKE_VERSION.split('.').slice(0, 2).join('.');
const SYSROOT_CMAKE_DATA = path.join(ROOT, 'sysroot', 'usr', 'share', `cmake-${CMAKE_MAJOR_MINOR}`);
shell.mkdir('-p', SYSROOT_CMAKE_DATA);

const modulesDir = path.join(SOURCE_DIR, 'Modules');
if (fs.existsSync(modulesDir)) {
    console.log(`Copying CMake Modules/ to ${SYSROOT_CMAKE_DATA}/Modules/`);
    shell.cp('-r', modulesDir, SYSROOT_CMAKE_DATA);
} else {
    console.warn('WARNING: CMake Modules/ directory not found in source tree');
}

const templatesDir = path.join(SOURCE_DIR, 'Templates');
if (fs.existsSync(templatesDir)) {
    console.log(`Copying CMake Templates/ to ${SYSROOT_CMAKE_DATA}/Templates/`);
    shell.cp('-r', templatesDir, SYSROOT_CMAKE_DATA);
} else {
    console.warn('WARNING: CMake Templates/ directory not found in source tree');
}

// Deploy Emception runtime toolchain file for cmake compiler detection
const toolchainSrc = path.join(ROOT, 'sysroot', 'usr', 'share', `cmake-${CMAKE_MAJOR_MINOR}`, 'toolchain-emception.cmake');
if (!fs.existsSync(toolchainSrc)) {
    console.warn('WARNING: toolchain-emception.cmake not found — cmake compiler detection may fail');
}
console.log(`Toolchain file: ${toolchainSrc}`);

// 7. Apply glue patches (systemCallback, Asyncify hooks, ENV merge) to the freshly
// deployed cmake.mjs in both build/ and sysroot/. Without these patches,
// std::system() returns -52 (ENOSYS) and callMain() cannot suspend for async I/O.
console.log('Applying glue patches to cmake.mjs...');
const patchGlueResult = shell.exec('npx tsx scripts/patch-glue.ts', { silent: false });
if (patchGlueResult.code !== 0) {
    console.error('WARNING: patch:glue failed — cmake.mjs may be missing systemCallback patches');
}

console.log('>>> CMake build complete.');
