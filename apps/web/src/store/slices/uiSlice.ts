import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export type Theme = 'light' | 'dark' | 'system';

export interface UiState {
  theme: Theme;
  sidebarOpen: boolean;
  mobileMenuOpen: boolean;
  modalStack: string[];
  isLoading: boolean;
  loadingMessage: string | null;
}

const initialState: UiState = {
  theme: 'system',
  sidebarOpen: true,
  mobileMenuOpen: false,
  modalStack: [],
  isLoading: false,
  loadingMessage: null,
};

const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    setTheme: (state, action: PayloadAction<Theme>) => {
      state.theme = action.payload;
    },
    toggleSidebar: (state) => {
      state.sidebarOpen = !state.sidebarOpen;
    },
    setSidebarOpen: (state, action: PayloadAction<boolean>) => {
      state.sidebarOpen = action.payload;
    },
    toggleMobileMenu: (state) => {
      state.mobileMenuOpen = !state.mobileMenuOpen;
    },
    setMobileMenuOpen: (state, action: PayloadAction<boolean>) => {
      state.mobileMenuOpen = action.payload;
    },
    openModal: (state, action: PayloadAction<string>) => {
      if (!state.modalStack.includes(action.payload)) {
        state.modalStack.push(action.payload);
      }
    },
    closeModal: (state, action: PayloadAction<string>) => {
      state.modalStack = state.modalStack.filter((id) => id !== action.payload);
    },
    closeAllModals: (state) => {
      state.modalStack = [];
    },
    setGlobalLoading: (state, action: PayloadAction<{ isLoading: boolean; message?: string }>) => {
      state.isLoading = action.payload.isLoading;
      state.loadingMessage = action.payload.message ?? null;
    },
  },
});

export const {
  setTheme,
  toggleSidebar,
  setSidebarOpen,
  toggleMobileMenu,
  setMobileMenuOpen,
  openModal,
  closeModal,
  closeAllModals,
  setGlobalLoading,
} = uiSlice.actions;

export const uiReducer = uiSlice.reducer;

// Selectors
export const selectTheme = (state: { ui: UiState }) => state.ui.theme;
export const selectSidebarOpen = (state: { ui: UiState }) => state.ui.sidebarOpen;
export const selectMobileMenuOpen = (state: { ui: UiState }) => state.ui.mobileMenuOpen;
export const selectModalStack = (state: { ui: UiState }) => state.ui.modalStack;
export const selectIsModalOpen = (modalId: string) => (state: { ui: UiState }) =>
  state.ui.modalStack.includes(modalId);
export const selectGlobalLoading = (state: { ui: UiState }) => ({
  isLoading: state.ui.isLoading,
  message: state.ui.loadingMessage,
});
