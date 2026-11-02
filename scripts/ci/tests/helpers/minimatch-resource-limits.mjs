import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { parentPort, workerData } from "node:worker_threads";

const require = createRequire(import.meta.url);
const { packageRoot, entry } = workerData;
const matcher = entry === "CommonJS"
  ? require(packageRoot)
  : await import(pathToFileURL(join(packageRoot, "dist/esm/index.js")).href);
const { Minimatch } = matcher;
const adversarial = Array.from({ length: 22 }, (_, index) => `**/../segment${index}/leaf`).join("/");
const bounded = new Minimatch(adversarial, { optimizationLevel: 2, braceExpandMax: 64 });
assert.equal(bounded.set.length, 64, "Globstar-parent splits must honor the configured expansion cap");

let comparisons = 0;
class CountingMinimatch extends Minimatch {
  partsMatch(...args) {
    comparisons++;
    return super.partsMatch(...args);
  }
}
const large = new CountingMinimatch("{candidate,*}/{0..749}", { optimizationLevel: 2 });
assert.equal(large.set.length, 1500);
assert.equal(comparisons, 0, "Large sets must skip the quadratic pairwise optimization");
assert.equal(large.match("other/700"), true);
assert.equal(large.match(".private/700"), false);
const small = new CountingMinimatch("{candidate,*}/{0..4}", { optimizationLevel: 2 });
assert.ok(comparisons > 0, "The small-set control must still exercise pairwise optimization");
assert.equal(small.match("other/3"), true);
assert.equal(small.match("other/9"), false);
parentPort.postMessage(entry);
