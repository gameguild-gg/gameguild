const fs = require('fs');
const d = JSON.parse(fs.readFileSync('coverage/coverage-final.json', 'utf8'));

for (const [file, info] of Object.entries(d)) {
  const s = info.s || {};
  const b = info.b || {};
  const f = info.f || {};
  const sm = info.statementMap || {};
  const bm = info.branchMap || {};
  const fm = info.fnMap || {};

  const uncovStmts = [];
  const uncovBranches = [];
  const uncovFuncs = [];

  for (const k of Object.keys(s)) {
    if (s[k] === 0) {
      const loc = sm[k];
      uncovStmts.push(`L${loc.start.line}`);
    }
  }

  for (const k of Object.keys(b)) {
    for (let i = 0; i < b[k].length; i++) {
      if (b[k][i] === 0) {
        const loc = bm[k].loc || bm[k].locations?.[i];
        const line = loc ? loc.start.line : '?';
        uncovBranches.push(`L${line}(branch ${i} of ${bm[k].type})`);
      }
    }
  }

  for (const k of Object.keys(f)) {
    if (f[k] === 0) {
      const loc = fm[k].loc;
      uncovFuncs.push(`L${loc.start.line} ${fm[k].name || '(anonymous)'}`);
    }
  }

  if (uncovStmts.length || uncovBranches.length || uncovFuncs.length) {
    const shortFile = file.replace(/.*\/src\//, 'src/');
    console.log(`\n=== ${shortFile} ===`);
    if (uncovStmts.length) console.log(`  Uncov Stmts: ${uncovStmts.join(', ')}`);
    if (uncovBranches.length) console.log(`  Uncov Branches: ${uncovBranches.join(', ')}`);
    if (uncovFuncs.length) console.log(`  Uncov Funcs: ${uncovFuncs.join(', ')}`);
  }
}
