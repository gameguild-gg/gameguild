// ESM loader hook for the element test. The real `@gameguild/emception-browser`
// package is browser-only: it transitively imports a `.py` file
// (subprocess_shim.py?raw) and `@xterm/xterm`, neither of which Node's ESM
// loader can handle. None of the tests exercise `compileAndRun`, so redirect
// the bare specifier to a stub module instead of loading the real package.
export async function resolve(specifier, context, nextResolve) {
    if (specifier === '@gameguild/emception-browser') {
        return {
            url: 'data:text/javascript,' + encodeURIComponent(
                'export const compileAndRun = async () =>' +
                ' ({ exitCode: 0, finalPhase: "done" });',
            ),
            shortCircuit: true,
        };
    }
    return nextResolve(specifier, context);
}
