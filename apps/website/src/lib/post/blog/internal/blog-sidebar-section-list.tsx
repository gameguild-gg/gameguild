import React from 'react';

type BlogSidebarSectionListProps = React.HtmlHTMLAttributes<HTMLUListElement>;

export const BlogSidebarSectionList: React.FunctionComponent<BlogSidebarSectionListProps> = ({ children, className, ...props }) => {
  return (
    <ul className={className} {...props}>
      {children}
    </ul>
  );
};
