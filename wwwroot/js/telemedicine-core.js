/**
 * Telemedicine Core — MedLink Consultation Module
 * Video/Audio: Agora RTC Web SDK v4 (replaces WebRTC)
 * Signaling/Chat/Prescription: SignalR ConsultationHub (unchanged)
 */

const Telemed = {
    // ── State ────────────────────────────────────────────────────────────────
    connection: null,       // SignalR hub connection
    context: null,          // Passed from ConsultationRoom.cshtml
    recognition: null,
    isListening: false,

    // Agora
    agoraClient: null,
    localAudioTrack: null,
    localVideoTrack: null,
    _agoraMuted: false,
    _agoraVideoOff: false,

    // ── Init ─────────────────────────────────────────────────────────────────
    init: async function (ctx) {
        console.log('[Telemed] Initializing with Agora RTC...');
        this.context = ctx;
        this.updateStatus('Initializing...');

        this.setupEvents();
        this.setupRoleUI();

        // 1. Connect SignalR (chat, prescription, transcription)
        try {
            this.updateStatus('Connecting server...');
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/consultationHub')
                .withAutomaticReconnect()
                .build();

            this.registerSignalHandlers();
            await this.connection.start();
            await this.connection.invoke('JoinRoom', ctx.roomId);
            console.log('[Telemed] SignalR connected, room joined');
            this.updateStatus('Room Joined — Starting Video...');
        } catch (err) {
            console.error('[Telemed] SignalR error:', err);
            this.updateStatus('Connection Failed');
            return;
        }

        // 2. Join Agora channel
        try {
            await this.joinAgoraChannel();
        } catch (err) {
            console.error('[Telemed] Agora join error:', err);
            this.updateStatus('Video Init Failed');
        }

        this.startTimer();
    },

    // ── Agora: Join Channel ───────────────────────────────────────────────────
    joinAgoraChannel: async function () {
        this.updateStatus('Starting video...');

        // Get token from our backend
        const channelName = this.context.roomId; // e.g. "room_42"
        const tokenRes = await fetch(
            `/api/agora/token?channelName=${encodeURIComponent(channelName)}&uid=0`,
            { credentials: 'include' }
        );
        if (!tokenRes.ok) throw new Error('Failed to get Agora token');
        const tokenData = await tokenRes.json();

        if (!tokenData.success) throw new Error(tokenData.message || 'Token error');

        const { token, appId, uid } = tokenData;

        // Create Agora client
        this.agoraClient = AgoraRTC.createClient({ mode: 'rtc', codec: 'vp8' });

        // Handle remote user published
        this.agoraClient.on('user-published', async (user, mediaType) => {
            await this.agoraClient.subscribe(user, mediaType);
            console.log('[Agora] Subscribed to remote user:', user.uid, mediaType);

            if (mediaType === 'video') {
                this.showRemoteAgoraVideo(user);
                this.updateStatus('Connected');
            }
            if (mediaType === 'audio') {
                user.audioTrack?.play();
            }
        });

        this.agoraClient.on('user-unpublished', (user, mediaType) => {
            console.log('[Agora] Remote user unpublished:', user.uid, mediaType);
            if (mediaType === 'video') {
                const container = document.getElementById('remoteVideoContainer');
                if (container) {
                    container.innerHTML = `
                        <div class="w-full h-full bg-slate-900 rounded-[1.8rem] flex flex-col items-center justify-center gap-4">
                            <div class="w-20 h-20 bg-white/10 rounded-full flex items-center justify-center">
                                <svg class="w-10 h-10 text-white/40" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 10l4.553-2.069A1 1 0 0121 8.87v6.26a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"/></svg>
                            </div>
                            <p class="text-white/60 text-sm font-semibold">Camera turned off</p>
                        </div>`;
                }
            }
        });

        this.agoraClient.on('user-left', (user) => {
            console.log('[Agora] Remote user left:', user.uid);
            this.updateStatus('Participant Left');
        });

        // Join the channel
        await this.agoraClient.join(appId, channelName, token, uid || null);
        console.log('[Agora] Joined channel:', channelName);

        // Create and publish local tracks
        [this.localAudioTrack, this.localVideoTrack] = await AgoraRTC.createMicrophoneAndCameraTracks(
            { encoderConfig: 'speech_low_quality' },
            { encoderConfig: { width: 640, height: 480, frameRate: 24, bitrateMax: 800 } }
        );

        // Show local video
        const localContainer = document.getElementById('localVideoContainer');
        if (localContainer) {
            localContainer.innerHTML = '';
            this.localVideoTrack.play(localContainer);
        }

        await this.agoraClient.publish([this.localAudioTrack, this.localVideoTrack]);
        console.log('[Agora] Local tracks published');
        this.updateStatus('Live');
    },

    // ── Agora: Show remote video ──────────────────────────────────────────────
    showRemoteAgoraVideo: function (user) {
        const container = document.getElementById('remoteVideoContainer');
        if (!container) return;
        container.innerHTML = '';
        // Create a div for Agora to inject the video into
        const videoDiv = document.createElement('div');
        videoDiv.id = `remote-video-${user.uid}`;
        videoDiv.style.cssText = 'width:100%;height:100%;border-radius:1.8rem;overflow:hidden;';
        container.appendChild(videoDiv);
        user.videoTrack?.play(videoDiv.id);
    },

    // ── Agora: Controls ───────────────────────────────────────────────────────
    toggleMute: function () {
        if (!this.localAudioTrack) return;
        this._agoraMuted = !this._agoraMuted;
        this.localAudioTrack.setEnabled(!this._agoraMuted);

        const btn = document.getElementById('btnMute');
        if (btn) {
            const icon = btn.querySelector('i');
            if (this._agoraMuted) {
                btn.classList.add('bg-rose-500', 'text-white');
                btn.classList.remove('bg-gray-50', 'text-gray-400');
                if (icon) icon.setAttribute('data-lucide', 'mic-off');
            } else {
                btn.classList.remove('bg-rose-500', 'text-white');
                btn.classList.add('bg-gray-50', 'text-gray-400');
                if (icon) icon.setAttribute('data-lucide', 'mic');
            }
            if (window.lucide) lucide.createIcons();
        }
    },

    toggleVideo: function () {
        if (!this.localVideoTrack) return;
        this._agoraVideoOff = !this._agoraVideoOff;
        this.localVideoTrack.setEnabled(!this._agoraVideoOff);

        const btn = document.getElementById('btnVideo');
        if (btn) {
            const icon = btn.querySelector('i');
            if (this._agoraVideoOff) {
                btn.classList.add('bg-rose-500', 'text-white');
                btn.classList.remove('bg-gray-50', 'text-gray-400');
                if (icon) icon.setAttribute('data-lucide', 'video-off');
            } else {
                btn.classList.remove('bg-rose-500', 'text-white');
                btn.classList.add('bg-gray-50', 'text-gray-400');
                if (icon) icon.setAttribute('data-lucide', 'video');
            }
            if (window.lucide) lucide.createIcons();
        }
    },

    endCall: async function () {
        try {
            // Leave Agora channel
            if (this.localAudioTrack) { this.localAudioTrack.stop(); this.localAudioTrack.close(); }
            if (this.localVideoTrack) { this.localVideoTrack.stop(); this.localVideoTrack.close(); }
            if (this.agoraClient) await this.agoraClient.leave();
        } catch (e) { console.warn('[Agora] Cleanup error:', e); }

        try {
            if (this.connection) await this.connection.invoke('EndCall', this.context.roomId);
        } catch (_) {}
    },

    // ── SignalR handlers (chat, prescription, transcription — unchanged) ───────
    registerSignalHandlers: function () {
        const self = this;

        // Room presence (Agora handles the actual call, SignalR just coordinates)
        this.connection.on('UserJoined', async (cid) => {
            console.log('[SignalR] User joined room:', cid);
            if (!self.context.isDoctor) {
                await self.connection.invoke('AnnouncePresence', self.context.roomId);
            }
        });

        this.connection.on('UserIsPresent', (cid) => {
            console.log('[SignalR] User is present:', cid);
        });

        // Chat
        this.connection.on('ReceiveMessage', (user, msg, type, url, name, senderId) => {
            self.addChatMessage(user, msg, type, url, name, senderId === self.context.senderId);
        });

        // Prescription
        this.connection.on('PrescriptionUpdated', (json) => {
            self.updatePrescriptionUI(JSON.parse(json));
        });

        // Session end
        this.connection.on('CallEnded', async () => {
            await self.endCall();
            if (typeof PremiumModal !== 'undefined') {
                await PremiumModal.alert('The consultation session has ended.', 'Call Ended');
            }
            window.location.reload();
        });

        // Transcription
        this.connection.on('ReceiveTranscription', (data) => {
            self.addTranscriptionLog(data.speakerName, data.originalText, data.englishText, data.urduText, data.timestamp);
        });
    },

    // ── Consultation Timer ────────────────────────────────────────────────────
    _timerInterval: null,
    _timerSeconds: 0,

    startTimer: function () {
        // Timer is also driven by initConsultationTimer() called from the view
        // This provides a fallback start
    },

    // ── UI helpers ────────────────────────────────────────────────────────────
    updateStatus: function (status) {
        const el = document.getElementById('debug-status');
        if (el) el.innerText = status;
    },

    // ── Setup events ──────────────────────────────────────────────────────────
    setupEvents: function () {
        const self = this;

        document.getElementById('btnMute')?.addEventListener('click', () => self.toggleMute());
        document.getElementById('btnVideo')?.addEventListener('click', () => self.toggleVideo());
        document.getElementById('btnEndCall')?.addEventListener('click', () => self.endCall());
        document.getElementById('leaveCall')?.addEventListener('click', () => self.endCall());

        // Chat send
        document.getElementById('sendBtn')?.addEventListener('click', () => self.sendMessage());
        document.getElementById('chatInput')?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') self.sendMessage();
        });

        // File attach
        document.getElementById('attachBtn')?.addEventListener('click', () => {
            document.getElementById('fileInput')?.click();
        });
        document.getElementById('fileInput')?.addEventListener('change', (e) => self.handleFileUpload(e));

        // Transcription
        document.getElementById('toggleTranscription')?.addEventListener('click', () => self.toggleTranscription());
    },

    // ── Transcription ─────────────────────────────────────────────────────────
    toggleTranscription: function () {
        if (this.isListening) {
            this.stopTranscription();
        } else {
            this.startTranscription();
        }
    },

    startTranscription: function () {
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognition) {
            if (typeof PremiumModal !== 'undefined')
                PremiumModal.error('Your browser does not support Speech Recognition.', 'AI Feature Unavailable');
            return;
        }

        this.recognition = new SpeechRecognition();
        this.recognition.continuous = true;
        this.recognition.interimResults = false;
        this.recognition.lang = 'en-US';

        const self = this;
        this.recognition.onresult = async (event) => {
            const transcript = event.results[event.results.length - 1][0].transcript.trim();
            if (transcript) {
                const speakerRole = self.context.isDoctor ? 'Doctor' : 'Patient';
                try {
                    const res = await fetch('/Dashboard/TranslateTranscription', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: `text=${encodeURIComponent(transcript)}&appointmentId=${self.context.appointmentId}&speakerRole=${speakerRole}`
                    });
                    const data = await res.json();
                    if (data.success) {
                        self.connection.invoke('SendTranscription', self.context.roomId,
                            self.context.userName, data.original, data.english, data.urdu);
                    }
                } catch (_) {}
            }
        };

        this.recognition.onstart = () => {
            this.isListening = true;
            this.updateTranscriptionUI(true);
        };
        this.recognition.onend = () => {
            if (this.isListening) this.recognition.start();
        };
        this.recognition.onerror = (event) => {
            if (event.error === 'not-allowed') {
                this.stopTranscription();
            }
        };

        this.recognition.start();
    },

    stopTranscription: function () {
        this.isListening = false;
        if (this.recognition) this.recognition.stop();
        this.updateTranscriptionUI(false);
    },

    updateTranscriptionUI: function (active) {
        const btn = document.getElementById('toggleTranscription');
        const dot = document.getElementById('listen-status-dot');
        const text = document.getElementById('listen-status-text');

        if (btn) btn.innerText = active ? 'Disable Live Aid' : 'Enable Live Aid';
        if (dot) {
            dot.className = active
                ? 'w-2 h-2 rounded-full bg-emerald-500 animate-pulse'
                : 'w-2 h-2 rounded-full bg-slate-700';
        }
        if (text) {
            text.innerText = active ? 'Listening...' : 'Idle';
            text.className = active
                ? 'text-[8px] font-semibold text-emerald-400 uppercase tracking-widest'
                : 'text-[8px] font-semibold text-slate-500 uppercase tracking-widest';
        }
    },

    addTranscriptionLog: function (speaker, original, english, urdu, time) {
        const container = document.getElementById('transcriptionLog');
        if (!container) return;
        const placeholder = container.querySelector('.opacity-20');
        if (placeholder) placeholder.remove();

        const isMe = speaker === this.context.userName;
        const div = document.createElement('div');
        div.className = 'space-y-2 group';
        div.innerHTML = `
            <div class="flex items-center justify-between">
                <span class="text-[8px] font-black ${isMe ? 'text-blue-400' : 'text-emerald-400'} uppercase tracking-widest">${speaker} <span class="text-white/20 ml-2">${time}</span></span>
            </div>
            <div class="bg-white/5 border border-white/5 p-3 rounded-2xl space-y-2">
                <p class="text-[11px] text-white font-bold leading-relaxed">${original}</p>
                <div class="border-t border-white/5 pt-2 space-y-1">
                    <p class="text-[10px] text-white/40 italic"><span class="text-blue-500/50 font-black mr-1">EN</span>${english}</p>
                    <p class="text-[10px] text-white/40 text-right" dir="rtl"><span class="text-emerald-500/50 font-black ml-1">UR</span>${urdu}</p>
                </div>
            </div>`;
        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    },

    // ── Chat ──────────────────────────────────────────────────────────────────
    sendMessage: function () {
        const input = document.getElementById('chatInput');
        const msg = input?.value?.trim();
        if (!msg || !this.connection) return;
        this.connection.invoke('SendMessage', this.context.roomId, this.context.userName,
            msg, 'text', '', '', this.context.senderId, this.context.receiverId);
        if (input) input.value = '';
    },

    addChatMessage: function (user, msg, type, url, name, isMe) {
        const container = document.getElementById('chatMessages');
        if (!container) return;
        const div = document.createElement('div');
        div.className = `flex ${isMe ? 'justify-end' : 'justify-start'} mb-4`;
        let body = `<p class="text-sm">${msg}</p>`;
        if (type === 'image') body = `<img src="${url}" class="max-w-[150px] rounded mt-2" alt="attachment">`;
        if (type === 'file' || type === 'document')
            body = `<a href="${url}" target="_blank" class="block mt-2 font-bold underline text-blue-100">${name}</a>`;
        div.innerHTML = `
            <div class="${isMe ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-800'} p-3 rounded-2xl max-w-[80%]">
                <p class="text-[10px] font-bold opacity-75 mb-1">${isMe ? 'You' : user}</p>
                ${body}
            </div>`;
        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    },

    handleFileUpload: async function (e) {
        const file = e.target.files[0];
        if (!file) return;
        const fd = new FormData();
        fd.append('file', file);
        try {
            const res = await fetch('/Dashboard/UploadConsultationFile', { method: 'POST', body: fd });
            const data = await res.json();
            if (data.success) {
                this.connection.invoke('SendMessage', this.context.roomId, this.context.userName,
                    'Shared a file', data.type, data.url, data.name,
                    this.context.senderId, this.context.receiverId);
            }
        } catch (_) {}
    },

    // ── Prescription ──────────────────────────────────────────────────────────
    setupRoleUI: function () {
        if (this.context.isDoctor) {
            document.getElementById('doctorPrescription')?.classList.remove('hidden');
            document.getElementById('diagnosis')?.addEventListener('input', () => this.syncPrescription());
            document.getElementById('addMedication')?.addEventListener('click', () => this.addMedInput());

            if (this.context.isLocked) {
                const diag = document.getElementById('diagnosis');
                if (diag) diag.disabled = true;
                const addBtn = document.getElementById('addMedication');
                if (addBtn) addBtn.style.display = 'none';
            }
        } else {
            document.getElementById('patientPrescription')?.classList.remove('hidden');
            const d = document.getElementById('diagnosis');
            if (d) d.readOnly = true;
        }

        if (this.context.initialData) {
            const data = this.context.initialData;
            const diag = document.getElementById('diagnosis');
            if (diag) diag.value = data.diagnosis || '';
            if (data.medications?.length) {
                const list = document.getElementById('medicationList');
                if (list) {
                    list.innerHTML = '';
                    data.medications.forEach(m => this.addMedWithData(m));
                }
            }
            this.updatePrescriptionUI(data);
        }
    },

    syncPrescription: function () {
        if (this.context.isLocked || !this.connection) return;
        const diagnosis = document.getElementById('diagnosis')?.value || '';
        const meds = [];
        document.querySelectorAll('.med-item').forEach(el => {
            const sel = el.querySelector('.med-select');
            const name = sel?.options[sel?.selectedIndex]?.text || '';
            meds.push({
                medicineId: sel?.value || 0,
                medicineName: name, name,
                dosage: el.querySelector('.med-dose')?.value || '',
                dose: el.querySelector('.med-dose')?.value || '',
                frequency: el.querySelector('.med-freq')?.value || '',
                duration: el.querySelector('.med-dur')?.value || '',
                quantity: parseInt(el.querySelector('.med-qty')?.value) || 0
            });
        });
        this.connection.invoke('UpdatePrescription', this.context.roomId,
            JSON.stringify({ diagnosis, medications: meds }));
    },

    updatePrescriptionUI: function (data) {
        if (this.context) this.context.initialData = data;
        const diagDisplay = document.getElementById('patientDiagnosisDisplay');
        if (diagDisplay) diagDisplay.innerText = data.diagnosis || 'Pending...';

        const list = document.getElementById('patientMedicationDisplay');
        if (list && data.medications) {
            if (!data.medications.length) {
                list.innerHTML = '<span class="text-xs text-gray-400 italic">No medications added yet</span>';
            } else {
                list.innerHTML = data.medications.map(m => {
                    const n = m.medicineName || m.name || 'Medicine';
                    const d = m.dosage || m.dose || 'N/A';
                    const f = m.frequency || '';
                    const dur = m.duration || '';
                    return `<div class="flex items-center gap-2 px-3 py-1.5 bg-white border border-blue-100 rounded-lg text-[10px] font-bold text-slate-700 shadow-sm">
                        <i data-lucide="pill" class="w-3 h-3 text-blue-500"></i>
                        ${n} — ${d}${f ? ` (${f})` : ''}${dur ? ` × ${dur}` : ''}
                    </div>`;
                }).join('');
                if (window.lucide) lucide.createIcons();
            }
        }
    },

    addMedInput: function () {
        const list = document.getElementById('medicationList');
        if (!list) return;
        const div = document.createElement('div');
        div.className = 'med-item grid grid-cols-12 gap-2 mb-3 bg-slate-50/50 p-3 rounded-2xl border border-slate-100 relative';
        div.innerHTML = `
            <div class="col-span-12 md:col-span-4 relative">
                <input type="text" class="med-search-input w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Search Medicine...">
                <select class="med-select hidden"><option value="">Select</option></select>
                <div class="med-dropdown-results hidden absolute top-full left-0 right-0 z-[100] mt-1 bg-white border border-slate-100 rounded-xl shadow-2xl max-h-40 overflow-y-auto"></div>
            </div>
            <div class="col-span-3 md:col-span-2"><input class="med-dose w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Dosage"></div>
            <div class="col-span-3 md:col-span-2"><input class="med-freq w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Freq"></div>
            <div class="col-span-3 md:col-span-2"><input class="med-dur w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Dur"></div>
            <div class="col-span-2 md:col-span-1"><input class="med-qty w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Qty"></div>
            <div class="col-span-1 flex items-center justify-end">
                <button type="button" class="text-rose-500 hover:text-rose-700 p-1">✕</button>
            </div>`;

        const searchInput = div.querySelector('.med-search-input');
        const resultsDiv = div.querySelector('.med-dropdown-results');
        const realSelect = div.querySelector('.med-select');

        this.searchMeds('', resultsDiv, realSelect, searchInput);

        let timeout = null;
        searchInput.addEventListener('input', (e) => {
            clearTimeout(timeout);
            timeout = setTimeout(() => this.searchMeds(e.target.value, resultsDiv, realSelect, searchInput), 300);
        });
        searchInput.addEventListener('focus', () => resultsDiv.classList.remove('hidden'));
        document.addEventListener('click', (e) => { if (!div.contains(e.target)) resultsDiv.classList.add('hidden'); });

        div.querySelector('button').onclick = () => { div.remove(); this.syncPrescription(); };
        div.querySelectorAll('input').forEach(i => i.oninput = () => this.syncPrescription());
        list.appendChild(div);
    },

    addMedWithData: function (m) {
        const list = document.getElementById('medicationList');
        if (!list) return;
        const div = document.createElement('div');
        div.className = 'med-item grid grid-cols-12 gap-2 mb-3 bg-slate-50/50 p-3 rounded-2xl border border-slate-100';
        const name = m.medicineName || m.name || 'Unknown';
        div.innerHTML = `
            <div class="col-span-12 md:col-span-4">
                <select class="med-select w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm" ${this.context.isLocked ? 'disabled' : ''}>
                    <option value="${m.medicineId || 0}">${name}</option>
                </select>
            </div>
            <div class="col-span-3 md:col-span-2"><input class="med-dose w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm" placeholder="Dosage" value="${m.dosage || m.dose || ''}" ${this.context.isLocked ? 'disabled' : ''}></div>
            <div class="col-span-3 md:col-span-2"><input class="med-freq w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm" placeholder="Freq" value="${m.frequency || ''}" ${this.context.isLocked ? 'disabled' : ''}></div>
            <div class="col-span-3 md:col-span-2"><input class="med-dur w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm" placeholder="Dur" value="${m.duration || ''}" ${this.context.isLocked ? 'disabled' : ''}></div>
            <div class="col-span-2 md:col-span-1"><input class="med-qty w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm" placeholder="Qty" value="${m.quantity || 0}" ${this.context.isLocked ? 'disabled' : ''}></div>
            <div class="col-span-1 flex items-center justify-end">
                <button type="button" class="text-rose-500 p-1" ${this.context.isLocked ? 'style="display:none"' : ''}>✕</button>
            </div>`;
        div.querySelector('button').onclick = () => { div.remove(); this.syncPrescription(); };
        div.querySelectorAll('input, select').forEach(i => i.oninput = () => this.syncPrescription());
        list.appendChild(div);
    },

    searchMeds: async function (term, resultsDiv, realSelect, searchInput) {
        try {
            const res = await fetch(`/Pharmacy/SearchMedicines?term=${encodeURIComponent(term)}`);
            const meds = await res.json();
            resultsDiv.innerHTML = '';
            if (!meds.length) {
                resultsDiv.innerHTML = '<div class="p-3 text-[10px] text-slate-400 text-center">No medicines found</div>';
                return;
            }
            meds.forEach(m => {
                const item = document.createElement('div');
                item.className = 'p-2 hover:bg-slate-50 cursor-pointer text-[10px] font-bold text-slate-700 border-b border-slate-50 last:border-0 flex justify-between items-center';
                item.innerHTML = `<span>${m.name}</span><span class="text-[8px] text-slate-300">${m.brand}</span>`;
                item.onclick = () => {
                    realSelect.innerHTML = `<option value="${m.id}" selected>${m.name}</option>`;
                    searchInput.value = m.name;
                    resultsDiv.classList.add('hidden');
                    this.syncPrescription();
                };
                resultsDiv.appendChild(item);
            });
        } catch (e) { console.error('[Telemed] Medicine search failed', e); }
    }
};
