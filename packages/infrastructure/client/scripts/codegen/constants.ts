/**
 * Code Generation Constants
 */

export const HTTP_METHODS = ['get', 'post', 'put', 'delete', 'patch', 'options', 'head'] as const;

export const SUCCESS_STATUS_PREFIX = '2';

export const CONTENT_TYPES = {
  JSON: 'application/json',
  FORM_DATA: 'multipart/form-data',
  FORM_URLENCODED: 'application/x-www-form-urlencoded',
} as const;

export const PARAMETER_LOCATIONS = {
  PATH: 'path',
  QUERY: 'query',
  HEADER: 'header',
  COOKIE: 'cookie',
} as const;

export const ERROR_STATUS_CODES = {
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  UNPROCESSABLE: 422,
  TOO_MANY_REQUESTS: 429,
  INTERNAL_SERVER_ERROR: 500,
  BAD_GATEWAY: 502,
  SERVICE_UNAVAILABLE: 503,
  GATEWAY_TIMEOUT: 504,
} as const;

export const ASP_NET_PATTERNS = {
  PROBLEM_DETAILS_SCHEMAS: ['ProblemDetails', 'HttpValidationProblemDetails', 'ValidationProblemDetails'],
  CONTROLLER_SUFFIX: 'Controller',
} as const;
