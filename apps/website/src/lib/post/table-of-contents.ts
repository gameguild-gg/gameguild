export type TableOfContents = {
  items: TableOfContentsItem[];
};

export type TableOfContentsItem = {
  id: string;
  title: string;
  items: TableOfContentsItem[];
};
