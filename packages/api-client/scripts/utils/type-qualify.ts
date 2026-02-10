/**
 * Type Qualification Utility
 *
 * Qualifies type references with namespace prefixes for generated code.
 */

const PRIMITIVE_TYPES = new Set([
  'void', 'string', 'number', 'boolean', 'unknown', 'any',
  'null', 'undefined', 'never', 'object', 'Blob', 'FormData',
  'File', 'ReadableStream', 'ArrayBuffer',
]);

/**
 * Qualify a type reference with the Types namespace prefix.
 * Primitives and built-in types are left as-is.
 */
export function qualifyType(type: string, namespace = 'Types'): string {
  // Handle inline object types
  if (type.startsWith('{') || type.startsWith('(')) {
    return type;
  }

  // Handle Array<T> generics
  const arrayMatch = type.match(/^Array<(.+)>(.*)$/);
  if (arrayMatch) {
    return `Array<${qualifyType(arrayMatch[1], namespace)}>${arrayMatch[2]}`;
  }

  // Handle union types (e.g. "string | null")
  if (type.includes(' | ')) {
    return type.split(' | ').map(t => qualifyType(t.trim(), namespace)).join(' | ');
  }

  // Handle Record<K,V>
  if (type.startsWith('Record<')) {
    return type;
  }

  // Strip nullable suffix for check
  const baseType = type.replace(/ \| null$/, '');
  const nullableSuffix = type.endsWith(' | null') ? ' | null' : '';

  // Primitives and built-ins don't need namespace
  if (PRIMITIVE_TYPES.has(baseType)) {
    return type;
  }

  // Already qualified
  if (baseType.startsWith('Types.') || baseType.startsWith('Errors.')) {
    return type;
  }

  return `${namespace}.${baseType}${nullableSuffix}`;
}
