declare module '*.py' {
  const content: string;
  export default content;
}

declare module '*.py?raw' {
  const content: string;
  export default content;
}
