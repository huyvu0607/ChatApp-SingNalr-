// chat.js - Main JavaScript file for Chat Application

class ChatApp {
    constructor() {
        this.currentConversationId = null;
        this.currentUserId = null;
        this.messagePollingInterval = null;
        this.typingTimeout = null;
        this.searchTimeout = null; // ✅ THÊM
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.setupAutoResize();
        this.loadUserInfo();
    }

    // Setup event listeners
    setupEventListeners() {
        // Message input
        $(document).on('submit', '#messageForm', (e) => {
            e.preventDefault();
            this.sendMessage();
        });

        // Enter key to send (Shift+Enter for new line)
        $(document).on('keydown', '#messageInput', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                $('#messageForm').submit();
            }
        });

        // Toggle info panel
        $(document).on('click', '#toggleInfoBtn', () => {
            $('#infoPanel').toggleClass('show');
        });

        // Close info panel on mobile
        $(document).on('click', '.chat-panel', (e) => {
            if ($(window).width() < 1200 && !$(e.target).closest('#toggleInfoBtn').length) {
                $('#infoPanel').removeClass('show');
            }
        });

        // ✅ THAY ĐỔI: Search conversations → Search All
        $(document).on('input', '#searchInput', (e) => {
            this.handleSearchInput($(e.target).val());
        });

        // ✅ THÊM: Clear search khi click ngoài
        $(document).on('click', (e) => {
            if (!$(e.target).closest('.conversations-search').length) {
                const query = $('#searchInput').val().trim();
                if (query === '') {
                    $('#searchResults').hide();
                }
            }
        });

        // Conversation tabs
        $(document).on('click', '.tab-btn', function () {
            $('.tab-btn').removeClass('active');
            $(this).addClass('active');
            const tab = $(this).data('tab');
            chatApp.filterConversations(tab);
        });

        // Create group button
        $(document).on('click', '#createGroupBtn', () => {
            this.openCreateGroupModal();
        });

        // Attach files
        $(document).on('click', '#attachImageBtn', () => {
            this.attachImage();
        });

        $(document).on('click', '#attachFileBtn', () => {
            this.attachFile();
        });

        // Emoji picker
        $(document).on('click', '#emojiBtn', () => {
            this.showEmojiPicker();
        });

        // Voice message
        $(document).on('click', '#voiceBtn', () => {
            this.recordVoice();
        });

        // Call buttons
        $(document).on('click', '#callBtn', () => {
            this.makeCall('audio');
        });

        $(document).on('click', '#videoCallBtn', () => {
            this.makeCall('video');
        });

        // Search messages in conversation
        $(document).on('click', '#searchMsgBtn', () => {
            this.showSearchMessages();
        });

        // Window resize
        $(window).on('resize', () => {
            this.handleResize();
        });
    }

    // Auto-resize textarea
    setupAutoResize() {
        $(document).on('input', '#messageInput', function () {
            this.style.height = 'auto';
            this.style.height = (this.scrollHeight) + 'px';
        });
    }

    // Load user info
    loadUserInfo() {
        // Get current user ID from page
        const userId = $('body').data('user-id');
        if (userId) {
            this.currentUserId = userId;
        }

        // Get conversation ID if on conversation page
        const convId = $('#conversationId').val();
        if (convId) {
            this.currentConversationId = convId;
            this.loadConversation(convId);
        }
    }

    // ✅ NEW: Handle search input với debounce
    handleSearchInput(query) {
        clearTimeout(this.searchTimeout);

        query = query.trim();

        // Nếu rỗng → ẩn kết quả search
        if (query.length === 0) {
            $('#searchResults').hide().empty();
            return;
        }

        // Chỉ search khi >= 2 ký tự
        if (query.length < 2) {
            return;
        }

        // Debounce: đợi 300ms sau khi ngừng gõ
        this.searchTimeout = setTimeout(() => {
            this.performSearchAll(query);
        }, 300);
    }

    // ✅ NEW: Search cả users và conversations
    performSearchAll(query) {
        $.ajax({
            url: '/Chat/SearchAll',
            method: 'GET',
            data: { query: query },
            success: (response) => {
                if (response.success) {
                    this.renderSearchResults(response.data);
                } else {
                    console.error('Search failed:', response.message);
                }
            },
            error: (xhr, status, error) => {
                console.error('Search error:', error);
            }
        });
    }

    // ✅ NEW: Render kết quả search
    renderSearchResults(data) {
        const { users, conversations } = data;
        let html = '';

        // ✅ Hiển thị Users
        if (users && users.length > 0) {
            html += '<div class="search-section">';
            html += '<h4><i class="fas fa-user"></i> Người dùng</h4>';

            users.forEach(user => {
                const avatar = user.avatar && user.avatar !== '/images/default-avatar.png'
                    ? `<img src="${user.avatar}" alt="${this.escapeHtml(user.fullName)}" />`
                    : `<div class="default-avatar">${user.fullName[0].toUpperCase()}</div>`;

                const onlineIndicator = user.isOnline
                    ? '<span class="online-indicator"></span>'
                    : '';

                const actionBtn = this.getRelationshipButton(user);

                html += `
                    <div class="search-item" data-user-id="${user.userId}">
                        <div class="search-avatar">
                            ${avatar}
                            ${onlineIndicator}
                        </div>
                        <div class="search-info">
                            <div class="search-name">${this.escapeHtml(user.fullName)}</div>
                            <div class="search-username">@${this.escapeHtml(user.username)}</div>
                        </div>
                        ${actionBtn}
                    </div>
                `;
            });

            html += '</div>';
        }

        // ✅ Hiển thị Conversations
        if (conversations && conversations.length > 0) {
            html += '<div class="search-section">';
            html += '<h4><i class="fas fa-comments"></i> Cuộc hội thoại</h4>';

            conversations.forEach(conv => {
                const avatar = conv.isGroup
                    ? '<div class="default-avatar" style="background:linear-gradient(135deg,#f59e0b,#ef4444);"><i class="fas fa-users"></i></div>'
                    : `<div class="default-avatar">${conv.conversationName[0].toUpperCase()}</div>`;

                html += `
                    <a href="/Chat/Conversation/${conv.conversationId}" class="search-item">
                        <div class="search-avatar">
                            ${avatar}
                        </div>
                        <div class="search-info">
                            <div class="search-name">${this.escapeHtml(conv.conversationName)}</div>
                            <div class="search-username">${conv.isGroup ? 'Nhóm' : 'Cá nhân'}</div>
                        </div>
                        <i class="fas fa-chevron-right" style="color:#9ca3af;"></i>
                    </a>
                `;
            });

            html += '</div>';
        }

        // Không có kết quả
        if (html === '') {
            html = `
                <div class="no-results">
                    <i class="fas fa-search"></i>
                    <p>Không tìm thấy kết quả</p>
                </div>
            `;
        }

        $('#searchResults').html(html).show();
    }

    // ✅ NEW: Lấy nút action theo relationship
    getRelationshipButton(user) {
        switch (user.relationshipStatus) {
            case 'friend':
                return `<button class="btn-chat" onclick="gotoChatWithUser(${user.userId})">
                            <i class="fas fa-comment"></i> Nhắn tin
                        </button>`;

            case 'request_sent':
                return `<button class="btn-pending" disabled>
                            <i class="fas fa-clock"></i> Đã gửi
                        </button>`;

            case 'request_received':
                return `<button class="btn-accept" onclick="acceptFriendRequest(${user.userId})">
                            <i class="fas fa-check"></i> Chấp nhận
                        </button>`;

            default:
                return `<button class="btn-add-friend" onclick="sendFriendRequest(${user.userId})">
                            <i class="fas fa-user-plus"></i> Kết bạn
                        </button>`;
        }
    }

    // Load conversation
    loadConversation(conversationId) {
        this.currentConversationId = conversationId;
        this.scrollToBottom();
        this.markAsRead(conversationId);

        // Start polling for new messages (temporary solution before implementing SignalR)
        this.startMessagePolling();
    }

    // Send message
    sendMessage() {
        const messageText = $('#messageInput').val().trim();

        if (!messageText) return;

        const conversationId = this.currentConversationId || $('#conversationId').val();

        if (!conversationId) {
            alert('Không tìm thấy cuộc hội thoại!');
            return;
        }

        // Disable send button
        $('#sendBtn').prop('disabled', true);

        $.ajax({
            url: '/Chat/SendMessage',
            type: 'POST',
            data: {
                conversationId: conversationId,
                messageText: messageText,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    $('#messageInput').val('');
                    $('#messageInput').css('height', 'auto');

                    // Add message to UI
                    this.addMessageToUI({
                        messageId: response.messageId,
                        messageText: messageText,
                        sentAt: response.sentAt,
                        isSent: true
                    });

                    this.scrollToBottom();
                } else {
                    alert(response.message);
                }
            },
            error: () => {
                alert('Có lỗi xảy ra khi gửi tin nhắn!');
            },
            complete: () => {
                $('#sendBtn').prop('disabled', false);
                $('#messageInput').focus();
            }
        });
    }

    // Add message to UI
    addMessageToUI(msg) {
        const isSent = msg.isSent || false;
        const time = msg.sentAt || new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

        let html = `
            <div class="message-wrapper ${isSent ? 'sent' : 'received'}" data-message-id="${msg.messageId}">
                <div class="message-content">
                    <div class="message-bubble ${msg.isEdited ? 'edited' : ''}">
                        ${this.escapeHtml(msg.messageText)}
                    </div>
                    <div class="message-meta">
                        <span>${time}</span>
                        ${isSent ? '<i class="fas fa-check-double" style="color: #0068ff;"></i>' : ''}
                    </div>
                </div>
            </div>
        `;

        $('#messagesArea').append(html);
    }

    // Scroll to bottom
    scrollToBottom(smooth = true) {
        const messagesArea = document.getElementById('messagesArea');
        if (messagesArea) {
            if (smooth) {
                messagesArea.scrollTo({
                    top: messagesArea.scrollHeight,
                    behavior: 'smooth'
                });
            } else {
                messagesArea.scrollTop = messagesArea.scrollHeight;
            }
        }
    }

    // Mark conversation as read
    markAsRead(conversationId) {
        // This would typically update LastReadAt on the server
        console.log('Marking conversation as read:', conversationId);
    }

    // Search conversations (OLD - giữ lại cho backward compatibility)
    searchConversations(query) {
        const searchText = query.toLowerCase();

        $('.conversation-item').each(function () {
            const name = $(this).find('.conversation-name').text().toLowerCase();
            const lastMsg = $(this).find('.last-message').text().toLowerCase();

            if (name.includes(searchText) || lastMsg.includes(searchText)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    }

    // Filter conversations by tab
    filterConversations(tab) {
        $('.conversation-item').show();

        switch (tab) {
            case 'unread':
                $('.conversation-item').each(function () {
                    const unreadCount = parseInt($(this).find('.unread-badge').text() || 0);
                    if (unreadCount === 0) {
                        $(this).hide();
                    }
                });
                break;
            case 'groups':
                $('.conversation-item').each(function () {
                    const hasGroupIcon = $(this).find('.fa-users').length > 0;
                    if (!hasGroupIcon) {
                        $(this).hide();
                    }
                });
                break;
            case 'archived':
                // Load archived conversations
                this.loadArchivedConversations();
                break;
        }
    }

    // Load archived conversations
    loadArchivedConversations() {
        // TODO: Implement AJAX call to load archived conversations
        console.log('Loading archived conversations...');
    }

    // React to message
    reactToMessage(messageId, reactionType) {
        $.ajax({
            url: '/Chat/ReactMessage',
            type: 'POST',
            data: {
                messageId: messageId,
                reactionType: reactionType,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    // Update UI
                    location.reload(); // Temporary
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Edit message
    editMessage(messageId) {
        const messageWrapper = $(`.message-wrapper[data-message-id="${messageId}"]`);
        const currentText = messageWrapper.find('.message-bubble').text().trim();

        const newText = prompt('Chỉnh sửa tin nhắn:', currentText);

        if (newText && newText !== currentText) {
            $.ajax({
                url: '/Chat/EditMessage',
                type: 'POST',
                data: {
                    messageId: messageId,
                    newText: newText,
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: (response) => {
                    if (response.success) {
                        messageWrapper.find('.message-bubble')
                            .text(newText)
                            .addClass('edited');
                    } else {
                        alert(response.message);
                    }
                },
                error: () => {
                    alert('Có lỗi xảy ra!');
                }
            });
        }
    }

    // Delete message
    deleteMessage(messageId) {
        if (!confirm('Bạn có chắc muốn thu hồi tin nhắn này?')) return;

        $.ajax({
            url: '/Chat/DeleteMessage',
            type: 'POST',
            data: {
                messageId: messageId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    $(`.message-wrapper[data-message-id="${messageId}"]`)
                        .fadeOut(300, function () {
                            $(this).remove();
                        });
                } else {
                    alert(response.message);
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Pin/Unpin message
    pinMessage(messageId) {
        $.ajax({
            url: '/Chat/PinMessage',
            type: 'POST',
            data: {
                messageId: messageId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    alert(response.message);
                    location.reload();
                } else {
                    alert(response.message);
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Save message
    saveMessage(messageId, note = null) {
        $.ajax({
            url: '/Chat/SaveMessage',
            type: 'POST',
            data: {
                messageId: messageId,
                note: note,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    alert(response.message);
                } else {
                    alert(response.message);
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Pin/Unpin conversation
    pinConversation(conversationId) {
        $.ajax({
            url: '/Chat/PinConversation',
            type: 'POST',
            data: {
                conversationId: conversationId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    location.reload();
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Archive conversation
    archiveConversation(conversationId) {
        if (!confirm('Bạn có chắc muốn lưu trữ hội thoại này?')) return;

        $.ajax({
            url: '/Chat/ArchiveConversation',
            type: 'POST',
            data: {
                conversationId: conversationId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    window.location.href = '/Chat/Index';
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Delete conversation
    deleteConversation(conversationId) {
        if (!confirm('Bạn có chắc muốn xóa hội thoại này?')) return;

        $.ajax({
            url: '/Chat/DeleteConversation',
            type: 'POST',
            data: {
                conversationId: conversationId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    window.location.href = '/Chat/Index';
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Open create group modal
    openCreateGroupModal() {
        $.get('/Chat/GetFriendsForGroup', function (html) {
            $('body').append(html);
            openCreateGroupModal(); // Defined in modal partial
        });
    }

    // Attach image
    attachImage() {
        // TODO: Implement file upload
        alert('Chức năng gửi ảnh - Coming soon!');
    }

    // Attach file
    attachFile() {
        // TODO: Implement file upload
        alert('Chức năng gửi file - Coming soon!');
    }

    // Show emoji picker
    showEmojiPicker() {
        // TODO: Implement emoji picker
        alert('Chức năng chọn emoji - Coming soon!');
    }

    // Record voice message
    recordVoice() {
        // TODO: Implement voice recording
        alert('Chức năng ghi âm - Coming soon!');
    }

    // Make call
    makeCall(type) {
        alert(`Chức năng gọi ${type === 'video' ? 'video' : 'thoại'} - Coming soon!`);
    }

    // Show search messages
    showSearchMessages() {
        const query = prompt('Tìm kiếm tin nhắn:');
        if (query) {
            this.searchMessages(query);
        }
    }

    // Search messages
    searchMessages(query) {
        $.ajax({
            url: '/Chat/SearchMessages',
            type: 'GET',
            data: {
                conversationId: this.currentConversationId,
                query: query
            },
            success: (response) => {
                if (response.success) {
                    // Display search results
                    console.log('Search results:', response.messages);
                    // TODO: Show in UI
                } else {
                    alert(response.message);
                }
            },
            error: () => {
                alert('Có lỗi xảy ra!');
            }
        });
    }

    // Start polling for new messages (temporary before SignalR)
    startMessagePolling() {
        // Clear existing interval
        if (this.messagePollingInterval) {
            clearInterval(this.messagePollingInterval);
        }

        // Poll every 3 seconds
        this.messagePollingInterval = setInterval(() => {
            this.checkNewMessages();
        }, 3000);
    }

    // Stop message polling
    stopMessagePolling() {
        if (this.messagePollingInterval) {
            clearInterval(this.messagePollingInterval);
            this.messagePollingInterval = null;
        }
    }

    // Check for new messages
    checkNewMessages() {
        // TODO: Implement AJAX call to check for new messages
    }

    // Handle window resize
    handleResize() {
        const width = $(window).width();

        // Auto-hide info panel on mobile
        if (width < 1200) {
            $('#infoPanel').removeClass('show');
        }
    }

    // Escape HTML
    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }
}

// Initialize app when document is ready
let chatApp;
$(document).ready(function () {
    chatApp = new ChatApp();
});

// ✅ Global functions for search results
function sendFriendRequest(userId) {
    $.ajax({
        url: '/Chat/SendFriendRequest',
        method: 'POST',
        data: {
            receiverId: userId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert(response.message);
                // Refresh search results
                $('#searchInput').trigger('input');
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('Có lỗi xảy ra!');
        }
    });
}

function acceptFriendRequest(senderId) {
    $.ajax({
        url: '/Chat/AcceptFriendRequest',
        method: 'POST',
        data: {
            senderId: senderId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert(response.message);
                // Reload page to show new conversation
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('Có lỗi xảy ra!');
        }
    });
}

function gotoChatWithUser(userId) {
    // Redirect to chat index, conversation will be in the list
    window.location.href = '/Chat/Index';
}

// Global functions for inline onclick handlers
function reactMessage(messageId, reactionType) {
    chatApp.reactToMessage(messageId, reactionType);
}

function editMessage(messageId) {
    chatApp.editMessage(messageId);
}

function deleteMessage(messageId) {
    chatApp.deleteMessage(messageId);
}

function pinMessage(messageId) {
    chatApp.pinMessage(messageId);
}

function saveMessage(messageId, note) {
    chatApp.saveMessage(messageId, note);
}

function pinConversation(conversationId) {
    chatApp.pinConversation(conversationId);
}

function archiveConversation(conversationId) {
    chatApp.archiveConversation(conversationId);
}

function deleteConversation(conversationId) {
    chatApp.deleteConversation(conversationId);
}

function scrollToMessage(messageId) {
    const messageElement = $(`.message-wrapper[data-message-id="${messageId}"]`);
    if (messageElement.length) {
        messageElement[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
        messageElement.addClass('highlight');
        setTimeout(() => messageElement.removeClass('highlight'), 2000);
    }
}