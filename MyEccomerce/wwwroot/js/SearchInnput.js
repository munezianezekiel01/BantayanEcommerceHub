
$(document).ready(function () {

    // Idugang kini sulod sa $(document).ready
    $("#searchInput").on("focus", function () {
        let term = $(this).val().trim();
        if (term.length >= 1) {
            $("#searchSuggestions").show();
        }
    });
    $("#searchInput").on("input", function () {
        let term = $(this).val().trim(); // Gi-add ang trim() para malimpyo ang space
        let container = $("#searchSuggestions");

        // Usba gikan sa 2 ngadto sa 1
        if (term.length < 1) {
            container.hide();
            return;
        }

        $.ajax({
            url: '/Products/GetSearchSuggestions',
            data: { term: term },
            success: function (data) {
                container.empty();
                if (data.length > 0) {
                    data.forEach(item => {
                        container.append(`
                                    <a href="/Product/Details/${item.id}" class="dropdown-item d-flex align-items-center p-2">
                                        <img src="${item.img || '/images/no-image.png'}" width="40" height="40" class="rounded me-2" style="object-fit: cover;">
                                        <div>
                                            <div class="small fw-bold text-dark text-truncate" style="max-width: 200px;">${item.name}</div>
                                            <div class="small text-primary">₱${parseFloat(item.price).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
                                        </div>
                                    </a>
                                `);
                    });
                    container.show();
                } else {
                    container.hide();
                }
            }
        });
    });


    // Isira ang dropdown kung mo-click sa gawas
    $(document).on("click", function (e) {
        if (!$(e.target).closest(".search-wrapper").length) {
            $("#searchSuggestions").hide();
        }
    });
});

const searchInput = document.getElementById('searchInput');

// 1. Kung i-click ang 'X' button, balik largo sa Home
function clearSearch() {
    window.location.href = '/Home';
}

// 2. Kung manual papason sa user ang text gamit ang Backspace
searchInput.addEventListener('input', function () {
    if (this.value.trim() === '') {
        // Inig kahanaw sa text, automatic mobalik sa tanan products
        window.location.href = '/Home';
    }
});

// 3. (Optional) Para inig 'Enter' mo-submit gihapon
searchInput.addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        if (this.value.trim() !== '') {
            this.form.submit();
        } else {
            window.location.href = '/Home';
        }
    }
});



