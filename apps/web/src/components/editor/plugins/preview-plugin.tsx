'use client';

import type React from 'react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import {
  AlertCircle,
  ArrowRight,
  ChevronLeft,
  ChevronRight,
  Copy,
  Download,
  ExternalLink,
  Eye,
  ImageIcon,
  LayoutGrid,
  Mail,
  RotateCcw,
} from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import type { SerializedEditorState } from 'lexical';
import type { SerializedQuizNode } from '../nodes/quiz-node';
import type { SerializedImageNode } from '../nodes/image-node';
import type { SerializedMarkdownNode } from '../nodes/markdown-node';
import type { SerializedHTMLNode } from '../nodes/html-node';
import type { SerializedVideoNode } from '../nodes/video-node';
import type { SerializedAudioNode } from '../nodes/audio-node';
import DOMPurify from 'dompurify';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';

// Adicione o import para o SerializedHeaderNode
import type { SerializedHeaderNode } from '../nodes/header-node';
import type { SerializedDividerNode } from '../nodes/divider-node';
// Add this import at the top with the other imports
import type { SerializedButtonNode } from '../nodes/button-node';
import { cn } from '@/lib/utils';
// Add this import at the top with the other imports
import type { SerializedCalloutNode } from '../nodes/callout-node';
import { Callout as UICallout } from '@/components/ui/callout';

// Add the import for SerializedGalleryNode
import type { SerializedGalleryNode } from '../nodes/gallery-node';

// Add the import for SerializedPresentationNode
import type { SerializedPresentationNode } from '../nodes/presentation-node';

// Add the import for SerializedSourceNode
import type { SerializedSourceNode } from '../nodes/source-node';

// Add this to the imports at the top of the file
import type { SerializedYouTubeNode } from '../nodes/youtube-node';

// Add this to the imports at the top of the file
import type { SerializedSpotifyNode } from '../nodes/spotify-node';

// Add this import at the top with the other imports
import type { SerializedSourceCodeNode } from '../nodes/source-code-node';
import { useQuizLogic } from '@/hooks/editor/use-quiz-logic';
import { QuizWrapper } from '../ui/quiz/quiz-wrapper';
import { QuizDisplay } from '../ui/quiz/quiz-display';

// Import the SourceCodeCore at the top with other imports
import { SourceCodeCore } from '../nodes/source-code-core';

// Função para detectar e extrair IDs de vídeos de diferentes plataformas
function getVideoEmbedInfo(url: string): { type: string; id: string } | null {
  // YouTube
  const youtubeRegex = /(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i;
  const youtubeMatch = url.match(youtubeRegex);
  if (youtubeMatch && youtubeMatch[1]) {
    return { type: 'youtube', id: youtubeMatch[1] };
  }

  // Vimeo
  const vimeoRegex = /(?:vimeo\.com\/(?:video\/)?|player\.vimeo\.com\/video\/)([0-9]+)/i;
  const vimeoMatch = url.match(vimeoRegex);
  if (vimeoMatch && vimeoMatch[1]) {
    return { type: 'vimeo', id: vimeoMatch[1] };
  }

  // Dailymotion
  const dailymotionRegex = /(?:dailymotion\.com\/(?:video\/|embed\/video\/)|dai\.ly\/)([a-zA-Z0-9]+)/i;
  const dailymotionMatch = url.match(dailymotionRegex);
  if (dailymotionMatch && dailymotionMatch[1]) {
    return { type: 'dailymotion', id: dailymotionMatch[1] };
  }

  // URL não reconhecida como plataforma de vídeo
  return null;
}

// Find the PreviewQuiz function and update it to include style rendering

function PreviewQuiz({ node }: { node: SerializedQuizNode }) {
  const quizLogic = useQuizLogic({
    answers: node.data?.answers,
    allowRetry: node.data?.allowRetry,
    correctFeedback: node.data?.correctFeedback,
    incorrectFeedback: node.data?.incorrectFeedback,
  });

  if (!node?.data) {
    console.error('Invalid quiz node structure:', node);
    return null;
  }

  const {
    question,
    questionType,
    answers,
    correctFeedback,
    incorrectFeedback,
    allowRetry,
    backgroundColor,
    blanks,
    fillBlankMode,
    fillBlankAlternatives,
    ratingScale,
    correctRating,
  } = node.data;

  const { selectedAnswers, showFeedback, isCorrect, checkAnswers, toggleAnswer, resetQuiz } = quizLogic;

  return (
    <QuizWrapper backgroundColor={backgroundColor}>
      <QuizDisplay
        question={question}
        questionType={questionType || 'multiple-choice'}
        answers={answers}
        selectedAnswers={selectedAnswers}
        setSelectedAnswers={() => {}} // Dummy function
        showFeedback={showFeedback}
        isCorrect={isCorrect}
        correctFeedback={correctFeedback ?? ''}
        incorrectFeedback={incorrectFeedback ?? ''}
        allowRetry={allowRetry}
        checkAnswers={checkAnswers}
        toggleAnswer={toggleAnswer}
        blanks={blanks}
        fillBlankMode={fillBlankMode}
        fillBlankAlternatives={fillBlankAlternatives}
        ratingScale={ratingScale}
        correctRating={correctRating}
      />
      {showFeedback && !allowRetry && (
        <div className="flex justify-end mt-4 pt-3 border-t border-border/50">
          <Button
            variant="outline"
            size="sm"
            onClick={resetQuiz}
            className="flex items-center gap-2 text-muted-foreground hover:text-foreground transition-colors bg-transparent"
          >
            <RotateCcw className="h-4 w-4" />
            Try Again
          </Button>
        </div>
      )}
    </QuizWrapper>
  );
}

function PreviewImage({ node }: { node: SerializedImageNode }) {
  if (!node?.data) {
    console.error('Invalid image node structure:', node);
    return null;
  }

  const { src, alt, caption, size = 100 } = node.data;

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        <img src={src || '/placeholder.svg'} alt={alt} style={{ width: `${size}%` }} className="h-auto rounded-lg" />
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

// Add this function after the PreviewImage function
function PreviewGallery({ node }: { node: SerializedGalleryNode }) {
  if (!node?.data) {
    console.error('Invalid gallery node structure:', node);
    return null;
  }

  const { images, layout, caption, captionStyle } = node.data;

  // Get caption style classes
  const getCaptionStyleClasses = () => {
    const classes = ['text-center text-muted-foreground'];

    // Font size
    if (captionStyle?.fontSize === 'xs') classes.push('text-xs');
    else if (captionStyle?.fontSize === 'sm') classes.push('text-sm');
    else if (captionStyle?.fontSize === 'base') classes.push('text-base');
    else if (captionStyle?.fontSize === 'lg') classes.push('text-lg');
    else classes.push('text-sm'); // Default

    // Font family
    if (captionStyle?.fontFamily === 'serif') classes.push('font-serif');
    else if (captionStyle?.fontFamily === 'mono') classes.push('font-mono');
    else classes.push('font-sans'); // Default

    // Font weight
    if (captionStyle?.fontWeight === 'medium') classes.push('font-medium');
    else if (captionStyle?.fontWeight === 'bold') classes.push('font-bold');
    else classes.push('font-normal'); // Default

    return classes.join(' ');
  };

  if (images.length === 0) {
    return (
      <div className="my-4 flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed">
        <div className="text-center">
          <ImageIcon className="h-8 w-8 mx-auto text-muted-foreground" />
          <p className="mt-2 text-sm text-muted-foreground">No images in gallery</p>
        </div>
      </div>
    );
  }

  // Get grid template columns based on layout
  const getGridTemplateColumns = () => {
    return `repeat(${layout}, 1fr)`;
  };

  return (
    <div className="my-4">
      <div
        className="grid gap-2"
        style={{
          gridTemplateColumns: getGridTemplateColumns(),
          gridAutoRows: 'auto',
        }}
      >
        {images.map((image) => {
          // Calculate grid area style
          const gridAreaStyle = {
            gridRow: image.span === '2x1' || image.span === '2x2' ? 'span 2' : undefined,
            gridColumn: image.span === '1x2' || image.span === '2x2' ? 'span 2' : undefined,
          };

          return (
            <div key={image.id} className="space-y-1" style={gridAreaStyle}>
              <div className="relative aspect-square overflow-hidden rounded-md" style={{ aspectRatio: image.displayMode === 'crop' ? '1/1' : 'auto' }}>
                <img
                  src={image.src || '/placeholder.svg'}
                  alt={image.alt}
                  className={image.displayMode === 'crop' ? 'h-full w-full object-cover' : 'h-auto w-full object-contain'}
                />
              </div>
              {image.caption && <div className={getCaptionStyleClasses()}>{image.caption}</div>}
            </div>
          );
        })}
      </div>
      {caption && <div className={`mt-2 ${getCaptionStyleClasses()}`}>{caption}</div>}
    </div>
  );
}

function PreviewMarkdown({ node }: { node: SerializedMarkdownNode }) {
  if (!node?.data) {
    console.error('Invalid markdown node structure:', node);
    return null;
  }

  return (
    <div className="my-4">
      <div className="prose prose-stone dark:prose-invert max-w-none">
        <ReactMarkdown>{node.data.content}</ReactMarkdown>
      </div>
    </div>
  );
}

function PreviewHTML({ node }: { node: SerializedHTMLNode }) {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [iframeHeight, setIframeHeight] = useState(0);

  const getPreviewContent = () => {
    const sanitizedHtml = DOMPurify.sanitize(node.data.content);
    return `
    <!DOCTYPE html>
    <html>
      <head>
        <base target="_blank">
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
      </head>
      <body>
        ${sanitizedHtml}
      </body>
    </html>
  `;
  };

  useEffect(() => {
    const iframe = iframeRef.current;
    if (!iframe) return;

    const handleLoad = () => {
      const height = iframe.contentWindow?.document.documentElement.scrollHeight || 0;
      setIframeHeight(height);
    };

    iframe.addEventListener('load', handleLoad);
    return () => iframe.removeEventListener('load', handleLoad);
  }, []);

  return (
    <div className="my-0">
      <iframe
        ref={iframeRef}
        srcDoc={getPreviewContent()}
        className="w-full rounded-md border"
        style={{ height: iframeHeight ? `${iframeHeight + 2}px` : '0' }}
        sandbox="allow-scripts allow-popups allow-same-origin"
        title="HTML Preview"
      />
    </div>
  );
}

// Componente para renderizar vídeos incorporados no preview
function EmbeddedVideoPreview({ embedInfo, size }: { embedInfo: { type: string; id: string }; size: number }) {
  let embedUrl = '';
  let title = '';

  switch (embedInfo.type) {
    case 'youtube':
      embedUrl = `https://www.youtube.com/embed/${embedInfo.id}`;
      title = 'YouTube video player';
      break;
    case 'vimeo':
      embedUrl = `https://player.vimeo.com/video/${embedInfo.id}`;
      title = 'Vimeo video player';
      break;
    case 'dailymotion':
      embedUrl = `https://www.dailymotion.com/embed/video/${embedInfo.id}`;
      title = 'Dailymotion video player';
      break;
    default:
      return null;
  }

  return (
    <div style={{ width: `${size}%` }} className="mx-auto">
      <div className="relative pt-[56.25%]">
        {' '}
        {/* 16:9 Aspect Ratio */}
        <iframe
          src={embedUrl}
          title={title}
          className="absolute top-0 left-0 w-full h-full rounded-lg"
          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
          allowFullScreen
        ></iframe>
      </div>
    </div>
  );
}

function PreviewVideo({ node }: { node: SerializedVideoNode }) {
  const [hasError, setHasError] = useState(false);
  const embedInfo = getVideoEmbedInfo(node.data?.src);

  if (!node?.data) {
    console.error('Invalid video node structure:', node);
    return null;
  }

  const { src, alt, caption, size = 100 } = node.data;

  const renderErrorMessage = () => (
    <div
      className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4 flex flex-col items-center justify-center min-h-[200px]"
      style={{ width: `${size}%` }}
    >
      <AlertCircle className="h-6 w-6 mb-2" />
      <p className="text-center">Could not load the video</p>
    </div>
  );

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        {hasError ? (
          renderErrorMessage()
        ) : embedInfo ? (
          <EmbeddedVideoPreview embedInfo={embedInfo} size={size} />
        ) : (
          <video
            src={src}
            controls
            style={{ width: `${size}%` }}
            className="h-auto rounded-lg"
            poster={alt ? `/placeholder.svg?text=${encodeURIComponent(alt)}` : undefined}
            onError={() => setHasError(true)}
          />
        )}
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

// Adicione esta função para renderizar áudio na visualização
function PreviewAudio({ node }: { node: SerializedAudioNode }) {
  const [hasError, setHasError] = useState(false);

  if (!node?.data) {
    console.error('Invalid audio node structure:', node);
    return null;
  }

  const { src, title, artist, caption, size = 100 } = node.data;

  // Verificar se é um áudio incorporado
  const getAudioEmbedInfo = (url: string): { type: string; id: string } | null => {
    // YouTube
    const youtubeRegex = /(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i;
    const youtubeMatch = url.match(youtubeRegex);
    if (youtubeMatch && youtubeMatch[1]) {
      return { type: 'youtube', id: youtubeMatch[1] };
    }

    // Spotify
    const spotifyRegex = /(?:spotify\.com\/track\/|spotify:track:)([a-zA-Z0-9]+)/i;
    const spotifyMatch = url.match(spotifyRegex);
    if (spotifyMatch && spotifyMatch[1]) {
      return { type: 'spotify', id: spotifyMatch[1] };
    }

    // SoundCloud
    const soundcloudRegex = /soundcloud\.com\/([^/]+\/[^/]+)/i;
    const soundcloudMatch = url.match(soundcloudRegex);
    if (soundcloudMatch && soundcloudMatch[1]) {
      return { type: 'soundcloud', id: soundcloudMatch[1] };
    }

    return null;
  };

  const embedInfo = getAudioEmbedInfo(src);

  // Extrair nome do arquivo de áudio da URL
  const getAudioTitle = () => {
    if (title) return title;

    try {
      const url = new URL(src);
      const pathSegments = url.pathname.split('/');
      const filename = pathSegments[pathSegments.length - 1];
      return decodeURIComponent(filename.split('.')[0]);
    } catch (e) {
      return 'Audio';
    }
  };

  // Renderizar áudio incorporado
  if (embedInfo) {
    let embedUrl = '';
    let embedTitle = '';
    let height = '80';
    let className = 'w-full rounded-lg border';

    switch (embedInfo.type) {
      case 'youtube':
        // Adicionar parâmetros para ocultar o vídeo e mostrar apenas os controles
        // Adicionando vq=small para forçar qualidade 144p e economizar recursos
        embedUrl = `https://www.youtube.com/embed/${embedInfo.id}?feature=oembed&enablejsapi=1&showinfo=0&controls=1&disablekb=1&rel=0&modestbranding=1&vq=small&iv_load_policy=3&fs=0`;
        embedTitle = 'YouTube audio player';
        height = '60'; // Altura reduzida para mostrar apenas os controles
        className = 'w-full rounded-lg border youtube-audio-embed'; // Classe especial para estilização
        break;
      case 'spotify':
        embedUrl = `https://open.spotify.com/embed/track/${embedInfo.id}`;
        embedTitle = 'Spotify audio player';
        height = '80';
        break;
      case 'soundcloud':
        embedUrl = `https://w.soundcloud.com/player/?url=https%3A//soundcloud.com/${embedInfo.id}&color=%23ff5500&auto_play=false&hide_related=false&show_comments=true&show_user=true&show_reposts=false&show_teaser=true`;
        embedTitle = 'SoundCloud audio player';
        height = '166';
        break;
      default:
        return null;
    }

    return (
      <div className="my-4 relative" style={{ width: `${size}%`, margin: '0 auto' }}>
        <iframe
          src={embedUrl}
          title={embedTitle}
          className={className}
          height={height}
          allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
          loading="lazy"
        ></iframe>
        {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
      </div>
    );
  }

  // Renderizar áudio nativo
  return (
    <div className="my-4 relative">
      <div className="w-full rounded-lg border bg-card p-4" style={{ width: `${size}%`, margin: '0 auto' }}>
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <div className="font-medium truncate">{getAudioTitle()}</div>
            {artist && <div className="text-sm text-muted-foreground">{artist}</div>}
          </div>

          <audio src={src} controls className="w-full" preload="metadata" onError={() => setHasError(true)} />
        </div>
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

// Add this function to the PreviewContent component, alongside the other node renderers
// Find the PreviewCallout function and update it to use the type name as default title
// and add the getTypeLabel function

function PreviewCallout({ node }: { node: SerializedCalloutNode }) {
  if (!node?.data) {
    console.error('Invalid callout node structure:', node);
    return null;
  }

  const { title, content, type } = node.data;

  return (
    <UICallout type={type}>
      {title && <div className="font-bold mb-1">{title}</div>}
      {content}
    </UICallout>
  );
}

// Add this function to the PreviewContent component, alongside the other node renderers
function PreviewButton({ node }: { node: SerializedButtonNode }) {
  if (!node?.data) {
    console.error('Invalid button node structure:', node);
    return null;
  }

  const { text, url, actionType, variant, size, showIcon } = node.data;

  const getActionIcon = () => {
    switch (actionType) {
      case 'url':
        return <ExternalLink className="h-4 w-4" />;
      case 'download':
        return <Download className="h-4 w-4" />;
      case 'copy':
        return <Copy className="h-4 w-4" />;
      case 'email':
        return <Mail className="h-4 w-4" />;
      default:
        return <ArrowRight className="h-4 w-4" />;
    }
  };

  const handleButtonAction = () => {
    switch (actionType) {
      case 'url':
        window.open(url, '_blank');
        break;
      case 'download':
        const link = document.createElement('a');
        link.href = url;
        link.download = '';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        break;
      case 'copy':
        navigator.clipboard.writeText(url);
        break;
      case 'email':
        window.location.href = `mailto:${url}`;
        break;
    }
  };

  return (
    <div className="my-4 flex justify-center">
      <Button variant={variant} size={size} className={cn(size === 'icon' && 'p-0 w-10 h-10 rounded-full')} onClick={handleButtonAction}>
        {size !== 'icon' ? text : ''}
        {showIcon && <span className={size !== 'icon' ? 'ml-2' : ''}>{getActionIcon()}</span>}
      </Button>
    </div>
  );
}

// Adicione a função de renderização para o nó de cabeçalho
// Adicione esta função junto com as outras funções de preview (como PreviewQuiz, PreviewImage, etc.)
// Atualize a função PreviewHeader para incluir os estilos
function PreviewHeader({ node }: { node: SerializedHeaderNode }) {
  if (!node?.data) {
    console.error('Invalid header node structure:', node);
    return null;
  }

  const { text, level, style = 'default' } = node.data;

  const getStyleClass = () => {
    switch (style) {
      case 'underlined':
        return 'border-b-2 border-primary pb-1';
      case 'bordered':
        return 'border-2 border-primary p-2 rounded-md';
      case 'gradient':
        return 'bg-gradient-to-r from-primary to-primary/30 text-primary-foreground p-2 rounded-md';
      case 'accent':
        return 'border-l-4 border-primary pl-2';
      default:
        return '';
    }
  };

  const styleClass = getStyleClass();

  switch (level) {
    case 1:
      return <h1 className={`text-4xl font-bold my-4 ${styleClass}`}>{text}</h1>;
    case 2:
      return <h2 className={`text-3xl font-bold my-4 ${styleClass}`}>{text}</h2>;
    case 3:
      return <h3 className={`text-2xl font-bold my-4 ${styleClass}`}>{text}</h3>;
    case 4:
      return <h4 className={`text-xl font-bold my-4 ${styleClass}`}>{text}</h4>;
    case 5:
      return <h5 className={`text-lg font-bold my-4 ${styleClass}`}>{text}</h5>;
    case 6:
      return <h6 className={`text-base font-bold my-4 ${styleClass}`}>{text}</h6>;
    default:
      return <h2 className={`text-3xl font-bold my-4 ${styleClass}`}>{text}</h2>;
  }
}

function PreviewDivider({ node }: { node: SerializedDividerNode }) {
  if (!node?.data) {
    console.error('Invalid divider node structure:', node);
    return null;
  }

  const { style } = node.data;

  switch (style) {
    case 'simple':
      return <hr className="my-6 border-t border-gray-300 dark:border-gray-700" />;
    case 'double':
      return <hr className="my-6 border-t-2 border-double border-gray-300 dark:border-gray-700" />;
    case 'dashed':
      return <hr className="my-6 border-t-2 border-dashed border-gray-300 dark:border-gray-700" />;
    case 'dotted':
    case 'dashed':
      return <hr className="my-6 border-t-2 border-dashed border-gray-300 dark:border-gray-700" />;
    case 'dotted':
      return <hr className="my-6 border-t-2 border-dotted border-gray-300 dark:border-gray-700" />;
    case 'gradient':
      return <div className="my-6 h-px bg-gradient-to-r from-transparent via-primary to-transparent" aria-hidden="true" />;
    case 'icon':
      return (
        <div className="my-6 flex items-center justify-center">
          <div className="flex-1 border-t border-gray-300 dark:border-gray-700" />
          <div className="mx-4 text-gray-500 dark:text-gray-400">●</div>
          <div className="flex-1 border-t border-gray-300 dark:border-gray-700" />
        </div>
      );
    default:
      return <hr className="my-6 border-t border-gray-300 dark:border-gray-700" />;
  }
}

function PreviewPresentation({ node }: { node: SerializedPresentationNode }) {
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0);

  if (!node?.data) {
    console.error('Invalid presentation node structure:', node);
    return null;
  }

  const { slides, title, theme } = node.data;

  if (slides.length === 0) {
    return (
      <div className="my-4 flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed">
        <div className="text-center">
          <LayoutGrid className="h-8 w-8 mx-auto text-muted-foreground" />
          <p className="mt-2 text-sm text-muted-foreground">No slides in presentation</p>
        </div>
      </div>
    );
  }

  const currentSlide = slides[currentSlideIndex];

  // Get background style based on theme
  const getBackgroundStyle = (slideTheme: string, backgroundImage?: string) => {
    switch (slideTheme) {
      case 'light':
        return { backgroundColor: 'white', color: 'black' };
      case 'dark':
        return { backgroundColor: '#1a1a1a', color: 'white' };
      case 'gradient':
        return {
          background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
          color: 'white',
        };
      case 'image':
        return {
          backgroundImage: `url(${backgroundImage || '/placeholder.svg'})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          color: 'white',
          position: 'relative' as const,
        };
      default:
        return { backgroundColor: 'white', color: 'black' };
    }
  };

  return (
    <div className="my-4">
      <div className="rounded-lg overflow-hidden border shadow-sm">
        <div className="bg-gray-100 dark:bg-gray-800 p-2 flex items-center justify-between">
          <h3 className="text-sm font-medium truncate">{title}</h3>
          <div className="flex items-center gap-1">
            <span className="text-xs text-muted-foreground">
              {currentSlideIndex + 1} / {slides.length}
            </span>
          </div>
        </div>

        <div className="aspect-video relative">
          <div className="w-full h-full overflow-hidden" style={getBackgroundStyle(currentSlide.theme || theme, currentSlide.backgroundImage)}>
            {currentSlide.theme === 'image' && <div className="absolute inset-0 bg-black/40"></div>}

            <div className="relative z-10 w-full h-full p-8">
              {currentSlide.title && <h2 className="text-3xl font-bold mb-4">{currentSlide.title}</h2>}
              {currentSlide.content && <div className="prose max-w-none">{currentSlide.content}</div>}
            </div>
          </div>

          {/* Navigation controls */}
          {slides.length > 1 && (
            <>
              <Button
                variant="ghost"
                size="icon"
                className="absolute left-2 top-1/2 -translate-y-1/2 bg-black/20 hover:bg-black/40 text-white rounded-full h-8 w-8"
                onClick={() => setCurrentSlideIndex((prev) => Math.max(0, prev - 1))}
                disabled={currentSlideIndex === 0}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                className="absolute right-2 top-1/2 -translate-y-1/2 bg-black/20 hover:bg-black/40 text-white rounded-full h-8 w-8"
                onClick={() => setCurrentSlideIndex((prev) => Math.min(slides.length - 1, prev + 1))}
                disabled={currentSlideIndex === slides.length - 1}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </>
          )}
        </div>

        {/* Slide thumbnails */}
        <div className="bg-gray-100 dark:bg-gray-800 p-2 overflow-x-auto">
          <div className="flex gap-2">
            {slides.map((slide, index) => (
              <button
                key={slide.id}
                className={cn(
                  'flex-shrink-0 w-16 h-10 rounded overflow-hidden border-2',
                  index === currentSlideIndex ? 'border-primary' : 'border-transparent hover:border-gray-300',
                )}
                onClick={() => setCurrentSlideIndex(index)}
              >
                <div className="w-full h-full relative" style={getBackgroundStyle(slide.theme || theme, slide.backgroundImage)}>
                  {slide.theme === 'image' && <div className="absolute inset-0 bg-black/40"></div>}
                  <div className="w-full h-full flex items-center justify-center">
                    <span className="text-xs">{index + 1}</span>
                  </div>
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

// Add this function to render sources in the preview
function PreviewSource({ node }: { node: SerializedSourceNode }) {
  if (!node?.data) {
    console.error('Invalid source node structure:', node);
    return null;
  }

  const { sources, title, style } = node.data;

  // Format a source according to the selected style
  const formatSource = (source: any): string => {
    switch (style) {
      case 'apa':
        return formatAPA(source);
      case 'mla':
        return formatMLA(source);
      case 'chicago':
        return formatChicago(source);
      case 'harvard':
        return formatHarvard(source);
      case 'ieee':
        return formatIEEE(source);
      default:
        return formatAPA(source);
    }
  };

  // APA style: Author, A. A. (Year). Title of work. Publication. URL
  const formatAPA = (source: any): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += `(${source.year}). `;
    }

    // Title
    if (source.title) {
      citation += source.title;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // MLA style: Author. "Title." Publication, Year. URL
  const formatMLA = (source: any): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Title
    if (source.title) {
      citation += `"${source.title}"`;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (source.year) {
        citation += `, ${source.year}`;
      }
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    } else if (source.year) {
      citation += source.year;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // Chicago style: Author. Year. "Title." Publication. URL
  const formatChicago = (source: any): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += source.year;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Title
    if (source.title) {
      citation += `"${source.title}"`;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // Harvard style: Author (Year) Title, Publication. URL
  const formatHarvard = (source: any): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += `(${source.year}) `;
    }

    // Title
    if (source.title) {
      citation += source.title;
      if (!citation.endsWith('.')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // IEEE style: [1] A. Author, "Title," Publication, Year. URL
  const formatIEEE = (source: any): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith(',')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Title
    if (source.title) {
      citation += `"${source.title}"`;
      if (!citation.endsWith(',')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith(',')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += source.year;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  return (
    <div className="my-8 rounded-lg p-4">
      <h3 className="text-xl font-bold mb-4">{title}</h3>
      {sources.length > 0 ? (
        <ol className="list-decimal list-inside space-y-2">
          {sources.map((source: any, index: number) => (
            <li key={source.id} className="pl-2">
              <div className="inline">{formatSource(source)}</div>
              {source.url && (
                <a href={source.url} target="_blank" rel="noopener noreferrer" className="ml-2 inline-flex items-center text-primary hover:underline">
                  <ExternalLink className="h-3 w-3 ml-1" />
                </a>
              )}
            </li>
          ))}
        </ol>
      ) : (
        <p className="text-muted-foreground">No sources added yet.</p>
      )}
    </div>
  );
}

// Add this function to render YouTube videos in the preview
function PreviewYouTube({ node }: { node: SerializedYouTubeNode }) {
  if (!node?.data) {
    console.error('Invalid YouTube node structure:', node);
    return null;
  }

  const { videoId, title, caption, size = 100, startAt, showControls, showInfo, showRelated } = node.data;

  // Build YouTube embed URL with parameters
  const getYouTubeEmbedUrl = () => {
    let url = `https://www.youtube.com/embed/${videoId}?enablejsapi=1`;

    // Add start time if specified
    if (startAt && startAt > 0) {
      url += `&start=${startAt}`;
    }

    // Add controls parameter
    url += `&controls=${showControls ? '1' : '0'}`;

    // Add showinfo parameter
    url += `&showinfo=${showInfo ? '1' : '0'}`;

    // Add related videos parameter
    url += `&rel=${showRelated ? '1' : '0'}`;

    // Add modestbranding parameter (always enabled to reduce YouTube branding)
    url += '&modestbranding=1';

    return url;
  };

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        <div style={{ width: `${size}%` }} className="relative">
          <div className="relative pt-[56.25%]">
            {/* 16:9 Aspect Ratio */}
            <iframe
              src={getYouTubeEmbedUrl()}
              title={title || 'YouTube video player'}
              className="absolute top-0 left-0 w-full h-full rounded-lg"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
              allowFullScreen
            ></iframe>
          </div>
        </div>
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

// Add this function to render Spotify embeds in the preview
function PreviewSpotify({ node }: { node: SerializedSpotifyNode }) {
  if (!node?.data) {
    console.error('Invalid Spotify node structure:', node);
    return null;
  }

  const { spotifyId, type, title, caption, size = 100, showTheme } = node.data;

  // Get the appropriate Spotify embed URL based on type
  const getSpotifyEmbedUrl = () => {
    const themeParam = showTheme ? 'theme=1' : 'theme=0';

    switch (type) {
      case 'track':
        return `https://open.spotify.com/embed/track/${spotifyId}?${themeParam}`;
      case 'album':
      case 'playlist':
        return `https://open.spotify.com/embed/album/${spotifyId}?${themeParam}`;
      case 'artist':
        return `https://open.spotify.com/embed/artist/${spotifyId}?${themeParam}`;
      default:
        return `https://open.spotify.com/embed/track/${spotifyId}?${themeParam}`;
    }
  };

  // Get the appropriate height based on type
  const getEmbedHeight = () => {
    switch (type) {
      case 'track':
        return '152'; // Height for track embeds
      case 'album':
      case 'playlist':
        return '380'; // Height for album/playlist embeds
      case 'artist':
        return '380'; // Height for artist embeds
      default:
        return '152';
    }
  };

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        <div style={{ width: `${size}%` }} className="relative">
          <iframe
            src={getSpotifyEmbedUrl()}
            width="100%"
            height={getEmbedHeight()}
            frameBorder="0"
            allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
            loading="lazy"
            title={title || `Spotify ${type}`}
            className="rounded-lg"
          ></iframe>
        </div>
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

// Replace the complex PreviewSourceCode function with this simple one
function PreviewSourceCode({ node }: { node: SerializedSourceCodeNode }) {
  if (!node?.data) {
    console.error('Invalid source code node structure:', node);
    return null;
  }

  return (
    <div className="my-4">
      <SourceCodeCore
        data={node.data}
        isPreview={true}
        onUpdateSourceCode={(newData) => {
          console.log('Source code updated in preview:', newData);
        }}
        onSave={() => {
          console.log('Save triggered in preview mode');
        }}
      />
    </div>
  );
}

function PreviewContent({ serializedState }: { serializedState: SerializedEditorState }) {
  const renderNode = (node: any) => {
    // Handle quiz nodes
    if (node.type === 'quiz') {
      return <PreviewQuiz key={node.version} node={node} />;
    }

    // Handle image nodes
    if (node.type === 'image') {
      return <PreviewImage key={node.version} node={node} />;
    }

    // Handle gallery nodes
    if (node.type === 'gallery') {
      return <PreviewGallery key={node.version} node={node} />;
    }

    // Handle markdown nodes
    if (node.type === 'markdown') {
      return <PreviewMarkdown key={node.version} node={node} />;
    }

    // Handle HTML nodes
    if (node.type === 'html') {
      return <PreviewHTML key={node.version} node={node} />;
    }

    // Handle video nodes
    if (node.type === 'video') {
      return <PreviewVideo key={node.version} node={node} />;
    }

    // Handle audio nodes
    if (node.type === 'audio') {
      return <PreviewAudio key={node.version} node={node} />;
    }

    // Adicione o caso para o nó de cabeçalho na função renderNode
    // Procure a função renderNode e adicione este caso junto com os outros casos
    // Handle header nodes
    if (node.type === 'header') {
      return <PreviewHeader key={node.version} node={node} />;
    }

    // Handle divider nodes
    if (node.type === 'divider') {
      return <PreviewDivider key={node.version} node={node} />;
    }

    // Add this case to the renderNode function in the PreviewContent component
    // Handle button nodes
    if (node.type === 'button') {
      return <PreviewButton key={node.version} node={node} />;
    }

    // Handle callout nodes
    if (node.type === 'callout') {
      return <PreviewCallout key={node.version} node={node} />;
    }

    // Handle presentation nodes
    if (node.type === 'presentation') {
      return <PreviewPresentation key={node.version} node={node} />;
    }

    // Handle source nodes
    if (node.type === 'source') {
      return <PreviewSource key={node.version} node={node} />;
    }

    // Add this case to the renderNode function in the PreviewContent component
    // Handle YouTube nodes
    if (node.type === 'youtube') {
      return <PreviewYouTube key={node.version} node={node} />;
    }

    // Add this case to the renderNode function in the PreviewContent component
    // Handle Spotify nodes
    if (node.type === 'spotify') {
      return <PreviewSpotify key={node.version} node={node} />;
    }

    // Add this case to the renderNode function in the PreviewContent component
    // Handle source code nodes
    if (node.type === 'source-code') {
      return <PreviewSourceCode key={node.version} node={node} />;
    }

    // For text content
    if (node.type === 'text') {
      let textContent = node.text;

      // Get inline styles from the node
      const inlineStyles: React.CSSProperties = {};
      if (node.style) {
        // Parse the style string and convert to React CSSProperties
        const styleString = node.style;
        const styleRules = styleString.split(';').filter((rule: string) => rule.trim());

        styleRules.forEach((rule: { split: (arg0: string) => { (): any; new (): any; map: { (arg0: (s: any) => any): [any, any]; new (): any } } }) => {
          const [property, value] = rule.split(':').map((s: string) => s.trim());
          if (property && value) {
            // Convert CSS property names to camelCase for React
            const camelCaseProperty = property.replace(/-([a-z])/g, (match: any, letter: string) => letter.toUpperCase());
            inlineStyles[camelCaseProperty as keyof React.CSSProperties] = value;
          }
        });
      }

      // Apply text formatting
      if (node.format) {
        if (node.format & 1) {
          // Bold
          textContent = (
            <strong key={`bold-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </strong>
          );
        }
        if (node.format & 2) {
          // Italic
          textContent = (
            <em key={`italic-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </em>
          );
        }
        if (node.format & 4) {
          // Underline (format flag 4)
          textContent = (
            <u key={`underline-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </u>
          );
        }
        if (node.format & 32) {
          // Subscript (format flag 32)
          textContent = (
            <sub key={`subscript-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </s>
          );
        }
        if (node.format & 16) {
          // Superscript (format flag 16)
          textContent = (
            <sup key={`superscript-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </sup>
          )
        }
        if (node.format & 64) {
          // Code
          textContent = (
            <code
              key={`code-${node.version || Math.random()}`}
              style={inlineStyles}
              className="bg-muted px-1 py-0.5 rounded text-sm"
            >
              {textContent}
            </code>
          )
        }
        if (node.format & 128) {
          // Assuming format flag 128 for short quote
          textContent = (
            <q key={`quote-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </q>
          )
        }
        // Remove the following lines from the `if (node.format)` block:
      } else if (Object.keys(inlineStyles).length > 0) {
        // Apply inline styles even if no formatting flags are set
        textContent = (
          <span key={`styled-${node.version || Math.random()}`} style={inlineStyles}>
            {textContent}
          </span>
        );
      }

      return textContent;
    }

    // For paragraphs and other block elements
    if (node.children) {
      const children = node.children.map((child: any) => renderNode(child));
      switch (node.type) {
        case 'paragraph':
          // Only render paragraph if children are text nodes
          if (node.children.every((child: any) => child.type === 'text')) {
            return <p key={node.version}>{children}</p>;
          }
          // Otherwise render a div to avoid nesting block elements in paragraphs
          return <div key={node.version}>{children}</div>;
        case 'quote':
          return (
            <blockquote key={node.version} className="border-l-4 border-muted pl-4 italic my-4">
              {children}
            </blockquote>
          );
        case 'list':
          const ListTag = node.listType === 'bullet' ? 'ul' : 'ol';
          const listClass = node.listType === 'bullet' ? 'list-disc list-inside' : 'list-decimal list-inside';
          return (
            <ListTag key={node.version} className={`${listClass} my-4`}>
              {children}
            </ListTag>
          );
        case 'listitem':
          return (
            <li key={node.version} className={listItemClasses.join(" ")}>
              {children}
            </li>
          );
        case 'link':
          return (
            <a key={node.version} href={node.url} className="text-primary hover:underline" target="_blank" rel="noopener noreferrer">
              {children}
            </a>
          );
        case 'heading':
          // Get inline styles from the node if they exist
          const headingStyles: React.CSSProperties = {};
          if (node.style) {
            const styleString = node.style;
            const styleRules = styleString.split(';').filter((rule: string) => rule.trim());

            styleRules.forEach(
              (rule: {
                split: (arg0: string) => {
                  (): any;
                  new (): any;
                  map: { (arg0: (s: any) => any): [any, any]; new (): any };
                };
              }) => {
                const [property, value] = rule.split(':').map((s: string) => s.trim());
                if (property && value) {
                  const camelCaseProperty = property.replace(/-([a-z])/g, (match: any, letter: string) => letter.toUpperCase());
                  headingStyles[camelCaseProperty as keyof React.CSSProperties] = value;
                }
              },
            );
          }

          switch (node.tag) {
            case 'h1':
            case 1:
              return (
                <h1 key={node.version} className={`text-4xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h1>
              );
            case 'h2':
            case 2:
              return (
                <h2 key={node.version} className={`text-3xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h2>
              );
            case 'h3':
            case 3:
              return (
                <h3 key={node.version} className={`text-2xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h3>
              );
            case 'h4':
            case 4:
              return (
                <h4 key={node.version} className={`text-xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h4>
              );
            case 'h5':
            case 5:
              return (
                <h5 key={node.version} className={`text-lg ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h5>
              );
            case 'h6':
            case 6:
              return (
                <h6 key={node.version} className={`text-base ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h6>
              );
            default:
              return (
                <h2 key={node.version} className={`text-3xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h2>
              );
          }
        default:
          return <div key={node.version}>{children}</div>;
      }
    }

    return null;
  };

  return <div className="prose prose-stone dark:prose-invert max-w-none">{serializedState.root.children.map((node: any) => renderNode(node))}</div>;
}

export function PreviewPlugin() {
  const [editor] = useLexicalComposerContext();
  const [isOpen, setIsOpen] = useState(false);
  const [serializedState, setSerializedState] = useState<SerializedEditorState | null>(null);

  const onClick = useCallback(() => {
    const editorState = editor.getEditorState();
    const serialized = editorState.toJSON();
    setSerializedState(serialized);
    setIsOpen(true);
  }, [editor]);

  return (
    <>
      <Button variant="outline" size="sm" onClick={onClick} className="flex items-center gap-2 bg-transparent">
        <Eye className="h-4 w-4" />
        Preview
      </Button>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Preview</DialogTitle>
            <DialogDescription>Preview of your content as it would appear to readers</DialogDescription>
          </DialogHeader>
          {serializedState && <PreviewContent serializedState={serializedState} />}
        </DialogContent>
      </Dialog>
    </>
  );
}
