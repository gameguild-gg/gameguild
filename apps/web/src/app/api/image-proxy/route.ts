import { NextRequest, NextResponse } from 'next/server';

/**
 * Image proxy API route that fetches external images and serves them
 * with proper CORS headers for WASM/SharedArrayBuffer compatibility.
 *
 * Usage: /api/image-proxy?url=https://i.imgur.com/example.jpeg
 */
export async function GET(request: NextRequest) {
    const url = request.nextUrl.searchParams.get('url');

    if (!url) {
        return NextResponse.json({ error: 'Missing url parameter' }, { status: 400 });
    }

    // Validate URL
    let parsedUrl: URL;
    try {
        parsedUrl = new URL(url);
    } catch {
        return NextResponse.json({ error: 'Invalid URL' }, { status: 400 });
    }

    // Only allow http/https protocols
    if (!['http:', 'https:'].includes(parsedUrl.protocol)) {
        return NextResponse.json({ error: 'Invalid protocol' }, { status: 400 });
    }

    // Optional: Allowlist of domains (uncomment to restrict)
    // const allowedDomains = ['i.imgur.com', 'imgur.com', 'upload.wikimedia.org'];
    // if (!allowedDomains.includes(parsedUrl.hostname)) {
    //   return NextResponse.json({ error: 'Domain not allowed' }, { status: 403 });
    // }

    try {
        const response = await fetch(url, {
            headers: {
                'User-Agent': 'Mozilla/5.0 (compatible; ImageProxy/1.0)',
            },
        });

        if (!response.ok) {
            return NextResponse.json(
                { error: `Failed to fetch image: ${response.status}` },
                { status: response.status }
            );
        }

        const contentType = response.headers.get('content-type') || 'image/jpeg';

        // Only allow image content types
        if (!contentType.startsWith('image/')) {
            return NextResponse.json({ error: 'URL does not point to an image' }, { status: 400 });
        }

        const imageBuffer = await response.arrayBuffer();

        return new NextResponse(imageBuffer, {
            status: 200,
            headers: {
                'Content-Type': contentType,
                'Cache-Control': 'public, max-age=86400, stale-while-revalidate=604800',
                'Cross-Origin-Resource-Policy': 'cross-origin',
            },
        });
    } catch (error) {
        console.error('Image proxy error:', error);
        return NextResponse.json({ error: 'Failed to fetch image' }, { status: 500 });
    }
}
