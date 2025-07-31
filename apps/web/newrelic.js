'use strict'

/**
 * New Relic agent configuration for Next.js.
 * This file is used when NEW_RELIC_NO_CONFIG_FILE is not set to true.
 * For Docker deployments, we use environment variables instead.
 */
exports.config = {
    /**
     * Array of application names.
     */
    app_name: [process.env.NEW_RELIC_APP_NAME || 'Game Guild Web'],
    /**
     * Your New Relic license key.
     */
    license_key: process.env.NEW_RELIC_LICENSE_KEY || 'your-license-key-here',
    logging: {
        /**
         * Level at which to log. 'trace' is most useful to New Relic when diagnosing
         * issues with the agent, 'info' and higher will impose the least overhead on
         * production applications.
         */
        level: process.env.NEW_RELIC_LOG_LEVEL || 'info'
    },
    /**
     * When true, all request headers except for those listed in attributes.exclude
     * will be captured for all traces, unless otherwise specified in a destination's
     * attributes include/exclude lists.
     */
    allow_all_headers: true,
    application_logging: {
        forwarding: {
            /**
             * Toggles whether the agent gathers log records for sending to New Relic.
             */
            enabled: true
        }
    },
    distributed_tracing: {
        enabled: process.env.NEW_RELIC_DISTRIBUTED_TRACING_ENABLED === 'true'
    },
    attributes: {
        /**
         * Prefix of attributes to exclude from all destinations. Allows for the
         * exclusion of attributes from external vendors when the agent is configured
         * to send data to multiple destinations.
         */
        exclude: [
            'request.headers.cookie',
            'request.headers.authorization',
            'request.headers.proxyAuthorization',
            'request.headers.setCookie*',
            'request.headers.x*',
            'response.headers.cookie',
            'response.headers.authorization',
            'response.headers.proxyAuthorization',
            'response.headers.setCookie*',
            'response.headers.x*'
        ]
    }
} 