#!/usr/bin/env python3
"""Fix remaining duplicate using directives in Resources module"""
import os
import re
from pathlib import Path

def fix_resources_duplicate_usings(directory):
    """Fix remaining duplicate using directives in Resources module"""
    files_fixed = 0
    
    # Patterns to remove duplicates
    duplicate_patterns = [
        'using GameGuild.Database;'
    ]
    
    resources_dir = Path(directory) / "apps/api/Source/Modules/Resources"
    
    if not resources_dir.exists():
        print(f"Resources directory not found: {resources_dir}")
        return
    
    for cs_file in resources_dir.rglob("*.cs"):
        try:
            with open(cs_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            original_content = content
            
            # Remove duplicate using directives
            for pattern in duplicate_patterns:
                lines = content.split('\n')
                seen_usings = set()
                new_lines = []
                
                for line in lines:
                    stripped = line.strip()
                    
                    # Check if this is a using directive we want to dedupe
                    if stripped == pattern:
                        if stripped not in seen_usings:
                            seen_usings.add(stripped)
                            new_lines.append(line)
                        # Skip duplicate
                    else:
                        new_lines.append(line)
                
                content = '\n'.join(new_lines)
            
            # Only write if we made changes
            if content != original_content:
                with open(cs_file, 'w', encoding='utf-8') as f:
                    f.write(content)
                
                print(f"Fixed duplicates in: {cs_file.relative_to(directory)}")
                files_fixed += 1
        
        except Exception as e:
            print(f"Error processing {cs_file}: {e}")
    
    print(f"\nFixed duplicate usings in {files_fixed} Resources module files")

if __name__ == "__main__":
    current_dir = Path.cwd()
    fix_resources_duplicate_usings(current_dir)