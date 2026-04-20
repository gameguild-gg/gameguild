import { FormInput, Mail, Search, ToggleLeft } from "lucide-react"
import type { HTMLTemplate } from "./types"

export const formTemplates: HTMLTemplate[] = [
  {
    id: "contact-form",
    title: "Contact Form",
    description: "Name, email, message fields",
    category: "forms",
    icon: Mail,
    code: `<form style="max-width:480px;margin:0 auto;font-family:system-ui,sans-serif;display:flex;flex-direction:column;gap:1.25rem">
  <h2 style="margin:0;font-size:1.5rem;color:#1f2937">Contact Us</h2>
  <div>
    <label style="display:block;margin-bottom:4px;font-size:0.875rem;font-weight:500;color:#374151">Name</label>
    <input type="text" placeholder="Your name" style="width:100%;padding:0.625rem 0.75rem;border:1px solid #d1d5db;border-radius:6px;font-size:0.875rem;outline:none;box-sizing:border-box">
  </div>
  <div>
    <label style="display:block;margin-bottom:4px;font-size:0.875rem;font-weight:500;color:#374151">Email</label>
    <input type="email" placeholder="you@example.com" style="width:100%;padding:0.625rem 0.75rem;border:1px solid #d1d5db;border-radius:6px;font-size:0.875rem;outline:none;box-sizing:border-box">
  </div>
  <div>
    <label style="display:block;margin-bottom:4px;font-size:0.875rem;font-weight:500;color:#374151">Message</label>
    <textarea rows="4" placeholder="Your message..." style="width:100%;padding:0.625rem 0.75rem;border:1px solid #d1d5db;border-radius:6px;font-size:0.875rem;outline:none;resize:vertical;box-sizing:border-box"></textarea>
  </div>
  <button type="submit" style="padding:0.625rem 1.5rem;background:#2563eb;color:#fff;border:none;border-radius:6px;font-size:0.875rem;font-weight:500;cursor:pointer;align-self:flex-start">
    Send Message
  </button>
</form>`,
  },
  {
    id: "login-form",
    title: "Login Form",
    description: "Sign-in with email and password",
    category: "forms",
    icon: FormInput,
    code: `<div style="max-width:400px;margin:0 auto;padding:2rem;border-radius:12px;border:1px solid #e5e7eb;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);font-family:system-ui,sans-serif">
  <h2 style="margin:0 0 0.25rem;font-size:1.5rem;text-align:center;color:#1f2937">Sign In</h2>
  <p style="margin:0 0 1.5rem;text-align:center;color:#6b7280;font-size:0.875rem">Welcome back!</p>
  <form style="display:flex;flex-direction:column;gap:1rem">
    <div>
      <label style="display:block;margin-bottom:4px;font-size:0.875rem;font-weight:500;color:#374151">Email</label>
      <input type="email" placeholder="you@example.com" style="width:100%;padding:0.625rem 0.75rem;border:1px solid #d1d5db;border-radius:6px;font-size:0.875rem;outline:none;box-sizing:border-box">
    </div>
    <div>
      <label style="display:block;margin-bottom:4px;font-size:0.875rem;font-weight:500;color:#374151">Password</label>
      <input type="password" placeholder="••••••••" style="width:100%;padding:0.625rem 0.75rem;border:1px solid #d1d5db;border-radius:6px;font-size:0.875rem;outline:none;box-sizing:border-box">
    </div>
    <div style="display:flex;justify-content:space-between;align-items:center;font-size:0.8125rem">
      <label style="display:flex;align-items:center;gap:0.375rem;color:#4b5563;cursor:pointer">
        <input type="checkbox" style="cursor:pointer"> Remember me
      </label>
      <a href="#" style="color:#2563eb;text-decoration:none">Forgot password?</a>
    </div>
    <button type="submit" style="width:100%;padding:0.625rem;background:#2563eb;color:#fff;border:none;border-radius:6px;font-size:0.875rem;font-weight:600;cursor:pointer">
      Sign In
    </button>
  </form>
</div>`,
  },
  {
    id: "search-box",
    title: "Search Box",
    description: "Search input with button",
    category: "forms",
    icon: Search,
    code: `<div style="max-width:500px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="display:flex;border:2px solid #e5e7eb;border-radius:10px;overflow:hidden;transition:border-color 0.2s">
    <input type="text" placeholder="Search anything..." style="flex:1;padding:0.75rem 1rem;border:none;font-size:1rem;outline:none;background:transparent">
    <button type="button" style="padding:0.75rem 1.5rem;background:#2563eb;color:#fff;border:none;font-size:0.875rem;font-weight:500;cursor:pointer">
      Search
    </button>
  </div>
</div>`,
  },
  {
    id: "survey-form",
    title: "Survey / Poll",
    description: "Radio and checkbox options",
    category: "forms",
    icon: ToggleLeft,
    code: `<form style="max-width:500px;margin:0 auto;font-family:system-ui,sans-serif;display:flex;flex-direction:column;gap:1.5rem">
  <h2 style="margin:0;font-size:1.25rem;color:#1f2937">Quick Survey</h2>

  <fieldset style="border:1px solid #e5e7eb;border-radius:8px;padding:1.25rem;margin:0">
    <legend style="font-weight:600;font-size:0.875rem;color:#374151;padding:0 0.5rem">How did you hear about us?</legend>
    <div style="display:flex;flex-direction:column;gap:0.5rem;margin-top:0.75rem">
      <label style="display:flex;align-items:center;gap:0.5rem;font-size:0.875rem;color:#4b5563;cursor:pointer"><input type="radio" name="source"> Social Media</label>
      <label style="display:flex;align-items:center;gap:0.5rem;font-size:0.875rem;color:#4b5563;cursor:pointer"><input type="radio" name="source"> Search Engine</label>
      <label style="display:flex;align-items:center;gap:0.5rem;font-size:0.875rem;color:#4b5563;cursor:pointer"><input type="radio" name="source"> Friend / Referral</label>
      <label style="display:flex;align-items:center;gap:0.5rem;font-size:0.875rem;color:#4b5563;cursor:pointer"><input type="radio" name="source"> Other</label>
    </div>
  </fieldset>

  <fieldset style="border:1px solid #e5e7eb;border-radius:8px;padding:1.25rem;margin:0">
    <legend style="font-weight:600;font-size:0.875rem;color:#374151;padding:0 0.5rem">What interests you? (select all)</legend>
    <div style="display:flex;flex-direction:column;gap:0.5rem;margin-top:0.75rem">
      <label style="display:flex;align-items:center;gap:0.5rem;font-size:0.875rem;color:#4b5563;cursor:pointer"><input type="checkbox"> Design</label>
      <label style="display:flex;align-items:center;gap:0.5rem;font-size:0.875rem;color:#4b5563;cursor:pointer"><input type="checkbox"> Development</label>
      <label style="display:flex;align-items:center;gap:0.5rem;font-size:0.875rem;color:#4b5563;cursor:pointer"><input type="checkbox"> Marketing</label>
    </div>
  </fieldset>

  <button type="submit" style="padding:0.625rem 1.5rem;background:#2563eb;color:#fff;border:none;border-radius:6px;font-size:0.875rem;font-weight:500;cursor:pointer;align-self:flex-start">
    Submit
  </button>
</form>`,
  },
]
