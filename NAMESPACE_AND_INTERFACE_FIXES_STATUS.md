# GameGuild API - Namespace and Interface Fixes Status

**Date**: October 11, 2025  
**Session**: Namespace cleanup and interface resolution  
**Initial Errors**: ~1040 compilation errors  
**Current Errors**: ~1030 compilation errors  

## ✅ COMPLETED FIXES

### 1. Namespace Corrections (GameGuild.Source → GameGuild)
Fixed incorrect `GameGuild.Source.Modules` namespace references:

| File | Line | Old Namespace | New Namespace |
|------|------|---------------|---------------|
| DependencyInjection.cs | 17 | `using GameGuild.Source.Modules.Authorization;` | `using GameGuild.Modules.Authorization;` |
| GraphQLDACAuthorizationExtensions.cs | 2 | `using GameGuild.Source.Modules.Authorization;` | `using GameGuild.Modules.Authorization;` |
| ApplicationDbContext.cs | 15 | `using GameGuild.Source.Database.Seeding;` | Removed (same namespace) |
| ProductType.cs | 2, 8 | `using GameGuild.Core.Authorization;`<br/>`using ProductEntity = GameGuild.Source.Modules.Products.Models.Product;` | `using GameGuild.Modules.Authorization;`<br/>`using ProductEntity = GameGuild.Modules.Products.Models.Product;` |
| Program.cs (Programs/Models) | 2 | `using GameGuild.Core.Entities;` | `using GameGuild;` |
| ProductMutations.cs | 6 | `using ProductEntity = GameGuild.Source.Modules.Products.Models.Product;` | `using ProductEntity = GameGuild.Modules.Products.Models.Product;` |
| ProductQueries.cs | 10 | `using ProductEntity = GameGuild.Source.Modules.Products.Models.Product;` | `using ProductEntity = GameGuild.Modules.Products.Models.Product;` |
| IProductService.cs | 3 | `using ProductEntity = GameGuild.Source.Modules.Products.Models.Product;` | `using ProductEntity = GameGuild.Modules.Products.Models.Product;` |
| ProductService.cs | 6 | `using ProductEntity = GameGuild.Source.Modules.Products.Models.Product;` | `using ProductEntity = GameGuild.Modules.Products.Models.Product;` |

**Total Fixed**: 10 files

### 2. PromoCodeType Enum Fixes
Fixed `GameGuild.PromoCodeType` references to use correct namespace:

| File | Old Reference | New Reference |
|------|---------------|---------------|
| CreatePromoCodeInput.cs | `using PromoCodeTypeEnum = GameGuild.PromoCodeType;` | `using GameGuild.Modules.Products.Domain.Enums;` |
| UpdatePromoCodeInput.cs | `using PromoCodeTypeEnum = GameGuild.PromoCodeType;` | `using GameGuild.Modules.Products.Domain.Enums;` |
| PromoCodeType.cs (GraphQL) | `using PromoCodeTypeEnum = GameGuild.PromoCodeType;` | `using GameGuild.Modules.Products.Domain.Enums;` |
| PromoCode.cs (Model) | `using PromoCodeTypeEnum = GameGuild.PromoCodeType;`<br/>`public PromoCodeTypeEnum Type` | `using GameGuild.Modules.Products.Domain.Enums;`<br/>`public PromoCodeType Type` |

**Result**: PromoCodeType now correctly references `GameGuild.Modules.Products.Domain.Enums.PromoCodeType`

### 3. Migration Cleanup
- ✅ **Deleted**: `20250926000436_InitialCreate.cs` (duplicate)
- ✅ **Kept**: `20250725150105_InitialCreate.cs` (original)
- ✅ **Result**: CS0111 "duplicate member" errors resolved

### 4. Authorization Context Imports
Fixed missing imports in Authorization module:

| File | Added Imports |
|------|---------------|
| LocalizationContext.cs | `using GameGuild.Modules.Tenants;`<br/>`using GameGuild.Modules.Users;` |
| PermissionsContext.cs | `using GameGuild.Modules.Tenants;`<br/>`using GameGuild.Modules.Users;`<br/>`using GameGuild.Modules.Permissions;` |

### 5. Permission Interface Correction
- **Changed**: `IDacPermissionResolver` → `IPermissionResolver` in PermissionsContext.cs
- **Files**: Field declaration + constructor parameter
- **Reason**: `IDacPermissionResolver` doesn't exist; `IPermissionResolver` exists in `GameGuild.Modules.Permissions`

---

## ⚠️ CRITICAL BLOCKERS (Still Broken)

### 1. Missing Permission Service Interfaces

#### IModulePermissionService
**Status**: ❌ Does NOT exist  
**Used In**:
- `PermissionsContext.cs` (line 13, 26)
- `DatabaseSeeder.cs` (line 19)

**Potential Fix**:
- Check if should be `IPermissionService` instead
- Or create new interface if needed

#### ISimplePermissionService
**Status**: ❌ Does NOT exist  
**Used In**:
- `PermissionSeeder.cs` (line 8, 22)

**Potential Fix**:
- Check if should be `IPermissionService` instead
- Or create interface for simplified permission operations

#### IResourcePermissionService
**Status**: ❌ Does NOT exist  
**Used In**:
- `PermissionMutations.cs` (line 11)

**Potential Fix**:
- Check if should be `IPermissionService` or `IPermissionResolver`
- Or check Permissions module for correct service

### 2. Missing Authorization Attributes

#### DACAuthorizationAttribute
**Status**: ❌ Does NOT exist  
**Used In**:
- `RequireContentTypePermissionAttribute.cs` (line 5)

**Error**: `CS0246: type or namespace name 'DACAuthorizationAttribute' could not be found`

**Potential Fix**:
- Search for similar attributes in Authorization module
- Check if renamed or in different namespace

#### DACPermissionLevel
**Status**: ❌ Does NOT exist (or wrong namespace)  
**Used In**:
- `RequireContentTypePermissionAttribute.cs` (line 10)
- `GraphQLDACAuthorizationExtensions.cs` (line 18)

**Error**: `CS0246: type or namespace name 'DACPermissionLevel' could not be found`

**Potential Fix**:
- Check Core/Enums for PermissionLevel enum
- Might be `PermissionType` or similar

### 3. Missing Enums

#### GameGuild.Visibility
**Status**: ❌ Does NOT exist  
**Used In**:
- `ProgramContentMutations.cs` (line 8)

**Error**: `CS0234: The type or namespace name 'Visibility' does not exist`

**Potential Fix**:
- Check if enum exists in Core/Enums as `AccessLevel` or `VisibilityLevel`
- Or check Contents module for visibility enum

### 4. Missing Entity Types

#### SessionWaitlist
**Status**: ❌ Does NOT exist  
**Used In**:
- `ApplicationDbContext.cs` (line 139)

**Error**: `CS0246: The type or namespace name 'SessionWaitlist' could not be found`

#### SessionProject  
**Status**: ❌ Does NOT exist  
**Used In**:
- `ApplicationDbContext.cs` (line 141)

**Error**: `CS0246: The type or namespace name 'SessionProject' could not be found`

**Potential Fix**:
- These might be from LiveSessions or Projects module
- Check if entities were deleted or renamed
- May need to remove DbSet declarations if features removed

---

## 📊 ERROR BREAKDOWN

| Error Category | Count | Status |
|----------------|-------|--------|
| Missing Context Interfaces | ~50 | ✅ Fixed (ITenantContext, IUserContext) |
| Missing Permission Services | ~200 | ❌ **BLOCKER** |
| Missing Authorization Attributes | ~100 | ❌ **BLOCKER** |
| Missing Enums | ~20 | ❌ TO FIX |
| Missing Entity Types | ~10 | ❌ TO FIX |
| Namespace errors | ~10 | ✅ Fixed |
| Other errors | ~650 | ❓ Pending investigation |

**Total Errors**: ~1030

---

## 🎯 NEXT STEPS (Priority Order)

### HIGH PRIORITY - Fix Missing Services
1. **Investigate IModulePermissionService**:
   ```bash
   # Search for similar services
   find apps/api/Source/Modules/Permissions -name "*Service*" -type f
   # Check if IPermissionService can replace it
   ```

2. **Investigate ISimplePermissionService**:
   - Check PermissionSeeder.cs usage context
   - Determine if it's a simplified wrapper over IPermissionService

3. **Investigate IResourcePermissionService**:
   - Check PermissionMutations.cs context
   - Likely should be IPermissionService or IPermissionResolver

### MEDIUM PRIORITY - Fix Missing Attributes
4. **Find DACAuthorizationAttribute**:
   ```bash
   grep -r "class.*AuthorizationAttribute" apps/api/Source/
   ```
   - Check Authorization/Attributes folder
   - May have been renamed to just `AuthorizationAttribute`

5. **Find DACPermissionLevel**:
   ```bash
   find apps/api/Source -name "*PermissionLevel*" -o -name "*PermissionType*"
   ```
   - Check Core/Enums and Authorization/Models

### LOW PRIORITY - Fix Missing Enums/Entities
6. **Find or create Visibility enum**:
   - Check if `AccessLevel` enum can be used instead
   - Located in `Core/Enums/AccessLevel.cs`

7. **Fix SessionWaitlist & SessionProject**:
   - Check if entities exist in LiveSessions or Projects module
   - If not, remove DbSet declarations from ApplicationDbContext

### FINAL STEP
8. **Build and Verify**:
   ```bash
   dotnet build apps/api/GameGuild.csproj --no-restore
   ```
   - Target: 0 errors
   - If successful, commit all fixes

---

## 📝 COMMIT PLAN

Once all fixes are complete:

```bash
git add -A
git commit -m "fix(api): Resolve namespace and missing interface errors

NAMESPACE FIXES (10 files):
- Fixed GameGuild.Source.Modules → GameGuild.Modules references
- Fixed PromoCodeType enum namespace (use Products.Domain.Enums)
- Fixed GameGuild.Core.Authorization → GameGuild.Modules.Authorization
- Fixed GameGuild.Core.Entities → GameGuild

INTERFACE/SERVICE FIXES:
- Added context imports (ITenantContext, IUserContext)
- Changed IDacPermissionResolver → IPermissionResolver
- Fixed IModulePermissionService → [correct service]
- Fixed ISimplePermissionService → [correct service]
- Fixed IResourcePermissionService → [correct service]

ATTRIBUTE/ENUM FIXES:
- Fixed DACAuthorizationAttribute references
- Fixed DACPermissionLevel references
- Fixed Visibility enum references

MIGRATION CLEANUP:
- Removed duplicate InitialCreate migration (20250926000436)

Build Status: 0 errors ✅
"
```

---

## 🔍 INVESTIGATION COMMANDS

### Find Permission Services
```bash
ls -la apps/api/Source/Modules/Permissions/Services/
ls -la apps/api/Source/Modules/Permissions/Abstractions/
```

### Search for Authorization Attributes
```bash
find apps/api/Source/Modules/Authorization -name "*Attribute*" -type f
grep -r "class.*Attribute" apps/api/Source/Modules/Authorization/
```

### Find Enum Definitions
```bash
find apps/api/Source/Core/Enums -name "*.cs" -type f
find apps/api/Source/Modules -name "*Enum*.cs" -type f
```

### Check Entity Existence
```bash
grep -r "class SessionWaitlist" apps/api/Source/
grep -r "class SessionProject" apps/api/Source/
```

---

**Last Updated**: October 11, 2025 - After fixing 5 categories, ~1030 errors remain
