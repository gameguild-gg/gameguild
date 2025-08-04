// Game Guild - Organized Lib Modules
// Feature-based organization with clean separation of concerns

// 🧑‍💼 User Management - Authentication, profiles, and user lifecycle
export * from './user-management';

// 📚 Content Management - Courses, projects, programs, and content interaction
export * from './content-management';

// 📊 Activity Tracking - Achievements, grades, and activity monitoring
export * from './activity-tracking';

// 💰 Commerce - Payments, subscriptions, and product management
export * from './commerce';

// 💬 Communication - Requests, notifications, posts, and feeds
export * from './communication';

// ⚙️ Admin - Tenant management, testing lab, and administrative tools
export * from './admin';

// 🔧 Core - Utilities, API, health checks, and shared functionality
export * from './core';

// 🗂️ Legacy - Deprecated patterns (contexts, hooks, old components)
// Note: Legacy files moved to /legacy folder for review and migration
// - users.context.tsx (replace with server actions)
// - requests.context.tsx (replace with server actions)
// - auth.hooks.ts (replace with server actions or new patterns)
