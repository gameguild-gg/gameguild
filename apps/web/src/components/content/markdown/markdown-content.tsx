import React, { type AnchorHTMLAttributes, type HTMLAttributes } from 'react';
import ReactMarkdown from 'react-markdown';

export type MarkdownComponentType = Record<string, (props: HTMLAttributes<HTMLElement>) => React.JSX.Element>;

// TODO: Migrate the tailwind classes to css classes that uses tailwind internally later.
const baseComponents: MarkdownComponentType = {
  h1: (props: HTMLAttributes<HTMLHeadingElement>) => <h1 className="text-foreground text-4xl font-bold mt-6 mb-4" {...props} />,
  h2: (props: HTMLAttributes<HTMLHeadingElement>) => <h2 className="text-foreground text-3xl font-semibold mt-5 mb-3" {...props} />,
  h3: (props: HTMLAttributes<HTMLHeadingElement>) => <h3 className="text-foreground text-2xl font-semibold mt-4 mb-2" {...props} />,
  h4: (props: HTMLAttributes<HTMLHeadingElement>) => <h4 className="text-foreground text-xl font-semibold mt-3 mb-2" {...props} />,
  h5: (props: HTMLAttributes<HTMLHeadingElement>) => <h5 className="text-foreground text-lg font-semibold mt-2 mb-1" {...props} />,
  h6: (props: HTMLAttributes<HTMLHeadingElement>) => <h6 className="text-foreground text-base font-semibold mt-2 mb-1" {...props} />,
  p: (props: HTMLAttributes<HTMLParagraphElement>) => <p className="text-foreground text-base leading-relaxed mb-4" {...props} />,
  ul: (props: HTMLAttributes<HTMLUListElement>) => <ul className="text-foreground list-disc mb-4 pl-5" {...props} />,
  ol: (props: HTMLAttributes<HTMLOListElement>) => <ol className="text-foreground list-decimal mb-4 pl- " {...props} />,
  li: (props: HTMLAttributes<HTMLLIElement>) => <li className="text-foreground mb-1" {...props} />,
  a: (props: AnchorHTMLAttributes<HTMLAnchorElement>) => <a className="text-primary hover:underline" {...props} />,
  blockquote: (props: HTMLAttributes<HTMLElement>) => <blockquote className="text-muted-foreground italic my-4 pl-4 border-l-4 border-border" {...props} />,
};

export interface MarkdownContentProps {
  content: string;
  components?: MarkdownComponentType;
}

export const MarkdownContent = ({ content, components }: MarkdownContentProps): React.JSX.Element => {
  return (
    <>
      <ReactMarkdown components={{ ...baseComponents, ...components }}>
        {/* TODO: Later add plugins and etc. so we can extend the Markdown rendering to each especific usage */}
        {content}
      </ReactMarkdown>
    </>
  );
};
