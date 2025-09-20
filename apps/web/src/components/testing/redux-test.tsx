'use client';

import { useAppDispatch, useAppSelector } from '@/lib/redux/hooks';
import { setUser } from '@/lib/redux/slices/auth.slice';
import { setTheme, toggleSidebar } from '@/lib/redux/slices/ui.slice';

/**
 * Simple component to test Redux functionality
 * This can be removed once we confirm Redux is working
 */
export function ReduxTest() {
    const dispatch = useAppDispatch();
    const { theme, sidebarOpen } = useAppSelector((state) => state.ui);
    const { isAuthenticated, user } = useAppSelector((state) => state.auth);

    const handleToggleSidebar = () => {
        dispatch(toggleSidebar());
    };

    const handleThemeChange = () => {
        dispatch(setTheme(theme === 'light' ? 'dark' : 'light'));
    };

    const handleLogin = () => {
        dispatch(setUser({
            id: 'test-user',
            name: 'Test User',
            email: 'test@example.com',
            roles: ['user'],
        }));
    };

    return (
        <div className="p-4 border rounded">
            <h3 className="text-lg font-semibold mb-4">Redux Test Component</h3>

            <div className="space-y-2">
                <p>Theme: {theme}</p>
                <p>Sidebar Open: {sidebarOpen.toString()}</p>
                <p>Authenticated: {isAuthenticated.toString()}</p>
                <p>User: {user?.name || 'None'}</p>
            </div>

            <div className="mt-4 space-x-2">
                <button
                    onClick={handleToggleSidebar}
                    className="px-3 py-1 bg-blue-500 text-white rounded"
                >
                    Toggle Sidebar
                </button>
                <button
                    onClick={handleThemeChange}
                    className="px-3 py-1 bg-green-500 text-white rounded"
                >
                    Toggle Theme
                </button>
                <button
                    onClick={handleLogin}
                    className="px-3 py-1 bg-purple-500 text-white rounded"
                >
                    Test Login
                </button>
            </div>
        </div>
    );
}