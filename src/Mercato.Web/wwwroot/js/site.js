// Mercato site-wide JavaScript

(function () {
    'use strict';

    // Search suggestions configuration
    const SEARCH_CONFIG = {
        minChars: 2,
        debounceMs: 300,
        suggestionsUrl: '/Product/SearchSuggestions'
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
                        <i class="bi bi-folder me-2"></i>${encodeHtml(cat.name)}
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

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSearchSuggestions);
    } else {
        initSearchSuggestions();
    }
})();
