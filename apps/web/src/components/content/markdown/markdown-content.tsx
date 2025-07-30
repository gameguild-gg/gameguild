import type React from 'react';

// TODO: Migrate the tailwind classes to css classes that uses tailwind internally later.
const components: Record<string, React.JSX.Element> = {
  h1: <h1 className="text-4xl font-bold mt-6 mb-4" />,
  h2: <h2 className="text-3xl font-semibold mt-5 mb-3" />,
  h3: <h3 className="text-2xl font-semibold mt-4 mb-2" />,
  h4: <h4 className="text-xl font-semibold mt-3 mb-2" />,
  h5: <h5 className="text-lg font-semibold mt-2 mb-1" />,
  h6: <h6 className="text-base font-semibold mt-2 mb-1" />,
  p: <p className="mb-4" />,
  ul: <ul className="list-disc pl-5 mb-4" />,
  ol: <ol className="list-decimal pl-5 mb-4" />,
  li: <li className="mb-1" />,
  a: <a className="text-blue-600 hover:underline" />,
  blockquote: <blockquote className="border-l-4 border-gray-300 pl-4 italic my-4" />,
};

export const MarkdownContent = (): React.JSX.Element => {
  return <></>;
};
