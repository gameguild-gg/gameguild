"""
Automated fix for catch(Exception) anti-patterns across the codebase.

Strategy:
1. TRUE SWALLOWERS (no log, no throw): Add logging + throw
2. CONTROLLERS returning StatusCode(500, ex.Message): Add logging, use generic error message
3. SERVICES/HANDLERS with log+return: Add throw after log (where safe) or ensure proper logging
"""
import re
import os
import sys

BASE = 'Source/Modules'
PATTERN = re.compile(r'catch\s*\(Exception\s*(ex)?\)')
DRY_RUN = '--dry-run' in sys.argv

stats = {'fixed_swallowers': 0, 'fixed_controllers': 0, 'fixed_log_only': 0, 'skipped': 0, 'files_modified': 0}
modified_files = set()


def has_logger_field(content):
    """Check if the file has a logger field or constructor param."""
    return bool(re.search(r'(ILogger\s*<|_logger|private.*logger|readonly.*logger)', content))


def get_class_name(content):
    """Extract the primary class name."""
    m = re.search(r'class\s+(\w+)', content)
    return m.group(1) if m else 'Unknown'


def get_logger_var(content):
    """Determine what logger variable name is used."""
    if '_logger' in content:
        return '_logger'
    if 'logger.' in content and 'ILogger' in content:
        return 'logger'
    return '_logger'


def needs_logger_injection(content):
    """Check if file needs ILogger added to constructor."""
    return not has_logger_field(content)


def find_catch_block_end(lines, start_idx):
    """Find the closing brace of a catch block."""
    brace_count = 0
    started = False
    for i in range(start_idx, min(start_idx + 30, len(lines))):
        for ch in lines[i]:
            if ch == '{':
                brace_count += 1
                started = True
            elif ch == '}':
                brace_count -= 1
                if started and brace_count == 0:
                    return i
    return start_idx + 5  # fallback


def classify_catch_block(lines, catch_line_idx):
    """Classify what pattern the catch block uses."""
    end_idx = find_catch_block_end(lines, catch_line_idx)
    block = '\n'.join(lines[catch_line_idx:end_idx + 1])
    
    has_log = bool(re.search(r'(Log(Error|Warning|Information|Critical)|_logger\.|logger\.)', block))
    has_throw_bare = 'throw;' in block
    has_throw_new = 'throw new' in block
    has_return = 'return' in block
    has_status_code = 'StatusCode(500' in block or 'StatusCode(500' in block
    has_bad_request = 'BadRequest(ex.Message)' in block
    has_ex_message_exposed = 'ex.Message' in block and ('StatusCode' in block or 'BadRequest' in block)
    
    return {
        'has_log': has_log,
        'has_throw': has_throw_bare or has_throw_new,
        'has_throw_bare': has_throw_bare,
        'has_throw_new': has_throw_new,
        'has_return': has_return,
        'has_status_code': has_status_code,
        'has_bad_request': has_bad_request,
        'has_ex_message_exposed': has_ex_message_exposed,
        'block': block,
        'end_idx': end_idx,
    }


def add_logger_to_constructor(content, class_name):
    """Add ILogger<T> to the constructor if not present."""
    if has_logger_field(content):
        return content
    
    # Pattern 1: Primary constructor - class Name(params) : Base
    m = re.search(r'(class\s+' + re.escape(class_name) + r'\s*\()([^)]*?)(\)\s*(?::\s*\w+(?:\([^)]*\))?\s*)?{)', content)
    if m:
        params = m.group(2).strip()
        if params:
            new_params = params + f', ILogger<{class_name}> logger'
        else:
            new_params = f'ILogger<{class_name}> logger'
        content = content[:m.start(2)] + new_params + content[m.end(2):]
        return content
    
    # Pattern 2: Regular constructor
    m = re.search(r'(public\s+' + re.escape(class_name) + r'\s*\()([^)]*?)(\))', content)
    if m:
        params = m.group(2).strip()
        if params:
            new_params = params + f', ILogger<{class_name}> logger'
        else:
            new_params = f'ILogger<{class_name}> logger'
        content = content[:m.start(2)] + new_params + content[m.end(2):]
        # Add field if it's a regular constructor (not primary)
        if 'class ' + class_name + '(' not in content:
            # Find the class body opening
            class_body = content.find('{', content.find('class ' + class_name))
            if class_body != -1:
                content = content[:class_body+1] + f'\n    private readonly ILogger<{class_name}> _logger = logger;\n' + content[class_body+1:]
        return content
    
    return content


def fix_true_swallower_in_controller(lines, catch_line_idx, info, logger_var, class_name):
    """Fix a catch block in a controller that returns error without logging."""
    end_idx = info['end_idx']
    
    # Find the catch line and extract exception variable name
    catch_line = lines[catch_line_idx]
    ex_var = 'ex'
    ex_match = re.search(r'catch\s*\(Exception\s+(\w+)\)', catch_line)
    if ex_match:
        ex_var = ex_match.group(1)
    elif 'catch (Exception)' in catch_line:
        # Need to add variable name
        lines[catch_line_idx] = catch_line.replace('catch (Exception)', 'catch (Exception ex)')
        ex_var = 'ex'
    
    # Find the method name for context
    method_name = 'Operation'
    for i in range(catch_line_idx - 1, max(catch_line_idx - 20, 0), -1):
        m = re.search(r'(?:public|private|protected|internal)\s+(?:async\s+)?(?:\w+<[^>]+>|\w+)\s+(\w+)\s*\(', lines[i])
        if m:
            method_name = m.group(1)
            break
    
    # Get indentation
    indent = re.match(r'(\s*)', lines[catch_line_idx]).group(1)
    inner_indent = indent + '    '
    
    # Build the catch block body
    block_lines = lines[catch_line_idx:end_idx + 1]
    block_text = '\n'.join(block_lines)
    
    if info['has_ex_message_exposed']:
        # Replace ex.Message exposure with generic message + logging
        new_block = [
            f'{indent}catch (Exception {ex_var})',
            f'{indent}{{',
            f'{inner_indent}{logger_var}.LogError({ex_var}, "Unexpected error in {method_name}");',
            f'{inner_indent}return StatusCode(500, "An unexpected error occurred. Please try again later.");',
            f'{indent}}}',
        ]
    elif 'return BadRequest(ex.Message)' in block_text:
        new_block = [
            f'{indent}catch (Exception {ex_var})',
            f'{indent}{{',
            f'{inner_indent}{logger_var}.LogError({ex_var}, "Unexpected error in {method_name}");',
            f'{inner_indent}return BadRequest("An unexpected error occurred.");',
            f'{indent}}}',
        ]
    else:
        # Generic swallower - add log + throw
        new_block = [
            f'{indent}catch (Exception {ex_var})',
            f'{indent}{{',
            f'{inner_indent}{logger_var}.LogError({ex_var}, "Unexpected error in {method_name}");',
            f'{inner_indent}throw;',
            f'{indent}}}',
        ]
    
    lines[catch_line_idx:end_idx + 1] = new_block
    return len(new_block) - (end_idx - catch_line_idx + 1)


def fix_swallower_in_service(lines, catch_line_idx, info, logger_var, class_name):
    """Fix a catch block in a service that swallows without logging."""
    end_idx = info['end_idx']
    catch_line = lines[catch_line_idx]
    
    ex_var = 'ex'
    ex_match = re.search(r'catch\s*\(Exception\s+(\w+)\)', catch_line)
    if ex_match:
        ex_var = ex_match.group(1)
    elif 'catch (Exception)' in catch_line:
        lines[catch_line_idx] = catch_line.replace('catch (Exception)', 'catch (Exception ex)')
        ex_var = 'ex'
    
    # Find method name
    method_name = 'Operation'
    for i in range(catch_line_idx - 1, max(catch_line_idx - 20, 0), -1):
        m = re.search(r'(?:public|private|protected|internal)\s+(?:async\s+)?(?:\w+<[^>]+>|\w+)\s+(\w+)\s*\(', lines[i])
        if m:
            method_name = m.group(1)
            break
    
    indent = re.match(r'(\s*)', lines[catch_line_idx]).group(1)
    inner_indent = indent + '    '
    
    # Add logging + throw
    new_block = [
        f'{indent}catch (Exception {ex_var})',
        f'{indent}{{',
        f'{inner_indent}{logger_var}.LogError({ex_var}, "Unexpected error in {method_name}");',
        f'{inner_indent}throw;',
        f'{indent}}}',
    ]
    
    lines[catch_line_idx:end_idx + 1] = new_block
    return len(new_block) - (end_idx - catch_line_idx + 1)


def fix_log_only_no_throw(lines, catch_line_idx, info, logger_var):
    """Fix a catch block that logs but doesn't throw or return - add throw."""
    end_idx = info['end_idx']
    indent = re.match(r'(\s*)', lines[catch_line_idx]).group(1)
    inner_indent = indent + '    '
    
    # Check if this is a "fire and forget" logging pattern (e.g., recording attempts)
    block_text = '\n'.join(lines[catch_line_idx:end_idx + 1])
    
    # Don't add throw for intentional fire-and-forget patterns
    fire_and_forget_patterns = [
        'Don\'t throw',
        "Don't throw",
        'just logging',
        'non-critical',
        'best effort',
        'ignore',
        'succeeded even if',
        'fire and forget',
        'cleanup',
    ]
    
    is_fire_and_forget = any(p.lower() in block_text.lower() for p in fire_and_forget_patterns)
    
    if is_fire_and_forget:
        return 0  # Leave as-is, it's intentional
    
    # Insert throw; before the closing brace
    closing_brace_line = lines[end_idx].rstrip()
    lines.insert(end_idx, f'{inner_indent}throw;')
    return 1


def fix_log_and_return_in_handler(lines, catch_line_idx, info, logger_var):
    """Fix handler/command catch blocks that log and return default.
    These should throw instead of returning null/false/empty."""
    end_idx = info['end_idx']
    block_text = '\n'.join(lines[catch_line_idx:end_idx + 1])
    
    # Skip if it has an intentional comment explaining why
    intentional_patterns = [
        'Don\'t throw',
        "Don't throw",
        'expected',
        'graceful',
        'fallback',
        'optional',
        'best effort',
        'non-critical',
        'may fail',
        'can fail',
        'validation',
        'decode',
        'verify',
        'parse',
    ]
    
    is_intentional = any(p.lower() in block_text.lower() for p in intentional_patterns)
    if is_intentional:
        return 0
    
    indent = re.match(r'(\s*)', lines[catch_line_idx]).group(1)
    inner_indent = indent + '    '
    
    # Replace "return null/false/default" with "throw;" 
    for i in range(catch_line_idx, end_idx + 1):
        line = lines[i]
        # Replace return null/false/default/empty with throw
        if re.search(r'return\s+(null|false|default|0|string\.Empty|Array\.Empty|Enumerable\.Empty|new\s+List)', line):
            lines[i] = f'{inner_indent}throw;'
            return 0
    
    return 0


def process_file(filepath):
    """Process a single C# file and fix catch(Exception) patterns."""
    global stats
    
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    if 'catch' not in content or 'Exception' not in content:
        return
    
    lines = content.split('\n')
    class_name = get_class_name(content)
    logger_var = get_logger_var(content)
    is_controller = 'Controller' in class_name or 'Controller' in filepath
    is_handler = 'Handler' in class_name or 'Handler' in filepath
    
    original_content = content
    offset = 0
    catch_indices = []
    
    # Find all catch(Exception) line indices
    for i, line in enumerate(lines):
        if PATTERN.search(line) and not line.strip().startswith('//'):
            catch_indices.append(i)
    
    if not catch_indices:
        return
    
    # Process each catch block (in reverse to preserve line numbers)
    for catch_idx in reversed(catch_indices):
        adjusted_idx = catch_idx
        info = classify_catch_block(lines, adjusted_idx)
        
        if info['has_throw']:
            # Already throws - skip (correct pattern)
            stats['skipped'] += 1
            continue
        
        if not info['has_log'] and not info['has_throw']:
            # TRUE SWALLOWER
            if is_controller:
                fix_true_swallower_in_controller(lines, adjusted_idx, info, logger_var, class_name)
                stats['fixed_swallowers'] += 1
            else:
                fix_swallower_in_service(lines, adjusted_idx, info, logger_var, class_name)
                stats['fixed_swallowers'] += 1
        elif info['has_log'] and not info['has_throw'] and not info['has_return']:
            # Logs but doesn't throw or return
            fix_log_only_no_throw(lines, adjusted_idx, info, logger_var)
            stats['fixed_log_only'] += 1
        elif info['has_log'] and info['has_return'] and not info['has_throw']:
            # Log and return pattern
            if is_handler or is_controller:
                fix_log_and_return_in_handler(lines, adjusted_idx, info, logger_var)
                stats['fixed_controllers'] += 1
            else:
                stats['skipped'] += 1
    
    new_content = '\n'.join(lines)
    
    # Add logger to constructor if needed and we modified catch blocks
    if new_content != original_content and needs_logger_injection(new_content):
        new_content = add_logger_to_constructor(new_content, class_name)
        # Add using for Microsoft.Extensions.Logging if not present
        if 'Microsoft.Extensions.Logging' not in new_content and 'ILogger' not in original_content:
            # Add using at the top
            new_content = 'using Microsoft.Extensions.Logging;\n' + new_content
    
    if new_content != original_content:
        if not DRY_RUN:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(new_content)
        modified_files.add(filepath)
        stats['files_modified'] += 1


# Process all files
for root, dirs, files in os.walk(BASE):
    for f in files:
        if not f.endswith('.cs'):
            continue
        filepath = os.path.join(root, f)
        try:
            process_file(filepath)
        except Exception as e:
            print(f'ERROR processing {filepath}: {e}')

print(f'\n=== catch(Exception) Fix Results ===')
print(f'  True swallowers fixed: {stats["fixed_swallowers"]}')
print(f'  Controller/handler return fixes: {stats["fixed_controllers"]}')
print(f'  Log-only (added throw): {stats["fixed_log_only"]}')
print(f'  Skipped (already correct or intentional): {stats["skipped"]}')
print(f'  Files modified: {stats["files_modified"]}')
print(f'\nModified files:')
for f in sorted(modified_files):
    print(f'  {f}')
