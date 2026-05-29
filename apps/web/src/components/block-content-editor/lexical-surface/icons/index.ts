/**
 * Centralized lucide-react icon re-exports used across the
 * lexical-surface plugins (toolbar, floating bubbles, component picker,
 * draggable handle). Keeping them in one place makes it easy to swap
 * the icon set later and gives ports of upstream playground code a
 * single import line.
 */
export {
  // Toolbar — history
  Undo2 as UndoIcon,
  Redo2 as RedoIcon,
  // Toolbar — inline format
  Bold as BoldIcon,
  Italic as ItalicIcon,
  Underline as UnderlineIcon,
  Strikethrough as StrikethroughIcon,
  Code as CodeInlineIcon,
  Subscript as SubscriptIcon,
  Superscript as SuperscriptIcon,
  Link as LinkIcon,
  Eraser as ClearFormatIcon,
  // Toolbar — block format
  Heading1 as Heading1Icon,
  Heading2 as Heading2Icon,
  Heading3 as Heading3Icon,
  Pilcrow as ParagraphIcon,
  List as BulletedListIcon,
  ListOrdered as NumberedListIcon,
  ListChecks as CheckListIcon,
  Quote as QuoteIcon,
  Code2 as CodeBlockIcon,
  // Toolbar — alignment & indent
  AlignLeft as AlignLeftIcon,
  AlignCenter as AlignCenterIcon,
  AlignRight as AlignRightIcon,
  AlignJustify as AlignJustifyIcon,
  Outdent as OutdentIcon,
  Indent as IndentIcon,
  // Toolbar — color & insert
  Type as FontFamilyIcon,
  Palette as TextColorIcon,
  PaintBucket as BgColorIcon,
  Plus as InsertIcon,
  Minus as HorizontalRuleIcon,
  FileText as PageIcon,
  // Picker & misc
  ChevronDown as ChevronDownIcon,
  ChevronUp as ChevronUpIcon,
  GripVertical as DragHandleIcon,
  Check as CheckIcon,
  X as CloseIcon,
  Pencil as EditIcon,
  Trash2 as DeleteIcon,
} from "lucide-react"
