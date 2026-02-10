import { createSlice, PayloadAction, nanoid } from '@reduxjs/toolkit';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  message?: string;
  duration?: number; // in milliseconds, undefined means persistent
  createdAt: number;
}

export interface NotificationsState {
  items: Notification[];
  maxItems: number;
}

const initialState: NotificationsState = {
  items: [],
  maxItems: 5,
};

const notificationsSlice = createSlice({
  name: 'notifications',
  initialState,
  reducers: {
    addNotification: {
      reducer: (state, action: PayloadAction<Notification>) => {
        state.items.unshift(action.payload);
        if (state.items.length > state.maxItems) {
          state.items = state.items.slice(0, state.maxItems);
        }
      },
      prepare: (notification: Omit<Notification, 'id' | 'createdAt'>) => ({
        payload: {
          ...notification,
          id: nanoid(),
          createdAt: Date.now(),
        },
      }),
    },
    removeNotification: (state, action: PayloadAction<string>) => {
      state.items = state.items.filter((item) => item.id !== action.payload);
    },
    clearAllNotifications: (state) => {
      state.items = [];
    },
    setMaxItems: (state, action: PayloadAction<number>) => {
      state.maxItems = action.payload;
      if (state.items.length > state.maxItems) {
        state.items = state.items.slice(0, state.maxItems);
      }
    },
  },
});

export const { addNotification, removeNotification, clearAllNotifications, setMaxItems } =
  notificationsSlice.actions;

export const notificationsReducer = notificationsSlice.reducer;

// Selectors
export const selectNotifications = (state: { notifications: NotificationsState }) =>
  state.notifications.items;
export const selectNotificationById = (id: string) => (state: { notifications: NotificationsState }) =>
  state.notifications.items.find((item) => item.id === id);
export const selectNotificationCount = (state: { notifications: NotificationsState }) =>
  state.notifications.items.length;

// Helper action creators for common notification types
export const showSuccessNotification = (title: string, message?: string, duration = 5000) =>
  addNotification({ type: 'success', title, message, duration });

export const showErrorNotification = (title: string, message?: string, duration?: number) =>
  addNotification({ type: 'error', title, message, duration });

export const showWarningNotification = (title: string, message?: string, duration = 7000) =>
  addNotification({ type: 'warning', title, message, duration });

export const showInfoNotification = (title: string, message?: string, duration = 5000) =>
  addNotification({ type: 'info', title, message, duration });
