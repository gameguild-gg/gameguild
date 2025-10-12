import React from 'react';

type BlogSidebarSectionHeaderProps = React.HtmlHTMLAttributes<HTMLHeadingElement>;

export const BlogSidebarSectionHeader: React.FunctionComponent<BlogSidebarSectionHeaderProps> = ({ children, className, ...props }) => {
  return (
    <h2 className={className} {...props}>
      {children}
    </h2>
  );
};
