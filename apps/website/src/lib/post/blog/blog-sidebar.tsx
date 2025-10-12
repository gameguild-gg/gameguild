import React from 'react';

type BlogSidebarProps = React.HtmlHTMLAttributes<HTMLElement>;

const BlogSidebar: React.FunctionComponent<BlogSidebarProps> = ({ children, className, ...props }) => {
  return (
    <aside className={className} {...props}>
      {children}
    </aside>
  );
};

export { BlogSidebar };
