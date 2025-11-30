// Mercato Push Notification Service Worker
// Handles push events and notification clicks

'use strict';

// Default notification configuration
const DEFAULT_NOTIFICATION = {
    title: 'Mercato Notification',
    body: 'You have a new notification.',
    icon: '/images/notification-icon.png',
    badge: '/images/notification-badge.png',
    data: {
        url: '/Notifications'
    }
};

// Handle push events
self.addEventListener('push', function(event) {
    console.log('[Push SW] Push event received');

    let data = { ...DEFAULT_NOTIFICATION };

    // Try to parse the push data and merge with defaults
    if (event.data) {
        try {
            const parsed = event.data.json();
            data = {
                ...DEFAULT_NOTIFICATION,
                ...parsed,
                data: {
                    ...DEFAULT_NOTIFICATION.data,
                    ...(parsed.data || {})
                }
            };
        } catch (e) {
            console.error('[Push SW] Error parsing push data:', e);
            data.body = event.data.text();
        }
    }

    const options = {
        body: data.body,
        icon: data.icon || DEFAULT_NOTIFICATION.icon,
        badge: data.badge || DEFAULT_NOTIFICATION.badge,
        vibrate: [100, 50, 100],
        data: data.data || DEFAULT_NOTIFICATION.data,
        actions: [
            {
                action: 'view',
                title: 'View'
            },
            {
                action: 'dismiss',
                title: 'Dismiss'
            }
        ],
        tag: (data.data && data.data.notificationId) ? data.data.notificationId : 'mercato-notification',
        requireInteraction: true
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});

// Handle notification clicks
self.addEventListener('notificationclick', function(event) {
    console.log('[Push SW] Notification clicked');

    event.notification.close();

    const action = event.action;
    const data = event.notification.data || {};

    // Handle dismiss action
    if (action === 'dismiss') {
        return;
    }

    // Determine the URL to open
    let urlToOpen = data.url || '/Notifications';

    // Ensure the URL is absolute
    if (!urlToOpen.startsWith('http')) {
        urlToOpen = self.location.origin + urlToOpen;
    }

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(function(clientList) {
                // Check if there's already an open window
                for (let i = 0; i < clientList.length; i++) {
                    const client = clientList[i];
                    if (client.url === urlToOpen && 'focus' in client) {
                        return client.focus();
                    }
                }
                // If no matching window, open a new one
                if (clients.openWindow) {
                    return clients.openWindow(urlToOpen);
                }
            })
    );
});

// Handle notification close
self.addEventListener('notificationclose', function(event) {
    console.log('[Push SW] Notification closed');
});

// Handle service worker activation
self.addEventListener('activate', function(event) {
    console.log('[Push SW] Service worker activated');
    event.waitUntil(self.clients.claim());
});

// Handle service worker installation
self.addEventListener('install', function(event) {
    console.log('[Push SW] Service worker installed');
    event.waitUntil(self.skipWaiting());
});
