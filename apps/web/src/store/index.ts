import { configureStore } from '@reduxjs/toolkit';
import { userReducer } from './slices/userSlice';
import { uiReducer } from './slices/uiSlice';
import { notificationsReducer } from './slices/notificationsSlice';

export const makeStore = () => {
  return configureStore({
    reducer: {
      user: userReducer,
      ui: uiReducer,
      notifications: notificationsReducer,
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

// Infer the type of makeStore
export type AppStore = ReturnType<typeof makeStore>;

// Infer the `RootState` and `AppDispatch` types from the store itself
export type RootState = ReturnType<AppStore['getState']>;
export type AppDispatch = AppStore['dispatch'];
