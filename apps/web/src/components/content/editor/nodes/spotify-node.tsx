'use client';

import { useEffect, useState } from 'react';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { Move, Type, X } from 'lucide-react';

import { ImageSizeControl } from '@/components/ui/image-size-control';
import { CaptionInput } from '@/components/ui/caption-input';
import { Button } from '@/components/ui/button';
import { ContentEditMenu, type EditMenuOption } from '@/components/ui/content-edit-menu';

export interface SpotifyData {
  spotifyId: string;
  type: 'track' | 'album' | 'playlist' | 'artist';
  title?: string;
  caption?: string;
  size?: number; // Size as a percentage (1-100)
  showTheme?: boolean; // Show Spotify theme color
  isNew?: boolean; // Flag to indicate if the Spotify embed was newly inserted
}

export interface SerializedSpotifyNode extends SerializedLexicalNode {
  type: 'spotify';
  data: SpotifyData;
  version: 1;
}

// Extract Spotify ID and type from URL
export function extractSpotifyInfo(url: string): {
  spotifyId: string;
  type: 'track' | 'album' | 'playlist' | 'artist';
} | null {
  // Track URL patterns
  // https://open.spotify.com/track/4cOdK2wGLETKBW3PvgPWqT
  // spotify:track:4cOdK2wGLETKBW3PvgPWqT
  const trackRegex = /(?:spotify\.com\/track\/|spotify:track:)([a-zA-Z0-9]+)/i;
  const trackMatch = url.match(trackRegex);
  if (trackMatch && trackMatch[1]) {
    return { spotifyId: trackMatch[1], type: 'track' };
  }

  // Album URL patterns
  // https://open.spotify.com/album/4cOdK2wGLETKBW3PvgPWqT
  // spotify:album:4cOdK2wGLETKBW3PvgPWqT
  const albumRegex = /(?:spotify\.com\/album\/|spotify:album:)([a-zA-Z0-9]+)/i;
  const albumMatch = url.match(albumRegex);
  if (albumMatch && albumMatch[1]) {
    return { spotifyId: albumMatch[1], type: 'album' };
  }

  // Playlist URL patterns
  // https://open.spotify.com/playlist/4cOdK2wGLETKBW3PvgPWqT
  // spotify:playlist:4cOdK2wGLETKBW3PvgPWqT
  const playlistRegex = /(?:spotify\.com\/playlist\/|spotify:playlist:)([a-zA-Z0-9]+)/i;
  const playlistMatch = url.match(playlistRegex);
  if (playlistMatch && playlistMatch[1]) {
    return { spotifyId: playlistMatch[1], type: 'playlist' };
  }

  // Artist URL patterns
  // https://open.spotify.com/artist/4cOdK2wGLETKBW3PvgPWqT
  // spotify:artist:4cOdK2wGLETKBW3PvgPWqT
  const artistRegex = /(?:spotify\.com\/artist\/|spotify:artist:)([a-zA-Z0-9]+)/i;
  const artistMatch = url.match(artistRegex);
  if (artistMatch && artistMatch[1]) {
    return { spotifyId: artistMatch[1], type: 'artist' };
  }

  return null;
}

export class SpotifyNode extends DecoratorNode<JSX.Element> {
  __data: SpotifyData;

  constructor(data: SpotifyData, key?: string) {
    super(key);
    this.__data = {
      ...data,
      size: data.size ?? 100, // Default to 100% if not specified
      showTheme: data.showTheme ?? true, // Default to showing theme if not specified
    };
  }

  static getType(): string {
    return 'spotify';
  }

  static clone(node: SpotifyNode): SpotifyNode {
    return new SpotifyNode(node.__data, node.__key);
  }

  static importJSON(serializedNode: SerializedSpotifyNode): SpotifyNode {
    return new SpotifyNode(serializedNode.data);
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: Partial<SpotifyData>): void {
    const writable = this.getWritable();
    writable.__data = { ...writable.__data, ...data };
  }

  exportJSON(): SerializedSpotifyNode {
    return {
      type: 'spotify',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <SpotifyComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface SpotifyComponentProps {
  data: SpotifyData;
  nodeKey: string;
}

function SpotifyComponent({ data, nodeKey }: SpotifyComponentProps) {
  const [editor] = useLexicalComposerContext();
  const [isEditing, setIsEditing] = useState(false);
  const [caption, setCaption] = useState(data.caption || '');
  const [size, setSize] = useState(data.size || 100);
  const [showSizeControls, setShowSizeControls] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const [hasBeenNew, setHasBeenNew] = useState(data.isNew || false);

  // Remove the isNew flag after first render
  useEffect(() => {
    if (hasBeenNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node instanceof SpotifyNode) {
          const { isNew, ...rest } = data;
          node.setData(rest);
          setHasBeenNew(false);
        }
      });
    }
  }, [data, editor, nodeKey, hasBeenNew]);

  const updateSpotify = (newData: Partial<SpotifyData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof SpotifyNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  const handleCaptionChange = (newCaption: string) => {
    setCaption(newCaption);
    updateSpotify({ caption: newCaption });
  };

  const handleSizeChange = (newSize: number) => {
    setSize(newSize);
    updateSpotify({ size: newSize });
  };

  const toggleTheme = () => {
    const newShowTheme = !data.showTheme;
    updateSpotify({ showTheme: newShowTheme });
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

  // Get the appropriate Spotify embed URL based on type
  const getSpotifyEmbedUrl = () => {
    const { spotifyId, type, showTheme } = data;
    const themeParam = showTheme ? 'theme=1' : 'theme=0';

    switch (type) {
      case 'track':
        return `https://open.spotify.com/embed/track/${spotifyId}?${themeParam}`;
      case 'album':
        return `https://open.spotify.com/embed/album/${spotifyId}?${themeParam}`;
      case 'playlist':
        return `https://open.spotify.com/embed/playlist/${spotifyId}?${themeParam}`;
      case 'artist':
        return `https://open.spotify.com/embed/artist/${spotifyId}?${themeParam}`;
      default:
        return `https://open.spotify.com/embed/track/${spotifyId}?${themeParam}`;
    }
  };

  // Get the appropriate height based on type
  const getEmbedHeight = () => {
    switch (data.type) {
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
    <div className="my-6 relative group spotify-wrapper" onMouseEnter={() => setShowMenu(true)} onMouseLeave={() => setShowMenu(false)}>
      <div className="relative flex justify-center">
        <div style={{ width: `${size}%` }} className="relative">
          <iframe
            src={getSpotifyEmbedUrl()}
            width="100%"
            height={getEmbedHeight()}
            frameBorder="0"
            allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
            loading="lazy"
            title={data.title || `Spotify ${data.type}`}
            className="rounded-lg"
          ></iframe>
        </div>

        {/* Edit menu */}
        {showMenu && <ContentEditMenu options={editMenuOptions} />}
      </div>

      {/* Size control */}
      {showSizeControls && (
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
            <ImageSizeControl size={size} onChange={handleSizeChange} />
          </div>
        </div>
      )}

      {/* Caption editor */}
      {isEditing && (
        <div className="absolute -bottom-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20" onClick={(e) => e.stopPropagation()}>
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
      {!isEditing && caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  );
}

export function $createSpotifyNode(data: SpotifyData): SpotifyNode {
  return new SpotifyNode(data);
}
