import { Category } from '@/lib/post/category';
import { TableOfContents } from '@/lib/post/table-of-contents';

export type Post = {
  id: string;
  slug: string;
  title: string;
  category?: Category[];
  tableOfContents?: TableOfContents;
};
