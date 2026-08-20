const PharmacyCart = {
    key: 'medlink_cart_v1',
    items: [],

    init() {
        const stored = localStorage.getItem(this.key);
        if (stored) {
            this.items = JSON.parse(stored);
        }
        this.updateBadge();
        this.renderDrawer();
    },

    addItem(medicine) {
        const existing = this.items.find(i => i.id === medicine.id);
        if (existing) {
            existing.quantity += 1;
        } else {
            this.items.push({ ...medicine, quantity: 1 });
        }
        this.save();
        this.updateBadge();
        this.renderDrawer();
        this.open(); // Auto-open on add

        // Show simplified toast/feedback if needed, but drawer open is enough feedback
    },

    removeItem(id) {
        this.items = this.items.filter(i => i.id !== id);
        this.save();
        this.updateBadge();
        this.renderDrawer();
    },

    updateQuantity(id, delta) {
        const item = this.items.find(i => i.id === id);
        if (item) {
            item.quantity += delta;
            if (item.quantity <= 0) {
                this.removeItem(id);
            } else {
                this.save();
                this.updateBadge();
                this.renderDrawer();
            }
        }
    },

    save() {
        localStorage.setItem(this.key, JSON.stringify(this.items));
    },

    updateBadge() {
        const count = this.items.reduce((sum, i) => sum + i.quantity, 0);
        const badges = document.querySelectorAll('.cart-badge');
        badges.forEach(el => {
            if (count > 0) {
                el.innerText = count;
                el.classList.remove('hidden');
            } else {
                el.classList.add('hidden');
            }
        });
    },

    calculateTotal() {
        return this.items.reduce((sum, i) => sum + (i.price * i.quantity), 0);
    },

    renderDrawer() {
        const container = document.getElementById('cartItemsContainer');
        const totalEl = document.getElementById('cartTotalDisplay');
        const countEl = document.getElementById('cartDrawerCount');
        const emptyState = document.getElementById('cartEmptyState');
        const footer = document.getElementById('cartFooter');

        if (!container) return; // Guard if drawer not present

        const count = this.items.reduce((sum, i) => sum + i.quantity, 0);
        if (countEl) countEl.innerText = count + ' Items';

        if (this.items.length === 0) {
            container.innerHTML = '';
            if (emptyState) emptyState.classList.remove('hidden');
            if (footer) footer.classList.add('hidden');
            return;
        }

        if (emptyState) emptyState.classList.add('hidden');
        if (footer) footer.classList.remove('hidden');

        container.innerHTML = this.items.map(item => `
            <div class="flex gap-4 p-4 bg-slate-50 rounded-2xl border border-slate-100 group hover:border-blue-100 transition-colors">
                <div class="w-16 h-16 bg-white rounded-xl flex items-center justify-center border border-slate-100 overflow-hidden flex-shrink-0">
                    ${item.imageUrl && item.imageUrl !== '/images/medicines/default.png'
                ? `<img src="${item.imageUrl}" class="w-full h-full object-cover">`
                : `<i data-lucide="pill" class="text-slate-300 w-6 h-6"></i>`}
                </div>
                <div class="flex-1 min-w-0">
                    <h4 class="font-bold text-slate-900 text-sm truncate">${item.name}</h4>
                    <p class="text-[10px] text-slate-400 font-bold uppercase tracking-widest mb-2">${item.brand}</p>
                    <div class="flex items-center justify-between">
                        <span class="font-black text-emerald-600 text-sm">PKR ${(item.price * item.quantity).toLocaleString()}</span>
                        
                        <div class="flex items-center gap-3 bg-white border border-slate-200 rounded-lg px-2 py-1">
                            <button onclick="PharmacyCart.updateQuantity(${item.id}, -1)" class="w-5 h-5 flex items-center justify-center text-slate-400 hover:text-rose-500 transition-colors">
                                <i data-lucide="minus" class="w-3 h-3"></i>
                            </button>
                            <span class="text-xs font-black text-slate-900 w-3 text-center">${item.quantity}</span>
                            <button onclick="PharmacyCart.updateQuantity(${item.id}, 1)" class="w-5 h-5 flex items-center justify-center text-slate-400 hover:text-blue-500 transition-colors">
                                <i data-lucide="plus" class="w-3 h-3"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');

        if (totalEl) totalEl.innerText = 'PKR ' + this.calculateTotal().toLocaleString();

        // Re-initialize Lucide icons for new content
        if (window.lucide) window.lucide.createIcons();
    },

    open() {
        const drawer = document.getElementById('cartDrawer');
        const overlay = document.getElementById('cartOverlay');
        if (drawer && overlay) {
            drawer.classList.remove('translate-x-full');
            overlay.classList.remove('hidden');
            setTimeout(() => overlay.classList.remove('opacity-0'), 10); // Fade in
            document.body.style.overflow = 'hidden';
        }
    },

    close() {
        const drawer = document.getElementById('cartDrawer');
        const overlay = document.getElementById('cartOverlay');
        if (drawer && overlay) {
            drawer.classList.add('translate-x-full');
            overlay.classList.add('opacity-0');
            setTimeout(() => overlay.classList.add('hidden'), 300); // Wait for fade out
            document.body.style.overflow = '';
        }
    },

    async checkout() {
        if (this.items.length === 0) return;

        const btn = document.getElementById('btnCartCheckout');
        const originalText = btn.innerHTML;
        btn.innerHTML = '<i data-lucide="loader" class="animate-spin"></i> Processing...';
        if (window.lucide) window.lucide.createIcons();
        btn.disabled = true;

        try {
            const checkoutData = this.items.map(i => ({
                medicineId: i.id,
                quantity: i.quantity
            }));

            // Post to controller
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = '/Pharmacy/CartCheckout';

            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'cartItemsJson';
            input.value = JSON.stringify(checkoutData);

            form.appendChild(input);
            document.body.appendChild(form);
            form.submit();
        } catch (e) {
            console.error(e);
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    PharmacyCart.init();
});
