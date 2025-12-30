import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';
import { ai4gamesProduct, ai4gamesProductProgram, ai4gamesProgram } from './ai4games';
import { ai4games2Product, ai4games2ProductProgram, ai4games2Program } from './ai4games2';
import databasesProgram, { databasesProduct, databasesProductProgram } from './databases';
import { dsaProduct, dsaProductProgram, dsaProgram } from './dsa';
import gamePublishingProgram, { gamePublishingProduct, gamePublishingProductProgram } from './game-publishing';
import intro2gproProgram, { intro2gproProduct, intro2gproProductProgram } from './intro2gpro';
import networkingProgram, { networkingProduct, networkingProductProgram } from './networking';
import portfolioProgram, { portfolioProduct, portfolioProductProgram } from './portfolio';
import { pythonProduct, pythonProductProgram, pythonProgram } from './python';

pythonProduct.productPrograms = [pythonProductProgram];
ai4gamesProduct.productPrograms = [ai4gamesProductProgram];
ai4games2Product.productPrograms = [ai4games2ProductProgram];
databasesProduct.productPrograms = [databasesProductProgram];
networkingProduct.productPrograms = [networkingProductProgram];
dsaProduct.productPrograms = [dsaProductProgram];

export const mockPrograms: Program[] = [
  networkingProgram,
  databasesProgram,
  pythonProgram,
  ai4gamesProgram,
  ai4games2Program,
  portfolioProgram,
  dsaProgram,
  intro2gproProgram,
  gamePublishingProgram,
];

export const mockProducts: Product[] = [
  networkingProduct,
  databasesProduct,
  pythonProduct,
  ai4gamesProduct,
  ai4games2Product,
  portfolioProduct,
  dsaProduct,
  intro2gproProduct,
  gamePublishingProduct,
];

export const mockProductPrograms: ProductProgram[] = [
  networkingProductProgram,
  databasesProductProgram,
  pythonProductProgram,
  ai4gamesProductProgram,
  ai4games2ProductProgram,
  portfolioProductProgram,
  dsaProductProgram,
  intro2gproProductProgram,
  gamePublishingProductProgram,
];

export const mockProgramContents: ProgramContent[] = [
  ...(networkingProgram.programContents ?? []),
  ...(databasesProgram.programContents ?? []),
  ...(pythonProgram.programContents ?? []),
  ...(ai4gamesProgram.programContents ?? []),
  ...(ai4games2Program.programContents ?? []),
  ...(portfolioProgram.programContents ?? []),
  ...(dsaProgram.programContents ?? []),
  ...(intro2gproProgram.programContents ?? []),
  ...(gamePublishingProgram.programContents ?? []),
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