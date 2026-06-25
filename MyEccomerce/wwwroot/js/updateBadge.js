// 2. Cart Logic
function updateBadge() {
    fetch('/Cart/GetCartCount')
        .then(res => res.json())
        .then(data => {
            const count = (typeof data === 'object') ? data.count : data;
            const badge = document.getElementById('cartBadgeCount');
            if (badge) {
                badge.innerText = count;
                badge.style.display = (count > 0) ? 'block' : 'none';
            }
        }).catch(err => console.error("Error updating badge:", err));
}
