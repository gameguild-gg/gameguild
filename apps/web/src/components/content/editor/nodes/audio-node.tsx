'use client';

import { useEffect, useRef, useState } from 'react';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { AlertCircle, Move, Pause, Play, Type, Volume2, VolumeX, X } from 'lucide-react';

import { ImageSizeControl } from '@/components/ui/image-size-control';
import { CaptionInput } from '@/components/ui/caption-input';
import { Button } from '@/components/ui/button';
import { Slider } from '@/components/ui/slider';
import { ContentEditMenu, type EditMenuOption } from '@/components/ui/content-edit-menu';

export interface AudioData {
  src: string;
  type?: string;
  title?: string;
  artist?: string;
  caption?: string;
  size?: number; // Size as a percentage (1-100)
  isNew?: boolean; // Flag to indicate if the audio was newly inserted
}

export interface SerializedAudioNode extends SerializedLexicalNode {
  type: 'audio';
  data: AudioData;
  version: 1;
}

export class AudioNode extends DecoratorNode<JSX.Element> {
  __data: AudioData;

  constructor(data: AudioData, key?: string) {
    super(key);
    this.__data = {
      ...data,
      size: data.size ?? 100, // Default to 100% if not specified
    };
  }

  static getType(): string {
    return 'audio';
  }

  static clone(node: AudioNode): AudioNode {
    return new AudioNode(node.__data, node.__key);
  }

  static importJSON(serializedNode: SerializedAudioNode): AudioNode {
    return new AudioNode(serializedNode.data);
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: AudioData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedAudioNode {
    return {
      type: 'audio',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <AudioComponent data={this.__data} nodeKey={this.__key} />;
  }
}

// Modify the getAudioEmbedInfo function to include YouTube support
function getAudioEmbedInfo(url: string): { type: string; id: string } | null {
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

  // URL not recognized as an audio platform
  return null;
}

// Modify the EmbeddedAudio function to properly handle YouTube
function EmbeddedAudio({
  embedInfo,
  size,
  onError,
  isEditing,
  setIsEditing,
  showSizeControls,
  setShowSizeControls,
  onSizeChange,
  caption,
  onCaptionChange,
}: {
  embedInfo: { type: string; id: string };
  size: number;
  onError: () => void;
  isEditing?: boolean;
  setIsEditing?: (editing: boolean) => void;
  showSizeControls?: boolean;
  setShowSizeControls?: (show: boolean) => void;
  onSizeChange?: (size: number) => void;
  caption?: string;
  onCaptionChange?: (caption: string) => void;
}) {
  const [isLoading, setIsLoading] = useState(true);
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const controlsRef = useRef<HTMLDivElement>(null);
  const captionControlsRef = useRef<HTMLDivElement>(null);
  const [showMenu, setShowMenu] = useState(false);

  let embedUrl = '';
  let title = '';
  let height = '80';
  let className = 'w-full rounded-lg border';

  switch (embedInfo.type) {
    case 'youtube':
      // Add parameters to hide the video and show only the audio
      // Adding vq=small to force 144p quality and save resources
      embedUrl = `https://www.youtube.com/embed/${embedInfo.id}?feature=oembed&enablejsapi=1&showinfo=0&controls=1&disablekb=1&rel=0&modestbranding=1&vq=small&iv_load_policy=3&fs=0`;
      title = 'YouTube audio player';
      height = '60'; // Reduced height to show only the controls
      className = 'w-full rounded-lg border youtube-audio-embed'; // Special class for styling
      break;
    case 'spotify':
      embedUrl = `https://open.spotify.com/embed/track/${embedInfo.id}`;
      title = 'Spotify audio player';
      height = '80';
      break;
    case 'soundcloud':
      embedUrl = `https://w.soundcloud.com/player/?url=https%3A//soundcloud.com/${embedInfo.id}&color=%23ff5500&auto_play=false&hide_related=false&show_comments=true&show_user=true&show_reposts=false&show_teaser=true`;
      title = 'SoundCloud audio player';
      height = '166';
      break;
    default:
      onError();
      return null;
  }

  const editMenuOptions: EditMenuOption[] = [
    {
      id: 'size',
      icon: <Move className="h-4 w-4" />,
      label: 'Adjust size',
      action: () => {
        if (setShowSizeControls) {
          setShowSizeControls(true);
        }
      },
    },
    {
      id: 'caption',
      icon: <Type className="h-4 w-4" />,
      label: caption ? 'Edit caption' : 'Add caption',
      action: () => {
        if (setIsEditing) {
          setIsEditing(true);
        }
      },
    },
  ];

  // Add CSS for YouTube
  useEffect(() => {
    if (embedInfo.type === 'youtube' && iframeRef.current) {
      // Add CSS to hide the video and show only the controls
      const style = document.createElement('style');
      style.textContent = `
        .youtube-audio-embed {
          height: 60px !important;
          overflow: hidden;
        }
        
        /* Completely hide the video and show only the controls */
        .youtube-audio-embed iframe {
          margin-top: -150px;
          opacity: 0.8;
        }
      `;
      document.head.appendChild(style);

      return () => {
        document.head.removeChild(style);
      };
    }
  }, [embedInfo.type]);

  return (
    <div style={{ width: `${size}%` }} className="relative" onMouseEnter={() => setShowMenu(true)} onMouseLeave={() => setShowMenu(false)}>
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-black/10 rounded-lg">
          <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full"></div>
        </div>
      )}
      <div className="relative">
        <iframe
          ref={iframeRef}
          src={embedUrl}
          title={title}
          className={className}
          height={height}
          allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
          loading="lazy"
          onLoad={() => setIsLoading(false)}
          onError={onError}
        ></iframe>
      </div>

      {/* Edit menu */}
      {showMenu && <ContentEditMenu options={editMenuOptions} />}

      {showSizeControls && onSizeChange && setShowSizeControls && (
        <div className="absolute -top-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20" onClick={(e) => e.stopPropagation()}>
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Adjust size</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation();
                  setShowSizeControls(false);
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <ImageSizeControl size={size} onChange={onSizeChange} />
          </div>
        </div>
      )}

      {isEditing && onCaptionChange && setIsEditing && (
        <div
          ref={captionControlsRef}
          className="absolute -bottom-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Add caption</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation();
                  setIsEditing(false);
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <CaptionInput caption={caption || ''} onChange={onCaptionChange} autoFocus={true} />
          </div>
        </div>
      )}

      {/* Display caption when not editing */}
      {!isEditing && caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

interface AudioComponentProps {
  data: AudioData;
  nodeKey: string;
}

function AudioComponent({ data, nodeKey }: AudioComponentProps) {
  const [editor] = useLexicalComposerContext();
  const [isEditing, setIsEditing] = useState(false);
  const [caption, setCaption] = useState(data.caption || '');
  const [size, setSize] = useState(data.size || 100);
  const [showSizeControls, setShowSizeControls] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const [hasBeenNew, setHasBeenNew] = useState(data.isNew || false);

  // Audio player state
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [volume, setVolume] = useState(0.7);
  const [muted, setMuted] = useState(false);

  // State for loading and error control
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  // State for embedded audio control
  const [embedInfo, setEmbedInfo] = useState<{ type: string; id: string } | null>(null);

  const audioRef = useRef<HTMLAudioElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const controlsRef = useRef<HTMLDivElement>(null);
  const sizeControlsRef = useRef<HTMLDivElement>(null);
  const captionControlsRef = useRef<HTMLDivElement>(null);

  // Detect if it's an embedded audio when loading the component
  useEffect(() => {
    if (data.src) {
      try {
        const info = getAudioEmbedInfo(data.src);
        setEmbedInfo(info);

        // If it's an embedded audio, we don't need to show the loading error
        if (info) {
          setIsLoading(false);
        }
      } catch (error) {
        console.error('Error processing audio URL:', error);
        setHasError(true);
        setErrorMessage('Invalid or unsupported audio URL.');
      }
    }
  }, [data.src]);

  // Remove the isNew flag after first render
  useEffect(() => {
    if (hasBeenNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node instanceof AudioNode) {
          const { isNew, ...rest } = data;
          node.setData(rest);
          setHasBeenNew(false);
        }
      });
    }
  }, [data, editor, nodeKey, hasBeenNew]);

  const updateAudio = (newData: Partial<AudioData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof AudioNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  const handleCaptionChange = (newCaption: string) => {
    setCaption(newCaption);
    updateAudio({ caption: newCaption });
  };

  const handleSizeChange = (newSize: number) => {
    setSize(newSize);
    updateAudio({ size: newSize });
  };

  // Audio player functions
  const togglePlay = () => {
    const audio = audioRef.current;
    if (!audio || !data.src) return;

    try {
      if (isPlaying) {
        audio.pause();
      } else {
        // Check if the audio is ready for playback
        if (audio.readyState >= 2) {
          // HAVE_CURRENT_DATA or higher
          const playPromise = audio.play();

          if (playPromise !== undefined) {
            playPromise
              .then(() => {
                setIsPlaying(true);
              })
              .catch((error) => {
                console.error('Error playing audio:', error);
                setIsPlaying(false);
              });
          }
        } else {
          console.warn('Audio is not ready for playback');
        }
      }
    } catch (error) {
      console.error('Error controlling audio:', error);
      setIsPlaying(false);
    }
  };

  const handleTimeUpdate = () => {
    const audio = audioRef.current;
    if (!audio) return;

    setCurrentTime(audio.currentTime);
    if (audio.duration && !duration) {
      setDuration(audio.duration);
    }
  };

  const handleSliderChange = (values: number[]) => {
    const audio = audioRef.current;
    if (!audio) return;

    const newTime = values[0];
    audio.currentTime = newTime;
    setCurrentTime(newTime);
  };

  const handleVolumeChange = (values: number[]) => {
    const audio = audioRef.current;
    if (!audio) return;

    const newVolume = values[0];
    audio.volume = newVolume;
    setVolume(newVolume);
    setMuted(newVolume === 0);
  };

  const toggleMute = () => {
    const audio = audioRef.current;
    if (!audio) return;

    audio.muted = !audio.muted;
    setMuted(!muted);
  };

  const formatTime = (time: number) => {
    const minutes = Math.floor(time / 60);
    const seconds = Math.floor(time % 60);
    return `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
  };

  const handleEmbedError = () => {
    setHasError(true);
    setErrorMessage('Could not load the embedded audio. Please check the URL.');
  };

  // Edit menu options
  const editMenuOptions: EditMenuOption[] = [
    {
      id: 'size',
      icon: <Move className="h-4 w-4" />,
      label: 'Adjust size',
      action: () => setShowSizeControls(true),
    },
    {
      id: 'caption',
      icon: <Type className="h-4 w-4" />,
      label: caption ? 'Edit caption' : 'Add caption',
      action: () => setIsEditing(true),
    },
  ];

  // Render error message
  const renderErrorMessage = () => (
    <div
      className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4 flex flex-col items-center justify-center min-h-[80px]"
      style={{ width: `${size}%` }}
    >
      <AlertCircle className="h-6 w-6 mb-2" />
      <p className="text-center">{errorMessage}</p>
    </div>
  );

  // Extract audio filename from URL
  const getAudioTitle = () => {
    if (data.title) return data.title;

    try {
      const url = new URL(data.src);
      const pathSegments = url.pathname.split('/');
      const filename = pathSegments[pathSegments.length - 1];
      return decodeURIComponent(filename.split('.')[0]);
    } catch (e) {
      return 'Audio';
    }
  };

  return (
    <div ref={containerRef} className="my-6 relative group audio-wrapper" onMouseEnter={() => setShowMenu(true)} onMouseLeave={() => setShowMenu(false)}>
      <div className="relative flex justify-center">
        {hasError ? (
          renderErrorMessage()
        ) : embedInfo ? (
          // Render embedded audio (Spotify, SoundCloud, etc.)
          <EmbeddedAudio
            embedInfo={embedInfo}
            size={size}
            onError={handleEmbedError}
            isEditing={isEditing}
            setIsEditing={setIsEditing}
            showSizeControls={showSizeControls}
            setShowSizeControls={setShowSizeControls}
            onSizeChange={handleSizeChange}
            caption={caption}
            onCaptionChange={handleCaptionChange}
          />
        ) : (
          // Render native audio
          <div className="w-full rounded-lg border bg-card p-4" style={{ width: `${size}%` }}>
            {isLoading && (
              <div className="absolute inset-0 flex items-center justify-center bg-black/10 rounded-lg z-10">
                <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full"></div>
              </div>
            )}

            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <div className="font-medium truncate">{getAudioTitle()}</div>
                {data.artist && <div className="text-sm text-muted-foreground">{data.artist}</div>}
              </div>

              <audio
                ref={audioRef}
                src={data.src || ''}
                className="hidden"
                onTimeUpdate={handleTimeUpdate}
                onPlay={() => setIsPlaying(true)}
                onPause={() => setIsPlaying(false)}
                onLoadedMetadata={() => {
                  if (audioRef.current) {
                    setDuration(audioRef.current.duration);
                  }
                }}
                onLoadStart={() => {
                  setIsLoading(true);
                  setHasError(false);
                }}
                onLoadedData={() => {
                  setIsLoading(false);
                }}
                onError={(e) => {
                  setIsLoading(false);
                  setHasError(true);
                  setErrorMessage('Could not load the audio. Please check the format or URL.');
                  console.error('Audio error:', e);
                }}
              />

              {/* Audio controls */}
              <div ref={controlsRef} className="flex flex-col gap-2">
                <div className="flex items-center gap-2">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={(e) => {
                      e.stopPropagation();
                      togglePlay();
                    }}
                  >
                    {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                  </Button>

                  <div className="flex-1 px-2">
                    <Slider value={[currentTime]} min={0} max={duration || 100} step={0.1} onValueChange={handleSliderChange} className="cursor-pointer" />
                  </div>

                  <span className="text-xs text-muted-foreground min-w-[60px] text-right">
                    {formatTime(currentTime)} / {formatTime(duration)}
                  </span>
                </div>

                <div className="flex items-center gap-2">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-6 w-6"
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleMute();
                    }}
                  >
                    {muted ? <VolumeX className="h-3 w-3" /> : <Volume2 className="h-3 w-3" />}
                  </Button>

                  <div className="w-24">
                    <Slider value={[muted ? 0 : volume]} min={0} max={1} step={0.1} onValueChange={handleVolumeChange} className="cursor-pointer" />
                  </div>

                  <div className="flex-1" />
                </div>
              </div>
            </div>

            {/* Edit menu */}
            {showMenu && !hasError && <ContentEditMenu options={editMenuOptions} />}
          </div>
        )}
      </div>

      {/* Size control */}
      {showSizeControls && !hasError && (
        <div
          ref={sizeControlsRef}
          className="absolute -top-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Adjust size</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation();
                  setShowSizeControls(false);
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <ImageSizeControl size={size} onChange={handleSizeChange} />
          </div>
        </div>
      )}

      {/* Caption editor */}
      {isEditing && !hasError && (
        <div
          ref={captionControlsRef}
          className="absolute -bottom-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Add caption</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation();
                  setIsEditing(false);
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <CaptionInput caption={caption} onChange={handleCaptionChange} autoFocus={true} />
          </div>
        </div>
      )}

      {/* Display caption when not editing */}
      {!isEditing && caption && !embedInfo && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

export function $createAudioNode(data: AudioData): AudioNode {
  return new AudioNode(data);
}
