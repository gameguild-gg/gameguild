import React from 'react';

// Example demonstrating the difference between string-based and type-safe approaches

interface User {
  id: string;
  name: string;
  email: string;
  role: 'admin' | 'user' | 'moderator';
  status: 'active' | 'inactive' | 'suspended';
  department: string;
}

// ❌ OLD WAY - String-based (Error Prone)
export function StringBasedFilterExample() {
  const users: User[] = [];

  // These would all compile but cause runtime errors:

  // 1. Typo in filter key
  const filterByStaus = (status: string) => {
    return users.filter((user) => user.status === status); // Works fine
  };

  // But if we had a filter system that used string keys:
  const registerFilter = (key: string, options: any[]) => {
    // This would accept any string, even invalid ones
    console.log(`Registering filter for ${key}`);
  };

  // ❌ These would compile but cause problems at runtime:
  registerFilter('staus', []); // Typo! Should be 'status'
  registerFilter('rol', []); // Typo! Should be 'role'
  registerFilter('nonexistentField', []); // Invalid field!

  return (
    <div className="p-4 border border-red-200 bg-red-50">
      <h3 className="font-semibold text-red-800">❌ String-Based Approach (Error Prone)</h3>
      <ul className="mt-2 text-sm text-red-700 space-y-1">
        <li>• Typos in filter keys cause runtime errors</li>
        <li>• No compile-time validation</li>
        <li>• Refactoring is dangerous - missed string updates</li>
        <li>• No IntelliSense support</li>
        <li>• Hard to maintain as codebase grows</li>
      </ul>
    </div>
  );
}

// ✅ NEW WAY - Type-Safe (Error Prevention)
export function TypeSafeFilterExample() {
  const users: User[] = [];

  // Type-safe filter registration function
  const registerTypeSafeFilter = <T extends Record<string, unknown>>(
    key: keyof T, // ✅ Must be a valid property of T
    options: any[],
  ) => {
    console.log(`Registering filter for ${String(key)}`);
  };

  // ✅ These work perfectly:
  registerTypeSafeFilter<User>('status', []); // ✅ Valid User property
  registerTypeSafeFilter<User>('role', []); // ✅ Valid User property
  registerTypeSafeFilter<User>('department', []); // ✅ Valid User property

  // ❌ These would cause TypeScript errors (preventing runtime bugs):
  // registerTypeSafeFilter<User>('staus', []); // ❌ TypeScript error: 'staus' doesn't exist
  // registerTypeSafeFilter<User>('rol', []); // ❌ TypeScript error: 'rol' doesn't exist
  // registerTypeSafeFilter<User>('invalidField', []); // ❌ TypeScript error: 'invalidField' doesn't exist

  // Type-safe filtering with guaranteed property existence
  const filterByProperty = <T extends Record<string, unknown>, K extends keyof T>(
    data: T[],
    key: K, // ✅ Must be a valid property of T
    value: T[K],
  ): T[] => {
    return data.filter((item) => item[key] === value);
  };

  // ✅ These work and are type-safe:
  const activeUsers = filterByProperty(users, 'status', 'active');
  const adminUsers = filterByProperty(users, 'role', 'admin');

  // ❌ These would cause TypeScript errors:
  // filterByProperty(users, 'staus', 'active'); // ❌ TypeScript error
  // filterByProperty(users, 'invalidField', 'value'); // ❌ TypeScript error

  return (
    <div className="p-4 border border-green-200 bg-green-50">
      <h3 className="font-semibold text-green-800">✅ Type-Safe Approach (Error Prevention)</h3>
      <ul className="mt-2 text-sm text-green-700 space-y-1">
        <li>• Compile-time validation prevents typos</li>
        <li>• TypeScript catches errors before runtime</li>
        <li>• Safe refactoring - rename properties and all references update</li>
        <li>• Full IntelliSense support with auto-completion</li>
        <li>• Self-documenting code through type constraints</li>
        <li>• Guaranteed property existence</li>
      </ul>
    </div>
  );
}

// Real-world example showing the difference
export function RealWorldComparison() {
  interface Product {
    id: string;
    name: string;
    category: 'electronics' | 'clothing' | 'books';
    price: number;
    inStock: boolean;
    brand: string;
  }

  const products: Product[] = [
    {
      id: '1',
      name: 'Laptop',
      category: 'electronics',
      price: 999,
      inStock: true,
      brand: 'TechCorp',
    },
  ];

  return (
    <div className="space-y-6">
      <div className="bg-gray-50 p-6 rounded-lg">
        <h2 className="text-xl font-bold mb-4">🔍 Filter System Comparison</h2>
        <p className="text-gray-600 mb-6">Here's how our enhanced type-safe filter system prevents common implementation errors:</p>

        <div className="grid gap-6 md:grid-cols-2">
          <StringBasedFilterExample />
          <TypeSafeFilterExample />
        </div>
      </div>

      <div className="bg-blue-50 p-6 rounded-lg">
        <h3 className="text-lg font-semibold text-blue-900 mb-3">🚀 Real-World Benefits</h3>
        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <h4 className="font-medium text-blue-800">Development Time</h4>
            <p className="text-sm text-blue-700">Catch errors at compile time instead of debugging in production</p>
          </div>
          <div>
            <h4 className="font-medium text-blue-800">Code Quality</h4>
            <p className="text-sm text-blue-700">Self-documenting code that's easier to understand and maintain</p>
          </div>
          <div>
            <h4 className="font-medium text-blue-800">Team Productivity</h4>
            <p className="text-sm text-blue-700">IntelliSense and auto-completion reduce cognitive load</p>
          </div>
          <div>
            <h4 className="font-medium text-blue-800">Refactoring Safety</h4>
            <p className="text-sm text-blue-700">Change data structures with confidence - TypeScript finds all references</p>
          </div>
        </div>
      </div>

      <div className="bg-yellow-50 p-6 rounded-lg">
        <h3 className="text-lg font-semibold text-yellow-900 mb-3">📋 Implementation Checklist</h3>
        <div className="space-y-2 text-sm text-yellow-800">
          <div className="flex items-center gap-2">
            <span className="text-green-600">✅</span>
            <span>Define data interfaces that extend Record&lt;string, unknown&gt;</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-600">✅</span>
            <span>Use FilterProvider&lt;YourDataType&gt; for type-safe context</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-600">✅</span>
            <span>Register filters with keyof T constraints</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-600">✅</span>
            <span>Use TypeSafeMultiSelectFilter for compile-time validation</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-600">✅</span>
            <span>Leverage useFilteredData hook for type-safe filtering</span>
          </div>
        </div>
      </div>
    </div>
  );
}

// Export the main comparison component
export default RealWorldComparison;
