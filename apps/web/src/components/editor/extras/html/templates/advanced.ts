import { Table2, BarChart3, Clock, Sparkles, Code } from "lucide-react"
import type { HTMLTemplate } from "./types"

export const advancedTemplates: HTMLTemplate[] = [
  {
    id: "data-table",
    title: "Data Table",
    description: "Styled HTML table",
    category: "advanced",
    icon: Table2,
    code: `<div style="max-width:700px;margin:0 auto;font-family:system-ui,sans-serif;overflow-x:auto">
  <table style="width:100%;border-collapse:collapse;font-size:0.875rem">
    <thead>
      <tr style="background:#f8fafc;border-bottom:2px solid #e2e8f0">
        <th style="padding:0.75rem 1rem;text-align:left;font-weight:600;color:#334155">Name</th>
        <th style="padding:0.75rem 1rem;text-align:left;font-weight:600;color:#334155">Role</th>
        <th style="padding:0.75rem 1rem;text-align:right;font-weight:600;color:#334155">Status</th>
      </tr>
    </thead>
    <tbody>
      <tr style="border-bottom:1px solid #f1f5f9">
        <td style="padding:0.75rem 1rem;color:#1e293b">Alice Johnson</td>
        <td style="padding:0.75rem 1rem;color:#64748b">Developer</td>
        <td style="padding:0.75rem 1rem;text-align:right"><span style="padding:0.125rem 0.5rem;background:#dcfce7;color:#166534;border-radius:99px;font-size:0.75rem">Active</span></td>
      </tr>
      <tr style="border-bottom:1px solid #f1f5f9;background:#fafafa">
        <td style="padding:0.75rem 1rem;color:#1e293b">Bob Smith</td>
        <td style="padding:0.75rem 1rem;color:#64748b">Designer</td>
        <td style="padding:0.75rem 1rem;text-align:right"><span style="padding:0.125rem 0.5rem;background:#dcfce7;color:#166534;border-radius:99px;font-size:0.75rem">Active</span></td>
      </tr>
      <tr style="border-bottom:1px solid #f1f5f9">
        <td style="padding:0.75rem 1rem;color:#1e293b">Carol White</td>
        <td style="padding:0.75rem 1rem;color:#64748b">Manager</td>
        <td style="padding:0.75rem 1rem;text-align:right"><span style="padding:0.125rem 0.5rem;background:#fef3c7;color:#92400e;border-radius:99px;font-size:0.75rem">Away</span></td>
      </tr>
    </tbody>
  </table>
</div>`,
  },
  {
    id: "progress-bars",
    title: "Progress Bars",
    description: "Horizontal progress indicators",
    category: "advanced",
    icon: BarChart3,
    code: `<div style="max-width:500px;margin:0 auto;font-family:system-ui,sans-serif;display:flex;flex-direction:column;gap:1.25rem">
  <div>
    <div style="display:flex;justify-content:space-between;margin-bottom:4px;font-size:0.875rem">
      <span style="font-weight:500;color:#374151">HTML</span><span style="color:#6b7280">95%</span>
    </div>
    <div style="height:10px;background:#e5e7eb;border-radius:99px;overflow:hidden">
      <div style="width:95%;height:100%;background:linear-gradient(90deg,#3b82f6,#2563eb);border-radius:99px"></div>
    </div>
  </div>
  <div>
    <div style="display:flex;justify-content:space-between;margin-bottom:4px;font-size:0.875rem">
      <span style="font-weight:500;color:#374151">CSS</span><span style="color:#6b7280">85%</span>
    </div>
    <div style="height:10px;background:#e5e7eb;border-radius:99px;overflow:hidden">
      <div style="width:85%;height:100%;background:linear-gradient(90deg,#8b5cf6,#7c3aed);border-radius:99px"></div>
    </div>
  </div>
  <div>
    <div style="display:flex;justify-content:space-between;margin-bottom:4px;font-size:0.875rem">
      <span style="font-weight:500;color:#374151">JavaScript</span><span style="color:#6b7280">75%</span>
    </div>
    <div style="height:10px;background:#e5e7eb;border-radius:99px;overflow:hidden">
      <div style="width:75%;height:100%;background:linear-gradient(90deg,#f59e0b,#d97706);border-radius:99px"></div>
    </div>
  </div>
</div>`,
  },
  {
    id: "timeline",
    title: "Timeline",
    description: "Vertical event timeline",
    category: "advanced",
    icon: Clock,
    code: `<div style="max-width:600px;margin:0 auto;font-family:system-ui,sans-serif;padding-left:2rem;position:relative">
  <div style="position:absolute;left:7px;top:8px;bottom:8px;width:2px;background:#e5e7eb"></div>

  <div style="position:relative;padding-bottom:2rem">
    <div style="position:absolute;left:-2rem;top:4px;width:16px;height:16px;border-radius:50%;background:#2563eb;border:3px solid #bfdbfe"></div>
    <div style="padding-left:0.5rem">
      <div style="font-size:0.75rem;color:#6b7280;margin-bottom:2px">January 2024</div>
      <h4 style="margin:0 0 0.25rem;font-size:1rem;color:#1f2937">Project Started</h4>
      <p style="margin:0;color:#4b5563;font-size:0.875rem;line-height:1.6">Initial planning and requirements gathering phase.</p>
    </div>
  </div>

  <div style="position:relative;padding-bottom:2rem">
    <div style="position:absolute;left:-2rem;top:4px;width:16px;height:16px;border-radius:50%;background:#8b5cf6;border:3px solid #ddd6fe"></div>
    <div style="padding-left:0.5rem">
      <div style="font-size:0.75rem;color:#6b7280;margin-bottom:2px">March 2024</div>
      <h4 style="margin:0 0 0.25rem;font-size:1rem;color:#1f2937">Development Phase</h4>
      <p style="margin:0;color:#4b5563;font-size:0.875rem;line-height:1.6">Core features implemented and tested.</p>
    </div>
  </div>

  <div style="position:relative">
    <div style="position:absolute;left:-2rem;top:4px;width:16px;height:16px;border-radius:50%;background:#22c55e;border:3px solid #bbf7d0"></div>
    <div style="padding-left:0.5rem">
      <div style="font-size:0.75rem;color:#6b7280;margin-bottom:2px">June 2024</div>
      <h4 style="margin:0 0 0.25rem;font-size:1rem;color:#1f2937">Launch</h4>
      <p style="margin:0;color:#4b5563;font-size:0.875rem;line-height:1.6">Public release and first users onboarded.</p>
    </div>
  </div>
</div>`,
  },
  {
    id: "pricing-table",
    title: "Pricing Table",
    description: "Three-tier pricing cards",
    category: "advanced",
    icon: Sparkles,
    code: `<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:1.5rem;max-width:900px;margin:0 auto;font-family:system-ui,sans-serif;align-items:start">
  <div style="padding:2rem;border:1px solid #e5e7eb;border-radius:12px;text-align:center">
    <h3 style="margin:0 0 0.25rem;color:#6b7280;font-size:0.875rem;text-transform:uppercase;letter-spacing:0.05em">Basic</h3>
    <div style="font-size:2.5rem;font-weight:800;color:#1f2937;margin:0.5rem 0">$9</div>
    <p style="color:#9ca3af;font-size:0.875rem;margin:0 0 1.5rem">/month</p>
    <ul style="list-style:none;padding:0;margin:0 0 1.5rem;text-align:left;font-size:0.875rem;color:#4b5563">
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ 5 Projects</li>
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ 1 GB Storage</li>
      <li style="padding:0.375rem 0">✓ Email Support</li>
    </ul>
    <button style="width:100%;padding:0.625rem;background:#fff;color:#374151;border:1px solid #d1d5db;border-radius:6px;font-size:0.875rem;cursor:pointer">Get Started</button>
  </div>

  <div style="padding:2rem;border:2px solid #2563eb;border-radius:12px;text-align:center;position:relative;box-shadow:0 4px 6px -1px rgba(37,99,235,0.1)">
    <div style="position:absolute;top:-12px;left:50%;transform:translateX(-50%);background:#2563eb;color:#fff;padding:0.125rem 0.75rem;border-radius:99px;font-size:0.75rem;font-weight:600">Popular</div>
    <h3 style="margin:0 0 0.25rem;color:#2563eb;font-size:0.875rem;text-transform:uppercase;letter-spacing:0.05em">Pro</h3>
    <div style="font-size:2.5rem;font-weight:800;color:#1f2937;margin:0.5rem 0">$29</div>
    <p style="color:#9ca3af;font-size:0.875rem;margin:0 0 1.5rem">/month</p>
    <ul style="list-style:none;padding:0;margin:0 0 1.5rem;text-align:left;font-size:0.875rem;color:#4b5563">
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ Unlimited Projects</li>
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ 50 GB Storage</li>
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ Priority Support</li>
      <li style="padding:0.375rem 0">✓ Analytics</li>
    </ul>
    <button style="width:100%;padding:0.625rem;background:#2563eb;color:#fff;border:none;border-radius:6px;font-size:0.875rem;font-weight:600;cursor:pointer">Get Started</button>
  </div>

  <div style="padding:2rem;border:1px solid #e5e7eb;border-radius:12px;text-align:center">
    <h3 style="margin:0 0 0.25rem;color:#6b7280;font-size:0.875rem;text-transform:uppercase;letter-spacing:0.05em">Enterprise</h3>
    <div style="font-size:2.5rem;font-weight:800;color:#1f2937;margin:0.5rem 0">$99</div>
    <p style="color:#9ca3af;font-size:0.875rem;margin:0 0 1.5rem">/month</p>
    <ul style="list-style:none;padding:0;margin:0 0 1.5rem;text-align:left;font-size:0.875rem;color:#4b5563">
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ Everything in Pro</li>
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ Unlimited Storage</li>
      <li style="padding:0.375rem 0;border-bottom:1px solid #f3f4f6">✓ Dedicated Support</li>
      <li style="padding:0.375rem 0">✓ Custom Integrations</li>
    </ul>
    <button style="width:100%;padding:0.625rem;background:#fff;color:#374151;border:1px solid #d1d5db;border-radius:6px;font-size:0.875rem;cursor:pointer">Contact Sales</button>
  </div>
</div>`,
  },
  {
    id: "code-snippet",
    title: "Code Snippet",
    description: "Styled code block",
    category: "advanced",
    icon: Code,
    code: `<div style="max-width:600px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="background:#1e293b;border-radius:8px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1)">
    <div style="display:flex;align-items:center;gap:0.5rem;padding:0.75rem 1rem;background:#0f172a;border-bottom:1px solid #334155">
      <span style="width:12px;height:12px;border-radius:50%;background:#ef4444"></span>
      <span style="width:12px;height:12px;border-radius:50%;background:#eab308"></span>
      <span style="width:12px;height:12px;border-radius:50%;background:#22c55e"></span>
      <span style="margin-left:auto;color:#64748b;font-size:0.75rem">script.js</span>
    </div>
    <pre style="margin:0;padding:1.25rem;color:#e2e8f0;font-family:'Fira Code',Consolas,monospace;font-size:0.875rem;line-height:1.7;overflow-x:auto"><code><span style="color:#c084fc">function</span> <span style="color:#38bdf8">greet</span>(<span style="color:#fb923c">name</span>) {
  <span style="color:#c084fc">const</span> message = <span style="color:#a3e635">\`Hello, \${</span><span style="color:#fb923c">name</span><span style="color:#a3e635">}!\`</span>;
  <span style="color:#38bdf8">console</span>.<span style="color:#38bdf8">log</span>(message);
  <span style="color:#c084fc">return</span> message;
}

<span style="color:#38bdf8">greet</span>(<span style="color:#a3e635">"World"</span>);</code></pre>
  </div>
</div>`,
  },
  {
    id: "accordion",
    title: "Accordion / FAQ",
    description: "Collapsible sections",
    category: "advanced",
    icon: Sparkles,
    code: `<div style="max-width:600px;margin:0 auto;font-family:system-ui,sans-serif;display:flex;flex-direction:column;gap:0.5rem">
  <details style="border:1px solid #e5e7eb;border-radius:8px;overflow:hidden">
    <summary style="padding:1rem 1.25rem;cursor:pointer;font-weight:500;color:#1f2937;background:#fafafa;list-style:none;display:flex;justify-content:space-between;align-items:center">
      What is this product?
      <span style="font-size:1.25rem;color:#9ca3af;transition:transform 0.2s">▸</span>
    </summary>
    <div style="padding:1rem 1.25rem;color:#4b5563;line-height:1.6;border-top:1px solid #e5e7eb">
      This is a powerful tool designed to help you build amazing things quickly and efficiently.
    </div>
  </details>
  <details style="border:1px solid #e5e7eb;border-radius:8px;overflow:hidden">
    <summary style="padding:1rem 1.25rem;cursor:pointer;font-weight:500;color:#1f2937;background:#fafafa;list-style:none;display:flex;justify-content:space-between;align-items:center">
      How do I get started?
      <span style="font-size:1.25rem;color:#9ca3af">▸</span>
    </summary>
    <div style="padding:1rem 1.25rem;color:#4b5563;line-height:1.6;border-top:1px solid #e5e7eb">
      Simply sign up for an account and follow the onboarding guide. You'll be up and running in minutes.
    </div>
  </details>
  <details style="border:1px solid #e5e7eb;border-radius:8px;overflow:hidden">
    <summary style="padding:1rem 1.25rem;cursor:pointer;font-weight:500;color:#1f2937;background:#fafafa;list-style:none;display:flex;justify-content:space-between;align-items:center">
      Is there a free plan?
      <span style="font-size:1.25rem;color:#9ca3af">▸</span>
    </summary>
    <div style="padding:1rem 1.25rem;color:#4b5563;line-height:1.6;border-top:1px solid #e5e7eb">
      Yes! We offer a generous free tier that includes all basic features to get you started.
    </div>
  </details>
</div>`,
  },
]
