import { AdminLoginForm } from '@/components/auth/admin-login-form.tsx';

export default function AdminLoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          <h2 className="mt-6 text-3xl font-extrabold text-gray-900">Admin Access</h2>
          <p className="mt-2 text-sm text-gray-600">Development login for testing super admin functionality</p>
        </div>
        <AdminLoginForm />
        <div className="text-center">
          <a href="/sign-in" className="text-sm text-blue-600 hover:text-blue-500">
            ← Back to regular sign in
          </a>
        </div>
      </div>
    </div>
  );
}
