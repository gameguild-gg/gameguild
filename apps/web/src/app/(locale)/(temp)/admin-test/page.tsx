import Link from 'next/link';

export default function AdminTestPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4">
      <div className="max-w-md w-full space-y-8 text-center">
        <h1 className="text-3xl font-bold">Admin Test Links</h1>
        <div className="space-y-4">
          <div>
            <Link href="/admin-login" className="block w-full bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600">
              Admin Login Form
            </Link>
          </div>
          <div>
            <Link href="/dashboard/tenant?admin=true" className="block w-full bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600">
              Direct Admin Access to Tenant Management
            </Link>
          </div>
          <div>
            <Link href="/sign-in" className="block w-full bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600">
              Regular Sign In
            </Link>
          </div>
        </div>
        <p className="text-sm text-gray-600">Use these links to test the admin login functionality</p>
      </div>
    </div>
  );
}
