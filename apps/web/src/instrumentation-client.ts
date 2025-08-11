// Set up performance monitoring
performance.mark('app-init');

// Initialize analytics
console.log('Analytics initialized');

// Set up error tracking
window.addEventListener('error', (event) => {
  // TODO: add additional error handling logic here, such as sending the error to an external logging service.
  // Send to your error tracking service
  reportError(event.error);
});
