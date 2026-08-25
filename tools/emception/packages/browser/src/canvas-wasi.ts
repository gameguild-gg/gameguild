export function createCanvasWasiImports(
    getMemory: () => WebAssembly.Memory | null,
    write: (text: string) => void,
): Record<string, CallableFunction> {
    const success = 0;
    const badFile = 8;
    const illegalSeek = 70;
    const setPair = (first: number, second: number): void => {
        const memory = getMemory();
        if (!memory) return;
        const view = new DataView(memory.buffer);
        view.setUint32(first, 0, true);
        view.setUint32(second, 0, true);
    };

    return {
        args_sizes_get(argc: number, bufferSize: number): number {
            setPair(argc, bufferSize);
            return success;
        },
        args_get: () => success,
        environ_sizes_get(count: number, bufferSize: number): number {
            setPair(count, bufferSize);
            return success;
        },
        environ_get: () => success,
        fd_write(fd: number, iovs: number, count: number, written: number): number {
            const memory = getMemory();
            if (!memory) return badFile;
            const view = new DataView(memory.buffer);
            const bytes = new Uint8Array(memory.buffer);
            const decoder = new TextDecoder();
            let total = 0;
            for (let index = 0; index < count; index += 1) {
                const offset = iovs + index * 8;
                const start = view.getUint32(offset, true);
                const length = view.getUint32(offset + 4, true);
                if (length > 0) write(decoder.decode(bytes.subarray(start, start + length)));
                total += length;
            }
            view.setUint32(written, total, true);
            return success;
        },
        fd_close: () => success,
        fd_seek: () => illegalSeek,
        fd_read: () => badFile,
        fd_fdstat_get: () => badFile,
        path_open: () => badFile,
        path_filestat_get: () => badFile,
        path_unlink_file: () => badFile,
        clock_time_get(clock: number, precisionLow: number, precisionHigh: number, target: number): number {
            void clock;
            void precisionLow;
            void precisionHigh;
            const memory = getMemory();
            if (memory) new DataView(memory.buffer).setBigUint64(target, BigInt(Math.round(performance.now() * 1_000_000)), true);
            return success;
        },
        clock_res_get(clock: number, target: number): number {
            void clock;
            const memory = getMemory();
            if (memory) new DataView(memory.buffer).setBigUint64(target, 1n, true);
            return success;
        },
        random_get(start: number, length: number): number {
            const memory = getMemory();
            if (memory) crypto.getRandomValues(new Uint8Array(memory.buffer, start, length));
            return success;
        },
        proc_exit(code: number): void {
            if (code !== 0) throw new Error(`Canvas runtime exited with ${code}`);
        },
    };
}
