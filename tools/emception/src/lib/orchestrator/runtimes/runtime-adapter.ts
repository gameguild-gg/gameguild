import type { RunOptions, ToolResult } from '../tool-runner';
import type { VFSManager } from '../vfs';

export interface RuntimeAdapterContext {
    tool: string;
    modulePath: string;
    argv: string[];
    options: RunOptions;
    vfs: VFSManager;
    runWasiFallback: (argv: string[], options: RunOptions) => Promise<ToolResult>;
    log: (message: string) => void;
}

/**
 * Runtime adapter contract for tool execution.
 */
export interface RuntimeAdapter {
    readonly name: string;
    run(context: RuntimeAdapterContext): Promise<ToolResult>;
}
