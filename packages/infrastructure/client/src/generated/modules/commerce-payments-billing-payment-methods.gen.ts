/**
 * @game-guild/client - CommercePaymentsBillingPaymentMethods Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsBillingPaymentMethodsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * List registered payment methods (own methods; system admins see the whole tenant).
   *
   * Lists the live payment methods registered for the tenant. Non-admin actors see their own methods; system admins see all methods in the requested tenant.
   */
  async getBillingPaymentMethods(query?: { tenantId?: string }): Promise<Result<Types.CommercePaymentsPaymentMethodDto[], ApiError>> {
    const url = '/api/v1/billing/payment-methods';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.CommercePaymentsPaymentMethodDto[], ApiError>;
  }

  /**
   * Register a payment method from a tokenized gateway result.
   *
   * Registers a gateway payment token. Opaque Braintree and Adyen tokens require an explicit GatewayProvider. Raw card numbers are rejected; Stripe tokens are verified through the gateway before registration.
   */
  async postBillingPaymentMethods(
    body: Types.CommercePaymentsPaymentMethodsControllerCreatePaymentMethodRequestBody,
  ): Promise<Result<Types.CommercePaymentsPaymentMethodDto, ApiError>> {
    const url = '/api/v1/billing/payment-methods';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsPaymentMethodsControllerCreatePaymentMethodRequestBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsPaymentMethodDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get a single payment method by ID.
   *
   * Returns a single registered payment method. Only the owner or a system administrator can read it.
   */
  async getPaymentMethodById(paymentMethodId: string): Promise<Result<Types.CommercePaymentsPaymentMethodDto, ApiError>> {
    const url = `/api/v1/billing/payment-methods/${paymentMethodId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsPaymentMethodDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Update display-only fields of a payment method.
   *
   * Updates display-only fields (nickname, billing address). Card details come from the gateway and cannot be edited here.
   */
  async putBillingPaymentMethods(
    paymentMethodId: string,
    body: Types.CommercePaymentsPaymentMethodsControllerUpdatePaymentMethodRequestBody,
  ): Promise<Result<Types.CommercePaymentsPaymentMethodDto, ApiError>> {
    const url = `/api/v1/billing/payment-methods/${paymentMethodId}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsPaymentMethodsControllerUpdatePaymentMethodRequestBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsPaymentMethodDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Remove a payment method (soft delete, explicit confirmation required).
   *
   * Soft-deletes a payment method after explicit confirmation (body confirm=true). Without confirmation the request fails with 400. Fails closed with 409 Conflict while any active subscription still bills with it; set a new default first to re-point subscriptions.
   */
  async deleteBillingPaymentMethods(
    paymentMethodId: string,
    body: Types.CommercePaymentsPaymentMethodsControllerRemovePaymentMethodRequestBody,
  ): Promise<Result<void, ApiError>> {
    const url = `/api/v1/billing/payment-methods/${paymentMethodId}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsPaymentMethodsControllerRemovePaymentMethodRequestBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Make this payment method the default (custom action per Google API guidelines).
   *
   * Makes this payment method its owner's default inside the tenant. Enforces the single-default invariant and re-points subscriptions billed with the previous default.
   */
  async postBillingPaymentMethodsSetDefault(paymentMethodId: string): Promise<Result<Types.CommercePaymentsPaymentMethodDto, ApiError>> {
    const url = `/api/v1/billing/payment-methods/${paymentMethodId}:set-default`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsPaymentMethodDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Current micro-deposit verification state of a bank-account payment method.
   *
   * Returns the micro-deposit verification state of a saved bank-account payment method (none, pending, verified, expired or locked) with the challenge deadline and remaining attempts. Owner or system administrator only.
   */
  async getBillingPaymentMethodsBankVerification(paymentMethodId: string): Promise<Result<Types.CommercePaymentsBankAccountVerificationStatusDto, ApiError>> {
    const url = `/api/v1/billing/payment-methods/${paymentMethodId}/bank-verification`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsBankAccountVerificationStatusDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Initiate micro-deposit verification: two random deposits ($0.01-$0.99) are
   * generated; only salted hashes are stored.
   *
   * Generates two random micro-deposits ($0.01-$0.99) for a saved bank-account payment method and returns them once (only salted hashes are stored). The owner has 3 business days and at most 3 failed echo attempts. Idempotent while a challenge is open. Subject to the configurable MFA step-up policy.
   */
  async postBillingPaymentMethodsBankVerificationInitiate(
    paymentMethodId: string,
  ): Promise<Result<Types.CommercePaymentsBankAccountVerificationInitiationDto, ApiError>> {
    const url = `/api/v1/billing/payment-methods/${paymentMethodId}/bank-verification/initiate`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsBankAccountVerificationInitiationDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Echo the two observed micro-deposit amounts to verify the bank account.
   *
   * Echoes the two micro-deposit amounts observed on the bank statement. Both must match within the 3-business-day deadline and the 3-attempt cap; success marks the bank account verified and therefore chargeable. Subject to the configurable MFA step-up policy.
   */
  async postBillingPaymentMethodsBankVerificationVerify(
    paymentMethodId: string,
    body: Types.CommercePaymentsPaymentMethodsControllerVerifyBankDepositsRequestBody,
  ): Promise<Result<Types.CommercePaymentsBankAccountVerificationStatusDto, ApiError>> {
    const url = `/api/v1/billing/payment-methods/${paymentMethodId}/bank-verification/verify`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsPaymentMethodsControllerVerifyBankDepositsRequestBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsBankAccountVerificationStatusDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommercePaymentsBillingPaymentMethodsModule(client: ApiClient): CommercePaymentsBillingPaymentMethodsModule {
  return new CommercePaymentsBillingPaymentMethodsModule(client);
}
