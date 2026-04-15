/**
 * Purges the entire Cloudflare cache for the configured zone.
 *
 * Requires env vars:
 *   CLOUDFLARE_ZONE_ID   – The Zone ID visible in the Cloudflare dashboard overview page.
 *   CLOUDFLARE_API_TOKEN  – An API Token with "Zone.Cache Purge" permission.
 *
 * Docs: https://developers.cloudflare.com/api/resources/cache/methods/purge/
 */
export async function purgeCloudflareCache(): Promise<void> {
    const zoneId = process.env.CLOUDFLARE_ZONE_ID;
    const apiToken = process.env.CLOUDFLARE_API_TOKEN;

    if (!zoneId || !apiToken) {
        console.log(
            '[cloudflare] Skipping cache purge – CLOUDFLARE_ZONE_ID or CLOUDFLARE_API_TOKEN not set.',
        );
        return;
    }

    const url = `https://api.cloudflare.com/client/v4/zones/${zoneId}/purge_cache`;

    try {
        console.log('[cloudflare] Purging entire cache for zone', zoneId, '…');

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${apiToken}`,
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ purge_everything: true }),
        });

        const data = (await response.json()) as {
            success: boolean;
            errors?: { code: number; message: string }[];
        };

        if (data.success) {
            console.log('[cloudflare] Cache purge succeeded.');
        } else {
            console.error('[cloudflare] Cache purge failed:', data.errors);
        }
    } catch (error) {
        // Non-fatal – the app should still start even if the purge fails.
        console.error('[cloudflare] Cache purge request error:', error);
    }
}
