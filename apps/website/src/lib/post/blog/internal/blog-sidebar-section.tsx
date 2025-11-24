import React from 'react';

type BlogSidebarSectionProps = React.HtmlHTMLAttributes<HTMLElement>;

export const BlogSidebarSection: React.FunctionComponent<BlogSidebarSectionProps> = ({ children, className, ...props }) => {
  return (
    <section className={className} {...props}>
      {children}
    </section>
  );
};
