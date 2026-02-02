# Zod Validation Integration - Summary

## Overview

Added comprehensive Zod schema generation to the `@game-guild/api-client` package for runtime validation of API types. The generator now produces both TypeScript interfaces (for compile-time safety) and Zod schemas (for runtime validation).

## Changes Made

### 1. Added Zod Dependency
**File**: `package.json`
- Added `"zod": "^3.23.8"` as a runtime dependency
- Zod schemas are exported in the generated client, so it's needed by consumers

### 2. Created Zod Schema Mapper
**File**: `scripts/codegen/strategies/ZodSchemaMapper.ts` (New)

Implements Strategy Pattern for mapping OpenAPI schemas to Zod schemas:

- **ZodReferenceMapper** - Maps `$ref` to schema references
- **ZodStringMapper** - Maps strings with format validation (email, uuid, url, datetime)
- **ZodNumberMapper** - Maps numbers/integers with min/max constraints
- **ZodBooleanMapper** - Maps boolean types
- **ZodArrayMapper** - Maps arrays with item schemas and constraints
- **ZodObjectMapper** - Maps objects with properties and additionalProperties
- **ZodUnionMapper** - Maps oneOf/anyOf unions
- **ZodSchemaMapperChain** - Chain of Responsibility coordinator

### 3. Enhanced Types Generator
**File**: `scripts/codegen/types.ts` (Modified)

Updated to generate both TypeScript types AND Zod schemas:

- Imports Zod at the top of generated files
- For each schema, generates:
  - TypeScript interface/type
  - Zod schema with `[TypeName]Schema` naming convention
- Handles all schema types:
  - Primitives (string, number, boolean)
  - Arrays with constraints (min/max items)
  - Objects with required/optional fields
  - Enums (z.enum for strings, z.union for mixed types)
  - Unions (oneOf/anyOf)
  - Intersections (allOf using z.merge)
  - Records (additionalProperties)
- Applies OpenAPI constraints:
  - String: minLength, maxLength, pattern, format
  - Number: minimum, maximum
  - Nullable types
  - Optional fields

### 4. Documentation
**File**: `README.md` (Updated)
- Added Zod to features list
- Updated dependencies note
- Added comprehensive "Runtime Validation with Zod" section
- Documented validation patterns and custom rules

**File**: `examples/zod-validation.ts` (New)
- Created 10 practical examples:
  1. Basic validation with `.parse()`
  2. Safe validation with `.safeParse()`
  3. Nested object validation
  4. Partial validation
  5. Transform data during validation
  6. Custom refinements
  7. Type inference from schemas
  8. API response validation
  9. Form data validation
  10. Array validation

## Generated Output Examples

### TypeScript Interface + Zod Schema
```typescript
export interface Commerce_Payments_TaxJurisdictionDto {
  id?: string;
  code?: string | null;
  name?: string | null;
  defaultRate?: number;
  isActive?: boolean;
}

export const Commerce_Payments_TaxJurisdictionDtoSchema = z.object({
  id: z.string().uuid().optional(),
  code: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  defaultRate: z.number().optional(),
  isActive: z.boolean().optional()
});
```

### Enum + Zod Enum Schema
```typescript
export type ObjectsAttestationConveyancePreference = 
  'none' | 'indirect' | 'direct' | 'enterprise';

export const ObjectsAttestationConveyancePreferenceSchema = 
  z.enum(['none', 'indirect', 'direct', 'enterprise']);
```

### Array + Zod Array Schema
```typescript
export interface APIControllersDependencyHealthOutput {
  dependencies?: Array<APIControllersDependencyHealthItem> | null;
  // ... other fields
}

export const APIControllersDependencyHealthOutputSchema = z.object({
  dependencies: z.array(APIControllersDependencyHealthItemSchema).nullable().optional(),
  // ... other fields
});
```

## Usage Examples

### Basic Validation
```typescript
import { 
  Commerce_Payments_TaxJurisdictionDtoSchema 
} from '@game-guild/api-client';

// Validate API response
const response = await fetch('/api/tax-jurisdictions/123');
const data = await response.json();

// Throws if invalid
const validated = Commerce_Payments_TaxJurisdictionDtoSchema.parse(data);
```

### Safe Validation
```typescript
const result = Commerce_Payments_TaxJurisdictionDtoSchema.safeParse(data);

if (result.success) {
  console.log('Valid:', result.data);
} else {
  console.error('Errors:', result.error.errors);
}
```

### Custom Validation Rules
```typescript
const CustomSchema = Commerce_Payments_TaxJurisdictionDtoSchema
  .refine(
    (data) => !data.isActive || !!data.name,
    { message: 'Active jurisdictions must have a name', path: ['name'] }
  );
```

## Benefits

1. **Runtime Type Safety** - Catch invalid data at runtime, not just compile-time
2. **API Response Validation** - Validate external API responses before use
3. **Form Validation** - Use Zod schemas directly in form libraries (React Hook Form, etc.)
4. **Custom Validation** - Extend generated schemas with business rules
5. **Better Error Messages** - Zod provides detailed validation error messages
6. **Type Inference** - Infer TypeScript types from Zod schemas using `z.infer<>`

## Test Results

✅ All 77 tests passing
✅ Successfully generated 46 module files
✅ Types.gen.ts now 6,015 lines (includes both TypeScript and Zod schemas)

## File Statistics

- **Generated Types**: 6,015 lines (was ~3,000 lines without Zod)
- **New Files**: 2 (ZodSchemaMapper.ts, zod-validation.ts)
- **Modified Files**: 3 (package.json, types.ts, README.md)
- **Dependencies Added**: 1 (zod ^3.23.8)

## Backward Compatibility

✅ **Fully backward compatible** - TypeScript interfaces unchanged
✅ Zod schemas are additive - existing code continues to work
✅ Zod is now a required dependency (not optional)

## Next Steps (Optional Enhancements)

1. **Auto-validation in Client** - Optionally validate responses automatically in the generated API client methods
2. **Request Validation** - Validate request payloads before sending
3. **Zod-to-OpenAPI** - Ability to generate OpenAPI from Zod schemas (reverse direction)
4. **Performance** - Consider lazy schema initialization for large APIs
5. **Custom Error Messages** - Allow OpenAPI description to customize Zod error messages
