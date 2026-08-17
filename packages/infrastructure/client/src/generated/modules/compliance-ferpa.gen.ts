/**
 * @game-guild/client - ComplianceFerpa Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ComplianceFerpaModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postApiComplianceFerpaConsents(
    body: Types.ComplianceFERPAGrantFerpaDisclosureConsentCommand,
  ): Promise<Result<Types.ComplianceFERPAFerpaDisclosureConsent, ApiError>> {
    const url = "/api/compliance/ferpa/consents";

    // Validate request body
    const validatedBody = safeParse(
      Types.ComplianceFERPAGrantFerpaDisclosureConsentCommandSchema,
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
        Types.ComplianceFERPAFerpaDisclosureConsentSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiComplianceFerpaConsentsRevoke(
    consentId: string,
  ): Promise<Result<void, ApiError>> {
    const url = `/api/compliance/ferpa/consents/${consentId}/revoke`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getApiComplianceFerpaDirectoryPolicy(query?: {
    tenantId?: string;
  }): Promise<
    Result<Types.ComplianceFERPAFerpaDirectoryInformationPolicy, ApiError>
  > {
    const url = "/api/compliance/ferpa/directory-policy";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.ComplianceFERPAFerpaDirectoryInformationPolicySchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiComplianceFerpaDirectoryPolicy(
    body: Types.ComplianceFERPAUpsertDirectoryInformationPolicyCommand,
  ): Promise<
    Result<Types.ComplianceFERPAFerpaDirectoryInformationPolicy, ApiError>
  > {
    const url = "/api/compliance/ferpa/directory-policy";

    // Validate request body
    const validatedBody = safeParse(
      Types.ComplianceFERPAUpsertDirectoryInformationPolicyCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.ComplianceFERPAFerpaDirectoryInformationPolicySchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiComplianceFerpaDisclosures(
    body: Types.ComplianceFERPARecordFerpaDisclosureCommand,
  ): Promise<Result<Types.ComplianceFERPAFerpaDisclosureLog, ApiError>> {
    const url = "/api/compliance/ferpa/disclosures";

    // Validate request body
    const validatedBody = safeParse(
      Types.ComplianceFERPARecordFerpaDisclosureCommandSchema,
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
        Types.ComplianceFERPAFerpaDisclosureLogSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiComplianceFerpaInspectionRequests(
    body: Types.ComplianceFERPASubmitFerpaInspectionRequestCommand,
  ): Promise<Result<Types.ComplianceFERPAFerpaInspectionInput, ApiError>> {
    const url = "/api/compliance/ferpa/inspection-requests";

    // Validate request body
    const validatedBody = safeParse(
      Types.ComplianceFERPASubmitFerpaInspectionRequestCommandSchema,
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
        Types.ComplianceFERPAFerpaInspectionInputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiComplianceFerpaInspectionRequestsPending(): Promise<
    Result<Array<Types.ComplianceFERPAFerpaInspectionInput>, ApiError>
  > {
    const url = "/api/compliance/ferpa/inspection-requests/pending";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.ComplianceFERPAFerpaInspectionInput>,
      ApiError
    >;
  }

  /**
   */
  async postApiComplianceFerpaInspectionRequestsComplete(
    requestId: string,
    body: Types.ComplianceFERPACompleteFerpaInspectionRequestBody,
  ): Promise<Result<Types.ComplianceFERPAFerpaInspectionInput, ApiError>> {
    const url = `/api/compliance/ferpa/inspection-requests/${requestId}/complete`;

    // Validate request body
    const validatedBody = safeParse(
      Types.ComplianceFERPACompleteFerpaInspectionRequestBodySchema,
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
        Types.ComplianceFERPAFerpaInspectionInputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiComplianceFerpaRecords(
    body: Types.ComplianceFERPARegisterEducationRecordCommand,
  ): Promise<Result<Types.ComplianceFERPAFerpaEducationRecord, ApiError>> {
    const url = "/api/compliance/ferpa/records";

    // Validate request body
    const validatedBody = safeParse(
      Types.ComplianceFERPARegisterEducationRecordCommandSchema,
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
        Types.ComplianceFERPAFerpaEducationRecordSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiComplianceFerpaStudentsConsents(
    studentUserId: string,
  ): Promise<
    Result<Array<Types.ComplianceFERPAFerpaDisclosureConsent>, ApiError>
  > {
    const url = `/api/compliance/ferpa/students/${studentUserId}/consents`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.ComplianceFERPAFerpaDisclosureConsent>,
      ApiError
    >;
  }

  /**
   */
  async getApiComplianceFerpaStudentsDirectoryInformation(
    studentUserId: string,
  ): Promise<
    Result<Array<Types.ComplianceFERPAFerpaEducationRecord>, ApiError>
  > {
    const url = `/api/compliance/ferpa/students/${studentUserId}/directory-information`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.ComplianceFERPAFerpaEducationRecord>,
      ApiError
    >;
  }

  /**
   */
  async getApiComplianceFerpaStudentsDisclosures(
    studentUserId: string,
  ): Promise<Result<Array<Types.ComplianceFERPAFerpaDisclosureLog>, ApiError>> {
    const url = `/api/compliance/ferpa/students/${studentUserId}/disclosures`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.ComplianceFERPAFerpaDisclosureLog>,
      ApiError
    >;
  }

  /**
   */
  async getApiComplianceFerpaStudentsRecords(
    studentUserId: string,
  ): Promise<
    Result<Array<Types.ComplianceFERPAFerpaEducationRecord>, ApiError>
  > {
    const url = `/api/compliance/ferpa/students/${studentUserId}/records`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.ComplianceFERPAFerpaEducationRecord>,
      ApiError
    >;
  }
}

export function createComplianceFerpaModule(
  client: ApiClient,
): ComplianceFerpaModule {
  return new ComplianceFerpaModule(client);
}
