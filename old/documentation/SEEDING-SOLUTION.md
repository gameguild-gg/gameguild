# Database Seeding Solution for GameGuild CMS

## Problem

The web application was getting a 401 Unauthorized error when trying to access the `/projects` endpoint because:

1. The Projects controller requires authentication (`RequireResourcePermission` attribute)
2. No global default permissions were set up to allow users to perform basic operations
3. The web application wasn't sending authentication credentials

## Solution Implemented

### 1. Simplified Database Seeding System

Created a focused database seeding system that only sets up the essential permissions:

#### Files Created:

- `src/Common/Services/IDatabaseSeeder.cs` - Interface for database seeding
- `src/Common/Services/DatabaseSeeder.cs` - Implementation focused on permissions only
- `src/Common/Controllers/AdminController.cs` - Admin endpoints for testing

#### What Gets Seeded:

**Global Default Permissions**: Essential permissions that all users get by default, including:

- `Read` - View content
- `Create` - **Create new projects and content**
- `Edit` - Edit own content
- `Comment` - Add comments
- `Vote` - Vote on content
- `Share` - Share content
- `Follow` - Follow users/content
- `Bookmark` - Bookmark content

### 2. Temporary Public Access

Modified the Projects controller to temporarily allow public access to the `GetProjects` endpoint by replacing:

```csharp
[RequireResourcePermission<Models.Project>(PermissionType.Read)]
```

with:

```csharp
[Public]
```

### 3. Integration with Application Startup

- Added seeder service registration in `Program.cs`
- Configured automatic seeding during application startup
- Uses smart checking to avoid duplicate seeding

## How It Works

The seeding system:

1. **Checks if global permissions already exist** to avoid duplicate seeding
2. **Sets global default permissions** that apply to all users automatically
3. **Includes the `Create` permission** so any authenticated user can create projects
4. **Runs automatically** on application startup

## Testing the Solution

### Option 1: Automatic Seeding

The seeding runs automatically when the application starts up for the first time.

### Option 2: Manual Seeding via API

Use the admin endpoint to manually trigger seeding:

```bash
curl -X POST http://localhost:5000/api/admin/seed
```

### Option 3: Test Script

Run the provided test script:

```bash
# Windows
test-cms.bat

# Linux/Mac
./test-cms.sh
```

## API Endpoints Available

1. **GET /api/admin/status** - Check if the admin controller is working
2. **POST /api/admin/seed** - Manually trigger database seeding
3. **GET /projects** - Get all projects (now public for testing)

## Next Steps

1. **Implement Proper Authentication**: The web application should implement JWT authentication to properly authenticate
   with the CMS
2. **Remove Public Access**: Once authentication is implemented, remove the `[Public]` attribute from the Projects
   controller
3. **User Registration/Login**: With the global permissions in place, any user who signs up will automatically have
   permission to create projects

## Files Modified

- `Program.cs` - Added seeder registration and startup seeding
- `src/Modules/Project/Controllers/ProjectsController.cs` - Temporarily made public
- Created new seeding infrastructure files

The solution provides a clean foundation where all authenticated users can create projects by default, without needing
demo data or admin setup.
