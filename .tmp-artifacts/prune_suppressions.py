import json, os
from collections import defaultdict

WEB = r'D:\repositories\gg-i414f\apps\web'

# actual violations from CI artifact (suppressedMessages + remaining messages)
with open(r'D:\repositories\gg-i414f\.tmp-artifacts\wl1069\x\eslint.json', encoding='utf-8') as f:
    results = json.load(f)

actual = defaultdict(dict)  # relfile -> rule -> count
for r in results:
    fp = r['filePath'].replace(chr(92), '/')
    idx = fp.find('/apps/web/')
    rel = fp[idx + len('/apps/web/'):] if idx >= 0 else fp
    counts = defaultdict(int)
    for m in (r.get('suppressedMessages') or []) + (r.get('messages') or []):
        if m.get('ruleId'):
            counts[m['ruleId']] += 1
    for rule, c in counts.items():
        actual[rel][rule] = c

with open(os.path.join(WEB, 'eslint-suppressions.json'), encoding='utf-8') as f:
    supp = json.load(f)

pruned = {}
changes = []
removed = []
for file, rules in supp.items():
    if not os.path.exists(os.path.join(WEB, file)):
        removed.append((file, '<file missing>', 0))
        continue
    new_rules = {}
    for rule, entry in rules.items():
        declared = entry['count']
        act = actual.get(file, {}).get(rule, 0)
        if act == 0:
            removed.append((file, rule, declared))
        elif act < declared:
            changes.append((file, rule, declared, act))
            new_rules[rule] = {'count': act}
        else:
            new_rules[rule] = entry
    if new_rules:
        pruned[file] = new_rules

print('entries with reduced counts:', len(changes))
for c in changes:
    print('  REDUCE', c)
print('entries removed entirely:', len(removed))
for c in removed:
    print('  REMOVE', c)
print('suppression total before:', sum(e['count'] for rules in supp.values() for e in rules.values()))
print('suppression total after :', sum(e['count'] for rules in pruned.values() for e in rules.values()))
with open(r'D:\repositories\gg-i414f\.tmp-artifacts\pruned-suppressions.json', 'w', encoding='utf-8', newline='\n') as f:
    json.dump(pruned, f, indent=2, ensure_ascii=False)
    f.write('\n')
