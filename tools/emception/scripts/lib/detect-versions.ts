/**
 * Detect tool versions from the active emsdk installation.
 *
 * After `setupEmsdk()` has been called, the emsdk directory contains
 * pre-built binaries for LLVM/Clang, Binaryen, and a bundled Python.
 * This module queries those binaries to determine their versions so
 * the build scripts can download and compile matching source code
 * instead of hardcoding version numbers.
 *
 * Each detector can be overridden via environment variables:
 *   LLVM_VERSION, BINARYEN_VERSION, PYTHON_VERSION
 */

import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { getEmsdkDir } from './emsdk.ts';

export interface ToolVersions {
    /** Full LLVM version, e.g. "20.0.0" */
    llvm: string;
    /** LLVM major version number, e.g. 20 */
    llvmMajor: number;
    /** Binaryen version number, e.g. "121" */
    binaryen: string;
    /** Full CPython version, e.g. "3.12.8" */
    python: string;
    /** Python major.minor, e.g. "3.12" */
    pythonMajorMinor: string;
    /** Python major+minor without dot, e.g. "312" */
    pythonMajorMinorCompact: string;
}

/**
 * Detect the LLVM/Clang version bundled with the active emsdk installation.
 * Parses the output of `clang --version` from emsdk's upstream/bin/.
 *
 * Returns the version string (e.g. "20.0.0"). Any non-numeric suffix
 * like "git" is stripped.
 *
 * NOTE: emsdk often bundles a pre-release/development Clang whose version
 * may not exist as a published LLVM source release. Use
 * `resolveAvailableLLVMRelease()` to find a downloadable version.
 */
export function detectLLVMVersion(): string {
    const emsdkDir = getEmsdkDir();
    const clangBin = path.join(emsdkDir, 'upstream', 'bin', 'clang');
    const result = shell.exec(`"${clangBin}" --version 2>&1`, { silent: true });
    if (result.code !== 0) {
        throw new Error(
            `Failed to run emsdk clang (${clangBin}). Is emsdk installed?\n` +
            `stdout: ${result.stdout}\nstderr: ${result.stderr}`
        );
    }
    // Match "clang version X.Y.Z" (possibly followed by "git" or other suffix)
    const match = result.stdout.match(/clang version (\d+)\.(\d+)\.(\d+)/);
    if (!match) {
        throw new Error(
            `Could not parse LLVM version from emsdk clang output:\n${result.stdout}`
        );
    }
    return `${match[1]}.${match[2]}.${match[3]}`;
}

/**
 * Detect the LLVM git commit hash from the emsdk clang binary.
 * Parses the "(https:/github.com/llvm/llvm-project <commit>)" from `clang --version`.
 * Returns the commit hash string, or null if not found.
 */
export function detectLLVMGitCommit(): string | null {
    const emsdkDir = getEmsdkDir();
    const clangBin = path.join(emsdkDir, 'upstream', 'bin', 'clang');
    const result = shell.exec(`"${clangBin}" --version 2>&1`, { silent: true });
    if (result.code !== 0) return null;
    // Match the git commit hash after llvm-project URL
    const match = result.stdout.match(/llvm-project\s+([0-9a-f]{40})/);
    return match ? match[1] : null;
}

/**
 * Resolve a downloadable LLVM source release version.
 *
 * emsdk often bundles a pre-release Clang (e.g. 23.0.0) that does not
 * have a published source tarball on GitHub.  This function:
 *   1. Tries the exact detected version
 *   2. Queries the GitHub Releases API for the latest release matching
 *      the detected major version
 *   3. Falls back to the previous major version (major-1)
 *
 * Returns a version string that has a downloadable source tarball.
 */
export function resolveAvailableLLVMRelease(detectedVersion: string): string {
    const major = parseInt(detectedVersion.split('.')[0], 10);

    // Quick check: does the exact detected version have a release?
    if (llvmReleaseExists(detectedVersion)) {
        return detectedVersion;
    }
    console.log(`    LLVM ${detectedVersion} has no published source release.`);

    // Try to find the latest release for the detected major version
    const sameMajor = findLatestLLVMReleaseForMajor(major);
    if (sameMajor) {
        console.log(`    Using latest release for major ${major}: ${sameMajor}`);
        return sameMajor;
    }

    // Fall back to previous major version
    const prevMajor = findLatestLLVMReleaseForMajor(major - 1);
    if (prevMajor) {
        console.log(`    Using latest release for major ${major - 1}: ${prevMajor}`);
        return prevMajor;
    }

    throw new Error(
        `Could not find a downloadable LLVM release for major version ${major} or ${major - 1}. ` +
        `Set LLVM_VERSION env var to a specific released version.`
    );
}

/** Check if an LLVM release tag exists on GitHub using a HEAD request. */
function llvmReleaseExists(version: string): boolean {
    const url = `https://github.com/llvm/llvm-project/releases/tag/llvmorg-${version}`;
    const authHeader = process.env.GITHUB_TOKEN
        ? `-H "Authorization: token ${process.env.GITHUB_TOKEN}"`
        : '';
    const result = shell.exec(
        `curl -sI ${authHeader} -o /dev/null -w "%{http_code}" -L "${url}"`,
        { silent: true }
    );
    return result.stdout.trim() === '200';
}

/** Query GitHub API for the latest LLVM release matching a major version. */
function findLatestLLVMReleaseForMajor(major: number): string | null {
    // GitHub API returns releases sorted newest first (paginated, 30 per page).
    // We check the first 2 pages which covers recent releases.
    const authHeader = process.env.GITHUB_TOKEN
        ? `-H "Authorization: token ${process.env.GITHUB_TOKEN}"`
        : '';
    for (let page = 1; page <= 2; page++) {
        const apiUrl = `https://api.github.com/repos/llvm/llvm-project/releases?per_page=30&page=${page}`;
        const result = shell.exec(
            `curl -sf ${authHeader} -H "Accept: application/vnd.github.v3+json" "${apiUrl}"`,
            { silent: true }
        );
        if (result.code !== 0) continue;

        try {
            const releases = JSON.parse(result.stdout) as Array<{ tag_name: string; prerelease: boolean; draft: boolean }>;
            for (const rel of releases) {
                if (rel.prerelease || rel.draft) continue;
                // Tags are like "llvmorg-20.1.0"
                const m = rel.tag_name.match(/^llvmorg-(\d+)\.(\d+)\.(\d+)$/);
                if (m && parseInt(m[1], 10) === major) {
                    return `${m[1]}.${m[2]}.${m[3]}`;
                }
            }
        } catch {
            continue;
        }
    }
    return null;
}

/**
 * Detect the Binaryen version bundled with the active emsdk installation.
 * Parses the output of `wasm-opt --version`.
 *
 * Returns the version number as a string (e.g. "121").
 */
export function detectBinaryenVersion(): string {
    const emsdkDir = getEmsdkDir();
    const wasmOptBin = path.join(emsdkDir, 'upstream', 'bin', 'wasm-opt');
    const result = shell.exec(`"${wasmOptBin}" --version 2>&1`, { silent: true });
    if (result.code !== 0) {
        throw new Error(
            `Failed to run emsdk wasm-opt (${wasmOptBin}). Is emsdk installed?\n` +
            `stdout: ${result.stdout}\nstderr: ${result.stderr}`
        );
    }
    // Match "version NNN" (e.g. "wasm-opt version 121 (version_121)")
    const match = result.stdout.match(/version (\d+)/);
    if (!match) {
        throw new Error(
            `Could not parse Binaryen version from emsdk wasm-opt output:\n${result.stdout}`
        );
    }
    return match[1];
}

/**
 * Detect the Python version bundled with the active emsdk installation.
 * Uses the EMSDK_PYTHON environment variable set by `emsdk activate`.
 *
 * Returns the full version string (e.g. "3.12.8").
 */
export function detectPythonVersion(): string {
    const candidates: string[] = [];

    // Legacy emsdk env var (not always present in newer SDK releases)
    if (process.env.EMSDK_PYTHON) {
        candidates.push(process.env.EMSDK_PYTHON);
    }

    // Some environments export PYTHON from emsdk_env.sh
    if (process.env.PYTHON) {
        candidates.push(process.env.PYTHON);
    }

    // Probe emsdk-managed Python installations directly from disk.
    const emsdkFromEnv = process.env.EMSDK;
    const emsdkDir = emsdkFromEnv || getEmsdkDir();
    const emsdkPythonDir = path.join(emsdkDir, 'python');
    if (fs.existsSync(emsdkPythonDir)) {
        const entries = fs.readdirSync(emsdkPythonDir, { withFileTypes: true })
            .filter(e => e.isDirectory())
            .map(e => path.join(emsdkPythonDir, e.name));

        for (const base of entries) {
            candidates.push(path.join(base, 'bin', 'python3'));
            candidates.push(path.join(base, 'bin', 'python'));
            candidates.push(path.join(base, 'python.exe'));
        }
    }

    // Final fallback: host python on PATH (common on GitHub runners).
    candidates.push('python3');
    candidates.push('python');

    const uniqueCandidates = [...new Set(candidates.filter(Boolean))];

    let chosenPython: string | null = null;
    let result: shell.ExecOutputReturnValue | null = null;
    for (const candidate of uniqueCandidates) {
        const cmd = candidate === 'python3' || candidate === 'python'
            ? `${candidate} --version 2>&1`
            : `"${candidate}" --version 2>&1`;

        const probe = shell.exec(cmd, { silent: true });
        if (probe.code === 0) {
            chosenPython = candidate;
            result = probe;
            break;
        }
    }

    if (!chosenPython || !result) {
        throw new Error(
            'Could not find a usable Python interpreter for emsdk tool detection. ' +
            'Tried EMSDK_PYTHON, PYTHON, emsdk/python/*, and system python3/python.'
        );
    }

    if (chosenPython !== process.env.EMSDK_PYTHON) {
        console.log(`    Using Python interpreter for detection: ${chosenPython}`);
    }

    if (result.code !== 0) {
        throw new Error(
            `Failed to run Python (${chosenPython}).\n` +
            `stdout: ${result.stdout}\nstderr: ${result.stderr}`
        );
    }
    // Match "Python X.Y.Z"
    const match = result.stdout.match(/Python (\d+\.\d+\.\d+)/);
    if (!match) {
        throw new Error(
            `Could not parse Python version from EMSDK_PYTHON output:\n${result.stdout}`
        );
    }
    return match[1];
}

/**
 * Helper: extract major.minor from a full Python version string.
 * e.g. "3.12.8" → "3.12"
 */
export function pythonMajorMinor(version: string): string {
    const parts = version.split('.');
    return `${parts[0]}.${parts[1]}`;
}

/**
 * Helper: extract compact major+minor from a full Python version string.
 * e.g. "3.12.8" → "312"
 */
export function pythonMajorMinorCompact(version: string): string {
    const parts = version.split('.');
    return `${parts[0]}${parts[1]}`;
}

/**
 * Detect all tool versions from the active emsdk installation.
 * Must be called after setupEmsdk().
 *
 * Each version can be overridden via environment variables:
 *   LLVM_VERSION, BINARYEN_VERSION, PYTHON_VERSION
 */
export function detectAllVersions(): ToolVersions {
    const llvm = process.env.LLVM_VERSION || detectLLVMVersion();
    const binaryen = process.env.BINARYEN_VERSION || detectBinaryenVersion();
    const python = process.env.PYTHON_VERSION || detectPythonVersion();

    const pyMM = pythonMajorMinor(python);
    const pyMMC = pythonMajorMinorCompact(python);

    console.log(`Detected tool versions:`);
    console.log(`  LLVM/Clang: ${llvm}${process.env.LLVM_VERSION ? ' (from LLVM_VERSION env)' : ' (from emsdk)'}`);
    console.log(`  Binaryen:   ${binaryen}${process.env.BINARYEN_VERSION ? ' (from BINARYEN_VERSION env)' : ' (from emsdk)'}`);
    console.log(`  Python:     ${python}${process.env.PYTHON_VERSION ? ' (from PYTHON_VERSION env)' : ' (from emsdk)'}`);

    return {
        llvm,
        llvmMajor: parseInt(llvm.split('.')[0], 10),
        binaryen,
        python,
        pythonMajorMinor: pyMM,
        pythonMajorMinorCompact: pyMMC,
    };
}
