#!/bin/bash

echo "🔧 Consolidating duplicate UI imports..."

# Find all files that import from @game-guild/ui/components
files=$(find src -name "*.tsx" -exec grep -l "@game-guild/ui/components" {} \;)

for file in $files; do
  echo "🔍 Processing $file"
  
  # Create a temporary file
  tmp_file=$(mktemp)
  
  # Extract all imports from @game-guild/ui/components
  ui_imports=$(grep "from '@game-guild/ui/components'" "$file" | sed "s/.*import { *\([^}]*\) *}.*/\1/" | tr '\n' ',' | sed 's/,$//')
  
  if [ ! -z "$ui_imports" ]; then
    # Remove all existing UI imports
    grep -v "from '@game-guild/ui/components'" "$file" > "$tmp_file"
    
    # Find the position after the last import
    last_import_line=$(grep -n "^import" "$tmp_file" | tail -1 | cut -d: -f1)
    
    if [ ! -z "$last_import_line" ]; then
      # Insert consolidated import after the last import
      {
        head -n "$last_import_line" "$tmp_file"
        echo "import { $ui_imports } from '@game-guild/ui/components';"
        tail -n +$((last_import_line + 1)) "$tmp_file"
      } > "$file"
    else
      # No other imports, add at the top
      {
        echo "import { $ui_imports } from '@game-guild/ui/components';"
        cat "$tmp_file"
      } > "$file"
    fi
  fi
  
  rm -f "$tmp_file"
done

echo "✅ Consolidation complete!"
