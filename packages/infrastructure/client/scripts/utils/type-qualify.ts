/**
 * Type Qualification Utility
 *
 * Qualifies type references with namespace prefixes for generated code.
 */

import ts from 'typescript';

const PRIMITIVE_TYPES = new Set([
  'void',
  'string',
  'number',
  'boolean',
  'unknown',
  'any',
  'null',
  'undefined',
  'never',
  'object',
  'Blob',
  'FormData',
  'File',
  'ReadableStream',
  'ArrayBuffer',
]);

const printer = ts.createPrinter();

/**
 * Qualify a type reference with the Types namespace prefix.
 * Primitives and built-in types are left as-is.
 */
export function qualifyType(type: string, namespace = 'Types'): string {
  const source = ts.createSourceFile('qualified-type.ts', `type Qualified = ${type};`, ts.ScriptTarget.Latest, true);
  const declaration = source.statements[0];
  if (!declaration || !ts.isTypeAliasDeclaration(declaration)) {
    throw new Error(`Invalid generated type expression: ${type}`);
  }
  if (ts.isTypeLiteralNode(declaration.type) || ts.isFunctionTypeNode(declaration.type)) return type;

  // Use TypeScript's nesting and literal boundaries rather than splitting on
  // separators that may be inside generic arguments or quoted enum values.
  const result = ts.transform(declaration.type, [
    (context) => {
      const visit: ts.Visitor = (node) => {
        // Inline structural/function types already carry their own declarations.
        if (ts.isTypeLiteralNode(node) || ts.isFunctionTypeNode(node)) return node;
        if (ts.isTypeReferenceNode(node) && ts.isIdentifier(node.typeName) && !node.typeArguments?.length && !PRIMITIVE_TYPES.has(node.typeName.text)) {
          return context.factory.updateTypeReferenceNode(
            node,
            context.factory.createQualifiedName(context.factory.createIdentifier(namespace), node.typeName),
            node.typeArguments,
          );
        }
        return ts.visitEachChild(node, visit, context);
      };
      return (node) => ts.visitNode(node, visit) as ts.TypeNode;
    },
  ]);
  try {
    return printer.printNode(ts.EmitHint.Unspecified, result.transformed[0], source);
  } finally {
    result.dispose();
  }
}
