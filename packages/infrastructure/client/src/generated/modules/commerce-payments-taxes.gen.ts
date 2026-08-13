/**
 * @game-guild/client - CommercePaymentsTaxes Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsTaxesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postPaymentsTaxCalculate(body: Types.CommercePaymentsCalculateTaxInput): Promise<Result<Types.CommercePaymentsTaxCalculationResult, ApiError>> {
    const url = '/api/v1/payments/tax/calculate';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsCalculateTaxInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsTaxCalculationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTaxesCalculate(body: Types.CommercePaymentsCalculateTaxInput): Promise<Result<Types.CommercePaymentsTaxCalculationResult, ApiError>> {
    const url = '/api/v1/taxes/calculate';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsCalculateTaxInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsTaxCalculationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Validate tax exemption
   *
   * Validates whether a tax exemption certificate or status is valid for a given transaction.
   */
  async postPaymentsTaxValidateVat(
    body: Types.CommercePaymentsValidateTaxExemptionInput,
  ): Promise<Result<Types.CommercePaymentsTaxExemptionValidationResult, ApiError>> {
    const url = '/api/v1/payments/tax/validate-vat';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsValidateTaxExemptionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsTaxExemptionValidationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Validate tax exemption
   *
   * Validates whether a tax exemption certificate or status is valid for a given transaction.
   */
  async postTaxesValidateVat(
    body: Types.CommercePaymentsValidateTaxExemptionInput,
  ): Promise<Result<Types.CommercePaymentsTaxExemptionValidationResult, ApiError>> {
    const url = '/api/v1/taxes/validate-vat';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsValidateTaxExemptionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsTaxExemptionValidationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Validate tax exemption
   *
   * Validates whether a tax exemption certificate or status is valid for a given transaction.
   */
  async postPaymentsTaxValidateExemption(
    body: Types.CommercePaymentsValidateTaxExemptionInput,
  ): Promise<Result<Types.CommercePaymentsTaxExemptionValidationResult, ApiError>> {
    const url = '/api/v1/payments/tax/validate-exemption';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsValidateTaxExemptionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsTaxExemptionValidationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Validate tax exemption
   *
   * Validates whether a tax exemption certificate or status is valid for a given transaction.
   */
  async postTaxesValidateExemption(
    body: Types.CommercePaymentsValidateTaxExemptionInput,
  ): Promise<Result<Types.CommercePaymentsTaxExemptionValidationResult, ApiError>> {
    const url = '/api/v1/taxes/validate-exemption';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsValidateTaxExemptionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsTaxExemptionValidationResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommercePaymentsTaxesModule(client: ApiClient): CommercePaymentsTaxesModule {
  return new CommercePaymentsTaxesModule(client);
}
