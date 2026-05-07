import { CreditCard, AlertCircle, Tag, Star, Users, MessageSquare } from "lucide-react"
import type { HTMLTemplate } from "./types"

export const componentTemplates: HTMLTemplate[] = [
  {
    id: "card",
    title: "Card",
    description: "Content card with shadow",
    category: "components",
    icon: CreditCard,
    code: `<div style="max-width:400px;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);font-family:system-ui,sans-serif;border:1px solid #e5e7eb">
  <div style="height:200px;background:linear-gradient(135deg,#6366f1,#8b5cf6);display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.5rem;font-weight:600">
    Image Area
  </div>
  <div style="padding:1.5rem">
    <h3 style="margin:0 0 0.5rem;font-size:1.25rem">Card Title</h3>
    <p style="margin:0;color:#6b7280;line-height:1.6">A short description of what this card is about. Keep it concise and informative.</p>
    <a href="#" style="display:inline-block;margin-top:1rem;padding:0.5rem 1rem;background:#6366f1;color:#fff;border-radius:6px;text-decoration:none;font-size:0.875rem;font-weight:500">
      Learn More
    </a>
  </div>
</div>`,
  },
  {
    id: "alert-box",
    title: "Alert Box",
    description: "Info, warning, error alerts",
    category: "components",
    icon: AlertCircle,
    code: `<div style="font-family:system-ui,sans-serif;max-width:600px;display:flex;flex-direction:column;gap:1rem">
  <div style="padding:1rem 1.25rem;background:#eff6ff;border-left:4px solid #3b82f6;border-radius:0 8px 8px 0;color:#1e40af">
    <strong>Info:</strong> This is an informational message.
  </div>
  <div style="padding:1rem 1.25rem;background:#fefce8;border-left:4px solid #eab308;border-radius:0 8px 8px 0;color:#854d0e">
    <strong>Warning:</strong> Please proceed with caution.
  </div>
  <div style="padding:1rem 1.25rem;background:#fef2f2;border-left:4px solid #ef4444;border-radius:0 8px 8px 0;color:#991b1b">
    <strong>Error:</strong> Something went wrong.
  </div>
  <div style="padding:1rem 1.25rem;background:#f0fdf4;border-left:4px solid #22c55e;border-radius:0 8px 8px 0;color:#166534">
    <strong>Success:</strong> Operation completed successfully.
  </div>
</div>`,
  },
  {
    id: "badge-tags",
    title: "Badges & Tags",
    description: "Colored label badges",
    category: "components",
    icon: Tag,
    code: `<div style="display:flex;flex-wrap:wrap;gap:0.5rem;font-family:system-ui,sans-serif">
  <span style="padding:0.25rem 0.75rem;background:#dbeafe;color:#1d4ed8;border-radius:99px;font-size:0.75rem;font-weight:500">Blue</span>
  <span style="padding:0.25rem 0.75rem;background:#dcfce7;color:#166534;border-radius:99px;font-size:0.75rem;font-weight:500">Green</span>
  <span style="padding:0.25rem 0.75rem;background:#fef3c7;color:#92400e;border-radius:99px;font-size:0.75rem;font-weight:500">Yellow</span>
  <span style="padding:0.25rem 0.75rem;background:#fce7f3;color:#9d174d;border-radius:99px;font-size:0.75rem;font-weight:500">Pink</span>
  <span style="padding:0.25rem 0.75rem;background:#ede9fe;color:#5b21b6;border-radius:99px;font-size:0.75rem;font-weight:500">Purple</span>
  <span style="padding:0.25rem 0.75rem;background:#fee2e2;color:#991b1b;border-radius:99px;font-size:0.75rem;font-weight:500">Red</span>
</div>`,
  },
  {
    id: "rating-stars",
    title: "Star Rating",
    description: "Star rating display",
    category: "components",
    icon: Star,
    code: `<div style="font-family:system-ui,sans-serif;max-width:400px">
  <div style="display:flex;align-items:center;gap:0.5rem;margin-bottom:0.5rem">
    <span style="font-size:1.5rem;letter-spacing:2px;color:#f59e0b">★★★★</span><span style="font-size:1.5rem;color:#d1d5db">★</span>
    <span style="font-size:1rem;font-weight:600;color:#1f2937">4.0</span>
    <span style="font-size:0.875rem;color:#6b7280">(128 reviews)</span>
  </div>
  <div style="display:flex;flex-direction:column;gap:4px">
    <div style="display:flex;align-items:center;gap:0.5rem;font-size:0.75rem;color:#6b7280">
      <span>5★</span><div style="flex:1;height:8px;background:#e5e7eb;border-radius:4px;overflow:hidden"><div style="width:60%;height:100%;background:#f59e0b;border-radius:4px"></div></div><span>60%</span>
    </div>
    <div style="display:flex;align-items:center;gap:0.5rem;font-size:0.75rem;color:#6b7280">
      <span>4★</span><div style="flex:1;height:8px;background:#e5e7eb;border-radius:4px;overflow:hidden"><div style="width:25%;height:100%;background:#f59e0b;border-radius:4px"></div></div><span>25%</span>
    </div>
    <div style="display:flex;align-items:center;gap:0.5rem;font-size:0.75rem;color:#6b7280">
      <span>3★</span><div style="flex:1;height:8px;background:#e5e7eb;border-radius:4px;overflow:hidden"><div style="width:10%;height:100%;background:#f59e0b;border-radius:4px"></div></div><span>10%</span>
    </div>
  </div>
</div>`,
  },
  {
    id: "testimonial",
    title: "Testimonial",
    description: "User quote card",
    category: "components",
    icon: MessageSquare,
    code: `<div style="max-width:500px;padding:2rem;background:#f8fafc;border-radius:12px;border:1px solid #e2e8f0;font-family:system-ui,sans-serif;position:relative">
  <div style="font-size:3rem;color:#cbd5e1;line-height:1;position:absolute;top:1rem;left:1.5rem">"</div>
  <p style="margin:1.5rem 0 1.5rem;color:#334155;font-size:1.1rem;line-height:1.7;font-style:italic">
    This product completely changed how I work. The interface is intuitive and the results are outstanding.
  </p>
  <div style="display:flex;align-items:center;gap:1rem">
    <div style="width:48px;height:48px;border-radius:50%;background:linear-gradient(135deg,#6366f1,#a855f7);display:flex;align-items:center;justify-content:center;color:#fff;font-weight:700;font-size:1.1rem">JD</div>
    <div>
      <div style="font-weight:600;color:#1e293b">Jane Doe</div>
      <div style="font-size:0.875rem;color:#64748b">CEO at Company</div>
    </div>
  </div>
</div>`,
  },
  {
    id: "team-member",
    title: "Team Member",
    description: "Profile card",
    category: "components",
    icon: Users,
    code: `<div style="max-width:300px;text-align:center;padding:2rem;border-radius:12px;border:1px solid #e5e7eb;font-family:system-ui,sans-serif">
  <div style="width:96px;height:96px;border-radius:50%;background:linear-gradient(135deg,#06b6d4,#3b82f6);margin:0 auto 1rem;display:flex;align-items:center;justify-content:center;color:#fff;font-size:2rem;font-weight:700">AB</div>
  <h3 style="margin:0;font-size:1.25rem;color:#1f2937">Alex Brown</h3>
  <p style="margin:0.25rem 0 1rem;color:#6b7280;font-size:0.875rem">Lead Developer</p>
  <p style="margin:0 0 1.5rem;color:#4b5563;font-size:0.875rem;line-height:1.6">Full-stack engineer passionate about clean code and great user experiences.</p>
  <div style="display:flex;justify-content:center;gap:0.75rem">
    <a href="#" style="color:#6b7280;text-decoration:none;font-size:0.875rem">GitHub</a>
    <a href="#" style="color:#6b7280;text-decoration:none;font-size:0.875rem">LinkedIn</a>
    <a href="#" style="color:#6b7280;text-decoration:none;font-size:0.875rem">Twitter</a>
  </div>
</div>`,
  },
]
