// Updated user-management.js with fixes for delete, update, and reset password functionality

document.addEventListener('DOMContentLoaded', function () {
    console.log('User Management JS loaded');

    // Filter functionality
    const roleFilter = document.getElementById('role-filter');
    const statusFilter = document.getElementById('status-filter');

    if (roleFilter && statusFilter) {
        roleFilter.addEventListener('change', applyFilters);
        statusFilter.addEventListener('change', applyFilters);
    }

    function applyFilters() {
        const roleValue = roleFilter.value;
        const statusValue = statusFilter.value;
        const userRows = document.querySelectorAll('.users-table tbody tr');

        userRows.forEach(row => {
            const userRole = row.querySelector('td:nth-child(2)')?.textContent.trim();
            const userStatusEl = row.querySelector('td:nth-child(3) .status');
            const userStatus = userStatusEl ? userStatusEl.textContent.trim() : '';

            const roleMatch = roleValue === 'All Roles' || userRole === roleValue;
            const statusMatch = statusValue === 'All Statuses' || userStatus === statusValue;

            if (roleMatch && statusMatch) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    // Search functionality
    const searchInput = document.getElementById('user-search');
    if (searchInput) {
        searchInput.addEventListener('keyup', performSearch);
    }

    function performSearch() {
        const searchTerm = searchInput.value.toLowerCase();
        const userRows = document.querySelectorAll('.users-table tbody tr');

        userRows.forEach(row => {
            const userNameEl = row.querySelector('.user-name');
            const userEmailEl = row.querySelector('.user-email');

            if (!userNameEl || !userEmailEl) return;

            const userName = userNameEl.textContent.toLowerCase();
            const userEmail = userEmailEl.textContent.toLowerCase();

            if (userName.includes(searchTerm) || userEmail.includes(searchTerm)) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    // Reset Password Modal
    const resetPasswordBtns = document.querySelectorAll('.reset-password-btn');
    const resetPasswordModal = document.getElementById('reset-password-modal');
    const resetUserId = document.getElementById('reset-user-id');

    if (resetPasswordBtns.length > 0 && resetPasswordModal && resetUserId) {
        resetPasswordBtns.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                const userId = this.getAttribute('data-user-id');
                resetUserId.value = userId;
                resetPasswordModal.classList.add('show');
                resetPasswordModal.style.display = 'flex';
            });
        });
    }

    // Delete User Confirmation
    const deleteUserBtns = document.querySelectorAll('.delete-user-btn');
    const deleteConfirmationModal = document.getElementById('delete-confirmation-modal');
    const deleteUserIdInput = document.getElementById('delete-user-id');
    const confirmDeleteBtn = document.getElementById('confirm-delete');
    const deleteUserForm = document.getElementById('delete-user-form');

    if (deleteUserBtns.length > 0 && deleteConfirmationModal) {
        deleteUserBtns.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                const userId = this.getAttribute('data-user-id');
                if (deleteUserIdInput) {
                    deleteUserIdInput.value = userId;
                }
                if (confirmDeleteBtn) {
                    confirmDeleteBtn.setAttribute('data-user-id', userId);
                }
                deleteConfirmationModal.classList.add('show');
                deleteConfirmationModal.style.display = 'flex';
            });
        });
    }

    // Handle Delete User confirmation
    if (confirmDeleteBtn && deleteUserForm) {
        confirmDeleteBtn.addEventListener('click', function (e) {
            e.preventDefault();

            // Get the user ID from the hidden field
            const userId = document.getElementById('delete-user-id').value;

            // Show loading indicator
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deleting...';
            this.disabled = true;

            // Submit the form
            deleteUserForm.submit();
        });
    }

    // Change User Status
    const statusChangeBtns = document.querySelectorAll('.change-status-btn');

    if (statusChangeBtns.length > 0) {
        statusChangeBtns.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                const userId = this.getAttribute('data-user-id');
                const newStatus = this.getAttribute('data-status');

                if (confirm(`Are you sure you want to change this user's status to ${newStatus}?`)) {
                    changeUserStatus(userId, newStatus);
                }
            });
        });
    }

    function changeUserStatus(userId, status) {
        // Get the token
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Create form data
        const formData = new FormData();
        formData.append('userId', userId);
        formData.append('status', status);
        formData.append('__RequestVerificationToken', token);

        // Send AJAX request
        fetch('/UserManagement/ChangeUserStatus', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showAlert('success', data.message || `User status changed to ${status} successfully`);
                    setTimeout(() => {
                        window.location.reload();
                    }, 1500);
                } else {
                    showAlert('danger', data.message || 'Failed to change user status');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showAlert('danger', 'An error occurred while changing user status');
            });
    }

    // Close Modals
    const closeModalBtns = document.querySelectorAll('.close-modal, .close-modal-btn');

    if (closeModalBtns.length > 0) {
        closeModalBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const modal = this.closest('.modal');
                if (modal) {
                    modal.classList.remove('show');
                    modal.style.display = 'none';
                }
            });
        });
    }

    // Outside click to close modals
    window.addEventListener('click', function (event) {
        const modals = document.querySelectorAll('.modal.show');
        modals.forEach(modal => {
            if (event.target === modal) {
                modal.classList.remove('show');
                modal.style.display = 'none';
            }
        });
    });

    // Toggle Password Visibility
    const togglePasswordBtns = document.querySelectorAll('.toggle-password');

    if (togglePasswordBtns.length > 0) {
        togglePasswordBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const passwordInput = this.previousElementSibling;
                const icon = this.querySelector('i');

                if (passwordInput.type === 'password') {
                    passwordInput.type = 'text';
                    icon.classList.remove('fa-eye');
                    icon.classList.add('fa-eye-slash');
                } else {
                    passwordInput.type = 'password';
                    icon.classList.remove('fa-eye-slash');
                    icon.classList.add('fa-eye');
                }
            });
        });
    }

    // Toggle role-specific fields based on selection (for AddUser form)
    const roleSelect = document.getElementById('role-select');
    if (roleSelect) {
        roleSelect.addEventListener('change', toggleRoleFields);

        // Initial call to set fields
        toggleRoleFields();
    }

    function toggleRoleFields() {
        const selectedRole = roleSelect.value;
        const staffFields = document.querySelector('.staff-fields');
        const homeownerFields = document.querySelector('.homeowner-fields');

        if (staffFields && homeownerFields) {
            if (selectedRole === 'Staff') {
                staffFields.style.display = 'block';
                homeownerFields.style.display = 'none';
            } else if (selectedRole === 'Homeowner') {
                staffFields.style.display = 'none';
                homeownerFields.style.display = 'block';
            } else {
                staffFields.style.display = 'none';
                homeownerFields.style.display = 'none';
            }
        }
    }

    // File upload handling
    const profileImageInput = document.getElementById('ProfileImage');
    const fileNameDisplay = document.getElementById('file-name');

    if (profileImageInput && fileNameDisplay) {
        profileImageInput.addEventListener('change', function () {
            const fileName = this.files[0]?.name || 'No file chosen';
            fileNameDisplay.textContent = fileName;
        });
    }

    const propertyDocsInput = document.getElementById('PropertyDocuments');
    const docsCountDisplay = document.getElementById('property-docs-count');

    if (propertyDocsInput && docsCountDisplay) {
        propertyDocsInput.addEventListener('change', function () {
            const fileCount = this.files.length;
            docsCountDisplay.textContent = fileCount ? `${fileCount} file(s) selected` : 'No files chosen';
        });
    }

    // Close Alert Messages
    const closeAlertBtns = document.querySelectorAll('.close-alert');

    if (closeAlertBtns.length > 0) {
        closeAlertBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const alert = this.closest('.alert');
                if (alert) {
                    alert.style.display = 'none';
                }
            });
        });
    }

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            alert.style.display = 'none';
        });
    }, 5000);

    // Helper function to show alerts
    function showAlert(type, message) {
        const alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
        const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';

        const alertHtml = `
            <div class="alert ${alertClass}">
                <i class="fas ${icon}"></i> ${message}
                <button class="close-alert"><i class="fas fa-times"></i></button>
            </div>
        `;

        // Find the page header to insert the alert after it
        const pageHeader = document.querySelector('.page-header');
        if (pageHeader) {
            pageHeader.insertAdjacentHTML('afterend', alertHtml);

            // Add event listener to the newly created close button
            const newAlert = pageHeader.nextElementSibling;
            const newCloseBtn = newAlert.querySelector('.close-alert');
            if (newCloseBtn) {
                newCloseBtn.addEventListener('click', function () {
                    newAlert.style.display = 'none';
                });
            }

            // Auto-hide this alert after 5 seconds
            setTimeout(function () {
                newAlert.style.display = 'none';
            }, 5000);
        }
    }
});