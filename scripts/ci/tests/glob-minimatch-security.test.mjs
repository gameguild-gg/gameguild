import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import * as fs from "node:fs";
import { once } from "node:events";
import { createRequire } from "node:module";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { test } from "node:test";
import { fileURLToPath, pathToFileURL } from "node:url";
import { Worker } from "node:worker_threads";
import { readPnpmSecurityConfig } from "../read-pnpm-config.mjs";

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const virtualStore = join(repositoryRoot, "node_modules/.pnpm");
const require = createRequire(import.meta.url);

function consumersOf(name) {
  const consumers = [];
  for (const entry of fs.readdirSync(virtualStore, { withFileTypes: true })) {
    if (!entry.isDirectory() || entry.name.startsWith(`${name}@`)) continue;
    const candidate = join(virtualStore, entry.name, "node_modules", name);
    if (!fs.existsSync(candidate)) continue;
    consumers.push({
      name: entry.name,
      root: fs.realpathSync(candidate),
      require: createRequire(join(virtualStore, entry.name, "security-probe.cjs")),
    });
  }
  assert.ok(consumers.length > 0, `Installed graph must contain consumers of ${name}`);
  return consumers;
}

const matcherConsumers = consumersOf("minimatch");
const matcherRoots = new Set(matcherConsumers.map(consumer => consumer.root));
test("only the reviewed compatibility patch is applied to the genuine fixed minimatch release", () => {
  const config = readPnpmSecurityConfig(repositoryRoot);
  assert.equal(config.patchedDependencies?.["minimatch@11.0.1"], "patches/minimatch@11.0.1.patch");
  assert.equal(config.auditConfig, undefined);
  const hash = bytes => createHash("sha256").update(bytes).digest("hex");
  assert.equal(hash(fs.readFileSync(join(repositoryRoot, "patches/minimatch@11.0.1.patch"))), "067e031a866fcf0476a34defca7836f77c92d7894f9952315ad0691b03aa1a4a");
  for (const root of matcherRoots) {
    assert.match(root, /minimatch@11\.0\.1_patch_hash[=_]/);
    const manifest = JSON.parse(fs.readFileSync(join(root, "package.json"), "utf8"));
    assert.equal(manifest.version, "11.0.1");
    for (const hook of ["preinstall", "install", "postinstall"]) assert.equal(manifest.scripts?.[hook], undefined);
    assert.equal(hash(fs.readFileSync(join(root, "dist/commonjs/index.js"))), "8a6d2d92870c4073f44b5eee2f479103a1dbc70c9e862d9551b4a449ed7b0989");
    assert.equal(hash(fs.readFileSync(join(root, "dist/esm/index.js"))), "c2ac65e82b196476009b53e1939811c788cb001d910304c6108a1f665fac3c3d");
  }
});

for (const consumer of matcherConsumers) {
  test(`${consumer.name}: resolved minimatch preserves callable, named, class and legacy APIs`, () => {
    const matcher = consumer.require("minimatch");
    assert.equal(typeof matcher, "function");
    assert.equal(matcher, matcher.minimatch);
    assert.equal(matcher("src/example.js", "**/*.js"), true);
    assert.equal(matcher("src/example.ts", "**/*.js"), false);
    assert.deepEqual(matcher.match(["a.js", "b.ts"], "*.js"), ["a.js"]);
    assert.deepEqual(["a.js", "b.ts"].filter(matcher.filter("*.js")), ["a.js"]);
    assert.equal(new matcher.Minimatch("*.js").match("a.js"), true);
    assert.equal(matcher.defaults({ dot: true })(".file.js", "*.js"), true);
    assert.deepEqual(matcher.braceExpand("{a,b}.js"), ["a.js", "b.js"]);
    assert.equal(consumer.require("minimatch/package.json").version, "11.0.1");
  });
}

async function resourceProbe(context, packageRoot, entry) {
  const worker = new Worker(new URL("./helpers/minimatch-resource-limits.mjs", import.meta.url), {
    workerData: { packageRoot, entry },
    resourceLimits: { maxOldGenerationSizeMb: 128 },
  });
  context.after(() => worker.terminate());
  await new Promise((resolveProbe, rejectProbe) => {
    let settled = false;
    const deadline = setTimeout(() => finish(new Error("Minimatch optimization exceeded ten seconds")), 10_000);
    function finish(error) {
      if (settled) return;
      settled = true;
      clearTimeout(deadline);
      if (error) rejectProbe(error);
      else resolveProbe();
    }
    worker.once("error", finish);
    worker.once("exit", code => finish(new Error(`Probe exited without success (${code})`)));
    worker.once("message", result => finish(result === entry ? undefined : new Error("Unexpected probe result")));
  });
}

for (const root of matcherRoots) {
  test(`${root}: ESM retains its named matcher contract`, async () => {
    const matcher = await import(pathToFileURL(join(root, "dist/esm/index.js")).href);
    assert.equal(matcher.minimatch("src/example.js", "**/*.js"), true);
    assert.equal(new matcher.Minimatch("**/*.ts").match("src/example.js"), false);
  });
  for (const entry of ["CommonJS", "ESM"]) {
    test(`${root}: ${entry} bounds adversarial preprocessing and preserves ordinary matches`, context => resourceProbe(context, root, entry));
  }
}

function fixture(context) {
  const cwd = fs.mkdtempSync(join(tmpdir(), "glob-security-"));
  context.after(() => fs.rmSync(cwd, { recursive: true, force: true }));
  for (const directory of ["first/child/deep", "second/child/deep", "skip"]) {
    fs.mkdirSync(join(cwd, directory), { recursive: true });
    fs.writeFileSync(join(cwd, directory, "file.js"), "fixture\n");
  }
  fs.writeFileSync(join(cwd, "file.js"), "fixture\n");
  fs.writeFileSync(join(cwd, ".hidden.js"), "fixture\n");
  return cwd;
}

const globConsumers = consumersOf("glob");
for (const root of new Set(globConsumers.map(consumer => consumer.root))) {
  const manifest = JSON.parse(fs.readFileSync(join(root, "package.json"), "utf8"));
  for (const hook of ["preinstall", "install", "postinstall"]) assert.equal(manifest.scripts?.[hook], undefined);
  const glob = require(root);
  if (manifest.version === "7.2.3") {
    test(`${root}: legacy traversal uses exactly the reviewed fixed-matcher adapter`, () => {
      const hash = bytes => createHash("sha256").update(bytes).digest("hex");
      assert.equal(readPnpmSecurityConfig(repositoryRoot).patchedDependencies?.["glob@7.2.3"], "patches/glob@7.2.3.patch");
      assert.equal(hash(fs.readFileSync(join(repositoryRoot, "patches/glob@7.2.3.patch"))), "6f4cf56a8afceea4c701eb42a77a86faeea3a5385845a0bd147763498f615375");
      assert.equal(hash(fs.readFileSync(join(root, "glob.js"))), "5ea8fdb86e83c0c178625a724a47c3ae19bf9d177de21f83d779de1764a1b7a7");
      assert.equal(hash(fs.readFileSync(join(root, "sync.js"))), "add06c3ea77da200e484dade81b88eebf53f138a70e6fe6f6f8a088229fda036");
      assert.equal(hash(fs.readFileSync(join(root, "common.js"))), "b867e089ab5d4ab19a83e5b34da3dd7f4018fdf255fcacc681aab87d41dc77e8");
    });
    test(`${root}: legacy callback and sync enumeration remain supported`, async context => {
      const cwd = fixture(context);
      assert.equal(typeof glob, "function");
      const expected = glob.sync("**/*.js", { cwd }).sort();
      assert.equal(expected.length, 4);
      const actual = await new Promise((resolveFiles, rejectFiles) => glob("**/*.js", { cwd }, (error, files) => error ? rejectFiles(error) : resolveFiles(files)));
      assert.deepEqual(actual.sort(), expected);
      assert.deepEqual(glob.sync("**/@(file|missing).js", { cwd }).sort(), expected);
      assert.equal(glob.sync("**/*.js", { cwd, ignore: "skip/**" }).length, 3);
      assert.equal(glob.sync("**/*.js", { cwd, dot: true }).length, 5);
    });
    continue;
  }
  test(`${root}: modern glob uses the fixed release and preserves traversal`, async context => {
    assert.equal(manifest.version, "14.0.2");
    const cwd = fixture(context);
    const expected = glob.globSync("**/*.js", { cwd }).sort();
    assert.equal(expected.length, 4);
    assert.deepEqual((await glob.glob("**/*.js", { cwd })).sort(), expected);
  });
  for (const mode of ["async", "sync-stream"]) {
    test(`${root}: ${mode} abort stops filesystem traversal`, async context => {
      assert.equal(manifest.version, "14.0.2");
      const cwd = fixture(context);
      const controller = new AbortController();
      const reason = new Error("Fixture cancellation");
      let reads = 0;
      const countRead = () => {
        reads++;
        if (reads === 1) controller.abort(reason);
      };
      const countedFs = {
        ...fs,
        readdir(...args) { countRead(); return fs.readdir(...args); },
        readdirSync(...args) { countRead(); return fs.readdirSync(...args); },
      };
      const options = { cwd, signal: controller.signal, fs: countedFs };
      if (mode === "async") await assert.rejects(glob.glob("**", options), error => error === reason);
      else {
        const stream = glob.globStreamSync("**", options);
        const error = once(stream, "error");
        stream.resume();
        assert.deepEqual(await error, [reason]);
      }
      assert.equal(reads, 1, "Cancellation must prevent reading descendant directories");
      await new Promise(resolveDrain => setTimeout(resolveDrain, 100));
      assert.equal(reads, 1, "Rejected traversal must not continue in the background");
    });
  }
}

test("the actual npm-run-all task selector preserves task names, ordering and missing-task errors", () => {
  const root = dirname(require.resolve("npm-run-all/package.json"));
  const selectTasks = require(join(root, "lib/match-tasks.js"));
  assert.deepEqual(selectTasks(["build:api", "build:web", "lint"], ["build:*", "lint"]), ["build:api", "build:web", "lint"]);
  assert.throws(() => selectTasks(["build:api"], ["missing:*"]), /Task not found/);
});

for (const root of new Set(consumersOf("@eslint/eslintrc").map(consumer => consumer.root))) {
  test(`${root}: actual legacy lint overrides preserve include and exclude filtering`, context => {
    const cwd = fixture(context);
    const { OverrideTester } = require(root).Legacy;
    const criteria = OverrideTester.create(["**/*.js"], ["skip/**"], cwd);
    assert.equal(criteria.test(join(cwd, "first/child/deep/file.js")), true);
    assert.equal(criteria.test(join(cwd, "skip/file.js")), false);
    assert.equal(criteria.test(join(cwd, "file.ts")), false);
  });
}

for (const consumer of consumersOf("eslint-plugin-import")) {
  test(`${consumer.name}: real import lint rule retains forbidden-path enforcement with the fixed matcher`, context => {
    const root = consumer.root;
    const hash = bytes => createHash("sha256").update(bytes).digest("hex");
    assert.equal(readPnpmSecurityConfig(repositoryRoot).patchedDependencies?.["eslint-plugin-import@2.32.0"], "patches/eslint-plugin-import@2.32.0.patch");
    assert.equal(hash(fs.readFileSync(join(repositoryRoot, "patches/eslint-plugin-import@2.32.0.patch"))), "2ffdfb53c367bdfc823dde00d2cde7c0e39915d90d96c1af9022f1c6e8a10bdc");
    assert.equal(hash(fs.readFileSync(join(root, "lib/rules/no-internal-modules.js"))), "e77e421c0bba00394cd1c94d57e649722e5c8ca27cd6f302f9f9661ffb9a45f1");
    const manifest = JSON.parse(fs.readFileSync(join(root, "package.json"), "utf8"));
    for (const hook of ["preinstall", "install", "postinstall"]) assert.equal(manifest.scripts?.[hook], undefined);
    const { Linter } = consumer.require("eslint");
    const rule = require(join(root, "lib/rules/no-internal-modules.js"));
    const cwd = fixture(context);
    const linter = new Linter({ cwd });
    const config = [{
      files: ["**/*.js"],
      plugins: { import: { rules: { "no-internal-modules": rule } } },
      rules: { "import/no-internal-modules": ["error", { forbid: ["skip/**"] }] },
    }];
    const options = { filename: join(cwd, "consumer.js") };
    const blocked = linter.verify('import value from "./skip/file.js";', config, options);
    assert.equal(blocked.length, 1);
    assert.equal(blocked[0].ruleId, "import/no-internal-modules");
    assert.deepEqual(linter.verify('import value from "./first/child/deep/file.js";', config, options), []);
  });
}

for (const consumer of consumersOf("test-exclude")) {
  test(`${consumer.name}: real coverage filtering remains compatible with the fixed matcher`, context => {
    const TestExclude = consumer.require("test-exclude");
    const cwd = fixture(context);
    const exclude = new TestExclude({ cwd, include: ["**/*.js"], exclude: ["skip/**"], extension: [".js"] });
    assert.equal(exclude.shouldInstrument(join(cwd, "file.js")), true);
    assert.equal(exclude.shouldInstrument(join(cwd, "first/child/deep/file.js")), true);
    assert.equal(exclude.shouldInstrument(join(cwd, "skip/file.js")), false);
    assert.equal(exclude.shouldInstrument(join(cwd, "file.ts")), false);
  });
}
