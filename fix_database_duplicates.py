#!/usr/bin/env python3

import os
import re


def fix_duplicate_database_usings():
    """Fix remaining duplicate GameGuild.Database using directives."""

    # Files that have duplicate GameGuild.Database usings based on error output
    files_with_duplicates = [
        "apps/api/Source/Modules/Resources/Events/OutboxEventPublisher.cs",
        "apps/api/Source/Modules/Resources/Handlers/ArchiveUsageRecordsHandler.cs",
        "apps/api/Source/Modules/Resources/Handlers/BulkResetUsageHandler.cs",
        "apps/api/Source/Modules/Resources/Handlers/GetResourceUsageDetailsHandler.cs",
        "apps/api/Source/Modules/Resources/Handlers/GetUsageByTypeHandler.cs",
        "apps/api/Source/Modules/Resources/Handlers/GetUsageHistoryHandler.cs",
        "apps/api/Source/Modules/Resources/Services/ResourceQuotaBackgroundService.cs",
        "apps/api/Source/Modules/SlaMonitoring/Repositories/ServiceLevelIndicatorRepository.cs",
        "apps/api/Source/Modules/SlaMonitoring/Repositories/ServiceLevelObjectiveRepository.cs",
        "apps/api/Source/Modules/SlaMonitoring/Repositories/SloViolationRepository.cs",
    ]

    fixed_count = 0

    for file_path in files_with_duplicates:
        if not os.path.exists(file_path):
            print(f"⚠️  File not found: {file_path}")
            continue

        try:
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()

            original_content = content
            lines = content.split("\n")

            # Track seen using statements to remove duplicates
            seen_usings = set()
            new_lines = []
            removed_duplicates = 0

            for line in lines:
                # Check if this is a using directive
                using_match = re.match(r"^\s*using\s+([^;]+);.*$", line)
                if using_match:
                    using_namespace = using_match.group(1).strip()

                    # If we've seen this exact using before, skip it (it's a duplicate)
                    if using_namespace in seen_usings:
                        removed_duplicates += 1
                        print(f"  🔄 Removing duplicate: using {using_namespace};")
                        continue
                    else:
                        seen_usings.add(using_namespace)

                new_lines.append(line)

            # Only write if we made changes
            if removed_duplicates > 0:
                new_content = "\n".join(new_lines)
                with open(file_path, "w", encoding="utf-8") as f:
                    f.write(new_content)

                print(
                    f"✅ Fixed {file_path} - removed {removed_duplicates} duplicate using directives"
                )
                fixed_count += 1
            else:
                print(f"ℹ️  No duplicates found in {file_path}")

        except Exception as e:
            print(f"❌ Error processing {file_path}: {str(e)}")

    return fixed_count


if __name__ == "__main__":
    print("🔧 Fixing remaining duplicate GameGuild.Database using directives...")
    fixed_count = fix_duplicate_database_usings()
    print(f"\n✨ Fixed duplicate using directives in {fixed_count} files")
