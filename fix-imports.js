const fs = require('fs');
const path = require('path');

// Component mapping from generic import to specific file
const componentMap = {
  'Avatar': 'avatar',
  'AvatarFallback': 'avatar',
  'AvatarImage': 'avatar',
  'Button': 'button',
  'Input': 'input',
  'Label': 'label',
  'Card': 'card',
  'CardContent': 'card',
  'CardDescription': 'card',
  'CardFooter': 'card',
  'CardHeader': 'card',
  'CardTitle': 'card',
  'DropdownMenu': 'dropdown-menu',
  'DropdownMenuContent': 'dropdown-menu',
  'DropdownMenuItem': 'dropdown-menu',
  'DropdownMenuLabel': 'dropdown-menu',
  'DropdownMenuSeparator': 'dropdown-menu',
  'DropdownMenuTrigger': 'dropdown-menu',
  'DropdownMenuSub': 'dropdown-menu',
  'DropdownMenuSubContent': 'dropdown-menu',
  'DropdownMenuSubTrigger': 'dropdown-menu',
  'DropdownMenuRadioGroup': 'dropdown-menu',
  'DropdownMenuRadioItem': 'dropdown-menu',
  'DropdownMenuCheckboxItem': 'dropdown-menu',
  'DropdownMenuGroup': 'dropdown-menu',
  'DropdownMenuPortal': 'dropdown-menu',
  'DropdownMenuShortcut': 'dropdown-menu',
  'Dialog': 'dialog',
  'DialogContent': 'dialog',
  'DialogDescription': 'dialog',
  'DialogFooter': 'dialog',
  'DialogHeader': 'dialog',
  'DialogTitle': 'dialog',
  'DialogTrigger': 'dialog',
  'DialogPortal': 'dialog',
  'DialogOverlay': 'dialog',
  'DialogClose': 'dialog',
  'Sheet': 'sheet',
  'SheetContent': 'sheet',
  'SheetDescription': 'sheet',
  'SheetFooter': 'sheet',
  'SheetHeader': 'sheet',
  'SheetTitle': 'sheet',
  'SheetTrigger': 'sheet',
  'SheetPortal': 'sheet',
  'SheetOverlay': 'sheet',
  'SheetClose': 'sheet',
  'Table': 'table',
  'TableBody': 'table',
  'TableCell': 'table',
  'TableFooter': 'table',
  'TableHead': 'table',
  'TableHeader': 'table',
  'TableRow': 'table',
  'TableCaption': 'table',
  'Select': 'select',
  'SelectContent': 'select',
  'SelectItem': 'select',
  'SelectTrigger': 'select',
  'SelectValue': 'select',
  'SelectGroup': 'select',
  'SelectLabel': 'select',
  'SelectSeparator': 'select',
  'SelectScrollUpButton': 'select',
  'SelectScrollDownButton': 'select',
  'Checkbox': 'checkbox',
  'Textarea': 'textarea',
  'Progress': 'progress',
  'Tabs': 'tabs',
  'TabsContent': 'tabs',
  'TabsList': 'tabs',
  'TabsTrigger': 'tabs',
  'Tooltip': 'tooltip',
  'TooltipContent': 'tooltip',
  'TooltipProvider': 'tooltip',
  'TooltipTrigger': 'tooltip',
  'NavigationMenu': 'navigation-menu',
  'NavigationMenuContent': 'navigation-menu',
  'NavigationMenuItem': 'navigation-menu',
  'NavigationMenuLink': 'navigation-menu',
  'NavigationMenuList': 'navigation-menu',
  'NavigationMenuTrigger': 'navigation-menu',
  'NavigationMenuIndicator': 'navigation-menu',
  'NavigationMenuViewport': 'navigation-menu',
  'HoverCard': 'hover-card',
  'HoverCardContent': 'hover-card',
  'HoverCardTrigger': 'hover-card',
  'RadioGroup': 'radio-group',
  'RadioGroupItem': 'radio-group',
  'Switch': 'switch',
  'Slider': 'slider',
  'ScrollArea': 'scroll-area',
  'Separator': 'separator',
  'Skeleton': 'skeleton',
  'Badge': 'badge',
  'Alert': 'alert',
  'AlertTitle': 'alert',
  'AlertDescription': 'alert',
  'Breadcrumb': 'breadcrumb',
  'BreadcrumbItem': 'breadcrumb',
  'BreadcrumbLink': 'breadcrumb',
  'BreadcrumbList': 'breadcrumb',
  'BreadcrumbPage': 'breadcrumb',
  'BreadcrumbSeparator': 'breadcrumb',
  'BreadcrumbEllipsis': 'breadcrumb',
  'LoadingSpinner': 'spinner',
  'Spinner': 'spinner',
  'useToast': 'sonner',
};

function fixImportsInFile(filePath) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    
    // Match imports from '@game-guild/ui/components' without a specific path
    const importPattern = /import\s*\{\s*([^}]+)\s*\}\s*from\s*['"]@game-guild\/ui\/components['"];?/g;
    
    let newContent = content;
    const matches = [...content.matchAll(importPattern)];
    
    for (const match of matches) {
      const importStatement = match[0];
      const componentList = match[1];
      
      // Parse the components
      const components = componentList.split(',').map(c => c.trim()).filter(c => c);
      
      // Group components by their file
      const groupedComponents = {};
      
      for (const component of components) {
        const cleanComponent = component.replace(/\s+as\s+\w+$/, ''); // Remove 'as alias'
        const fileName = componentMap[cleanComponent];
        
        if (fileName) {
          if (!groupedComponents[fileName]) {
            groupedComponents[fileName] = [];
          }
          groupedComponents[fileName].push(component);
        } else {
          console.warn(`Unknown component: ${cleanComponent} in file: ${filePath}`);
        }
      }
      
      // Create new import statements
      const newImports = Object.entries(groupedComponents).map(([fileName, comps]) => {
        return `import { ${comps.join(', ')} } from '@game-guild/ui/components/${fileName}';`;
      }).join('\n');
      
      // Replace the old import with new imports
      newContent = newContent.replace(importStatement, newImports);
    }
    
    if (newContent !== content) {
      fs.writeFileSync(filePath, newContent, 'utf8');
      console.log(`Fixed imports in: ${filePath}`);
      return true;
    }
    
  } catch (error) {
    console.error(`Error processing ${filePath}:`, error.message);
  }
  
  return false;
}

function walkDirectory(dir) {
  const files = fs.readdirSync(dir);
  let fixedCount = 0;
  
  for (const file of files) {
    const fullPath = path.join(dir, file);
    const stat = fs.statSync(fullPath);
    
    if (stat.isDirectory()) {
      fixedCount += walkDirectory(fullPath);
    } else if (file.endsWith('.tsx') || file.endsWith('.ts')) {
      if (fixImportsInFile(fullPath)) {
        fixedCount++;
      }
    }
  }
  
  return fixedCount;
}

// Start from the components directory
const componentsDir = './apps/web/src/components';
const fixedCount = walkDirectory(componentsDir);

console.log(`Fixed imports in ${fixedCount} files.`);
