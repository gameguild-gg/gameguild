/**
 * Application constants shared across projects
 */

export const APP_CONFIG = {
  name: 'Matheus Martins',
  description: 'Personal website and related projects',
  url: 'https://matheusmartins.com',
  author: {
    name: 'Matheus Martins',
    email: 'contact@matheusmartins.com',
    url: 'https://matheusmartins.com',
  },
} as const;

export const API_CONFIG = {
  baseUrl: process.env.NODE_ENV === 'production' ? 'https://api.matheusmartins.com' : 'http://localhost:3001',
  timeout: 10000,
  retries: 3,
} as const;

export const STORAGE_KEYS = {
  theme: 'theme',
  user: 'user',
  preferences: 'preferences',
  authToken: 'auth_token',
  refreshToken: 'refresh_token',
} as const;

export const ROUTES = {
  home: '/',
  about: '/about',
  contact: '/contact',
  blog: '/blog',
  projects: '/projects',
  console: {
    home: '/console',
    dashboard: '/console/dashboard',
    users: '/console/users',
    settings: '/console/settings',
  },
} as const;

export const HTTP_STATUS = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  UNPROCESSABLE_ENTITY: 422,
  INTERNAL_SERVER_ERROR: 500,
  BAD_GATEWAY: 502,
  SERVICE_UNAVAILABLE: 503,
} as const;

export const PAGINATION = {
  defaultLimit: 20,
  maxLimit: 100,
  defaultPage: 1,
} as const;

export const VALIDATION = {
  password: {
    minLength: 8,
    maxLength: 128,
  },
  name: {
    minLength: 2,
    maxLength: 50,
  },
  email: {
    maxLength: 254,
  },
} as const;
