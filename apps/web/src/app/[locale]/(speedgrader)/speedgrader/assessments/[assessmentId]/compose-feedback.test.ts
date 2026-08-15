import { describe, expect, it } from 'vitest';
import { composeFeedback } from './compose-feedback';

describe('composeFeedback', () => {
  it('substitutes placeholders when no comments are provided', () => {
    const out = composeFeedback({ autoFeedback: 'auto' });
    expect(out).toContain('## Overall');
    expect(out).toContain('No overall comment');
    expect(out).toContain('## Auto-generated feedback');
    expect(out).toContain('auto');
    expect(out).toContain('## Per-file comments');
    expect(out).toContain('No per-file comments.');
  });

  it('uses instructor overall comment when provided', () => {
    const out = composeFeedback({
      overallComment: '  Good work, watch your heap usage.  ',
      autoFeedback: 'auto',
    });
    expect(out).toContain('Good work, watch your heap usage.');
    // Trimmed.
    expect(out).not.toContain('  Good work');
  });

  it('emits per-file comments in stable path order', () => {
    const out = composeFeedback({
      autoFeedback: 'auto',
      perFileComments: {
        '/home/user/main.cpp': 'rename loop counter',
        '/home/user/util.h': '',
      },
    });
    // Lexicographic order: main.cpp before util.h.
    const mainIdx = out.indexOf('### /home/user/main.cpp');
    const utilIdx = out.indexOf('### /home/user/util.h');
    expect(mainIdx).toBeGreaterThan(-1);
    expect(utilIdx).toBeGreaterThan(mainIdx);
    expect(out).toContain('rename loop counter');
    // Empty comment falls back to placeholder.
    expect(out).toContain('No comment');
  });

  it('treats whitespace-only overall as missing', () => {
    const out = composeFeedback({
      overallComment: '   ',
      autoFeedback: 'auto',
    });
    expect(out).toContain('No overall comment');
  });

  it('treats whitespace-only per-file comment as missing', () => {
    const out = composeFeedback({
      autoFeedback: 'auto',
      perFileComments: { '/home/user/x.cpp': '   ' },
    });
    expect(out).toContain('### /home/user/x.cpp');
    expect(out).toContain('No comment');
  });

  it('joins sections with the documented headers', () => {
    const out = composeFeedback({
      overallComment: 'overall body',
      autoFeedback: 'auto body',
      perFileComments: { '/p': 'per file body' },
    });
    const expectedLines = ['## Overall', 'overall body', '', '## Auto-generated feedback', 'auto body', '', '## Per-file comments', '### /p', 'per file body'];
    expect(out).toBe(expectedLines.join('\n'));
  });
});
