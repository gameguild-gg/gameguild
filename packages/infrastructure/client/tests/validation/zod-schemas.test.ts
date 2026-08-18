/**
 * Zod Schema Validation Tests
 * 
 * Tests to verify that generated Zod schemas correctly validate data
 */

import { describe, it, expect } from 'vitest';
import { z } from 'zod';

// Mock some generated schemas for testing
const TaxJurisdictionDtoSchema = z.object({
  id: z.string().uuid().optional(),
  code: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  defaultRate: z.number().optional(),
  isActive: z.boolean().optional(),
});

const HealthOutputSchema = z.object({
  status: z.string().nullable().optional(),
  timestamp: z.string().datetime().optional(),
  healthyCount: z.number().int().optional(),
  dependencies: z.array(z.object({
    name: z.string().optional(),
    status: z.string().optional(),
  })).nullable().optional(),
});

const AttestationConveyancePreferenceSchema = z.enum(['none', 'indirect', 'direct', 'enterprise']);

describe('Zod Schema Validation', () => {
  describe('Object Validation', () => {
    it('should validate valid TaxJurisdiction object', () => {
      const validData = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        code: 'US-CA',
        name: 'California',
        country: 'USA',
        defaultRate: 7.25,
        isActive: true,
      };

      const result = TaxJurisdictionDtoSchema.safeParse(validData);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data).toEqual(validData);
      }
    });

    it('should reject invalid UUID', () => {
      const invalidData = {
        id: 'not-a-uuid',
        code: 'US-CA',
      };

      const result = TaxJurisdictionDtoSchema.safeParse(invalidData);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toEqual(['id']);
      }
    });

    it('should allow null values for nullable fields', () => {
      const dataWithNulls = {
        code: null,
        name: null,
        country: null,
      };

      const result = TaxJurisdictionDtoSchema.safeParse(dataWithNulls);
      expect(result.success).toBe(true);
    });

    it('should allow missing optional fields', () => {
      const minimalData = {};

      const result = TaxJurisdictionDtoSchema.safeParse(minimalData);
      expect(result.success).toBe(true);
    });
  });

  describe('Datetime Validation', () => {
    it('should validate valid datetime strings', () => {
      const validData = {
        status: 'healthy',
        timestamp: '2024-01-15T10:30:00Z',
        healthyCount: 5,
      };

      const result = HealthOutputSchema.safeParse(validData);
      expect(result.success).toBe(true);
    });

    it('should reject invalid datetime strings', () => {
      const invalidData = {
        timestamp: 'not-a-datetime',
      };

      const result = HealthOutputSchema.safeParse(invalidData);
      expect(result.success).toBe(false);
    });
  });

  describe('Array Validation', () => {
    it('should validate arrays with correct item types', () => {
      const validData = {
        dependencies: [
          { name: 'Database', status: 'healthy' },
          { name: 'Cache', status: 'healthy' },
        ],
      };

      const result = HealthOutputSchema.safeParse(validData);
      expect(result.success).toBe(true);
    });

    it('should reject arrays with invalid item types', () => {
      const invalidData = {
        dependencies: [
          { name: 123, status: 'healthy' }, // name should be string
        ],
      };

      const result = HealthOutputSchema.safeParse(invalidData);
      expect(result.success).toBe(false);
    });

    it('should allow null for nullable array fields', () => {
      const dataWithNullArray = {
        dependencies: null,
      };

      const result = HealthOutputSchema.safeParse(dataWithNullArray);
      expect(result.success).toBe(true);
    });
  });

  describe('Enum Validation', () => {
    it('should validate valid enum values', () => {
      const validValues = ['none', 'indirect', 'direct', 'enterprise'];

      validValues.forEach(value => {
        const result = AttestationConveyancePreferenceSchema.safeParse(value);
        expect(result.success).toBe(true);
      });
    });

    it('should reject invalid enum values', () => {
      const invalidValues = ['invalid', 'unknown', 123, null, undefined];

      invalidValues.forEach(value => {
        const result = AttestationConveyancePreferenceSchema.safeParse(value);
        expect(result.success).toBe(false);
      });
    });
  });

  describe('Number Validation', () => {
    it('should validate integer values', () => {
      const validData = {
        healthyCount: 5,
      };

      const result = HealthOutputSchema.safeParse(validData);
      expect(result.success).toBe(true);
    });

    it('should reject non-integer values for integer fields', () => {
      const invalidData = {
        healthyCount: 5.5,
      };

      const result = HealthOutputSchema.safeParse(invalidData);
      expect(result.success).toBe(false);
    });
  });

  describe('Custom Refinements', () => {
    it('should support custom validation rules', () => {
      const CustomSchema = TaxJurisdictionDtoSchema.refine(
        (data) => {
          // Active jurisdictions must have a name
          if (data.isActive && !data.name) {
            return false;
          }
          return true;
        },
        {
          message: 'Active jurisdictions must have a name',
          path: ['name'],
        }
      );

      // Should fail: active but no name
      const invalidData = {
        isActive: true,
        code: 'US-CA',
      };

      const result1 = CustomSchema.safeParse(invalidData);
      expect(result1.success).toBe(false);

      // Should pass: active with name
      const validData = {
        isActive: true,
        name: 'California',
        code: 'US-CA',
      };

      const result2 = CustomSchema.safeParse(validData);
      expect(result2.success).toBe(true);

      // Should pass: inactive without name
      const validInactiveData = {
        isActive: false,
        code: 'US-CA',
      };

      const result3 = CustomSchema.safeParse(validInactiveData);
      expect(result3.success).toBe(true);
    });
  });

  describe('Type Inference', () => {
    it('should correctly infer TypeScript types from schemas', () => {
      type TaxJurisdiction = z.infer<typeof TaxJurisdictionDtoSchema>;
      
      const data: TaxJurisdiction = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        code: 'US-CA',
        name: 'California',
      };

      expect(data).toBeDefined();
    });
  });

  describe('Transformation', () => {
    it('should support data transformation', () => {
      const TransformSchema = TaxJurisdictionDtoSchema.transform((val) => ({
        ...val,
        displayName: val.name || val.code || 'Unknown',
      }));

      const data = {
        code: 'US-CA',
        name: 'California',
      };

      const result = TransformSchema.parse(data);
      expect(result.displayName).toBe('California');
    });

    it('should use fallback in transformation when name is missing', () => {
      const TransformSchema = TaxJurisdictionDtoSchema.transform((val) => ({
        ...val,
        displayName: val.name || val.code || 'Unknown',
      }));

      const data = {
        code: 'US-CA',
      };

      const result = TransformSchema.parse(data);
      expect(result.displayName).toBe('US-CA');
    });
  });

  describe('Partial Schemas', () => {
    it('should support partial validation', () => {
      const PartialSchema = TaxJurisdictionDtoSchema.partial();

      // All fields optional
      const result = PartialSchema.safeParse({});
      expect(result.success).toBe(true);
    });

    it('should support pick and omit', () => {
      const PickedSchema = TaxJurisdictionDtoSchema.pick({ code: true, name: true });

      const validData = {
        code: 'US-CA',
        name: 'California',
      };

      const result = PickedSchema.safeParse(validData);
      expect(result.success).toBe(true);

      // Extra fields should be stripped
      const dataWithExtra = {
        code: 'US-CA',
        name: 'California',
        country: 'USA', // This will be stripped
      };

      const result2 = PickedSchema.safeParse(dataWithExtra);
      expect(result2.success).toBe(true);
      if (result2.success) {
        expect(result2.data).not.toHaveProperty('country');
      }
    });
  });
});
