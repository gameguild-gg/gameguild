import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

import intro2gproProgram, { intro2gproProduct, intro2gproProductProgram } from './intro2gpro';


export const mockPrograms: Program[] = [
  intro2gproProgram,
];

export const mockProducts: Product[] = [
  intro2gproProduct,
];

export const mockProductPrograms: ProductProgram[] = [
  intro2gproProductProgram,
];

export const mockProgramContents: ProgramContent[] = [
  ...(intro2gproProgram.programContents ?? []),
];

export function getProgramBySlug(slug: string): Program | null {
  return mockPrograms.find(program => program.slug === slug) || null;
}

export function getProductBySlug(slug: string): Product | null {
  return mockProducts.find(product => product.slug === slug) || null;
}

export function getAllPrograms(): Program[] {
  return mockPrograms;
}

export function getAllProducts(): Product[] {
  return mockProducts;
}

export function getProgramContentBySlug(programSlug: string, contentPath: string[]): ProgramContent | null {
  const program = getProgramBySlug(programSlug);
  if (!program) {
    console.log('🔍 Program not found:', programSlug);
    return null;
  }

  console.log('🔍 Searching for content:', {
    programSlug,
    contentPath,
    programContentsCount: program.programContents?.length,
  });
  console.log(
    '🔍 Available content:',
    program.programContents?.map((item: any) => ({ id: item.id, title: item.title, parent: item.parent, parentId: item.parentId }))
  );

  let content: ProgramContent | null =
    program.programContents?.find(
      (item: any) => (item.parent === null || item.parent === undefined) && (item as any).slug === contentPath[0]
    ) || null;

  console.log('🔍 First level search result:', content ? { id: content.id, title: content.title } : 'Not found');

  if (contentPath.length > 1) {
    for (const slug of contentPath.slice(1)) {
      content = content?.children?.find((item: any) => (item as any).slug === slug) || null;
      if (!content) {
        return null;
      }
    }
  }
  return content;
}