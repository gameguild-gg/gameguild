import React from 'react';

type BlogSidebarSectionListItemProps = React.HtmlHTMLAttributes<HTMLLIElement>;

const BlogSidebarSectionListItem: React.FunctionComponent<BlogSidebarSectionListItemProps> = ({ children, className, ...props }) => {
  return (
    <li className={className} {...props}>
      {children}
    </li>
  );
};

export { BlogSidebarSectionListItem };
