/**
 * Telemedicine Core - Dedicated Module for MedLink Consultation
 * Handles: WebRTC (p2p), Signaling (SignalR), Chat, and Prescriptions.
 */

const Telemed = {
    // State
    connection: null,
    localStream: null,
    peerConnection: null,
    context: null,
    recognition: null,
    isListening: false,
    iceQueue: [],
    isRemoteDescriptionSet: false,

    // Config
    rtcConfig: {
        iceServers: [
            { urls: "stun:stun.l.google.com:19302" },
            { urls: "stun:stun1.l.google.com:19302" },
            { urls: "stun:stun2.l.google.com:19302" },
            { urls: "stun:stun3.l.google.com:19302" },
            { urls: "stun:stun4.l.google.com:19302" }
        ],
        sdpSemantics: 'unified-plan'
    },

    // Initialization
    init: async function (ctx) {
        console.log("Telemed: Initializing...", ctx);
        this.context = ctx;
        this.updateStatus("Initializing...");

        // 1. Setup UI Events
        this.setupEvents();
        this.setupRoleUI();

        // 2. Start Media (Camera/Mic)
        try {
            this.updateStatus("Requesting Camera...");
            this.localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
            this.showLocalVideo(this.localStream);
            this.updateStatus("Camera Active");
        } catch (err) {
            console.error("Media Error:", err);
            await PremiumModal.error("Camera blocked! Please allow access.", "Media Error");
            this.updateStatus("Camera Blocked");
            return; // Stop if no media
        }

        // 3. Connect SignalR
        try {
            this.updateStatus("Connecting Server...");
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/consultationHub")
                .withAutomaticReconnect()
                .build();

            this.registerSignalHandlers();

            await this.connection.start();
            console.log("Telemed: Connected to Hub");

            await this.connection.invoke("JoinRoom", this.context.roomId);
            this.updateStatus("Room Joined - Waiting...");

            // Start Call Timer
            this.startTimer();

        } catch (err) {
            console.error("SignalR Error:", err);
            this.updateStatus("Connection Failed");
            await PremiumModal.error("Could not connect to server. Please refresh.", "Connection Error");
        }
    },

    // --- Signaling Handlers ---
    registerSignalHandlers: function () {
        const self = this;

        // Presence Handshake (Auto-Connect Logic)
        this.connection.on("UserJoined", async (cid) => {
            console.log("Telemed: User Joined", cid);
            // If I am Patient, I announce presence so Doctor knows to call me
            if (!self.context.isDoctor) {
                console.log("Telemed: Announcing Presence...");
                await self.connection.invoke("AnnouncePresence", self.context.roomId);
            }
            // If I am Doctor, I initiate call to the new user
            else {
                console.log("Telemed: Doctor initiating call...");
                self.initiateCall();
            }
        });

        this.connection.on("UserIsPresent", (cid) => {
            console.log("Telemed: User Is Present", cid);
            // If I am Doctor and see a user is present, I initiate
            if (self.context.isDoctor) {
                console.log("Telemed: Doctor initiating call...");
                self.initiateCall();
            }
        });

        this.connection.on("IncomingCall", async (callerId, type, offerSdp) => {
            console.log("Telemed: Incoming Call (Auto-Accepting)");
            self.updateStatus("Connecting...");
            // Add a slight delay to ensure peer connection is ready
            setTimeout(async () => {
                await self.handleOffer(offerSdp);
            }, 500);
        });

        this.connection.on("CallAccepted", async (userId, answerSdp) => {
            console.log("Telemed: Call Accepted");
            self.updateStatus("Connected");
            await self.handleAnswer(answerSdp);
        });

        this.connection.on("ReceiveIceCandidate", async (userId, candidateJson) => {
            const candidate = new RTCIceCandidate(JSON.parse(candidateJson));
            if (self.isRemoteDescriptionSet && self.peerConnection) {
                try {
                    await self.peerConnection.addIceCandidate(candidate);
                } catch (e) { console.error("ICE Error", e); }
            } else {
                self.iceQueue.push(candidate);
            }
        });

        // Data Signals
        this.connection.on("ReceiveMessage", (user, msg, type, url, name, senderId) => {
            self.addChatMessage(user, msg, type, url, name, senderId === self.context.senderId);
        });

        this.connection.on("PrescriptionUpdated", (json) => {
            self.updatePrescriptionUI(JSON.parse(json));
        });

        this.connection.on("CallEnded", async () => {
            await PremiumModal.alert("The consultation session has ended.", "Call Ended");
            window.location.reload();
        });

        this.connection.on("ReceiveTranscription", (data) => {
            self.addTranscriptionLog(data.speakerName, data.originalText, data.englishText, data.urduText, data.timestamp);
        });
    },

    // --- WebRTC Logic ---
    createPeerConnection: function () {
        const self = this;
        if (this.peerConnection) this.peerConnection.close();

        this.peerConnection = new RTCPeerConnection(this.rtcConfig);
        this.isRemoteDescriptionSet = false;
        this.iceQueue = [];

        // Add Local Tracks
        this.localStream.getTracks().forEach(track => {
            self.peerConnection.addTrack(track, self.localStream);
        });

        // Handle Remote Stream
        this.peerConnection.ontrack = (event) => {
            console.log("Telemed: Remote Stream Received", event.track.kind);
            const stream = event.streams && event.streams[0] ? event.streams[0] : new MediaStream([event.track]);
            self.showRemoteVideo(stream);
        };

        // Handle ICE
        this.peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                console.log("Telemed: Sending ICE Candidate");
                self.connection.invoke("SendIceCandidate", self.context.roomId, JSON.stringify(event.candidate));
            }
        };

        this.peerConnection.oniceconnectionstatechange = () => {
            console.log("Telemed: ICE State Change:", self.peerConnection.iceConnectionState);
            if (self.peerConnection.iceConnectionState === "failed") {
                self.updateStatus("ICE Failed - Retry Sync");
            }
        };
    },

    initiateCall: async function () {
        console.log("Telemed: Manually initiating/syncing call...");
        this.createPeerConnection();
        const offer = await this.peerConnection.createOffer({
            offerToReceiveAudio: true,
            offerToReceiveVideo: true
        });
        await this.peerConnection.setLocalDescription(offer);

        await this.connection.invoke("InitiateCall", this.context.roomId, "video", offer.sdp);
        this.updateStatus("Calling...");
    },

    handleOffer: async function (offerSdp) {
        this.createPeerConnection();
        await this.peerConnection.setRemoteDescription(new RTCSessionDescription({ type: 'offer', sdp: offerSdp }));
        this.isRemoteDescriptionSet = true;
        await this.processIceQueue();

        const answer = await this.peerConnection.createAnswer();
        await this.peerConnection.setLocalDescription(answer);

        await this.connection.invoke("AcceptCall", this.context.roomId, answer.sdp);
        this.updateStatus("Connected");
    },

    handleAnswer: async function (answerSdp) {
        await this.peerConnection.setRemoteDescription(new RTCSessionDescription({ type: 'answer', sdp: answerSdp }));
        this.isRemoteDescriptionSet = true;
        await this.processIceQueue();
    },

    processIceQueue: async function() {
        while(this.iceQueue.length > 0) {
            const candidate = this.iceQueue.shift();
            try {
                if (this.peerConnection) await this.peerConnection.addIceCandidate(candidate);
            } catch(e) { console.error("Telemed: Queued ICE Error", e); }
        }
    },

    // --- UI Helpers ---
    showLocalVideo: function (stream) {
        const vid = document.createElement("video");
        vid.srcObject = stream;
        vid.muted = true;
        vid.autoplay = true;
        vid.className = "w-full h-full object-cover rounded-[2rem] transform -scale-x-100";

        const el = document.getElementById("localVideoContainer");
        if (el) { el.innerHTML = ""; el.appendChild(vid); }
    },

    showRemoteVideo: function (stream) {
        let el = document.getElementById("remoteVideoContainer") || document.getElementById("mainVideoContainer");
        if (!el) return;

        // Find existing video or create one
        let vid = el.querySelector("video.remote-stream");
        if (!vid) {
            vid = document.createElement("video");
            vid.autoplay = true;
            vid.playsInline = true;
            vid.className = "w-full h-full object-cover rounded-[1.8rem] remote-stream";
            
            // We want to replace the skeleton stream, but keep the container structure if possible.
            // But since the original HTML has a nested div, let's just clear it and append video.
            // To prevent white background, we add bg-slate-900 directly to the video.
            vid.classList.add("bg-slate-900");
            
            el.innerHTML = "";
            el.appendChild(vid);
        }
        
        if (vid.srcObject !== stream) {
            vid.srcObject = stream;
        }
    },

    updateStatus: function (status) {
        const el = document.getElementById("debug-status");
        if (el) {
            el.innerText = status;
            el.parentElement.parentElement.style.opacity = "1"; // Ensure visibility
        }
    },

    // --- Transcription Logic ---
    toggleTranscription: function () {
        console.log("Toggle transcription called, current state:", this.isListening);
        if (this.isListening) {
            this.stopTranscription();
        } else {
            this.startTranscription();
        }
    },

    startTranscription: function () {
        console.log("Starting transcription...");
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognition) {
            console.error("Speech Recognition not supported");
            PremiumModal.error("Your browser does not support Speech Recognition.", "AI Feature Unavailable");
            return;
        }

        this.recognition = new SpeechRecognition();
        this.recognition.continuous = true;
        this.recognition.interimResults = false;
        this.recognition.lang = 'en-US'; // Default, AI will detect later anyway

        const self = this;
        this.recognition.onresult = async (event) => {
            const transcript = event.results[event.results.length - 1][0].transcript.trim();
            console.log("Transcript received:", transcript);
            if (transcript) {
                // Send to server for translation and broadcast
                const speakerRole = self.context.isDoctor ? 'Doctor' : 'Patient';
                const res = await fetch('/Dashboard/TranslateTranscription', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `text=${encodeURIComponent(transcript)}&appointmentId=${self.context.appointmentId}&speakerRole=${speakerRole}`
                });
                const data = await res.json();
                console.log("Translation response:", data);
                if (data.success) {
                    self.connection.invoke("SendTranscription", self.context.roomId, self.context.userName, data.original, data.english, data.urdu);
                }
            }
        };

        this.recognition.onstart = () => {
            console.log("Speech recognition started");
            this.isListening = true;
            this.updateTranscriptionUI(true);
        };

        this.recognition.onend = () => {
            console.log("Speech recognition ended, isListening:", this.isListening);
            if (this.isListening) this.recognition.start(); // Auto-restart if we didn't manually stop
        };

        this.recognition.onerror = (event) => {
            console.error("Speech Recognition Error:", event.error);
            if (event.error === 'not-allowed') {
                this.stopTranscription();
                PremiumModal.error("Microphone access denied for transcription.", "Permission Error");
            }
        };

        this.recognition.start();
        console.log("Recognition.start() called");
    },

    stopTranscription: function () {
        this.isListening = false;
        if (this.recognition) {
            this.recognition.stop();
        }
        this.updateTranscriptionUI(false);
    },

    updateTranscriptionUI: function (active) {
        console.log("Updating transcription UI, active:", active);
        const btn = document.getElementById("toggleTranscription");
        const dot = document.getElementById("listen-status-dot");
        const text = document.getElementById("listen-status-text");

        if (btn) {
            btn.innerText = active ? "Disable Live Aid" : "Enable Live Aid";
            if (active) {
                btn.classList.remove("bg-blue-600", "shadow-blue-600/20", "hover:bg-blue-700");
                btn.classList.add("bg-rose-600", "shadow-rose-600/20", "hover:bg-rose-700");
            } else {
                btn.classList.remove("bg-rose-600", "shadow-rose-600/20", "hover:bg-rose-700");
                btn.classList.add("bg-blue-600", "shadow-blue-600/20", "hover:bg-blue-700");
            }
        }

        if (dot) {
            if (active) {
                dot.classList.remove("bg-white/10", "bg-slate-700");
                dot.classList.add("bg-emerald-500", "animate-pulse");
            } else {
                dot.classList.remove("bg-emerald-500", "animate-pulse");
                dot.classList.add("bg-white/10");
            }
        }

        if (text) {
            text.innerText = active ? "Listening..." : "Standby";
            if (active) {
                text.classList.remove("text-white/30", "text-slate-500");
                text.classList.add("text-emerald-400");
            } else {
                text.classList.remove("text-emerald-400");
                text.classList.add("text-white/30");
            }
        }
    },

    addTranscriptionLog: function (speaker, original, english, urdu, time) {
        const container = document.getElementById("transcriptionLog");
        if (!container) return;

        // Remove placeholder if present
        const placeholder = container.querySelector(".opacity-20");
        if (placeholder) placeholder.remove();

        const isMe = speaker === this.context.userName;

        const div = document.createElement("div");
        div.className = "space-y-2 group animate-in slide-in-from-bottom-2 duration-300";

        div.innerHTML = `
            <div class="flex items-center justify-between">
                <span class="text-[8px] font-black ${isMe ? 'text-blue-400' : 'text-emerald-400'} uppercase tracking-widest">${speaker} <span class="text-white/20 ml-2">${time}</span></span>
            </div>
            <div class="bg-white/5 border border-white/5 p-3 rounded-2xl space-y-2">
                <p class="text-[11px] text-white font-bold leading-relaxed">${original}</p>
                <div class="grid grid-cols-1 gap-2 border-t border-white/5 pt-2">
                    <div class="flex items-start gap-2">
                        <span class="text-[7px] font-black text-blue-500/50 uppercase mt-1">EN</span>
                        <p class="text-[10px] text-white/40 italic">${english}</p>
                    </div>
                    <div class="flex items-start gap-2">
                        <span class="text-[7px] font-black text-emerald-500/50 uppercase mt-1">UR</span>
                        <p class="text-[10px] text-white/40 font-urdu leading-relaxed text-right w-full" dir="rtl">${urdu}</p>
                    </div>
                </div>
            </div>
        `;

        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    },

    // --- Chat Logic ---
    sendMessage: function () {
        const input = document.getElementById("chatInput");
        const msg = input.value.trim();
        if (!msg) return;

        this.connection.invoke("SendMessage", this.context.roomId, this.context.userName, msg, "text", "", "", this.context.senderId, this.context.receiverId);
        input.value = "";
    },

    addChatMessage: function (user, msg, type, url, name, isMe) {
        const container = document.getElementById("chatMessages");
        if (!container) return;

        const div = document.createElement("div");
        div.className = `flex ${isMe ? 'justify-end' : 'justify-start'} mb-4`;

        let body = `<p class="text-sm">${msg}</p>`;
        if (type === 'image') body = `<img src="${url}" class="max-w-[150px] rounded mt-2">`;
        if (type === 'file' || type === 'document') body = `<a href="${url}" target="_blank" class="block mt-2 font-bold underline text-blue-100">${name}</a>`;

        div.innerHTML = `
            <div class="${isMe ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-800'} p-3 rounded-2xl max-w-[80%]">
                <p class="text-[10px] font-bold opacity-75 mb-1">${isMe ? 'You' : user}</p>
                ${body}
            </div>
        `;
        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    },

    handleFileUpload: async function (e) {
        const file = e.target.files[0];
        if (!file) return;

        const fd = new FormData();
        fd.append("file", file);

        const res = await fetch("/Dashboard/UploadConsultationFile", { method: "POST", body: fd });
        const data = await res.json();

        if (data.success) {
            Telemed.connection.invoke("SendMessage", Telemed.context.roomId, Telemed.context.userName, "Shared a file", data.type, data.url, data.name, Telemed.context.senderId, Telemed.context.receiverId);
        }
    },

    // --- Prescription Logic ---
    setupRoleUI: function () {
        if (this.context.isDoctor) {
            document.getElementById("doctorPrescription")?.classList.remove("hidden");
            document.getElementById("diagnosis")?.addEventListener("input", () => this.syncPrescription());
            document.getElementById("addMedication")?.addEventListener("click", () => this.addMedInput());

            if (this.context.isLocked) {
                const diag = document.getElementById("diagnosis");
                if (diag) diag.disabled = true;
                const addBtn = document.getElementById("addMedication");
                if (addBtn) addBtn.style.display = "none";
                const finalizeBtn = document.getElementById("finalizePrescription");
                if (finalizeBtn) {
                    finalizeBtn.disabled = true;
                    finalizeBtn.innerText = "Locked";
                }
            }
        } else {
            document.getElementById("patientPrescription")?.classList.remove("hidden");
            const d = document.getElementById("diagnosis");
            if (d) d.readOnly = true;
        }

        // Load existing data if provided
        if (this.context.initialData) {
            const data = this.context.initialData;
            const diag = document.getElementById("diagnosis");
            if (diag) diag.value = data.diagnosis || "";
            if (data.medications && data.medications.length > 0) {
                const list = document.getElementById("medicationList");
                if (list) {
                    list.innerHTML = ""; // Clear loader
                    data.medications.forEach(m => {
                        this.addMedWithData(m);
                    });
                }
            }
            this.updatePrescriptionUI(data);
        }
    },

    addMedWithData: function (m) {
        const list = document.getElementById("medicationList");
        if (!list) return;
        const div = document.createElement("div");
        div.className = "med-item grid grid-cols-12 gap-2 mb-3 bg-slate-50/50 p-3 rounded-2xl border border-slate-100";
        const medicineName = m.medicineName || m.name || "Unknown Medicine";
        const dosage = m.dosage || m.dose || "";
        const frequency = m.frequency || "";
        const duration = m.duration || "";
        const quantity = m.quantity || 0;

        div.innerHTML = `
            <div class="col-span-12 md:col-span-4">
                <select class="med-select w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" ${this.context.isLocked ? 'disabled' : ''}>
                    <option value="${m.medicineId || 0}">${medicineName}</option>
                </select>
            </div>
            <div class="col-span-3 md:col-span-2">
                <input class="med-dose w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Dosage" value="${dosage}" ${this.context.isLocked ? 'disabled' : ''}>
            </div>
            <div class="col-span-3 md:col-span-2">
                <input class="med-freq w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Freq" value="${frequency}" ${this.context.isLocked ? 'disabled' : ''}>
            </div>
            <div class="col-span-3 md:col-span-2">
                <input class="med-dur w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Dur" value="${duration}" ${this.context.isLocked ? 'disabled' : ''}>
            </div>
            <div class="col-span-2 md:col-span-1">
                <input class="med-qty w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Qty" value="${quantity}" ${this.context.isLocked ? 'disabled' : ''}>
            </div>
            <div class="col-span-1 flex items-center justify-end">
                <button type="button" class="text-rose-500 hover:text-rose-700 transition-colors p-1" ${this.context.isLocked ? 'style="display:none"' : ''}><i class="fas fa-times"></i></button>
            </div>
        `;
        div.querySelector("button").onclick = () => { div.remove(); this.syncPrescription(); };
        div.querySelectorAll("input, select").forEach(i => i.oninput = () => this.syncPrescription());
        list.appendChild(div);
    },
    syncPrescription: function () {
        if (this.context.isLocked) return;

        const diagnosis = document.getElementById("diagnosis").value;
        const meds = [];
        document.querySelectorAll(".med-item").forEach(el => {
            const selectEl = el.querySelector(".med-select");
            const medicineName = selectEl?.options[selectEl?.selectedIndex]?.text || "Unknown Medicine";

            meds.push({
                medicineId: selectEl?.value || 0,
                medicineName: medicineName,
                name: medicineName, // For backward compatibility
                dosage: el.querySelector(".med-dose")?.value || "",
                dose: el.querySelector(".med-dose")?.value || "", // For backward compatibility
                frequency: el.querySelector(".med-freq")?.value || "",
                duration: el.querySelector(".med-dur")?.value || "",
                quantity: parseInt(el.querySelector(".med-qty")?.value) || 0
            });
        });
        this.connection.invoke("UpdatePrescription", this.context.roomId, JSON.stringify({ diagnosis, medications: meds }));
    },

    updatePrescriptionUI: function (data) {
        // Sync to context so checkout/buy flow picks it up
        if (this.context) {
            this.context.initialData = data;
        }

        // Update display fields for both (Live Preview)
        const diagDisplay = document.getElementById("patientDiagnosisDisplay");
        if (diagDisplay) diagDisplay.innerText = data.diagnosis || "Pending...";

        const list = document.getElementById("patientMedicationDisplay");
        if (list && data.medications) {
            if (data.medications.length === 0) {
                list.innerHTML = '<span class="text-xs text-gray-400 italic">No medications added yet</span>';
            } else {
                list.innerHTML = data.medications.map(m => {
                    const name = m.medicineName || m.name || "Medicine";
                    const dosage = m.dosage || m.dose || "N/A";
                    const freq = m.frequency || "";
                    const dur = m.duration || "";

                    return `
                        <div class="flex items-center gap-2 px-3 py-1.5 bg-white border border-blue-100 rounded-lg text-[10px] font-bold text-slate-700 shadow-sm">
                            <i data-lucide="pill" class="w-3 h-3 text-blue-500"></i>
                            ${name} - ${dosage} ${freq ? `(${freq})` : ''} ${dur ? `x ${dur}` : ''}
                        </div>
                    `;
                }).join("");
                if (typeof lucide !== 'undefined') lucide.createIcons();
            }
        }
    },

    addMedInput: function () {
        const list = document.getElementById("medicationList");
        const div = document.createElement("div");
        div.className = "med-item grid grid-cols-12 gap-2 mb-3 bg-slate-50/50 p-3 rounded-2xl border border-slate-100 relative";
        div.innerHTML = `
            <div class="col-span-12 md:col-span-4 relative group">
                <input type="text" class="med-search-input w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none focus:ring-2 focus:ring-blue-500/20" placeholder="Search Medicine...">
                <select class="med-select hidden">
                    <option value="">Select Medicine</option>
                </select>
                <div class="med-dropdown-results hidden absolute top-full left-0 right-0 z-[100] mt-1 bg-white border border-slate-100 rounded-xl shadow-2xl max-h-40 overflow-y-auto custom-scrollbar">
                    <!-- Results dynamic -->
                </div>
            </div>
            <div class="col-span-3 md:col-span-2">
                <input class="med-dose w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Dosage">
            </div>
            <div class="col-span-3 md:col-span-2">
                <input class="med-freq w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Freq">
            </div>
            <div class="col-span-3 md:col-span-2">
                <input class="med-dur w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Dur">
            </div>
            <div class="col-span-2 md:col-span-1">
                <input class="med-qty w-full border-none bg-white rounded-xl p-2 text-[10px] font-bold shadow-sm outline-none" placeholder="Qty">
            </div>
            <div class="col-span-1 flex items-center justify-end">
                <button type="button" class="text-rose-500 hover:text-rose-700 transition-colors p-1"><i class="fas fa-times"></i></button>
            </div>
        `;

        const searchInput = div.querySelector(".med-search-input");
        const resultsDiv = div.querySelector(".med-dropdown-results");
        const realSelect = div.querySelector(".med-select");

        // Initial Load
        this.searchMeds("", resultsDiv, realSelect, searchInput);

        // Search Logic
        let timeout = null;
        searchInput.addEventListener("input", (e) => {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                this.searchMeds(e.target.value, resultsDiv, realSelect, searchInput);
            }, 300);
        });

        // Toggle visibility
        searchInput.addEventListener("focus", () => resultsDiv.classList.remove("hidden"));
        document.addEventListener("click", (e) => {
            if (!div.contains(e.target)) resultsDiv.classList.add("hidden");
        });

        div.querySelector("button").onclick = () => { div.remove(); this.syncPrescription(); };
        div.querySelectorAll("input").forEach(i => i.oninput = () => this.syncPrescription());

        list.appendChild(div);

        const empty = list.querySelector(".opacity-50");
        if (empty) empty.remove();
    },

    searchMeds: async function (term, resultsDiv, realSelect, searchInput) {
        try {
            const res = await fetch(`/Pharmacy/SearchMedicines?term=${encodeURIComponent(term)}`);
            const meds = await res.json();

            resultsDiv.innerHTML = "";
            if (meds.length === 0) {
                resultsDiv.innerHTML = '<div class="p-3 text-[10px] text-slate-400 text-center">No medicines found</div>';
                return;
            }

            meds.forEach(m => {
                const item = document.createElement("div");
                item.className = "p-2 hover:bg-slate-50 cursor-pointer text-[10px] font-bold text-slate-700 border-b border-slate-50 last:border-0 flex justify-between items-center";
                item.innerHTML = `<span>${m.name}</span> <span class="text-[8px] text-slate-300 font-normal">${m.brand}</span>`;
                item.onclick = () => {
                    realSelect.innerHTML = `<option value="${m.id}" selected>${m.name}</option>`;
                    searchInput.value = m.name;
                    resultsDiv.classList.add("hidden");
                    this.syncPrescription();
                };
                resultsDiv.appendChild(item);
            });
        } catch (e) { console.error("Search failed", e); }
    },

    populateMeds: async function (select) {
        try {
            const res = await fetch('/Pharmacy/SearchMedicines?term=');
            const meds = await res.json();
            // Since we want SEARCH, let's keep it simple for now and load top 20 or use a better search later
            // Better: Load all for now if small, or implement real-time search on dropdown
            meds.forEach(m => {
                const opt = document.createElement("option");
                opt.value = m.id;
                opt.text = m.name;
                select.appendChild(opt);
            });
        } catch (e) { console.error("Search failed", e); }
    },

    finalizePrescription: async function () {
        const confirmed = await PremiumModal.confirm("Are you sure you want to finalize this EHR? It will be locked for further edits.", "Finalize EHR");
        if (!confirmed) return;

        const diagnosis = document.getElementById("diagnosis").value;
        const meds = [];
        document.querySelectorAll(".med-item").forEach(el => {
            meds.push({
                medicineId: parseInt(el.querySelector(".med-select").value),
                dosage: el.querySelector(".med-dose").value,
                frequency: el.querySelector(".med-freq").value,
                duration: el.querySelector(".med-dur").value,
                quantity: parseInt(el.querySelector(".med-qty").value) || 0,
                instructions: ""
            });
        });

        const res = await fetch('/Pharmacy/SubmitStructuredPrescription', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                appointmentId: this.context.appointmentId,
                diagnosis: diagnosis,
                notes: "",
                finalize: true,
                medicines: meds
            })
        });

        const result = await res.json();
        if (result.success) {
            await PremiumModal.success("Prescription finalized and locked.", "EHR Secured");
            window.location.reload();
        } else {
            await PremiumModal.error("Error: " + result.message, "Update Failed");
        }
    },

    // --- Control Logic ---
    toggleMute: function () {
        if (!this.localStream) return;
        const audioTrack = this.localStream.getAudioTracks()[0];
        if (audioTrack) {
            audioTrack.enabled = !audioTrack.enabled;
            // Update UI
            const btn = document.getElementById("btnMute");
            if (btn) {
                if (audioTrack.enabled) {
                    btn.classList.remove("bg-red-500", "text-white");
                    btn.classList.add("bg-gray-50", "text-gray-400"); // Reset style
                    // Handle icon change if needed, but color is usually enough
                } else {
                    btn.classList.remove("bg-gray-50", "text-gray-400");
                    btn.classList.add("bg-red-500", "text-white");
                }
            }
        }
    },

    toggleVideo: function () {
        if (!this.localStream) return;
        const videoTrack = this.localStream.getVideoTracks()[0];
        if (videoTrack) {
            videoTrack.enabled = !videoTrack.enabled;
            // Update UI
            const btn = document.getElementById("btnVideo");
            if (btn) {
                if (videoTrack.enabled) {
                    btn.classList.remove("bg-red-500", "text-white");
                    btn.classList.add("bg-gray-50", "text-gray-400"); // Reset style
                } else {
                    btn.classList.remove("bg-gray-50", "text-gray-400");
                    btn.classList.add("bg-red-500", "text-white");
                }
            }
        }
    },

    // --- Setup ---
    setupEvents: function () {
        const self = this;
        document.getElementById("sendBtn")?.addEventListener("click", () => self.sendMessage());
        document.getElementById("chatInput")?.addEventListener("keypress", e => { if (e.key === "Enter") self.sendMessage(); });

        document.getElementById("attachBtn")?.addEventListener("click", () => document.getElementById("fileInput").click());
        document.getElementById("fileInput")?.addEventListener("change", (e) => self.handleFileUpload(e));

        // Call Controls
        document.getElementById("btnVideo")?.addEventListener("click", () => self.toggleVideo());
        document.getElementById("finalizePrescription")?.addEventListener("click", () => self.finalizePrescription());
        document.getElementById("finalizeEhr")?.addEventListener("click", () => self.finalizePrescription()); // Support both IDs if any

        // Handle both "leaveCall" (existing) and "btnEndCall" (standardized)
        const endCallHandler = (e) => {
            if (e) e.preventDefault();
            self.connection.invoke("EndCall", self.context.roomId);
            window.location.href = self.context.isDoctor ? "/Doctor" : "/Dashboard";
        };
        document.getElementById("leaveCall")?.addEventListener("click", endCallHandler);
        document.getElementById("btnEndCall")?.addEventListener("click", endCallHandler);

        // Transcription Button
        document.getElementById("toggleTranscription")?.addEventListener("click", () => self.toggleTranscription());
    },

    startTimer: function () {
        let sc = 0;
        setInterval(() => {
            sc++;
            const f = n => n.toString().padStart(2, '0');
            const str = `${f(Math.floor(sc / 3600))}:${f(Math.floor((sc % 3600) / 60))}:${f(sc % 60)}`;
            const el = document.getElementById("callTimer");
            if (el) el.innerText = str;
        }, 1000);
    }
};

// Global Exposure
window.Telemed = Telemed;
