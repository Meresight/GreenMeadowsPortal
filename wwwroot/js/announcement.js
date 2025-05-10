/**
 * Announcements functionality for Green Meadows Portal
 */

// Global variables
let currentUser = {
    id: '',
    name: '',
    firstName: '',
    role: '',
    avatar: '/images/default-avatar.png',
    notificationCount: 0
};

/**
 * Initializes the announcement page functionality
 */
function initAnnouncementPage() {
    setupUIEventHandlers();
    fetchCurrentUser();
    loadAnnouncements();
}

/**
 * Sets up event handlers for UI elements
 */
function setupUIEventHandlers() {
    // User dropdown toggle
    const userDropdown = document.querySelector('.user-dropdown');
    if (userDropdown) {
        userDropdown.addEventListener('click', function(e) {
            e.stopPropagation();
            const dropdownMenu = this.querySelector('.dropdown-menu');
            dropdownMenu.classList.toggle('show');
            
            // Close dropdown when clicking outside
            document.addEventListener('click', function closeUserDropdown(e) {
                if (!userDropdown.contains(e.target)) {
                    dropdownMenu.classList.remove('show');
                    document.removeEventListener('click', closeUserDropdown);
                }
            });
        });
    }

    // Mobile sidebar toggle
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const sidebar = document.getElementById('sidebar');
    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function() {
            sidebar.classList.toggle('active');
        });
    }

    // Filter change handler
    const filterType = document.getElementById('filter-type');
    if (filterType) {
        filterType.addEventListener('change', function() {
            updateFilter(this.value);
        });
    }

    // Close alert buttons
    const closeButtons = document.querySelectorAll('.close-alert');
    closeButtons.forEach(function(button) {
        button.addEventListener('click', function() {
            var alert = this.parentElement;
            alert.style.display = 'none';
        });
    });

    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        var alerts = document.querySelectorAll('.alert');
        alerts.forEach(function(alert) {
            alert.style.display = 'none';
        });
    }, 5000);
}

/**
 * Fetches current user data from the API
 */
async function fetchCurrentUser() {
    try {
        const response = await fetch('/api/user/getCurrentUser');
        if (response.ok) {
            const data = await response.json();
            currentUser = {
                id: data.id,
                name: data.fullName,
                firstName: data.firstName,
                role: data.role,
                avatar: data.profileImageUrl || '/images/default-avatar.png',
                notificationCount: data.notificationCount
            };

            // Update UI with user data
            updateUserUI();
            
            // Start polling for notification updates
            startNotificationPolling();
        }
    } catch (error) {
        console.error('Failed to fetch user data:', error);
    }
}

/**
 * Updates UI elements with user data
 */
function updateUserUI() {
    // Update user avatar and name in sidebar
    const userAvatar = document.getElementById('user-avatar');
    const userName = document.getElementById('user-name');
    const userRole = document.getElementById('user-role');
    
    if (userAvatar) userAvatar.src = currentUser.avatar;
    if (userName) userName.textContent = currentUser.firstName;
    if (userRole) userRole.textContent = currentUser.role;

    // Update navbar user info
    const navUserAvatar = document.getElementById('nav-user-avatar');
    const navUserName = document.getElementById('nav-user-name');
    const notificationCount = document.getElementById('notification-count');
    
    if (navUserAvatar) navUserAvatar.src = currentUser.avatar;
    if (navUserName) navUserName.textContent = currentUser.firstName;
    if (notificationCount) notificationCount.textContent = currentUser.notificationCount;

    // Update dashboard link based on role
    const dashboardLink = document.getElementById('dashboard-link');
    if (dashboardLink) {
        if (currentUser.role === 'Admin') {
            dashboardLink.href = '/Dashboard/AdminDashboard';
        } else if (currentUser.role === 'Staff') {
            dashboardLink.href = '/Dashboard/StaffDashboard';
        } else {
            dashboardLink.href = '/Dashboard/HomeownerDashboard';
        }
    }
}

/**
 * Loads announcements from the API
 */
async function loadAnnouncements(page = 1, filter = 'all') {
    try {
        const announcementsContainer = document.getElementById('announcements-container');
        if (!announcementsContainer) return;
        
        announcementsContainer.innerHTML = '<div class="loading-spinner"><i class="fas fa-spinner fa-spin"></i><p>Loading announcements...</p></div>';

        const response = await fetch(`/api/announcements?page=${page}&filter=${filter}`);
        
        if (!response.ok) {
            throw new Error('Failed to fetch announcements');
        }
        
        const data = await response.json();
        renderAnnouncements(data.announcements);
        renderPagination(data.totalPages, data.currentPage);
    } catch (error) {
        console.error('Error loading announcements:', error);
        const announcementsContainer = document.getElementById('announcements-container');
        if (announcementsContainer) {
            announcementsContainer.innerHTML = '<div class="error-state"><i class="fas fa-exclamation-circle"></i><p>Failed to load announcements. Please try again later.</p></div>';
        }
    }
}

/**
 * Renders announcements to the DOM
 */
function renderAnnouncements(announcements) {
    const container = document.getElementById('announcements-container');
    if (!container) return;

    if (!announcements || announcements.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">
                    <i class="fas fa-bullhorn"></i>
                </div>
                <h3>No Announcements Found</h3>
                <p>There are no announcements to display at this time.</p>
            </div>
        `;
        return;
    }

    let html = '';

    announcements.forEach(announcement => {
        let priorityClass = 'general';
        let priorityText = 'General';
        
        if (announcement.priority === 0) {
            priorityClass = 'urgent';
            priorityText = 'Urgent';
        } else if (announcement.priority === 1) {
            priorityClass = 'important';
            priorityText = 'Important';
        }

        let statusHtml = '';
        if (announcement.status === 0) { // Draft
            statusHtml = '<span class="tag draft">Draft</span>';
        } else if (announcement.publishDate > new Date().toISOString()) {
            statusHtml = '<span class="tag scheduled">Scheduled</span>';
        } else if (announcement.status === 2) { // Archived
            statusHtml = '<span class="tag archived">Archived</span>';
        }

        const publishDate = announcement.publishDate ? new Date(announcement.publishDate) : new Date(announcement.createdDate);
        const formattedDate = publishDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

        html += `
            <div class="announcement-card">
                <div class="announcement-header">
                    <span class="tag ${priorityClass}">${priorityText}</span>
                    ${statusHtml}
                    <span class="date">${formattedDate}</span>
                </div>
                <h3>${announcement.title}</h3>
                <p>${truncateText(announcement.content, 150)}</p>
                <div class="announcement-footer">
                    <a href="/Announcement/Details/${announcement.id}" class="read-more">Read More</a>
                    <span class="announcement-meta">
                        Posted by: ${announcement.authorName} |
                        ${announcement.readCount} readers
                    </span>
                </div>
            </div>
        `;
    });

    container.innerHTML = html;
}

/**
 * Renders pagination controls
 */
function renderPagination(totalPages, currentPage) {
    const container = document.getElementById('pagination-container');
    if (!container) return;
    
    if (!totalPages || totalPages <= 1) {
        container.innerHTML = '';
        return;
    }

    let html = '';
    const filter = getParameterByName('filter') || 'all';

    if (currentPage > 1) {
        html += `<a href="#" onclick="navigatePage(${currentPage - 1}); return false;" class="prev"><i class="fas fa-chevron-left"></i> Previous</a>`;
    }

    for (let i = 1; i <= totalPages; i++) {
        if (i === currentPage) {
            html += `<a class="active">${i}</a>`;
        } else if (i <= 3 || i > totalPages - 3 || Math.abs(i - currentPage) <= 1) {
            html += `<a href="#" onclick="navigatePage(${i}); return false;">${i}</a>`;
        } else if (Math.abs(i - currentPage) === 2) {
            html += `<span class="ellipsis">...</span>`;
            // Skip to the next relevant page
            i = (i < currentPage) ? currentPage - 2 : totalPages - 3;
        }
    }

    if (currentPage < totalPages) {
        html += `<a href="#" onclick="navigatePage(${currentPage + 1}); return false;" class="next">Next <i class="fas fa-chevron-right"></i></a>`;
    }

    container.innerHTML = html;
}

/**
 * Navigate to a specific page
 */
function navigatePage(page) {
    const filter = getParameterByName('filter') || 'all';
    loadAnnouncements(page, filter);
    
    // Update URL without refreshing the page
    const url = new URL(window.location.href);
    url.searchParams.set('page', page);
    window.history.pushState({ page }, '', url);
}

/**
 * Update filter and reload announcements
 */
function updateFilter(filter) {
    loadAnnouncements(1, filter);
    
    // Update URL without refreshing the page
    const url = new URL(window.location.href);
    url.searchParams.set('filter', filter);
    url.searchParams.set('page', '1');
    window.history.pushState({ filter, page: 1 }, '', url);
}

/**
 * Get URL parameter by name
 */
function getParameterByName(name, url = window.location.href) {
    name = name.replace(/[\[\]]/g, '\\$&');
    var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)'),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, ' '));
}

/**
 * Truncate text to specified length and add ellipsis
 */
function truncateText(text, maxLength) {
    if (!text) return '';
    
    // Strip HTML tags
    const div = document.createElement('div');
    div.innerHTML = text;
    const plainText = div.textContent || div.innerText || '';
    
    if (plainText.length <= maxLength) return plainText;
    return plainText.slice(0, maxLength) + '...';
}

/**
 * Start polling for notification count updates
 */
function startNotificationPolling() {
    // Initial fetch
    updateNotificationCount();
    
    // Poll every 30 seconds
    setInterval(updateNotificationCount, 30000);
}

/**
 * Update notification count
 */
async function updateNotificationCount() {
    try {
        const response = await fetch('/api/announcements/unread-count');
        if (response.ok) {
            const data = await response.json();
            currentUser.notificationCount = data.count;
            
            // Update notification count in UI
            const notificationCount = document.getElementById('notification-count');
            if (notificationCount) {
                notificationCount.textContent = data.count;
            }
        }
    } catch (error) {
        console.error('Error updating notification count:', error);
    }
}

// Initialize on document load
document.addEventListener('DOMContentLoaded', initAnnouncementPage);                                                                                                                                                        