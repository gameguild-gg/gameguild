import type { Tenant } from '@/components/tenant';

declare module 'next-auth' {
  interface Session {
    user: {
      id: string;
      name: string;
      email: string;
      image?: string;
    };
    currentTenant?: Tenant;
    availableTenants?: Tenant[];
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
    currentTenant?: Tenant;
    availableTenants?: Tenant[];
    accessToken?: string;
  }
}
