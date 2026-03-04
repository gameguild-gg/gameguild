import { exec } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const ROOT = path.resolve(__dirname, '..');
const CDN_DIR = process.env.CDN_DIR || path.join(ROOT, 'build', 'cdn');

console.log('Compressing CDN files...');
console.log(`CDN Dir: ${CDN_DIR}`);

if (!shell.which('brotli')) {
    console.warn("Warning: brotli not found. Install with 'brew install brotli' or 'apt install brotli'.");
    console.warn("Files will not be compressed.");
    process.exit(0);
}

const concurrency = os.cpus().length;
console.log(`Using ${concurrency} threads for compression.`);

interface Task {
    fullPath: string;
    brFile: string;
}

const tasks: Task[] = [];

// Producer: Walk the directory and create tasks
function walk(dir: string) {
    if (!fs.existsSync(dir)) return;

    const list = fs.readdirSync(dir);
    for (const file of list) {
        const fullPath = path.join(dir, file);
        const stat = fs.statSync(fullPath);

        if (stat.isDirectory()) {
            walk(fullPath);
        } else if (stat.isFile()) {
            if (file.endsWith('.br')) continue;

            const brFile = fullPath + '.br';
            let shouldCompress = false;

            if (!fs.existsSync(brFile)) {
                shouldCompress = true;
            } else {
                const brStat = fs.statSync(brFile);
                if (stat.mtime > brStat.mtime) {
                    shouldCompress = true;
                }
            }

            if (shouldCompress) {
                tasks.push({ fullPath, brFile });
            }
        }
    }
}

walk(CDN_DIR);

console.log(`Found ${tasks.length} files to compress.`);

if (tasks.length === 0) {
    console.log("No files to compress.");
    process.exit(0);
}

// Consumer: Process tasks from the queue
let completed = 0;
let taskIndex = 0;

function runTask(task: Task): Promise<void> {
    return new Promise((resolve) => {
        // Use child_process.exec for async execution
        exec(`brotli -q 11 -f "${task.fullPath}" -o "${task.brFile}"`, (error) => {
            if (error) {
                console.error(`\nFailed to compress: ${task.fullPath}`);
                console.error(error.message);
            }
            completed++;
            if (completed % 10 === 0 || completed === tasks.length) {
                process.stdout.write(`\rCompressed ${completed}/${tasks.length}`);
            }
            resolve();
        });
    });
}

async function worker(id: number) {
    while (true) {
        // Atomic fetch of next task index (Node.js is single-threaded, so this is safe)
        const currentTaskIndex = taskIndex++;
        if (currentTaskIndex >= tasks.length) {
            break;
        }

        const task = tasks[currentTaskIndex];
        await runTask(task);
    }
}

async function main() {
    const workers: Promise<void>[] = [];
    for (let i = 0; i < concurrency; i++) {
        workers.push(worker(i));
    }

    await Promise.all(workers);
    console.log(`\nCompression complete. Processed ${completed} files.`);
}

main().catch(err => {
    console.error("\nCompression failed:", err);
    process.exit(1);
});
