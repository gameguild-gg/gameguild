/**
 * @game-guild/client - CommercePayments Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Retrieve all payment transactions with optional filtering
   *
   * Retrieves a paginated list of all payment transactions with support for filtering by tenant, status, and date range. This is the primary endpoint for payment administration and reporting.
   */
  async getPayments(query?: {
    tenantId?: string;
    status?: string;
    startDate?: string;
    endDate?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Result<Array<Types.CommercePaymentsPaymentResult>, ApiError>> {
    const url = "/api/v1/payments";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.CommercePaymentsPaymentResult>,
      ApiError
    >;
  }

  /**
   * Process a new payment transaction
   *
   * Initiates a new payment transaction for a subscription. This endpoint handles the complete payment processing workflow including payment method validation, amount verification, and transaction execution. Returns the payment result immediately with a transaction ID that can be used to track payment status.
   */
  async postPayments(
    body: Types.CommercePaymentsPaymentsControllerProcessPaymentInput,
  ): Promise<Result<Types.CommercePaymentsPaymentResult, ApiError>> {
    const url = "/api/v1/payments";

    // Validate request body
    const validatedBody = safeParse(
      Types.CommercePaymentsPaymentsControllerProcessPaymentInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.CommercePaymentsPaymentResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Retrieve a specific payment by its unique identifier
   *
   * Retrieves detailed information about a specific payment transaction, including its current status, amount, payment method, and processing details. Use this endpoint to track payment progress and verify transaction completion.
   */
  async getPaymentById(
    paymentId: string,
  ): Promise<Result<Types.CommercePaymentsPaymentResult, ApiError>> {
    const url = `/api/v1/payments/${paymentId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.CommercePaymentsPaymentResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Cancel a payment transaction
   *
   * Cancels a payment transaction that is in progress or pending. Custom action per Google API guidelines. Once canceled, a payment cannot be processed and may require a new payment attempt.
   */
  async postPaymentsCancel(
    paymentId: string,
    body: Types.CommercePaymentsPaymentsControllerCancelPaymentInput,
  ): Promise<
    Result<Types.CommercePaymentsPaymentCancellationResult, ApiError>
  > {
    const url = `/api/v1/payments/${paymentId}:cancel`;

    // Validate request body
    const validatedBody = safeParse(
      Types.CommercePaymentsPaymentsControllerCancelPaymentInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.CommercePaymentsPaymentCancellationResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Process a refund for a completed payment
   *
   * Processes a full or partial refund for a completed payment. Custom action per Google API guidelines. Refunds are processed back to the original payment method.
   */
  async postPaymentsRefund(
    paymentId: string,
    body: Types.CommercePaymentsPaymentsControllerRefundInput,
  ): Promise<Result<Types.CommercePaymentsProcessRefundResult, ApiError>> {
    const url = `/api/v1/payments/${paymentId}:refund`;

    // Validate request body
    const validatedBody = safeParse(
      Types.CommercePaymentsPaymentsControllerRefundInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.CommercePaymentsProcessRefundResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Retry a failed payment transaction
   *
   * Retries a failed payment using the original payment method. Custom action per Google API guidelines. Creates a new transaction attempt while maintaining the link to the original payment record.
   */
  async postPaymentsRetry(
    paymentId: string,
  ): Promise<Result<Types.CommercePaymentsPaymentRetryResult, ApiError>> {
    const url = `/api/v1/payments/${paymentId}:retry`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.CommercePaymentsPaymentRetryResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Create a Stripe SetupIntent for subscription checkout
   *
   * Creates or reuses a Stripe customer for the subscription and returns a SetupIntent client secret for PaymentElement-based card collection.
   */
  async postPaymentsSetupIntents(
    body: Types.CommercePaymentsPaymentsControllerCreateSetupIntentInput,
  ): Promise<
    Result<
      Types.CommercePaymentsPaymentsControllerCreateSetupIntentOutput,
      ApiError
    >
  > {
    const url = "/api/v1/payments/setup-intents";

    // Validate request body
    const validatedBody = safeParse(
      Types.CommercePaymentsPaymentsControllerCreateSetupIntentInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.CommercePaymentsPaymentsControllerCreateSetupIntentOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Complete subscription checkout after setup confirmation
   *
   * Sets the confirmed Stripe payment method as the customer's default and processes the first subscription charge.
   */
  async postPaymentsSubscriptionCheckoutsComplete(
    body: Types.CommercePaymentsPaymentsControllerCompleteSubscriptionCheckoutInput,
  ): Promise<Result<Types.CommercePaymentsPaymentResult, ApiError>> {
    const url = "/api/v1/payments/subscription-checkouts:complete";

    // Validate request body
    const validatedBody = safeParse(
      Types.CommercePaymentsPaymentsControllerCompleteSubscriptionCheckoutInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.CommercePaymentsPaymentResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommercePaymentsModule(
  client: ApiClient,
): CommercePaymentsModule {
  return new CommercePaymentsModule(client);
}
