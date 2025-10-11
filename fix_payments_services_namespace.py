#!/usr/bin/env python3
"""Fix Payments module service namespace imports"""
import os
import re
from pathlib import Path


def fix_payments_service_imports(directory):
    """Fix Payments module service namespace imports"""
    files_fixed = 0

    # Find all .cs files in the Payments module
    payments_dir = Path(directory) / "apps/api/Source/Modules/Payments"

    if not payments_dir.exists():
        print(f"Payments directory not found: {payments_dir}")
        return

    for cs_file in payments_dir.rglob("*.cs"):
        try:
            with open(cs_file, "r", encoding="utf-8") as f:
                content = f.read()

            # Replace the incorrect service namespace import
            old_import = "using GameGuild.Modules.Payments.Services;"
            new_import = (
                "using GameGuild.Modules.Payments.Payments.Application.Services;"
            )

            if old_import in content:
                new_content = content.replace(old_import, new_import)

                with open(cs_file, "w", encoding="utf-8") as f:
                    f.write(new_content)

                print(f"Fixed: {cs_file.relative_to(directory)}")
                files_fixed += 1

        except Exception as e:
            print(f"Error processing {cs_file}: {e}")

    print(f"\nFixed {files_fixed} service namespace imports in Payments module")


if __name__ == "__main__":
    # Run from game-guild directory
    current_dir = Path.cwd()
    fix_payments_service_imports(current_dir)
