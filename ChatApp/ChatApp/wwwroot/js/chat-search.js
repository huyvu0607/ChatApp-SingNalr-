// ========== SHARED SEARCH FUNCTIONS ==========
// File: wwwroot/js/chat-search.js
// Dùng chung cho Index.cshtml và Conversation.cshtml

let searchTimeout;

// ===== INITIALIZE SEARCH =====
function initializeSearch() {
    console.log('🔍 Initializing search...');

    // Search input handler
    $('#searchInput').off('input').on('input', function () {
        const query = $(this).val().trim();

        clearTimeout(searchTimeout);

        if (query.length >= 2) {
            $('#searchResults').show();
            $('#conversationsList').hide();

            searchTimeout = setTimeout(function () {
                searchAll(query);
            }, 500);
        } else {
            $('#searchResults').hide();
            $('#conversationsList').show();
        }
    });

    console.log('✅ Search initialized');
}

// ===== SEARCH ALL (Users + Conversations) =====
function searchAll(query) {
    console.log('🔎 Searching all:', query);

    $.ajax({
        url: '/Chat/SearchAll',
        type: 'GET',
        data: { query: query },
        success: function (response) {
            console.log('✅ Search response:', response);

            if (response.success) {
                displaySearchAllResults(response.data);
            } else {
                $('#searchResults').html(`
                    <div style="padding: 20px; text-align: center; color: #9ca3af;">
                        Không tìm thấy kết quả nào!
                    </div>
                `);
            }
        },
        error: function (xhr, status, error) {
            console.error('❌ Search error:', error);
            $('#searchResults').html(`
                <div style="padding: 20px; text-align: center; color: #ef4444;">
                    Có lỗi xảy ra khi tìm kiếm!
                </div>
            `);
        }
    });
}

// ===== DISPLAY SEARCH ALL RESULTS (Users + Conversations) =====
function displaySearchAllResults(data) {
    let html = '';

    // ✅ SECTION: NGƯỜI DÙNG
    if (data.users && data.users.length > 0) {
        html += `
            <div style="padding: 12px 16px; font-size: 11px; font-weight: 600; color: #6b7280; 
                        text-transform: uppercase; letter-spacing: 0.5px; background: #f9fafb; 
                        border-bottom: 1px solid #e5e7eb;">
                <i class="fas fa-user" style="margin-right: 6px;"></i> NGƯỜI DÙNG
            </div>
        `;

        data.users.forEach(function (user) {
            let buttonClass = '';
            let buttonText = '';
            let buttonAction = '';

            // ✅ Xác định button dựa vào relationship status
            switch (user.relationshipStatus) {
                case 'friend':
                    buttonClass = 'success';
                    buttonText = '<i class="fas fa-comment"></i> Nhắn tin';
                    buttonAction = `onclick="openConversationWithFriend(${user.userId})"`;
                    break;
                case 'request_sent':
                    buttonClass = 'secondary';
                    buttonText = 'Hủy lời mời';
                    buttonAction = `onclick="cancelFriendRequest(${user.userId})"`;
                    break;
                case 'request_received':
                    buttonClass = 'warning';
                    buttonText = 'Chấp nhận';
                    buttonAction = `onclick="acceptFriendRequest(${user.userId})"`;
                    break;
                default:
                    buttonClass = 'primary';
                    buttonText = '<i class="fas fa-user-plus"></i> Kết bạn';
                    buttonAction = `onclick="sendFriendRequest(${user.userId})"`;
            }

            // Avatar
            let avatarHtml = '';
            if (user.avatar && user.avatar !== '/images/default-avatar.png') {
                avatarHtml = `<img src="${user.avatar}" alt="${escapeHtml(user.fullName || user.username)}">`;
            } else {
                let initial = user.fullName ? user.fullName.charAt(0).toUpperCase() : 'U';
                avatarHtml = `<div class="default-avatar">${initial}</div>`;
            }

            html += `
                <div class="search-result-item">
                    <div class="search-result-avatar">
                        ${avatarHtml}
                        ${user.isOnline ? '<span class="online-indicator"></span>' : ''}
                    </div>
                    <div class="search-result-info">
                        <div class="search-result-name">${escapeHtml(user.fullName || user.username)}</div>
                        <div class="search-result-email">@${escapeHtml(user.username || 'user')}</div>
                    </div>
                    <div class="search-result-action">
                        <button class="add-friend-btn ${buttonClass}" ${buttonAction} data-user-id="${user.userId}">
                            ${buttonText}
                        </button>
                    </div>
                </div>
            `;
        });
    }

    // ✅ SECTION: CUỘC HỘI THOẠI
    if (data.conversations && data.conversations.length > 0) {
        html += `
            <div style="padding: 12px 16px; font-size: 11px; font-weight: 600; color: #6b7280; 
                        text-transform: uppercase; letter-spacing: 0.5px; background: #f9fafb; 
                        border-bottom: 1px solid #e5e7eb; margin-top: ${data.users && data.users.length > 0 ? '8px' : '0'};">
                <i class="fas fa-comments" style="margin-right: 6px;"></i> CUỘC HỘI THOẠI
            </div>
        `;

        data.conversations.forEach(function (conv) {
            let avatarHtml = '';

            if (conv.isGroup) {
                // Nhóm - dùng icon users
                avatarHtml = `
                    <div class="default-avatar" style="background: linear-gradient(135deg, #f59e0b 0%, #ef4444 100%);">
                        <i class="fas fa-users" style="font-size: 18px;"></i>
                    </div>
                `;
            } else {
                // 1-1 chat
                let initial = conv.conversationName.charAt(0).toUpperCase();
                avatarHtml = `<div class="default-avatar">${initial}</div>`;
            }

            html += `
                <a href="/Chat/Conversation/${conv.conversationId}" class="search-result-item" style="text-decoration: none; color: inherit;">
                    <div class="search-result-avatar">
                        ${avatarHtml}
                    </div>
                    <div class="search-result-info">
                        <div class="search-result-name">${escapeHtml(conv.conversationName)}</div>
                        <div class="search-result-email">${conv.isGroup ? 'Nhóm' : 'Trò chuyện'}</div>
                    </div>
                    <div class="search-result-action">
                        <i class="fas fa-chevron-right" style="color: #9ca3af; font-size: 14px;"></i>
                    </div>
                </a>
            `;
        });
    }

    // ✅ Nếu không có kết quả nào
    if (!html) {
        html = `
            <div style="padding: 40px 20px; text-align: center; color: #9ca3af;">
                <i class="fas fa-search" style="font-size: 48px; margin-bottom: 12px; color: #d1d5db;"></i>
                <p style="font-size: 14px; font-weight: 500; color: #6b7280; margin-bottom: 4px;">Không tìm thấy kết quả</p>
                <p style="font-size: 13px;">Thử tìm kiếm với từ khóa khác</p>
            </div>
        `;
    }

    $('#searchResults').html(html);
}

// ===== OPEN CONVERSATION WITH FRIEND =====
function openConversationWithFriend(userId) {
    console.log('💬 Opening conversation with user:', userId);

    // Tìm conversation với user này trong danh sách
    $.ajax({
        url: '/Chat/GetConversationWithUser',
        type: 'GET',
        data: { userId: userId },
        success: function (response) {
            if (response.success && response.conversationId) {
                window.location.href = '/Chat/Conversation/' + response.conversationId;
            } else {
                alert('Không tìm thấy cuộc hội thoại!');
            }
        },
        error: function () {
            alert('Có lỗi xảy ra!');
        }
    });
}

// ===== SEND FRIEND REQUEST =====
function sendFriendRequest(userId) {
    console.log('📤 Sending friend request to:', userId);

    $.ajax({
        url: '/Chat/SendFriendRequest',
        type: 'POST',
        data: {
            receiverId: userId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert(response.message);
                updateButtonState(userId, response.relationshipStatus);
            } else {
                alert(response.message);
            }
        },
        error: function (xhr, status, error) {
            console.error('❌ Send friend request error:', error);
            alert('Có lỗi xảy ra!');
        }
    });
}

// ===== CANCEL FRIEND REQUEST =====
function cancelFriendRequest(userId) {
    if (!confirm('Bạn có chắc muốn hủy lời mời kết bạn?')) return;

    console.log('❌ Cancelling friend request to:', userId);

    $.ajax({
        url: '/Chat/CancelFriendRequest',
        type: 'POST',
        data: {
            receiverId: userId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert(response.message);
                updateButtonState(userId, response.relationshipStatus);
            }
        },
        error: function (xhr, status, error) {
            console.error('❌ Cancel friend request error:', error);
            alert('Có lỗi xảy ra!');
        }
    });
}

// ===== ACCEPT FRIEND REQUEST =====
function acceptFriendRequest(userId) {
    console.log('✅ Accepting friend request from:', userId);

    $.ajax({
        url: '/Chat/AcceptFriendRequest',
        type: 'POST',
        data: {
            senderId: userId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert(response.message);
                // Reload để cập nhật conversations list
                location.reload();
            }
        },
        error: function (xhr, status, error) {
            console.error('❌ Accept friend request error:', error);
            alert('Có lỗi xảy ra!');
        }
    });
}

// ===== UPDATE BUTTON STATE =====
function updateButtonState(userId, status) {
    console.log('🔄 Updating button state for user:', userId, 'to:', status);

    let button = $(`.add-friend-btn[data-user-id="${userId}"]`);
    button.removeClass('primary secondary success warning');

    switch (status) {
        case 'friend':
            button.addClass('success')
                .html('<i class="fas fa-comment"></i> Nhắn tin')
                .attr('onclick', `openConversationWithFriend(${userId})`);
            break;
        case 'request_sent':
            button.addClass('secondary')
                .html('Hủy lời mời')
                .attr('onclick', `cancelFriendRequest(${userId})`);
            break;
        case 'none':
            button.addClass('primary')
                .html('<i class="fas fa-user-plus"></i> Kết bạn')
                .attr('onclick', `sendFriendRequest(${userId})`);
            break;
    }
}

// ===== UTILITY: ESCAPE HTML =====
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}