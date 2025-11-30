// Mercato site-wide JavaScript

(function () {
    'use strict';

    // Search suggestions configuration
    const SEARCH_CONFIG = {
        minChars: 2,
        debounceMs: 300,
        suggestionsUrl: '/Product/SearchSuggestions'
    };

    // Recently viewed products configuration
    const RECENTLY_VIEWED_CONFIG = {
        storageKey: 'mercato_recently_viewed',
        maxItems: 10,
        apiUrl: '/Buyer/RecentlyViewed'
    };

    // Debounce utility function
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // HTML encoding utility to prevent XSS
    function encodeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Initialize search suggestions
    function initSearchSuggestions() {
        const searchInput = document.getElementById('search-input');
        const suggestionsDropdown = document.getElementById('search-suggestions');
        const categoriesSection = document.getElementById('suggestions-categories');
        const categoriesList = document.getElementById('suggestions-categories-list');
        const productsSection = document.getElementById('suggestions-products');
        const productsList = document.getElementById('suggestions-products-list');
        const emptySection = document.getElementById('suggestions-empty');

        if (!searchInput || !suggestionsDropdown) {
            return;
        }

        let currentController = null;

        // Fetch suggestions from API
        async function fetchSuggestions(query) {
            // Cancel previous request if in progress
            if (currentController) {
                currentController.abort();
            }

            currentController = new AbortController();

            try {
                const response = await fetch(
                    `${SEARCH_CONFIG.suggestionsUrl}?q=${encodeURIComponent(query)}`,
                    { signal: currentController.signal }
                );

                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }

                return await response.json();
            } catch (error) {
                if (error.name === 'AbortError') {
                    return null; // Request was cancelled
                }
                console.error('Error fetching suggestions:', error);
                return null;
            }
        }

        // Update the suggestions dropdown
        function updateSuggestions(data) {
            if (!data) {
                hideSuggestions();
                return;
            }

            const hasCategories = data.categories && data.categories.length > 0;
            const hasProducts = data.products && data.products.length > 0;

            if (!hasCategories && !hasProducts) {
                // Show empty message
                categoriesSection.style.display = 'none';
                productsSection.style.display = 'none';
                emptySection.style.display = 'block';
                suggestionsDropdown.style.display = 'block';
                return;
            }

            emptySection.style.display = 'none';

            // Render categories
            if (hasCategories) {
                categoriesList.innerHTML = data.categories.map(cat =>
                    `<a class="dropdown-item" href="/Product/Category/${encodeURIComponent(cat.id)}">
                        ${encodeHtml(cat.name)}
                    </a>`
                ).join('');
                categoriesSection.style.display = 'block';
            } else {
                categoriesSection.style.display = 'none';
            }

            // Render products
            if (hasProducts) {
                productsList.innerHTML = data.products.map(product =>
                    `<a class="dropdown-item d-flex justify-content-between align-items-center" href="/Product/Details/${encodeURIComponent(product.id)}">
                        <span class="text-truncate me-2">${encodeHtml(product.title)}</span>
                        <span class="badge bg-primary">$${product.price.toFixed(2)}</span>
                    </a>`
                ).join('');
                productsSection.style.display = 'block';
            } else {
                productsSection.style.display = 'none';
            }

            suggestionsDropdown.style.display = 'block';
        }

        // Hide suggestions dropdown
        function hideSuggestions() {
            suggestionsDropdown.style.display = 'none';
        }

        // Debounced search handler
        const handleSearch = debounce(async function (query) {
            if (query.length < SEARCH_CONFIG.minChars) {
                hideSuggestions();
                return;
            }

            const data = await fetchSuggestions(query);
            updateSuggestions(data);
        }, SEARCH_CONFIG.debounceMs);

        // Event listeners
        searchInput.addEventListener('input', function () {
            const query = this.value.trim();
            handleSearch(query);
        });

        searchInput.addEventListener('focus', function () {
            const query = this.value.trim();
            if (query.length >= SEARCH_CONFIG.minChars) {
                handleSearch(query);
            }
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', function (event) {
            if (!searchInput.contains(event.target) && !suggestionsDropdown.contains(event.target)) {
                hideSuggestions();
            }
        });

        // Handle keyboard navigation
        searchInput.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                hideSuggestions();
                this.blur();
            }
        });

        // Handle suggestion click - populate search and submit form
        suggestionsDropdown.addEventListener('click', function (event) {
            const link = event.target.closest('a.dropdown-item');
            if (link) {
                // Allow default navigation for product and category links
                return;
            }
        });
    }

    // ============================================
    // Recently Viewed Products Module
    // ============================================

    // Validate if a string is a valid GUID format
    function isValidGuid(str) {
        if (typeof str !== 'string') {
            return false;
        }
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        return guidRegex.test(str);
    }

    // Get recently viewed product IDs from localStorage
    function getRecentlyViewedIds() {
        try {
            const stored = localStorage.getItem(RECENTLY_VIEWED_CONFIG.storageKey);
            if (!stored) {
                return [];
            }
            const parsed = JSON.parse(stored);
            if (!Array.isArray(parsed)) {
                return [];
            }
            // Validate each ID is a valid GUID
            return parsed.filter(id => isValidGuid(id));
        } catch (e) {
            console.error('Error reading recently viewed products:', e);
            return [];
        }
    }

    // Save recently viewed product IDs to localStorage
    function saveRecentlyViewedIds(ids) {
        try {
            // Validate all IDs are valid GUIDs before saving
            const validIds = ids.filter(id => isValidGuid(id));
            localStorage.setItem(RECENTLY_VIEWED_CONFIG.storageKey, JSON.stringify(validIds));
        } catch (e) {
            console.error('Error saving recently viewed products:', e);
        }
    }

    // Add a product to recently viewed list
    function addToRecentlyViewed(productId) {
        if (!productId || !isValidGuid(productId)) {
            return;
        }

        const ids = getRecentlyViewedIds();

        // Remove the product if it already exists (will be moved to front)
        const index = ids.indexOf(productId);
        if (index > -1) {
            ids.splice(index, 1);
        }

        // Add to the beginning (most recent)
        ids.unshift(productId);

        // Keep only the configured maximum number of items
        const trimmedIds = ids.slice(0, RECENTLY_VIEWED_CONFIG.maxItems);

        saveRecentlyViewedIds(trimmedIds);
    }

    // Fetch recently viewed products from the server
    async function fetchRecentlyViewedProducts(ids, maxItems) {
        if (!ids || ids.length === 0) {
            return { products: [] };
        }

        try {
            const idsParam = ids.join(',');
            const response = await fetch(
                `${RECENTLY_VIEWED_CONFIG.apiUrl}?ids=${encodeURIComponent(idsParam)}&maxItems=${maxItems}`
            );

            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching recently viewed products:', error);
            return { products: [] };
        }
    }

    // Validate image URL against allowed prefixes
    function isValidImageUrl(url) {
        if (!url || typeof url !== 'string') {
            return false;
        }
        const allowedPrefixes = ['/uploads/', '/images/'];
        return allowedPrefixes.some(prefix => url.toLowerCase().startsWith(prefix));
    }

    // Render recently viewed products in a container
    function renderRecentlyViewedProducts(container, products) {
        if (!container || !products || products.length === 0) {
            if (container) {
                container.style.display = 'none';
            }
            return;
        }

        const productsHtml = products.map(product => {
            // Image URL is already validated by isValidImageUrl(), so we can use it directly
            // since it only allows URLs starting with /uploads/ or /images/
            const imageHtml = product.imageUrl && isValidImageUrl(product.imageUrl)
                ? `<img src="${product.imageUrl}" class="card-img-top" alt="${encodeHtml(product.title)}" style="height: 150px; object-fit: cover;">`
                : `<div class="card-img-top bg-secondary d-flex align-items-center justify-content-center" style="height: 150px;"><span class="text-white">No Image</span></div>`;

            const stockBadge = product.isInStock
                ? '<span class="badge bg-success">In Stock</span>'
                : '<span class="badge bg-secondary">Out of Stock</span>';

            // For the title attribute, we HTML encode to prevent XSS
            const encodedTitle = encodeHtml(product.title);

            return `
                <div class="col">
                    <div class="card h-100">
                        ${imageHtml}
                        <div class="card-body">
                            <h6 class="card-title text-truncate" title="${encodedTitle}">${encodedTitle}</h6>
                            <p class="card-text mb-1">
                                <strong class="text-primary">$${product.price.toFixed(2)}</strong>
                            </p>
                            ${stockBadge}
                        </div>
                        <div class="card-footer bg-transparent">
                            <a href="/Product/Details/${encodeURIComponent(product.id)}" class="btn btn-outline-primary btn-sm w-100">View Details</a>
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        const listContainer = container.querySelector('.recently-viewed-list');
        if (listContainer) {
            listContainer.innerHTML = productsHtml;
        }

        container.style.display = 'block';
    }

    // Initialize recently viewed products display
    async function initRecentlyViewedProducts() {
        const container = document.getElementById('recently-viewed-section');
        if (!container) {
            return;
        }

        const ids = getRecentlyViewedIds();
        if (ids.length === 0) {
            container.style.display = 'none';
            return;
        }

        const data = await fetchRecentlyViewedProducts(ids, RECENTLY_VIEWED_CONFIG.maxItems);
        renderRecentlyViewedProducts(container, data.products);
    }

    // Track product view on product details page
    function trackProductView() {
        const productIdElement = document.querySelector('[data-product-id]');
        if (productIdElement) {
            const productId = productIdElement.getAttribute('data-product-id');
            addToRecentlyViewed(productId);
        }
    }

    // Clear recently viewed products
    function clearRecentlyViewed() {
        try {
            localStorage.removeItem(RECENTLY_VIEWED_CONFIG.storageKey);
        } catch (e) {
            console.error('Error clearing recently viewed products:', e);
        }
    }

    // Expose functions globally for external use
    window.MercatoRecentlyViewed = {
        add: addToRecentlyViewed,
        getIds: getRecentlyViewedIds,
        clear: clearRecentlyViewed,
        refresh: initRecentlyViewedProducts
    };

    // Initialize when DOM is ready
    // ============================================
    // Push Notifications Module
    // ============================================

    const PUSH_CONFIG = {
        serviceWorkerPath: '/push-sw.js',
        subscriptionApiUrl: '/Api/PushSubscription'
    };

    // Check if push notifications are supported
    function isPushSupported() {
        return 'serviceWorker' in navigator && 'PushManager' in window;
    }

    // Get push notification permission status
    function getPushPermission() {
        if (!isPushSupported()) {
            return 'unsupported';
        }
        return Notification.permission;
    }

    // Request push notification permission
    async function requestPushPermission() {
        if (!isPushSupported()) {
            return 'unsupported';
        }
        return await Notification.requestPermission();
    }

    // Register the push notification service worker
    async function registerPushServiceWorker() {
        if (!isPushSupported()) {
            return null;
        }
        try {
            const registration = await navigator.serviceWorker.register(PUSH_CONFIG.serviceWorkerPath);
            await navigator.serviceWorker.ready;
            return registration;
        } catch (error) {
            console.error('Error registering push service worker:', error);
            return null;
        }
    }

    // Get current push subscription
    async function getCurrentPushSubscription() {
        if (!isPushSupported()) {
            return null;
        }
        try {
            const registration = await navigator.serviceWorker.getRegistration(PUSH_CONFIG.serviceWorkerPath);
            if (registration) {
                return await registration.pushManager.getSubscription();
            }
            return null;
        } catch (error) {
            console.error('Error getting push subscription:', error);
            return null;
        }
    }

    // Expose push notification functions globally
    window.MercatoPushNotifications = {
        isSupported: isPushSupported,
        getPermission: getPushPermission,
        requestPermission: requestPushPermission,
        registerServiceWorker: registerPushServiceWorker,
        getSubscription: getCurrentPushSubscription
    };

    // ============================================
    // Initialization
    // ============================================

    function init() {
        initSearchSuggestions();
        trackProductView();
        initRecentlyViewedProducts();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
