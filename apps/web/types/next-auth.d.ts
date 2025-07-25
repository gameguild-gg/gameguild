import type { TenantResponse } from '@/lib/tenants';

declare module 'next-auth' {
  interface Session {
    user: {
      id: string;
      name: string;
      email: string;
      image?: string;
    };
    currentTenant?: TenantResponse;
    availableTenants?: TenantResponse[];
    accessToken?: string;
  }

  interface User {
    id: string;
    name: string;
    email: string;
    image?: string;
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    userId?: string;
    currentTenant?: TenantResponse;
    availableTenants?: TenantResponse[];
    accessToken?: string;
  }
}
