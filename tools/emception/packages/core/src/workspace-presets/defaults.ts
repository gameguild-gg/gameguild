// Default file contents shared across multiple workspace presets.
// Kept in a separate module so each preset file only imports what it needs,
// allowing bundlers to tree-shake unused assets.

export const DEFAULT_CODE = `#include <iostream>
#include <string>
int main() {
  std::string name;
  std::cout << "Enter your name: ";
  std::getline(std::cin, name);
  std::cout << "Hello, " << name << "! Welcome to WebAssembly!" << std::endl;
  return 0;
}
`;

export const DEFAULT_HEADER = `#pragma once

inline const char* greeting() {
  return "Welcome to multi-file mode!";
}
`;

export const DEFAULT_IMAGE = `data:image/svg+xml;utf8,${encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="520" viewBox="0 0 800 520">
      <defs>
        <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stop-color="#313244" />
          <stop offset="100%" stop-color="#181825" />
        </linearGradient>
      </defs>
      <rect width="800" height="520" fill="url(#g)"/>
      <circle cx="190" cy="150" r="72" fill="#89b4fa" opacity="0.75"/>
      <circle cx="610" cy="370" r="90" fill="#f38ba8" opacity="0.55"/>
      <text x="50%" y="45%" font-size="42" text-anchor="middle" fill="#cdd6f4" font-family="Inter, Segoe UI, Arial">Emception Workspace</text>
      <text x="50%" y="55%" font-size="22" text-anchor="middle" fill="#a6adc8" font-family="Inter, Segoe UI, Arial">Image tab preview</text>
    </svg>`,
)}`;
