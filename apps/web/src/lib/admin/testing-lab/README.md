# Testing Lab Server Actions

This directory contains comprehensive server actions for the Testing Lab module, organized by feature area for better
maintainability and discoverability.

## 📁 Directory Structure

```
testing-lab/
├── index.ts                           # Main export file
├── testing-lab.actions.ts             # Legacy main actions file
├── requests/
│   └── requests.actions.ts            # Testing request CRUD & management
├── sessions/
│   └── testing-sessions.actions.ts    # Testing session CRUD & management
├── participants/
│   └── participants.actions.ts        # Participant management
├── feedback/
│   ├── general-feedback.actions.ts    # General feedback & user activity
│   └── request-feedback.actions.ts    # Request-specific feedback
├── analytics/
│   └── testing-analytics.actions.ts   # Analytics & statistics
└── attendance/
    └── (future attendance-specific actions)
```

## 🎯 Feature Coverage

### ✅ **Fully Implemented**

#### **Testing Requests** (`requests/`)

- ✅ CRUD operations (create, read, update, delete)
- ✅ Request details & restoration
- ✅ Filtering by project version, creator, status
- ✅ Search functionality

#### **Testing Sessions** (`sessions/`)

- ✅ CRUD operations (create, read, update, delete)
- ✅ Session details & restoration
- ✅ Registration management (register, unregister, get registrations)
- ✅ Waitlist management (add, remove, get waitlist)
- ✅ Filtering by request, location, status, manager
- ✅ Search functionality
- ✅ Statistics & attendance tracking

#### **Participant Management** (`participants/`)

- ✅ Add/remove participants from requests
- ✅ Get request participants
- ✅ Check user participation status

#### **Feedback Management** (`feedback/`)

- ✅ General feedback submission & management
- ✅ Request-specific feedback
- ✅ Feedback quality rating & reporting
- ✅ User activity dashboards

#### **Analytics & Statistics** (`analytics/`)

- ✅ Request analytics & statistics
- ✅ Session analytics & statistics
- ✅ Student & session attendance analytics
- ✅ Project version analytics
- ✅ Creator/manager performance analytics
- ✅ Basic performance reporting

## 🔧 API Endpoint Coverage

### **Complete Coverage (47/47 endpoints)**

| Endpoint Category          | Endpoints | Status      |
| -------------------------- | --------- | ----------- |
| **Testing Requests**       | 14        | ✅ Complete |
| **Testing Sessions**       | 17        | ✅ Complete |
| **Registration/Waitlist**  | 6         | ✅ Complete |
| **Participant Management** | 4         | ✅ Complete |
| **Feedback Management**    | 3         | ✅ Complete |
| **Analytics/Statistics**   | 3         | ✅ Complete |

## 📖 Usage Examples

### Import from Main Index

```typescript
import { getTestingRequestsData, createTestingSession, addParticipantToRequest, submitTestingFeedback, getTestingRequestAnalytics } from '@/lib/testing-lab';
```

### Import from Specific Feature

```typescript
import { getTestingRequestsData } from '@/lib/testing-lab/requests/requests.actions';
import { createTestingSession } from '@/lib/testing-lab/sessions/testing-sessions.actions';
```

## 🛠️ Technical Features

- **Type Safety**: Full TypeScript support with generated SDK types
- **Authentication**: Integrated with `configureAuthenticatedClient()`
- **Caching**: Appropriate cache invalidation with `revalidateTag()`
- **Error Handling**: Comprehensive error handling with meaningful messages
- **Performance**: Optimized API calls with proper headers

## 🚀 Migration Guide

### From Legacy Actions

The legacy `testing-lab.actions.ts` file is maintained for backward compatibility. New development should use the
organized structure:

```typescript
// ❌ Old way
import { getTestingRequests } from '@/lib/testing-lab/testing-lab.actions';

// ✅ New way
import { getTestingRequestsData } from '@/lib/testing-lab';
// or
import { getTestingRequestsData } from '@/lib/testing-lab/requests/requests.actions';
```

## 📋 Development Guidelines

1. **Feature Organization**: Keep related actions in the same feature folder
2. **Naming Convention**: Use descriptive function names that indicate the action
3. **Error Handling**: Always include proper try-catch blocks
4. **Type Safety**: Use proper TypeScript types from the generated SDK
5. **Documentation**: Include JSDoc comments for all public functions
6. **Testing**: Ensure all new actions have corresponding tests

## 🔮 Future Enhancements

- **Attendance Module**: Dedicated attendance-specific actions
- **Real-time Updates**: WebSocket integration for live updates
- **Advanced Analytics**: More sophisticated reporting and dashboard functions
- **Caching Strategy**: Enhanced caching with React Query integration
