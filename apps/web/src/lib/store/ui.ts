import create from 'zustand'
import { persist } from 'zustand/middleware'

type UiState = {
    theme: 'light' | 'dark'
    setTheme: (t: 'light' | 'dark') => void
    isSidebarOpen: boolean
    toggleSidebar: () => void
}

export const useUiStore = create<UiState>()(
    persist(
        (set) => ({
            theme: 'light',
            setTheme: (t) => set({ theme: t }),
            isSidebarOpen: false,
            toggleSidebar: () => set((s) => ({ isSidebarOpen: !s.isSidebarOpen }))
        }),
        {
            name: 'ui-store'
        }
    )
)

export default useUiStore
