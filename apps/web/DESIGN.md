# GameGuild Web Design System

## 1. Atmosphere & Identity

GameGuild Web is a focused community and operations workspace. It is calm, compact, and task-led: people should be able to understand the current state, make a deliberate change, and return to their work without decorative interruption. The signature is neutral tonal depth, where panels, overlays, and navigation separate through the existing semantic surface tokens instead of bespoke color treatments.

## 2. Color

All application surfaces use the semantic tokens exported by `@game-guild/ui/globals.css`.

| Role | Token | Light | Dark | Usage |
| --- | --- | --- | --- |
| Canvas | `background` | `oklch(1 0 0)` | `oklch(0.145 0 0)` | Page background |
| Primary surface | `card` | `oklch(1 0 0)` | `oklch(0.205 0 0)` | Cards and persistent panels |
| Elevated surface | `popover` | `oklch(1 0 0)` | `oklch(0.205 0 0)` | Dialogs, sheets, and menus |
| Primary text | `foreground` | `oklch(0.145 0 0)` | `oklch(0.985 0 0)` | Labels and body copy |
| Secondary text | `muted-foreground` | `oklch(0.556 0 0)` | `oklch(0.708 0 0)` | Descriptions and metadata |
| Interactive emphasis | `primary` / `primary-foreground` | Neutral inverse | Neutral inverse | Primary actions |
| Error | `destructive` | `oklch(0.577 0.245 27.325)` | `oklch(0.704 0.191 22.216)` | Failures and destructive actions |
| Separation | `border`, `input`, `ring` | Semantic UI values | Semantic UI values | Inputs, dividers, focus |

Rules:
- No raw color values in web components.
- Status always combines text or an icon with color.
- Accent is reserved for actions and selected controls, never decoration.

## 3. Typography

The application uses the configured Geist Sans and Geist Mono stacks exposed as `font-sans` and `font-mono` by `@game-guild/ui`.

| Level | Tailwind usage | Weight | Usage |
| --- | --- | --- | --- |
| Page title | `text-2xl` | `font-semibold` | Top-level workflow titles |
| Section title | `text-lg` | `font-semibold` | Drawer and panel headings |
| Body | `text-sm` | Regular | Forms, actions, and descriptions |
| Metadata | `text-sm text-muted-foreground` | Regular | Supporting context |
| Label | `text-sm` | `font-medium` | Form controls |

Body text is never smaller than `text-sm`; long labels wrap rather than forcing horizontal primary-content overflow.

## 4. Spacing & Layout

The base unit is 4px. Existing Tailwind scale values map to the following intent:

| Token | Tailwind | Usage |
| --- | --- | --- |
| Tight | `gap-2` / `p-2` | Icon-to-label and compact actions |
| Default | `gap-3` / `p-3` | Dense panels and grouped controls |
| Form | `gap-4` / `p-4` | Field rhythm and drawer sections |
| Section | `gap-5` / `p-6` | Major form regions |
| Page | `gap-8` | Independent content regions |

The dashboard uses a fixed navigation shell with a fluid main region. All grids use intrinsic or responsive columns (`sm:grid-cols-2` where two related fields fit), and forms collapse to one column on narrow viewports. Overlay bodies own their scroll area; page scroll is not used to reach an overlay footer.

## 5. Components

### Workflow Drawer
- **Structure:** `Sheet` -> header -> scrollable form body -> fixed footer.
- **Variants:** create and edit.
- **Spacing:** `p-6` content regions, `gap-4` field groups, `gap-5` form sections.
- **States:** closed, clean open, dirty open, submitting, success, recoverable error.
- **Accessibility:** labelled Sheet title/description, keyboard escape and close affordance; a dirty dismissal opens an `AlertDialog` that keeps focus within the confirmation until the manager decides.
- **Motion:** existing Sheet transform/opacity transition; no custom motion. Reduced-motion users receive the primitive's non-essential animation fallback.
- **Layout:** right-side `sidebar` overlay; the form body is the scroll owner and footer remains available.

### Event Schedule Fields
- **Structure:** application window, event window, recurrence disclosure, feedback requirement.
- **Variants:** one-off event; daily, weekly, and monthly recurrence.
- **States:** default, valid, invalid server response, disabled while submitting.
- **Accessibility:** each control has an explicit label; recurrence fields remain keyboard reachable and the selected days use named checkbox labels.
- **Layout:** intrinsic two-column grid on sufficient width and one column otherwise.

### Discard Confirmation
- **Structure:** `AlertDialog` with continue editing and discard actions.
- **States:** hidden, open, dismiss to editing, discard draft.
- **Accessibility:** modal focus trap, explicit consequence copy, safe action first in DOM order.

## 6. Motion & Interaction

- Micro feedback uses the existing UI primitive transitions (100-300ms) and only `opacity` or `transform`.
- Opening a Sheet and dismissing a confirmation must be interruptible; controls remain available as soon as their state changes.
- No layout-property animation is introduced.
- The recurrence disclosure is stateful rather than decorative; it changes only in response to the manager's explicit selection.

## 7. Depth & Surface

The application uses the shared **mixed** strategy: semantic card/popover tonal surfaces, semantic borders, and the existing modal/sheet shadow. New Testing Lab UI composes these primitives rather than adding new shadows, gradients, or radii.

## 8. Accessibility Constraints & Accepted Debt

- Target: WCAG 2.2 AA, visible focus rings, full keyboard access, and semantic labels for every form control.
- A dirty event draft is never discarded by overlay click, Escape, close control, or Cancel without explicit confirmation.
- Form submission and server errors use a polite live region.
- Primary content must remain horizontally scroll-free at 390px.

| Item | Location | Why accepted | Owner / Exit |
| --- | --- | --- | --- |
| React diagnostic tooling is not wired in this package | `apps/web` | Existing app tooling does not include `react-scan`, `react-grab`, or `react-doctor`; adding them is outside this focused workflow change. | Platform tooling before the next web-wide performance audit |
