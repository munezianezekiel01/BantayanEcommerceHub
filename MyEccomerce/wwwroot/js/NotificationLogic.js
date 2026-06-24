// 3. Notification Logic

function updateUserNotifications() {
    fetch('/Orders/GetStatusUpdates')
        .then(res => res.json())
        .then(data => {
            const badge = document.getElementById('userNotifBadgeCount');
            const container = document.getElementById('userNotifItemsContainer');

            // 1. Update Badge Count
            if (badge) {
                badge.innerText = data.unreadCount;
                badge.style.display = (data.unreadCount > 0) ? 'block' : 'none';
            }

            container.innerHTML = '';

            // 2. Render Notifications
            if (data.updates && data.updates.length > 0) {
                data.updates.forEach(notif => {
                    const isReadClass = notif.isRead ? '' : 'bg-light';
                    const timeLabel = typeof timeAgo === 'function' ? timeAgo(notif.createdAt) : notif.createdAt;

                    // WARNING FIX: Siguroha nga husto ang ngalan sa ID gikan sa C# backend!
                    // Kung sa C# "NotificationId" ang ngalan sa column, usba kini ngadto sa notif.notificationId
                    const trueNotifId = notif.id || notif.notificationId;

                    // --- DYNAMIC COLLAGE LOGIC (FIXED SIZE) ---
                    let itemsGridHtml = '';
                    if (notif.orderItems && notif.orderItems.length > 0) {
                        const itemCount = notif.orderItems.length;
                        let gridTemplate = "";

                        if (itemCount === 1) {
                            gridTemplate = "grid-template-columns: 1fr; grid-template-rows: 1fr;";
                        } else if (itemCount === 2) {
                            gridTemplate = "grid-template-columns: 1fr 1fr; grid-template-rows: 1fr;";
                        } else if (itemCount === 3) {
                            gridTemplate = "grid-template-columns: 1.2fr 1fr; grid-template-rows: 1fr 1fr;";
                        } else {
                            gridTemplate = "grid-template-columns: 1fr 1fr; grid-template-rows: 1fr 1fr;";
                        }

                        let collageHtml = `
                                    <div class="notif-collage-box"
                                         style="display: grid; ${gridTemplate} gap: 1px; background: #fff; border-radius: 4px;
                                                overflow: hidden; border: 1px solid #dee2e6; width: 48px; height: 48px; flex-shrink: 0;">`;

                        notif.orderItems.slice(0, 4).forEach((item, index) => {
                            let gridStyle = (itemCount === 3 && index === 0) ? "grid-row: span 2;" : "";
                            collageHtml += `
                                        <img src="${item.itemImage || '/images/no-image.png'}"
                                             style="width: 100%; height: 100%; object-fit: cover; ${gridStyle}"
                                             alt="item">`;
                        });
                        collageHtml += `</div>`;

                        let namesHtml = `<div style="display: flex; flex-direction: column; gap: 0px; min-width: 0; flex-grow: 1; justify-content: center;">`;
                        notif.orderItems.slice(0, 2).forEach(item => {
                            namesHtml += `
                                        <div class="notif-item-name"
                                             style="font-size: 0.68rem; color: #495057; white-space: nowrap; overflow: hidden;
                                                    text-overflow: ellipsis; display: flex; align-items: center; gap: 4px;">
                                            <span style="color: #0d6efd;">•</span> ${item.itemName}
                                        </div>`;
                        });

                        if (notif.orderItems.length > 2) {
                            namesHtml += `<div style="font-size: 0.55rem; color: #adb5bd; font-style: italic; margin-left: 10px;">+ ${notif.orderItems.length - 2} more...</div>`;
                        }
                        namesHtml += `</div>`;

                        itemsGridHtml = `
                                    <div class="d-flex align-items-center gap-2 p-1"
                                         style="background: #f8f9fa; border: 1px dashed #ced4da; border-radius: 6px; margin-top: 5px;">
                                        ${collageHtml}
                                        ${namesHtml}
                                    </div>`;
                    }

                    // --- GI-AYO NGA RENDERING LOGIC (LI ATTACHMENT) ---
                    const li = document.createElement('li');
                    li.style.listStyle = 'none';

                    li.innerHTML = `
                        <a class="dropdown-item py-2 px-2 border-bottom ${isReadClass}"
                           href="javascript:void(0);"
                           style="white-space: normal; display: block;">

                            <div class="d-flex align-items-start gap-2">
                                <img src="https://ui-avatars.com/api/?name=Admin&background=0D6EFD&color=fff"
                                     class="rounded-circle mt-1" width="26" height="26" style="flex-shrink: 0;">

                                <div class="flex-grow-1" style="min-width: 0;">
                                    <div class="d-flex justify-content-between align-items-start">
                                        <div class="notif-msg-text fw-bold text-dark"
                                             style="line-height: 1.2; flex-grow: 1; font-size: 0.8rem; min-width: 0; overflow: hidden; text-overflow: ellipsis;">
                                            ${notif.message}
                                        </div>
                                        <span class="text-muted" style="font-size: 0.55rem; white-space: nowrap; margin-left: 8px; margin-top: 2px;">
                                            ${timeLabel}
                                        </span>
                                    </div>

                                    ${itemsGridHtml}

                                    <div class="d-flex justify-content-between align-items-center mt-1">
                                        <span class="text-muted" style="font-size: 0.6rem; letter-spacing: 0.3px;">#${notif.orderId}</span>
                                        <span class="badge rounded-pill bg-success-subtle text-success border border-success"
                                              style="font-size: 0.5rem; padding: 1px 6px;">
                                            ${notif.status.toUpperCase()}
                                        </span>
                                    </div>
                                </div>
                            </div>
                        </a>`;

                    // Paggamit og limpyo nga Event Listener aron dili ma-corrupt ang function click!
                    li.querySelector('a').addEventListener('click', function (e) {
                        e.preventDefault();
                        if (trueNotifId) {
                            markAsRead(trueNotifId);
                        } else {
                            console.error("Critical: Notification ID is missing/undefined in JSON response!", notif);
                        }
                    });

                    container.appendChild(li);
                });
            } else {
                container.innerHTML = '<li class="p-4 text-center text-muted small">Walay bag-ong update.</li>';
            }
        })
        .catch(err => console.error("Notif error:", err));
}

document.addEventListener('DOMContentLoaded', updateUserNotifications);


function markAsRead(notificationId) {
    console.log("Gisulayan pagtawag ang MarkAsRead para sa ID:", notificationId);

    fetch(`/Orders/MarkAsRead/${notificationId}`, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    })
        .then(res => {
            if (!res.ok) {
                throw new Error(`Nierror ang Server: ${res.status}`);
            }
            return res.json();
        })
        .then(data => {
            if (data.success) {
                updateUserNotifications();
                console.log(`Success! Notification #${notificationId} marked as read inside database.`);
            } else {
                console.error("Server refuesed update:", data.message);
            }
        })
        .catch(err => console.error("Error sa pagtawag sa AJAX:", err));
}