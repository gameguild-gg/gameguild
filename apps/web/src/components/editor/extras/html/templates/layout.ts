import { Layout, Columns, PanelTop, Grid3X3, Layers } from "lucide-react"
import type { HTMLTemplate } from "./types"

export const layoutTemplates: HTMLTemplate[] = [
  {
    id: "page-basic",
    title: "Basic Page",
    description: "Simple HTML page structure",
    category: "layout",
    icon: Layout,
    code: `<div style="max-width:800px;margin:0 auto;padding:2rem;font-family:system-ui,sans-serif">
  <header style="margin-bottom:2rem">
    <h1 style="font-size:2rem;margin:0">Page Title</h1>
    <p style="color:#6b7280;margin-top:0.5rem">A brief description of this page.</p>
  </header>

  <main>
    <p>Your content goes here.</p>
  </main>

  <footer style="margin-top:3rem;padding-top:1rem;border-top:1px solid #e5e7eb;color:#9ca3af;font-size:0.875rem">
    &copy; ${new Date().getFullYear()} Your Name
  </footer>
</div>`,
  },
  {
    id: "two-columns",
    title: "Two Columns",
    description: "Side-by-side layout with flex",
    category: "layout",
    icon: Columns,
    code: `<div style="display:flex;gap:2rem;max-width:900px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="flex:1;padding:1.5rem;background:#f9fafb;border-radius:8px">
    <h2 style="margin-top:0">Left Column</h2>
    <p>Content for the left side.</p>
  </div>
  <div style="flex:1;padding:1.5rem;background:#f9fafb;border-radius:8px">
    <h2 style="margin-top:0">Right Column</h2>
    <p>Content for the right side.</p>
  </div>
</div>`,
  },
  {
    id: "three-columns",
    title: "Three Columns",
    description: "Three equal columns with grid",
    category: "layout",
    icon: Grid3X3,
    code: `<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:1.5rem;max-width:1000px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="padding:1.5rem;background:#f0f9ff;border-radius:8px;border:1px solid #bae6fd">
    <h3 style="margin-top:0;color:#0369a1">Column 1</h3>
    <p style="color:#4b5563">First column content.</p>
  </div>
  <div style="padding:1.5rem;background:#f0fdf4;border-radius:8px;border:1px solid #bbf7d0">
    <h3 style="margin-top:0;color:#15803d">Column 2</h3>
    <p style="color:#4b5563">Second column content.</p>
  </div>
  <div style="padding:1.5rem;background:#fefce8;border-radius:8px;border:1px solid #fde68a">
    <h3 style="margin-top:0;color:#a16207">Column 3</h3>
    <p style="color:#4b5563">Third column content.</p>
  </div>
</div>`,
  },
  {
    id: "hero-section",
    title: "Hero Section",
    description: "Full-width hero with CTA",
    category: "layout",
    icon: PanelTop,
    code: `<div style="text-align:center;padding:4rem 2rem;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);border-radius:12px;color:#fff;font-family:system-ui,sans-serif">
  <h1 style="font-size:2.5rem;margin:0;font-weight:800">Welcome to Our Site</h1>
  <p style="font-size:1.25rem;margin:1rem 0 2rem;opacity:0.9;max-width:600px;margin-left:auto;margin-right:auto">
    A brief tagline that describes what makes this special.
  </p>
  <a href="#" style="display:inline-block;padding:0.75rem 2rem;background:#fff;color:#667eea;border-radius:8px;text-decoration:none;font-weight:600;font-size:1rem">
    Get Started
  </a>
</div>`,
  },
  {
    id: "sidebar-layout",
    title: "Sidebar Layout",
    description: "Content with fixed sidebar",
    category: "layout",
    icon: Layers,
    code: `<div style="display:flex;gap:1.5rem;max-width:1000px;margin:0 auto;font-family:system-ui,sans-serif">
  <aside style="width:240px;shrink:0;padding:1.5rem;background:#f8fafc;border-radius:8px;border:1px solid #e2e8f0">
    <h3 style="margin-top:0;font-size:0.875rem;text-transform:uppercase;color:#64748b;letter-spacing:0.05em">Navigation</h3>
    <nav>
      <a href="#" style="display:block;padding:0.5rem 0;color:#334155;text-decoration:none;border-bottom:1px solid #f1f5f9">Home</a>
      <a href="#" style="display:block;padding:0.5rem 0;color:#334155;text-decoration:none;border-bottom:1px solid #f1f5f9">About</a>
      <a href="#" style="display:block;padding:0.5rem 0;color:#334155;text-decoration:none;border-bottom:1px solid #f1f5f9">Services</a>
      <a href="#" style="display:block;padding:0.5rem 0;color:#334155;text-decoration:none">Contact</a>
    </nav>
  </aside>
  <main style="flex:1;padding:1.5rem">
    <h1 style="margin-top:0">Main Content</h1>
    <p>This is the main content area next to the sidebar.</p>
  </main>
</div>`,
  },
]
