import {
  AuthServiceUnavailableError,
  CredentialsSignInError,
  parseBackendAuthResponse,
  type CredentialsProviderConfig,
} from "@game-guild/client";

type CredentialsAuthorize = CredentialsProviderConfig["authorize"];

const MAGIC_LINK_CONSUME_PATH = "/v1/auth/magic-link:consume";

/**
 * Extend the existing credentials provider with the one-time magic-link flow
 * while keeping email/password authentication on its existing implementation.
 */
export function createMagicLinkCredentialsAuthorize(
  passwordAuthorize: CredentialsAuthorize,
): CredentialsAuthorize {
  return async (credentials, request) => {
    const rawToken = credentials.magicLinkToken;
    if (rawToken === undefined) return passwordAuthorize(credentials, request);

    if (typeof rawToken !== "string" || rawToken.trim().length === 0) {
      throw new CredentialsSignInError(
        "This sign-in link is invalid or has expired.",
      );
    }

    const apiUrl = credentials.__apiUrl;
    if (typeof apiUrl !== "string" || apiUrl.trim().length === 0) {
      throw new AuthServiceUnavailableError(
        "The authentication service is not configured.",
      );
    }

    const tenantId = credentials.tenantId;
    const body: Record<string, string> = { token: rawToken.trim() };
    if (typeof tenantId === "string" && tenantId.trim().length > 0) {
      body.tenantId = tenantId.trim();
    }

    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };
    const userAgent = request?.headers.get("user-agent");
    if (userAgent) headers["User-Agent"] = userAgent;

    let response: Response;
    try {
      response = await fetch(
        `${apiUrl.replace(/\/+$/, "")}${MAGIC_LINK_CONSUME_PATH}`,
        {
          method: "POST",
          headers,
          body: JSON.stringify(body),
          cache: "no-store",
          credentials: "omit",
        },
      );
    } catch (error) {
      throw new AuthServiceUnavailableError(
        "Authentication service is unreachable. Please try again.",
        error instanceof Error ? error : undefined,
      );
    }

    if (!response.ok) {
      if (response.status >= 500) throw new AuthServiceUnavailableError();
      throw new CredentialsSignInError(
        "This sign-in link is invalid or has expired.",
      );
    }

    let data: unknown;
    try {
      data = await response.json();
    } catch {
      throw new AuthServiceUnavailableError(
        "The authentication service returned an invalid response.",
      );
    }

    if (typeof data !== "object" || data === null || Array.isArray(data)) {
      throw new AuthServiceUnavailableError(
        "The authentication service returned an invalid response.",
      );
    }

    const result = parseBackendAuthResponse(data as Record<string, unknown>);
    if (
      !result.user.id ||
      !result.tokens.accessToken ||
      !result.tokens.refreshToken
    ) {
      throw new AuthServiceUnavailableError(
        "The authentication service returned an incomplete sign-in response.",
      );
    }

    return result;
  };
}
