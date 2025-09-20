import { configureStore } from '@reduxjs/toolkit';
import { authSlice } from './slices/auth.slice';
import { uiSlice } from './slices/ui.slice';

export const makeStore = () => {
    return configureStore({
        reducer: {
            ui: uiSlice.reducer,
            auth: authSlice.reducer,
        },
        middleware: (getDefaultMiddleware) =>
            getDefaultMiddleware({
                serializableCheck: {
                    ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE'],
                },
            }),
        devTools: process.env.NODE_ENV !== 'production',
    });
};

export type AppStore = ReturnType<typeof makeStore>;
export type RootState = ReturnType<AppStore['getState']>;
export type AppDispatch = AppStore['dispatch'];