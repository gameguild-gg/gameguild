/**
 * Logger utility for Next.js 15+ applications
 * Provides structured logging with different levels
 */

type LogLevel = 'debug' | 'info' | 'warn' | 'error';
type LogData = Record<string, unknown> | string | number | boolean | null | undefined;

interface LogEntry {
  level: LogLevel;
  message: string;
  data?: LogData;
  timestamp: string;
  url?: string;
  userAgent?: string;
}

class Logger {
  private isDevelopment = process.env.NODE_ENV === 'development';

  private formatMessage(level: LogLevel, message: string, data?: LogData): LogEntry {
    const entry: LogEntry = {
      level,
      message,
      timestamp: new Date().toISOString(),
    };

    if (data) {
      entry.data = data;
    }

    if (typeof window !== 'undefined') {
      entry.url = window.location.href;
      entry.userAgent = navigator.userAgent;
    }

    return entry;
  }

  private log(level: LogLevel, message: string, data?: LogData) {
    const entry = this.formatMessage(level, message, data);

    // Console logging
    if (this.isDevelopment) {
      const consoleMethod = level === 'debug' ? 'log' : level;
      console[consoleMethod](`[${level.toUpperCase()}] ${message}`, data || '');
    }

    // Send to external logging service in production
    if (!this.isDevelopment && typeof window !== 'undefined') {
      this.sendToLoggingService(entry);
    }
  }

  private async sendToLoggingService(entry: LogEntry) {
    try {
      // Replace with your actual logging service endpoint
      await fetch('/api/logs', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(entry),
      });
    } catch (error) {
      // Fallback to console in case of logging service failure
      console.error('Failed to send log to service:', error);
    }
  }

  debug(message: string, data?: LogData) {
    this.log('debug', message, data);
  }

  info(message: string, data?: LogData) {
    this.log('info', message, data);
  }

  warn(message: string, data?: LogData) {
    this.log('warn', message, data);
  }

  error(message: string, data?: LogData) {
    this.log('error', message, data);
  }
}

export const logger = new Logger();
export default logger;
