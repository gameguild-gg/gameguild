"""
Automated fix for missing ConfigureAwait(false) calls.

In library/non-UI code, every `await` should have `.ConfigureAwait(false)` to prevent
thread pool starvation and deadlocks. This script adds it where missing.

Rules:
- Only modify files in Source/Modules/ (not in Tests/ or Controllers/)
- Skip files that already have ConfigureAwait
- Skip ASP.NET controllers (they need the sync context)
- Add ConfigureAwait(false) to all await expressions that don't have it
"""
import re
import os
import sys

BASE = 'Source/Modules'
DRY_RUN = '--dry-run' in sys.argv

stats = {'awaits_fixed': 0, 'files_modified': 0, 'files_skipped': 0}
modified_files = []

# Pattern to match await expressions that DON'T already have ConfigureAwait
# Matches: await something; or await something.Method(); etc.
# But NOT: await something.ConfigureAwait(false);
AWAIT_PATTERN = re.compile(
    r'(await\s+)'          # 'await' keyword
    r'(.+?)'               # the awaited expression  
    r'(?<!ConfigureAwait\(false\))'  # not already having ConfigureAwait
    r'(;)'                 # semicolon
)

def should_skip_file(filepath, content):
    """Determine if a file should be skipped."""
    # Skip controllers - they need the sync context for HttpContext
    basename = os.path.basename(filepath)
    if 'Controller' in basename:
        return True
    
    # Skip if no async/await
    if 'await ' not in content:
        return True
    
    # Skip test files
    if 'Tests/' in filepath or 'Test/' in filepath:
        return True
    
    return False


def add_configure_await(content):
    """Add ConfigureAwait(false) to all await expressions missing it."""
    lines = content.split('\n')
    fixed_count = 0
    
    for i, line in enumerate(lines):
        if 'await ' not in line:
            continue
        
        # Skip if already has ConfigureAwait
        if 'ConfigureAwait' in line:
            continue
        
        # Skip comments
        stripped = line.strip()
        if stripped.startswith('//') or stripped.startswith('/*') or stripped.startswith('*'):
            continue
        
        # Skip if it's in a string
        if 'await' in line and '"' in line:
            # Simple check - if 'await' appears after an opening quote, skip
            first_quote = line.find('"')
            await_pos = line.find('await')
            if first_quote < await_pos:
                continue
        
        # Skip LINQ expressions or lambda expressions with complex await
        if 'Select(' in line or 'Where(' in line or '=>' in line:
            # These are complex expressions, skip for safety
            continue
        
        # Skip multi-line await expressions (await on this line, semicolon on next)
        if 'await ' in line and ';' not in line:
            # Multi-line await - need to find the semicolon line
            # Look ahead for the semicolon
            for j in range(i + 1, min(i + 10, len(lines))):
                if ';' in lines[j] and 'ConfigureAwait' not in lines[j]:
                    # Find the position just before the semicolon
                    semi_pos = lines[j].rindex(';')
                    # Check if there's a closing paren before semicolon
                    before_semi = lines[j][:semi_pos].rstrip()
                    if before_semi.endswith(')'):
                        lines[j] = lines[j][:semi_pos] + '.ConfigureAwait(false)' + lines[j][semi_pos:]
                    else:
                        lines[j] = lines[j][:semi_pos] + '.ConfigureAwait(false)' + lines[j][semi_pos:]
                    fixed_count += 1
                    break
                elif ';' in lines[j]:
                    break  # Already has ConfigureAwait
            continue
        
        # Single-line await with semicolon
        # Pattern: await expr;  or  await expr.Method();  or  var x = await expr;
        # We need to insert .ConfigureAwait(false) before the semicolon
        
        # Find the await and the semicolon on this line
        await_match = re.search(r'await\s+', line)
        if not await_match:
            continue
        
        # Find the last semicolon on the line
        last_semi = line.rindex(';')
        
        # Check what's before the semicolon
        before_semi = line[:last_semi].rstrip()
        
        # Don't add if the expression is something like: await Task.CompletedTask;
        if 'Task.CompletedTask' in line or 'Task.FromResult' in line:
            continue
        
        # Don't add if it's a using statement
        if 'using' in line and 'await' in line:
            continue
        
        # Don't add to yield return
        if 'yield' in line:
            continue
        
        # Add ConfigureAwait(false) before the semicolon
        lines[i] = line[:last_semi] + '.ConfigureAwait(false)' + line[last_semi:]
        fixed_count += 1
    
    return '\n'.join(lines), fixed_count


for root, dirs, files in os.walk(BASE):
    for f in files:
        if not f.endswith('.cs'):
            continue
        
        filepath = os.path.join(root, f)
        
        try:
            with open(filepath, 'r', encoding='utf-8-sig') as fh:
                content = fh.read()
        except Exception:
            continue
        
        if should_skip_file(filepath, content):
            stats['files_skipped'] += 1
            continue
        
        new_content, fixed = add_configure_await(content)
        
        if fixed > 0:
            if not DRY_RUN:
                with open(filepath, 'w', encoding='utf-8') as fh:
                    fh.write(new_content)
            
            stats['awaits_fixed'] += fixed
            stats['files_modified'] += 1
            modified_files.append((filepath, fixed))

print(f'\n=== ConfigureAwait(false) Fix Results ===')
print(f'  Awaits fixed: {stats["awaits_fixed"]}')
print(f'  Files modified: {stats["files_modified"]}')
print(f'  Files skipped: {stats["files_skipped"]}')
print(f'\nTop modified files:')
modified_files.sort(key=lambda x: x[1], reverse=True)
for f, count in modified_files[:30]:
    print(f'  {count:3d} fixes: {f}')
