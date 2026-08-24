// 1. Initialize Components
const cartSidebarEl = document.getElementById('cartSidebar');
const cartSidebar = cartSidebarEl ? new bootstrap.Offcanvas(cartSidebarEl) : null;

document.addEventListener('DOMContentLoaded', () => {
    updateBadge();
    updateUserNotifications();
    setInterval(updateUserNotifications, 30000);

    if (document.getElementById('cartTrigger')) {
        document.getElementById('cartTrigger').addEventListener('click', () => cartSidebar && cartSidebar.show());
    }
    if (cartSidebarEl) {
        cartSidebarEl.addEventListener('show.bs.offcanvas', loadSidebarContent);
    }
});

function loadSidebarContent() {
    const container = document.getElementById('cartItemsContainer');
    const totalAmountSpan = document.getElementById('cartTotalAmount');
    const checkoutBtn = document.getElementById('checkoutBtn');

    fetch('/Cart/GetCartItems')
        .then(res => res.json())
        .then(data => {
            container.innerHTML = '';
            let runningTotal = 0;

            if (!data || data.length === 0) {
                container.innerHTML = '<p class="text-center text-muted mt-5 small">Walay sulod imong cart.</p>';
                if (checkoutBtn) checkoutBtn.disabled = true;
                if (totalAmountSpan) totalAmountSpan.innerText = "₱0.00";
                return;
            }

            if (checkoutBtn) checkoutBtn.disabled = false;

            // Map and Render Cart Items
            container.innerHTML = data.map(item => {
                let price = parseFloat(item.price ?? item.Price ?? 0);
                let qty = parseFloat(item.quantity ?? item.Quantity ?? 0);
                let cartId = item.cartId || item.CartId;
                runningTotal += (price * qty);
                let qtyDisplay = (qty % 1 !== 0) ? qty.toFixed(1) : qty;

                return `
                    <div class="d-flex align-items-center gap-2 mb-3 small border-bottom pb-2 px-3" id="cart-item-${cartId}">
                        <img src="${item.imageUrl || item.ImageUrl || '/images/no-image.png'}" width="45" height="45" class="rounded border object-fit-cover">
                        <div class="flex-grow-1">
                            <div class="fw-bold text-truncate" style="max-width: 140px;">${item.name || item.Name}</div>
                            <div class="text-primary small">₱${price.toLocaleString(undefined, { minimumFractionDigits: 2 })} x ${qtyDisplay}</div>
                        </div>

                        <div class="input-group input-group-sm" style="width: 85px; border: 1px solid #ddd; border-radius: 6px; overflow: hidden;">
                            <button class="btn btn-light btn-sm border-0 minus-btn" data-id="${cartId}">&minus;</button>
                            <input type="text" id="qty-${cartId}" class="form-control text-center border-0 bg-white fw-bold small" value="${qtyDisplay}">
                            <button class="btn btn-light btn-sm border-0 plus-btn" data-id="${cartId}">&plus;</button>
                        </div>                              

                        <button onclick="removeItem(${cartId})" class="btn text-danger p-0 shadow-none"><i class="bi bi-trash"></i></button>
                    </div>`;
            }).join('');

            // Attach Event Listeners dynamic base sa data-id
            container.querySelectorAll('.minus-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const id = e.currentTarget.getAttribute('data-id');
                    updateQuantity(id, -1);
                });
            });

            container.querySelectorAll('.plus-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const id = e.currentTarget.getAttribute('data-id');
                    updateQuantity(id, 1);
                });
            });

            if (totalAmountSpan) {
                totalAmountSpan.innerText = "₱" + runningTotal.toLocaleString(undefined, { minimumFractionDigits: 2 });
            }
        })
        .catch(err => console.error("Error fetching cart items:", err));
}

// Function para sa Quantity Updates
function updateQuantity(cartId, change) {
    const qtyInput = document.getElementById(`qty-${cartId}`);
    if (!qtyInput) return;

    let currentQty = parseFloat(qtyInput.value) || 1;
    let newQty = currentQty + change;

    if (newQty < 1) return;

    const formData = new FormData();
    formData.append('cartId', cartId);
    formData.append('newQty', newQty);

    fetch('/Cart/UpdateQuantity', { method: 'POST', body: formData })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                qtyInput.value = (newQty % 1 !== 0) ? newQty.toFixed(1) : newQty;
                loadSidebarContent(); // Re-render content ug totals
                if (typeof updateBadge === "function") updateBadge();
            } else {
                Swal.fire('Error', data.message || 'Failed to update quantity', 'error');
            }
        })
        .catch(err => console.error("Error updating quantity:", err));
}

function removeItem(id) {
    const params = new URLSearchParams();
    params.append('id', id);
    fetch('/Cart/RemoveItem', { method: 'POST', body: params })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                loadSidebarContent();
                if (typeof updateBadge === "function") updateBadge();
            }
        })
        .catch(err => console.error("Error removing item:", err));
}

function goToCheckout() {
    window.location.href = '/Cart/Checkout';
}