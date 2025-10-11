#!/usr/bin/env python3
"""
Bulk fix script for GameGuild build errors.
Fixes the most common patterns causing 1,797 build errors.
"""

import os
import re
import sys
from pathlib import Path

# Base directory
BASE_DIR = Path(__file__).parent / "Source"

# Statistics
stats = {
    "files_processed": 0,
    "files_modified": 0,
    "total_replacements": 0,
}


def fix_result_failure_string_to_error(content):
    """Fix Result.Failure("string") -> Result.Failure(Error.Failure("Code", "string"))"""
    replacements = 0

    # Pattern: Result.Failure("message") or Result<T>.Failure("message")
    pattern = r'Result(?:<[^>]+>)?\.Failure\("([^"]+)"\)'

    def replace_func(match):
        nonlocal replacements
        message = match.group(1)
        replacements += 1
        # Generate a code from the message (use first few words)
        code_words = message.split()[:2]
        code = "".join(word.capitalize() for word in code_words)
        return f'Result.Failure(Error.Failure("{code}", "{message}"))'

    new_content = re.sub(pattern, replace_func, content)
    return new_content, replacements


def fix_result_not_found(content):
    """Fix Result.NotFound("message") -> use correct Error.NotFound"""
    replacements = 0

    pattern = r'Result(?:<[^>]+>)?\.NotFound\("([^"]+)"(?:,\s*([^)]+))?\)'

    def replace_func(match):
        nonlocal replacements
        resource = match.group(1)
        identifier = match.group(2) if match.group(2) else None
        replacements += 1

        if identifier:
            return f'Result.Failure(Error.NotFound("{resource}.NotFound", "{resource} not found", {identifier}))'
        else:
            return f'Result.Failure(Error.NotFound("{resource}.NotFound", "{resource} not found"))'

    new_content = re.sub(pattern, replace_func, content)
    return new_content, replacements


def fix_init_only_properties(content):
    """Fix init-only properties that are assigned outside of initializers"""
    replacements = 0

    # Change { get; init; } to { get; set; } for problematic properties
    patterns = [
        (
            r"public\s+(\w+)\s+Status\s*{\s*get;\s*init;\s*}",
            r"public \1 Status { get; set; }",
        ),
        (
            r"public\s+(\w+)\??\s+CompletedAt\s*{\s*get;\s*init;\s*}",
            r"public \1? CompletedAt { get; set; }",
        ),
        (
            r"public\s+(\w+)\??\s+ErrorMessage\s*{\s*get;\s*init;\s*}",
            r"public \1? ErrorMessage { get; set; }",
        ),
    ]

    new_content = content
    for pattern, replacement in patterns:
        matches = len(re.findall(pattern, new_content))
        if matches > 0:
            new_content = re.sub(pattern, replacement, new_content)
            replacements += matches

    return new_content, replacements


def process_file(file_path):
    """Process a single C# file and apply fixes"""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()

        original_content = content
        total_file_replacements = 0

        # Apply all fixes
        content, count = fix_result_failure_string_to_error(content)
        total_file_replacements += count

        content, count = fix_result_not_found(content)
        total_file_replacements += count

        content, count = fix_init_only_properties(content)
        total_file_replacements += count

        # Only write if changes were made
        if content != original_content:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(content)
            stats["files_modified"] += 1
            stats["total_replacements"] += total_file_replacements
            print(
                f"✓ {file_path.relative_to(BASE_DIR.parent)}: {total_file_replacements} fixes"
            )
            return True

        return False

    except Exception as e:
        print(f"✗ Error processing {file_path}: {e}", file=sys.stderr)
        return False


def main():
    """Main entry point"""
    print("GameGuild Build Error Fix Script")
    print("=" * 60)
    print(f"Scanning directory: {BASE_DIR}")
    print()

    # Find all C# files
    cs_files = list(BASE_DIR.rglob("*.cs"))
    print(f"Found {len(cs_files)} C# files")
    print()

    # Process each file
    for cs_file in cs_files:
        stats["files_processed"] += 1
        process_file(cs_file)

    # Print summary
    print()
    print("=" * 60)
    print("Summary:")
    print(f"  Files processed: {stats['files_processed']}")
    print(f"  Files modified:  {stats['files_modified']}")
    print(f"  Total fixes:     {stats['total_replacements']}")
    print()
    print("✓ Done! Run 'dotnet build' to verify fixes.")


if __name__ == "__main__":
    main()
