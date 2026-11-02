import { describe, expect, it, vi } from 'vitest'
import { loadPyodideRuntime } from './pyodide-runtime'
import { gzip as pakoGzip } from 'pako'

/**
 * End-to-end loader test: the worker-side loader must fetch the gzipped
 * bootstrap, gunzip it, wrap the UMD script so it can be dynamically imported
 * as an ES module, and recover the `loadPyodide` factory from its default
 * export. Only the browser-provided blob-URL plumbing is stubbed (with a
 * data: URL), everything else — fetch, pako, the wrapper, dynamic import —
 * runs for real.
 */

const APPENDED_EXPORT = `\n;export default typeof loadPyodide !== 'undefined' ? loadPyodide : self.loadPyodide;\n`;

function gzip(text: string): ArrayBuffer {
  const bytes = pakoGzip(new TextEncoder().encode(text));
  const buffer = new ArrayBuffer(bytes.byteLength);
  new Uint8Array(buffer).set(bytes);
  return buffer;
}

function toDataUrl(moduleSource: string): string {
  return `data:text/javascript;base64,${Buffer.from(moduleSource, 'utf8').toString('base64')}`;
}

describe('loadPyodideRuntime', () => {
  it('loads the gzipped UMD bootstrap, wraps it as an ES module and boots Pyodide with indexURL', async () => {
    const umdSource = 'var loadPyodide = async function (options) { return { bootedWith: options }; };';
    const expectedModule = umdSource + APPENDED_EXPORT;

    const fetchMock = vi.fn(async () => new Response(gzip(umdSource), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);
    const createObjectUrlSpy = vi.spyOn(URL, 'createObjectURL').mockImplementation(() => toDataUrl(expectedModule));
    const revokeObjectUrlSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});

    try {
      const runtime = (await loadPyodideRuntime('/langs/pyodide.js.gz', '/langs/')) as unknown as { bootedWith: { indexURL?: string } };

      expect(fetchMock).toHaveBeenCalledWith('/langs/pyodide.js.gz', { cache: 'force-cache' });
      expect(createObjectUrlSpy).toHaveBeenCalledTimes(1);
      expect(revokeObjectUrlSpy).toHaveBeenCalledWith(createObjectUrlSpy.mock.results[0]?.value)
      expect(runtime.bootedWith).toEqual({ indexURL: '/langs/', fullStdLib: false })
    } finally {
      vi.unstubAllGlobals()
      createObjectUrlSpy.mockRestore()
      revokeObjectUrlSpy.mockRestore()
    }
  })

  it('surfaces fetch failures with the HTTP status', async () => {
    const fetchMock = vi.fn(async () => new Response('nope', { status: 404, statusText: 'Not Found' }));
    vi.stubGlobal('fetch', fetchMock);
    try {
      await expect(loadPyodideRuntime('/langs/pyodide.js.gz', '/langs/')).rejects.toThrow(/HTTP 404/)
    } finally {
      vi.unstubAllGlobals()
    }
  })
})
