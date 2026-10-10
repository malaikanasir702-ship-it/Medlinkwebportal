/**
 * Premium Modal System - MedLink Portal
 * Replaces native alert(), confirm(), and prompt() with high-end glassmorphic modals.
 */

const PremiumModal = {
    _createModal: function (options) {
        const id = 'premium-modal-' + Date.now();
        const isDark = document.documentElement.classList.contains('dark');

        const modalHtml = `
            <div id="${id}" class="fixed inset-0 z-[10000] flex items-center justify-center p-4 opacity-0 transition-all duration-300 pointer-events-none">
                <div class="absolute inset-0 bg-slate-900/60 backdrop-blur-sm transition-opacity duration-300"></div>
                <div class="relative bg-white/90 dark:bg-slate-900/90 backdrop-blur-2xl border border-white/20 dark:border-white/10 rounded-[2.5rem] shadow-[0_32px_64px_-12px_rgba(0,0,0,0.3)] p-8 max-w-sm w-full transform scale-95 transition-all duration-300">
                    <div class="flex flex-col items-center text-center gap-6">
                        <div class="w-16 h-16 rounded-[1.5rem] ${options.iconBg || 'bg-blue-600/10 text-blue-600'} flex items-center justify-center relative">
                             <div class="absolute inset-0 blur-xl opacity-20 ${options.iconBg || 'bg-blue-600 text-blue-600'}"></div>
                             <i class="${options.icon || 'fas fa-info-circle'} text-2xl relative z-10"></i>
                        </div>
                        <div class="space-y-2">
                            <h3 class="text-xl font-black text-slate-900 dark:text-white tracking-tight">${options.title || 'Attention'}</h3>
                            <p class="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest leading-loose">${options.message}</p>
                        </div>
                        
                        ${options.prompt ? `
                            <div class="w-full">
                                <input type="text" id="${id}-input" class="w-full px-5 py-4 bg-slate-50 dark:bg-white/5 border border-slate-100 dark:border-white/5 focus:border-blue-500 rounded-2xl text-sm font-black outline-none transition-all dark:text-white" value="${options.defaultValue || ''}" spellcheck="false">
                            </div>
                        ` : ''}

                        <div class="flex items-center gap-3 w-full pt-2">
                            ${options.showCancel ? `
                                <button id="${id}-cancel" class="flex-1 px-6 py-4 bg-slate-100 dark:bg-white/5 text-slate-600 dark:text-slate-400 font-black text-[10px] uppercase tracking-widest rounded-2xl hover:bg-slate-200 dark:hover:bg-white/10 transition-all">
                                    ${options.cancelText || 'Cancel'}
                                </button>
                            ` : ''}
                            <button id="${id}-confirm" class="flex-1 px-6 py-4 ${options.confirmBg || 'bg-blue-600'} text-white font-black text-[10px] uppercase tracking-widest rounded-2xl shadow-xl shadow-blue-500/20 hover:opacity-90 transition-all">
                                ${options.confirmText || 'Okay'}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = document.getElementById(id);
        const container = modal.querySelector('.relative');

        // Trigger Animations
        setTimeout(() => {
            modal.classList.remove('opacity-0', 'pointer-events-none');
            container.classList.remove('scale-95');
            container.classList.add('scale-100');
        }, 10);

        return new Promise((resolve) => {
            const confirmBtn = document.getElementById(`${id}-confirm`);
            const cancelBtn = document.getElementById(`${id}-cancel`);
            const input = document.getElementById(`${id}-input`);

            const close = (result) => {
                modal.classList.add('opacity-0', 'pointer-events-none');
                container.classList.remove('scale-100');
                container.classList.add('scale-95');
                setTimeout(() => modal.remove(), 300);
                resolve(result);
            };

            confirmBtn.onclick = () => {
                if (options.prompt) close(input.value);
                else close(true);
            };

            if (cancelBtn) {
                cancelBtn.onclick = () => close(null);
            }

            if (input) {
                input.focus();
                input.onkeypress = (e) => {
                    if (e.key === 'Enter') confirmBtn.click();
                };
            }
        });
    },

    alert: function (message, title = 'Notice') {
        return this._createModal({
            message,
            title,
            showCancel: false,
            icon: 'fas fa-info-circle',
            iconBg: 'bg-blue-500/10 text-blue-500'
        });
    },

    success: function (message, title = 'Success') {
        return this._createModal({
            message,
            title,
            showCancel: false,
            icon: 'fas fa-check-circle',
            iconBg: 'bg-emerald-500/10 text-emerald-500',
            confirmBg: 'bg-emerald-500'
        });
    },

    error: function (message, title = 'Error') {
        return this._createModal({
            message,
            title,
            showCancel: false,
            icon: 'fas fa-exclamation-triangle',
            iconBg: 'bg-rose-500/10 text-rose-500',
            confirmBg: 'bg-rose-500'
        });
    },

    confirm: function (message, title = 'Are you sure?') {
        return this._createModal({
            message,
            title,
            showCancel: true,
            icon: 'fas fa-question-circle',
            iconBg: 'bg-indigo-500/10 text-indigo-500',
            confirmBg: 'bg-indigo-600',
            confirmText: 'Confirm'
        });
    },

    prompt: function (message, defaultValue = '', title = 'Input Required') {
        return this._createModal({
            message,
            title,
            showCancel: true,
            prompt: true,
            defaultValue,
            icon: 'fas fa-edit',
            iconBg: 'bg-blue-500/10 text-blue-500',
            confirmText: 'Done'
        });
    }
};

window.PremiumModal = PremiumModal;
