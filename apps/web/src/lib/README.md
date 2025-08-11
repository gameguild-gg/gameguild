# Game Guild - Lib Folder Organization

This directory has been reorganized into feature-based modules for better maintainability and developer experience.

## 📁 Folder Structure

### 🧑‍💼 User Management (`/user-management`)

- **users/** - Core user CRUD operations and management
- **auth/** - Authentication and authorization
- **profiles/** - User profile management and customization
- **achievements/** - User achievement tracking (duplicated, see activity-tracking)

### 📚 Content Management (`/content-management`)

- **courses/** - Course creation, management, and enrollment
- **projects/** - Project management and collaboration
- **programs/** - Program lifecycle and SDK integration (100% coverage achieved)
- **content-interaction/** - Content engagement and interaction tracking

### 📊 Activity Tracking (`/activity-tracking`)

- **achievements/** - Achievement system and progress tracking
- **activity-grades/** - Grade management and scoring
- **activity-grading/** - Grading workflows and processes

### 💰 Commerce (`/commerce`)

- **payments/** - Payment processing and transactions
- **subscriptions/** - Subscription management and billing
- **products/** - Product catalog and management
- **payment-commerce/** - E-commerce specific payment handling

### 💬 Communication (`/communication`)

- **requests/** - Request management and workflow
- **notifications/** - Notification system and delivery
- **posts/** - Post creation and management
- **feed/** - Activity feeds and social features

### ⚙️ Admin (`/admin`)

- **tenants/** - Multi-tenant management
- **testing-lab/** - Testing environment and tools
- **testing-feedback/** - Feedback collection and analysis

### 🔧 Core (`/core`)

- **api/** - Core API utilities and configuration
- **utils/** - Shared utility functions
- **health/** - System health monitoring
- **dashboard/** - Dashboard data and analytics
- **credentials/** - Credential management
- **integrations/** - External service integrations

### 🗂️ Legacy (`/legacy`)

**Deprecated patterns moved here for review:**

- `users.context.tsx` - Replace with server actions from user-management
- `requests.context.tsx` - Replace with server actions from communication
- `auth.hooks.ts` - Replace with server actions or modern patterns

## 🎯 Benefits

1. **Feature-based Organization**: Related functionality is grouped together
2. **Clear Separation**: Each module has a specific responsibility
3. **Easy Imports**: Use module-level imports (`from '@/lib/user-management'`)
4. **Legacy Isolation**: Old patterns are clearly separated for migration
5. **Scalability**: Easy to add new features within appropriate modules

## 🚀 Usage

```typescript
// Instead of scattered imports:
import { createUser } from '@/lib/users/users.actions';
import { getUserProfile } from '@/lib/profiles/profiles.actions';

// Use feature-based imports:
import { createUser, getUserProfile } from '@/lib/user-management';
```

## 🔄 Migration Notes

- All server actions maintain 100% SDK coverage (297/297 functions)
- Legacy React patterns (contexts, hooks) moved to `/legacy` folder
- Index files provide clean module exports
- No breaking changes to existing functionality

## 📈 Next Steps

1. Update import statements throughout the app to use new module structure
2. Review legacy files and migrate to modern server action patterns
3. Consider further subdividing large modules if they grow significantly
4. Add feature-specific documentation within each module
