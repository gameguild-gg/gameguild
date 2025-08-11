'use client';

import { useEffect } from 'react';

export function YouTubeAudioStyle() {
  useEffect(() => {
    // Adicionar CSS para ocultar o vídeo e mostrar apenas os controles
    const style = document.createElement('style');
    style.textContent = `
      .youtube-audio-embed {
        height: 60px !important;
        overflow: hidden;
      }
      
      /* Esconder completamente o vídeo e mostrar apenas os controles */
      .youtube-audio-embed iframe {
        margin-top: -150px;
        opacity: 0.8;
      }
    `;
    document.head.appendChild(style);

    return () => {
      document.head.removeChild(style);
    };
  }, []);

  return null;
}
