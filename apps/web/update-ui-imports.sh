#!/bin/bash

# Script to update UI component imports to use the shared UI package

echo "🔄 Updating UI imports to use @game-guild/ui/components..."

# Find all TypeScript React files that import from @/components/ui/
files=$(find src -name "*.tsx" -exec grep -l "@/components/ui/" {} \;)

for file in $files; do
  echo "📝 Updating $file"
  
  # Replace individual component imports
  sed -i "s|from '@/components/ui/[^']*'|from '@game-guild/ui/components'|g" "$file"
  
  # Also handle the .tsx extension variant
  sed -i "s|from '@/components/ui/[^']*\.tsx'|from '@game-guild/ui/components'|g" "$file"
done

echo "✅ Done! Updated $(echo "$files" | wc -l) files"
echo "🔍 You may need to manually review and consolidate duplicate imports"
