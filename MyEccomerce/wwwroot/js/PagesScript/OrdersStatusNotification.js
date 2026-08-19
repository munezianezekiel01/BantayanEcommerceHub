document.addEventListener("DOMContentLoaded", function () {
    loadAllNotifications();
});

async function loadAllNotifications() {
    const container = document.getElementById('notification-full-list');
    const badgeContainer = document.getElementById('unread-badge-container');

    if (!container) {
        console.error("Target container '#notification-full-list' not found.");
        return;
    }

    try {
        const response = await fetch('/Orders/GetStatusUpdates');
        if (!response.ok) throw new Error('Dili ma-reach ang server.');

        const data = await response.json();
        container.innerHTML = '';

        const rawUpdates = data.updates || [];

        // 1. Sort: Pinakabag-o nga notification ang naa sa ibabaw
        const updates = rawUpdates.sort((a, b) => {
            return new Date(b.createdAt) - new Date(a.createdAt);
        });

        // 2. Update Badge Counter
        const unreadCount = updates.filter(n => !n.isRead).length;
        if (badgeContainer) {
            if (unreadCount > 0) {
                badgeContainer.innerHTML = `<span class="badge bg-danger rounded-pill fw-medium responsive-meta">${unreadCount} Unread</span>`;
            } else {
                badgeContainer.innerHTML = '';
            }
        }

        // 3. Empty State Handling
        if (updates.length === 0) {
            container.innerHTML = `
                <div class="text-center py-5 bg-white border-0">
                    <i class="bi bi-bell-slash text-muted mb-3" style="font-size: clamp(2rem, 4vw + 1.5rem, 2.8rem); display: block;"></i>
                    <p class="text-muted responsive-meta mb-3">Walay bag-ong notifications sa pagkakaron.</p>
                    <a href="/" class="btn btn-primary btn-sm px-4 rounded-pill responsive-meta">Shop Now</a>
                </div>`;
            return;
        }

        // 4. Render Row Items
        const cardsHtml = updates.map(notif => {
            // Highlighting class ug New Badge indicator
            const isUnread = !notif.isRead;
            const unreadClass = isUnread ? 'notif-unread-bg' : '';
            const unreadDot = isUnread ? `<span class="badge bg-primary rounded-pill ms-2 style="font-size: 0.65rem;">NEW</span>` : '';

            const orderItems = notif.orderItems || [];

            let gridClass = 'grid-4-items';
            if (orderItems.length === 1) gridClass = 'grid-1-item';
            else if (orderItems.length === 2) gridClass = 'grid-2-items';
            else if (orderItems.length === 3) gridClass = 'grid-3-items';

            let imagesHtml = '';
            orderItems.slice(0, 4).forEach(item => {
                imagesHtml += `<img src="${item.itemImage || '/images/no-image.png'}" class="notif-img" alt="Product Image">`;
            });

            const firstItem = orderItems[0]?.itemName || "Product";
            const extraCount = orderItems.length - 1;
            const itemsSummary = extraCount > 0 ? `${firstItem} + ${extraCount} pa ka item` : firstItem;

            let statusColorClass = 'status-default';
            const statusLower = (notif.status || '').toLowerCase();
            if (statusLower.includes('deliv') || statusLower.includes('nadawat')) {
                statusColorClass = 'status-delivered';
            } else if (statusLower.includes('ship') || statusLower.includes('byahe') || statusLower.includes('out')) {
                statusColorClass = 'status-shipped';
            } else if (statusLower.includes('pend') || statusLower.includes('hulat') || statusLower.includes('confirm')) {
                statusColorClass = 'status-pending';
            } else if (statusLower.includes('cancel') || statusLower.includes('khina')) {
                statusColorClass = 'status-cancelled';
            }

            return `
                <a href="/Orders/OrderDetails/${notif.orderId}" class="list-group-item notif-item p-3 ${unreadClass}">
                    <div class="d-flex align-items-start gap-2 gap-sm-3">

                        <!-- Product Image Collage -->
                        <div class="notif-collage-wrapper ${gridClass}">
                            ${imagesHtml}
                        </div>

                        <!-- Content Details -->
                        <div class="flex-grow-1" style="min-width: 0;">
                            <div class="d-flex justify-content-between align-items-start notif-header-row mb-1">
                                <h6 class="mb-0 text-dark text-truncate fw-bold notif-title" style="max-width: 80%;">
                                    ${escapeHtml(notif.message || 'Order Update')}
                                    ${unreadDot}
                                </h6>
                                <small class="text-muted flex-shrink-0 notif-time responsive-meta">
                                    ${timeAgo(notif.createdAt)}
                                </small>
                            </div>

                            <div class="d-flex align-items-center gap-2 mb-1 notif-meta-row flex-wrap responsive-meta">
                                <span class="text-secondary">Order ID: <span class="fw-bold text-dark">#${notif.orderId}</span></span>
                                <span class="dot-separator text-muted">•</span>
                                <span class="notif-status-badge ${statusColorClass}">${escapeHtml(notif.status || 'Updated')}</span>
                            </div>

                            <div class="d-flex align-items-center text-muted responsive-meta">
                                <i class="bi bi-bag me-1" style="font-size: 0.8rem;"></i>
                                <span class="text-truncate" style="max-width: 100%;">${escapeHtml(itemsSummary)}</span>
                            </div>
                        </div>

                    </div>
                </a>
            `;
        }).join('');

        container.innerHTML = cardsHtml;

    } catch (error) {
        console.error("Error loading notifications:", error);
        container.innerHTML = `
            <div class="text-center py-5 bg-white">
                <p class="text-danger responsive-meta mb-2">Dili ma-load ang imong updates sa pagkakaron.</p>
                <button class="btn btn-outline-primary btn-sm px-3 rounded-pill responsive-meta" onclick="loadAllNotifications()">Sulayi Pag-usab</button>
            </div>`;
    }
}

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function timeAgo(dateString) {
    const now = new Date();
    const past = new Date(dateString);
    const diffMs = now - past;
    if (isNaN(diffMs)) return "Kadiyot lang";

    const diffMins = Math.round(diffMs / 60000);
    const diffHrs = Math.round(diffMins / 60);
    const diffDays = Math.round(diffHrs / 24);

    if (diffMins < 1) return `Karon lang`;
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHrs < 24) return `${diffHrs}h ago`;
    return `${diffDays}d ago`;
}