import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export interface AuthState {
    isAuthenticated: boolean;
    user: {
        id: string;
        name: string;
        email: string;
        avatar?: string;
        roles: string[];
    } | null;
    currentTenant: {
        id: string;
        name: string;
        slug: string;
    } | null;
    availableTenants: Array<{
        id: string;
        name: string;
        slug: string;
    }>;
    permissions: string[];
    sessionExpiry: number | null;
}

const initialState: AuthState = {
    isAuthenticated: false,
    user: null,
    currentTenant: null,
    availableTenants: [],
    permissions: [],
    sessionExpiry: null,
};

export const authSlice = createSlice({
    name: 'auth',
    initialState,
    reducers: {
        setUser: (state, action: PayloadAction<AuthState['user']>) => {
            state.user = action.payload;
            state.isAuthenticated = !!action.payload;
        },
        setCurrentTenant: (state, action: PayloadAction<AuthState['currentTenant']>) => {
            state.currentTenant = action.payload;
        },
        setAvailableTenants: (state, action: PayloadAction<AuthState['availableTenants']>) => {
            state.availableTenants = action.payload;
        },
        setPermissions: (state, action: PayloadAction<string[]>) => {
            state.permissions = action.payload;
        },
        setSessionExpiry: (state, action: PayloadAction<number | null>) => {
            state.sessionExpiry = action.payload;
        },
        updateUserProfile: (state, action: PayloadAction<Partial<NonNullable<AuthState['user']>>>) => {
            if (state.user) {
                state.user = { ...state.user, ...action.payload };
            }
        },
        logout: (state) => {
            state.isAuthenticated = false;
            state.user = null;
            state.currentTenant = null;
            state.permissions = [];
            state.sessionExpiry = null;
            // Keep availableTenants for potential re-login
        },
        hydrate: (state, action: PayloadAction<Partial<AuthState>>) => {
            // Used for SSR hydration from NextAuth session
            return { ...state, ...action.payload };
        },
    },
});

export const {
    setUser,
    setCurrentTenant,
    setAvailableTenants,
    setPermissions,
    setSessionExpiry,
    updateUserProfile,
    logout,
    hydrate,
} = authSlice.actions;

export default authSlice.reducer;