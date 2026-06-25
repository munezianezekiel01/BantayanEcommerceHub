// 1. Initialize Components
const cartSidebarEl = document.getElementById('cartSidebar');
const cartSidebar = cartSidebarEl ? new bootstrap.Offcanvas(cartSidebarEl) : null;

document.addEventListener('DOMContentLoaded', () => {
    updateBadge();
    updateUserNotifications();
    setInterval(updateUserNotifications, 30000);

    if (document.getElementById('cartTrigger')) {
        document.getElementById('cartTrigger').addEventListener('click', () => cartSidebar.show());
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

            data.forEach(item => {
                let price = parseFloat(item.price ?? item.Price ?? 0);
                let qty = parseFloat(item.quantity ?? item.Quantity ?? 0);
                runningTotal += (price * qty);
                let qtyDisplay = (qty % 1 !== 0) ? qty.toFixed(1) : qty;

                container.innerHTML += `
                            <div class="d-flex align-items-center gap-2 mb-3 small border-bottom pb-2 px-3">
                                <img src="${item.imageUrl || item.ImageUrl || '/images/no-image.png'}" width="45" height="45" class="rounded border object-fit-cover">
                                <div class="flex-grow-1">
                                    <div class="fw-bold text-truncate" style="max-width: 140px;">${item.name || item.Name}</div>
                                    <div class="text-primary small">₱${price.toLocaleString(undefined, { minimumFractionDigits: 2 })} x ${qtyDisplay}</div>
                                </div>
                                <button onclick="removeItem(${item.cartId || item.CartId})" class="btn text-danger p-0 shadow-none"><i class="bi bi-trash"></i></button>
                            </div>`;
            });

            if (totalAmountSpan) {
                totalAmountSpan.innerText = "₱" + runningTotal.toLocaleString(undefined, { minimumFractionDigits: 2 });
            }
        });
}

function removeItem(id) {
    const params = new URLSearchParams();
    params.append('id', id);
    fetch('/Cart/RemoveItem', { method: 'POST', body: params })
        .then(res => res.json())
        .then(data => {
            if (data.success) { loadSidebarContent(); updateBadge(); }
        });
}

function goToCheckout() { window.location.href = '/Cart/Checkout'; }