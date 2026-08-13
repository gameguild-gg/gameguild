/**
 * Page-layout settings for `<LexicalSurface />`. Mirrors the Google
 * Docs-style controls (Page size, Orientation, Margins) that show up
 * in the playground reference UI.
 *
 * Sizes are stored as inches (matching the dropdown labels) and
 * converted to CSS pixels at 96 DPI when applied to the editable
 * container. `Pageless` removes the width cap altogether.
 */

export type PageSizeId =
  | "pageless"
  | "a4"
  | "letter"
  | "legal"
  | "tabloid"
  | "a3"
  | "a5"
  | "b4"
  | "b5"
  | "statement"
  | "executive"
  | "folio"

export type PageOrientation = "portrait" | "landscape"
export type PageMargin = "none" | "narrow" | "normal" | "moderate" | "wide"

export interface PageSize {
  id: PageSizeId
  label: string
  /** Width in inches (portrait). `null` means pageless / no cap. */
  widthIn: number | null
  /** Height in inches (portrait). `null` means pageless / no cap. */
  heightIn: number | null
}

export const PAGE_SIZES: PageSize[] = [
  { id: "pageless", label: "Pageless", widthIn: null, heightIn: null },
  { id: "a4", label: 'A4 (8.27" x 11.69")', widthIn: 8.27, heightIn: 11.69 },
  { id: "letter", label: 'Letter (8.5" x 11")', widthIn: 8.5, heightIn: 11 },
  { id: "legal", label: 'Legal (8.5" x 14")', widthIn: 8.5, heightIn: 14 },
  { id: "tabloid", label: 'Tabloid (11" x 17")', widthIn: 11, heightIn: 17 },
  { id: "a3", label: 'A3 (11.69" x 16.54")', widthIn: 11.69, heightIn: 16.54 },
  { id: "a5", label: 'A5 (5.83" x 8.27")', widthIn: 5.83, heightIn: 8.27 },
  { id: "b4", label: 'B4 (9.84" x 13.90")', widthIn: 9.84, heightIn: 13.9 },
  { id: "b5", label: 'B5 (6.93" x 9.84")', widthIn: 6.93, heightIn: 9.84 },
  { id: "statement", label: 'Statement (5.5" x 8.5")', widthIn: 5.5, heightIn: 8.5 },
  { id: "executive", label: 'Executive (7.25" x 10.5")', widthIn: 7.25, heightIn: 10.5 },
  { id: "folio", label: 'Folio (8.5" x 13")', widthIn: 8.5, heightIn: 13 },
]

export const PAGE_MARGIN_INCHES: Record<PageMargin, number> = {
  none: 0,
  narrow: 0.25,
  normal: 0.4,
  moderate: 0.75,
  wide: 1,
}

export const PAGE_MARGIN_LABELS: Record<PageMargin, string> = {
  none: 'None (0")',
  narrow: 'Narrow (0.25")',
  normal: 'Normal (0.4")',
  moderate: 'Moderate (0.75")',
  wide: 'Wide (1")',
}

export const PAGE_ORIENTATION_LABELS: Record<PageOrientation, string> = {
  portrait: "Portrait",
  landscape: "Landscape",
}

export interface PageSettings {
  size: PageSizeId
  orientation: PageOrientation
  margin: PageMargin
}

export const DEFAULT_PAGE_SETTINGS: PageSettings = {
  size: "letter",
  orientation: "portrait",
  margin: "normal",
}

const PX_PER_INCH = 96

/**
 * Returns the CSS style that should be applied to the editable
 * container so the user-visible width/padding matches the requested
 * page settings.
 */
export function pageSettingsToStyle(settings: PageSettings): React.CSSProperties {
  const size = PAGE_SIZES.find((s) => s.id === settings.size) ?? PAGE_SIZES[0]!
  const marginIn = PAGE_MARGIN_INCHES[settings.margin]

  if (size.widthIn == null || size.heightIn == null) {
    // Pageless: no width cap, only horizontal padding.
    return {
      maxWidth: "none",
      width: "100%",
      paddingLeft: `${marginIn}in`,
      paddingRight: `${marginIn}in`,
    }
  }

  const widthIn = settings.orientation === "portrait" ? size.widthIn : size.heightIn
  const heightIn = settings.orientation === "portrait" ? size.heightIn : size.widthIn
  const widthPx = Math.round(widthIn * PX_PER_INCH)
  const heightPx = Math.round(heightIn * PX_PER_INCH)
  const padPx = Math.round(marginIn * PX_PER_INCH)

  return {
    maxWidth: `${widthPx}px`,
    width: "100%",
    minHeight: `${heightPx}px`,
    paddingLeft: `${padPx}px`,
    paddingRight: `${padPx}px`,
    paddingTop: `${padPx}px`,
    paddingBottom: `${padPx}px`,
  }
}

/** True when the settings produce a bounded (page-shaped) layout. */
export function isPagedLayout(settings: PageSettings): boolean {
  const size = PAGE_SIZES.find((s) => s.id === settings.size)
  return !!(size && size.widthIn != null && size.heightIn != null)
}

/** Margin in CSS px (used to draw the dashed usable-area guide). */
export function pageMarginPx(settings: PageSettings): number {
  return Math.round(PAGE_MARGIN_INCHES[settings.margin] * PX_PER_INCH)
}

export interface PageBoxPx {
  /** Sheet width in CSS px (accounts for orientation). */
  widthPx: number
  /** Sheet height in CSS px (accounts for orientation). */
  heightPx: number
  /** Margin in CSS px on every edge. */
  marginPx: number
}

/**
 * Sheet dimensions in CSS px for a bounded (paged) layout, or `null`
 * for pageless. Used by the multi-page background to know how tall one
 * physical sheet is and how many sheets the content currently spans.
 */
export function pageBoxPx(settings: PageSettings): PageBoxPx | null {
  const size = PAGE_SIZES.find((s) => s.id === settings.size)
  if (!size || size.widthIn == null || size.heightIn == null) return null
  const widthIn = settings.orientation === "portrait" ? size.widthIn : size.heightIn
  const heightIn = settings.orientation === "portrait" ? size.heightIn : size.widthIn
  return {
    widthPx: Math.round(widthIn * PX_PER_INCH),
    heightPx: Math.round(heightIn * PX_PER_INCH),
    marginPx: pageMarginPx(settings),
  }
}
