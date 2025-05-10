// File: wwwroot/js/admin.js

jQuery(function ($) {
    // Toggle sidebar
    $('#sidebar-toggle').on('click', function () {
        $('.wrapper').toggleClass('sidebar-collapsed');
    });

    // User dropdown menu
    $('.user-dropdown').on('click', function (e) {
        e.stopPropagation();
        $(this).find('.dropdown-menu').toggleClass('show');
    });

    // Close dropdown when clicking outside
    $(document).on('click', function () {
        $('.dropdown-menu').removeClass('show');
    });

    // Close alerts when clicking the X button
    $(document).on('click', '.close-alert', function () {
        $(this).parent().fadeOut(300, function () {
            $(this).remove();
        });
    });

    // Toggle password visibility
    $('.toggle-password').on('click', function () {
        const passwordInput = $(this).siblings('input');
        const passwordIcon = $(this).find('i');

        if (passwordInput.attr('type') === 'password') {
            passwordInput.attr('type', 'text');
            passwordIcon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            passwordInput.attr('type', 'password');
            passwordIcon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

    // Handle modal close
    $('.close-modal, .close-modal-btn').on('click', function () {
        $(this).closest('.modal').removeClass('show');
    });

    // File upload preview
    $('.file-upload').on('change', function () {
        const fileName = $(this).val().split('\\').pop();
        if (fileName) {
            $(this).siblings('.file-name').text(fileName);
        } else {
            $(this).siblings('.file-name').text('No file chosen');
        }
    });

    // Confirm delete actions
    $('#confirm-delete').on('click', function () {
        const userId = $(this).data('user-id');
        deleteUser(userId);
    });

    function deleteUser(userId) {
        $.ajax({
            url: '/Admin/UserManagement/DeleteUser',
            type: 'POST',
            data: {
                id: userId
            },
            success: function (response) {
                if (response.success) {
                    $('#delete-confirmation-modal').removeClass('show');
                    showAlert('success', 'User deleted successfully.');

                    // Reload page after successful action
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    showAlert('danger', response.message);
                }
            },
            error: function () {
                showAlert('danger', 'An error occurred while deleting the user.');
            }
        });
    }

    // Show alert message
    function showAlert(type, message) {
        const alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
        const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';

        const alertHtml = `
            <div class="alert ${alertClass}">
                <i class="fas ${icon}"></i> ${message}
                <button class="close-alert"><i class="fas fa-times"></i></button>
            </div>
        `;

        $('.page-header').after(alertHtml);

        // Auto hide alert after 5 seconds
        setTimeout(function () {
            $('.alert').fadeOut(300, function () {
                $(this).remove();
            });
        }, 5000);
    }
});