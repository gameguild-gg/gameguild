/**
 * @game-guild/client - CommerceBillingWebhooks Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommerceBillingWebhooksModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Handle Apple Pay webhook events for transaction notifications
   *
   * Processes Apple Pay webhook notifications for payment completions and transaction status updates.
   */
  async postBillingWebhooksApplePay(): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = '/api/v1/billing/webhooks/apple-pay';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Handle Google Pay webhook events for transaction notifications
   *
   * Processes Google Pay webhook notifications for payment processing, subscription billing, and transaction status updates. Google Pay webhooks provide real-time notifications for payment completions, failures, refunds, and subscription lifecycle events.
   */
  async postBillingWebhooksGooglePay(): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = '/api/v1/billing/webhooks/google-pay';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Handle PayPal IPN (Instant Payment Notification) webhook events
   *
   * Processes PayPal Instant Payment Notification (IPN) webhook events for subscription billing, payment confirmations, and account updates. PayPal IPN provides real-time transaction status updates and subscription lifecycle management for PayPal-based billing integrations.
   */
  async postBillingWebhooksPaypal(): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = '/api/v1/billing/webhooks/paypal';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Handle Stripe webhook events with signature verification
   *
   * Processes Stripe webhook notifications with enhanced security through signature verification. Handles subscription lifecycle events, payment confirmations, invoice updates, and customer changes. Stripe signatures are verified using the webhook signing secret to ensure event authenticity.
   */
  async postBillingWebhooksStripe(): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = '/api/v1/billing/webhooks/stripe';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Retrieve webhook event details by event ID
   *
   * Retrieves detailed information about a specific webhook event for debugging and monitoring purposes. Shows event payload, processing status, timestamps, and any error messages. Useful for troubleshooting webhook processing issues and verifying event delivery.
   */
  async getBillingWebhooksWebhookEvents(eventId: string): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = `/api/v1/billing/webhooks/webhook-events/${eventId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Retry failed webhook event processing
   *
   * Manually retries processing of a previously failed webhook event. Useful for handling temporary failures such as downstream service unavailability, network timeouts, or transient processing errors. The retry operation uses the original event payload and applies current business logic.
   */
  async postBillingWebhooksWebhookEventsRetry(eventId: string): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = `/api/v1/billing/webhooks/webhook-events/${eventId}:retry`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }
}

export function createCommerceBillingWebhooksModule(client: ApiClient): CommerceBillingWebhooksModule {
  return new CommerceBillingWebhooksModule(client);
}
