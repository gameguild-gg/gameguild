import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';

const PYTHON_HELLO = `# Python 3 — runs directly in the browser via WebAssembly
name = input("What is your name? ")
print(f"Hello, {name}! Welcome to Python in the browser.")

for i in range(5):
    print(f"  {i+1}. Python + WebAssembly = ❤️")
`;

export const PYTHON_PRESET: WorkspaceConfig = {
    id: 'python',
    label: 'Python Script',
    description: 'Run Python 3 directly in the browser terminal',
    version: 1,
    compile: {
        tool: 'python3',
        args: ['python3', '{sourceFile}'],
        output: '',
        toolchain: ToolchainPreset.Python,
        sourceDetect: { extensions: ['.py'], entryPoint: 'main.py' },
    },
    run: {
        type: 'python-script',
        tool: 'python3',
        args: ['python3', '{sourceFile}'],
    },
    features: {
        canvas: false,
        terminalInput: true,
        showTestButton: false,
    },
    files: {
        'main.py': { encoding: 'text', content: PYTHON_HELLO },
    },
};
