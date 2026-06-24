document.addEventListener('DOMContentLoaded', function () {
    const trigger = document.getElementById('mobileSearchTrigger');
    const close = document.getElementById('closeMobileSearch');
    const wrapper = document.getElementById('searchFormWrapper');
    const input = document.getElementById('searchInput');
    const clearBtn = document.getElementById('clearSearch');
    const suggestions = document.getElementById('searchSuggestions');

    // 1. OPEN SEARCH OVERLAY
    if (trigger) {
        trigger.addEventListener('click', function () {
            wrapper.classList.add('active');
            document.body.style.overflow = 'hidden'; // I-disable ang scroll sa luyo
            setTimeout(() => input.focus(), 200);
        });
    }

    // 2. CLOSE SEARCH OVERLAY
    if (close) {
        close.addEventListener('click', function () {
            wrapper.classList.remove('active');
            document.body.style.overflow = 'auto'; // I-enable balik ang scroll
            suggestions.style.display = 'none'; // Tagoan suggestions inig close
        });
    }

    // 3. X (CLEAR) BUTTON LOGIC
    function toggleClearButton() {
        if (input.value.length > 0) {
            clearBtn.style.display = 'block';
            if (window.innerWidth < 768) suggestions.style.display = 'block'; // Pakita suggestions sa mobile
        } else {
            clearBtn.style.display = 'none';
        }
    }

    input.addEventListener('input', toggleClearButton);

    clearBtn.addEventListener('click', function () {
        input.value = '';
        toggleClearButton();
        input.focus();
    });

    // Close overlay kung mapislit ang "Esc" key
    document.addEventListener('keydown', function (e) {
        if (e.key === "Escape" && wrapper.classList.contains('active')) {
            close.click();
        }
    });
});


