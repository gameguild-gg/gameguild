/**
 * Exports the subprocess shim Python source as a string.
 *
 * The actual implementation lives in subprocess_shim.py (proper Python file
 * with syntax highlighting & linting). It gets imported as a raw string via
 * bundler `?raw` support.
 *
 * This shim is injected into CPython's module path as `subprocess.py`,
 * replacing the standard library's subprocess module so that
 * subprocess.run() / Popen() route through the ToolRunner via Asyncify.
 */

import SUBPROCESS_SHIM from './subprocess_shim.py?raw';

export { SUBPROCESS_SHIM };
