// Theme initialization script to prevent FOUC
// This script runs before React hydration to set the correct theme class

export const themeScript = `
(function() {
  try {
    var theme = localStorage.getItem('theme');
    var systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    var resolvedTheme = theme === 'system' || !theme ? systemTheme : theme;
    
    if (resolvedTheme === 'dark') {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
    
    // Set a data attribute to indicate theme has been initialized
    document.documentElement.setAttribute('data-theme-initialized', 'true');
  } catch (e) {
    // Fallback to light theme if there's an error
    document.documentElement.classList.remove('dark');
  }
})();
`;