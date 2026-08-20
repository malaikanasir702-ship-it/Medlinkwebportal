let connection = null;
let localStream = null;
let peerConnection = null;
let roomContext = null;

const rtcConfig = {
    iceServers: [
        { urls: "stun:stun.l.google.com:19302" },
        { urls: "stun:stun1.l.google.com:19302" }
    ]
};

async function initConsultation(context) {
    console.log("Starting Consultation Init...", context);
    roomContext = context;

    // UI Setup
    setupUIListeners();
    setupRoleBasedUI();

    // Start Media & SignalR in parallel for speed
    await Promise.all([initMedia(), initSignalR()]);
}

async function initMedia() {
    try {
        console.log("Requesting Media Access...");
        localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });

        // Show Local Video
        displayLocalStream(localStream);

        // Update Status
        updateDebugStatus("Media Active");
    } catch (err) {
        console.error("Media Error:", err);
        alert("Camera/Microphone access is required for the consultation. Please check your browser settings.");
        updateDebugStatus("Media Denied");
    }
}

function displayLocalStream(stream) {
    const localVideo = document.createElement("video");
    localVideo.srcObject = stream;
    localVideo.muted = true;
    localVideo.autoplay = true;
    localVideo.playsInline = true;
    localVideo.className = "w-full h-full object-cover rounded-[2rem] transform -scale-x-100";

    const container = document.getElementById("localVideoContainer");
    if (container) {
        container.innerHTML = "";
        container.appendChild(localVideo);
    }
}

async function initSignalR() {
    updateDebugStatus("Connecting SignalR...");
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/consultationHub")
        .withAutomaticReconnect()
        .build();

    // --- Signaling Handlers ---

    connection.on("UserJoined", (connectionId) => {
        console.log("User Joined:", connectionId);
        // If I am Patient, tell them I am here
        // If I am Doctor, I initiate call
        if (roomContext.isDoctor) {
            console.log("Doctor: User joined, initiating call...");
            initiateCall();
        } else {
            console.log("Patient: Doctor/User joined, announcing presence...");
            connection.invoke("AnnouncePresence", roomContext.roomId);
        }
    });

    connection.on("UserIsPresent", (connectionId) => {
        console.log("User Is Present:", connectionId);
        // If I am Doctor and I just joined (or other person was already there), I initiate
        if (roomContext.isDoctor) {
            console.log("Doctor: User present, initiating call...");
            initiateCall();
        }
    });

    /* Redundant: Handled globally by webrtc-handler.js
    connection.on("IncomingCall", async (callerId, callType, offerSdp) => {
        console.log("Incoming Call...", callType);
        updateDebugStatus("Incoming Call");
        await handleOffer(offerSdp);
    });
    */

    connection.on("CallAccepted", async (userId, answerSdp) => {
        console.log("Call Accepted!");
        updateDebugStatus("Call Connected");
        await handleAnswer(answerSdp);
    });

    connection.on("ReceiveIceCandidate", async (userId, candidateJson) => {
        if (peerConnection) {
            try {
                await peerConnection.addIceCandidate(new RTCIceCandidate(JSON.parse(candidateJson)));
            } catch (e) { console.error("ICE Error:", e); }
        }
    });

    // --- Data Handlers ---

    connection.on("ReceiveMessage", (user, message, type, url, name, senderId) => {
        addMessageToChat(user, message, type, url, name, senderId === roomContext.senderId);
    });

    connection.on("PrescriptionUpdated", (json) => {
        updatePrescriptionUI(JSON.parse(json));
    });

    try {
        await connection.start();
        console.log("SignalR Connected");
        updateDebugStatus("Connected. Joining Room...");

        await connection.invoke("JoinRoom", roomContext.roomId);
        updateDebugStatus("Room Joined. Waiting...");

        // If I am joining late, I should announce myself just in case?
        // Actually JoinRoom triggers UserJoined for others.
        // If I am Doctor and Patient is ALREADY there, Patient gets UserJoined -> AnnouncePresence -> Doctor gets UserIsPresent -> InitiateCall.
        // If I am Patient and Doctor is ALREADY there, Doctor gets UserJoined -> InitiateCall.

        // Start timers
        startTimer();
        hideSkeletonChat();

    } catch (err) {
        console.error("SignalR Connection Error:", err);
        updateDebugStatus("Connection Failed");
    }
}

// --- WebRTC Logic ---

async function createPeerConnection() {
    if (peerConnection) peerConnection.close();

    peerConnection = new RTCPeerConnection(rtcConfig);

    // Add Tracks
    if (localStream) {
        localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));
    }

    // Handle Remote Stream
    peerConnection.ontrack = (event) => {
        console.log("Remote Stream Received");
        displayRemoteStream(event.streams[0]);
    };

    // Handle ICE
    peerConnection.onicecandidate = (event) => {
        if (event.candidate) {
            connection.invoke("SendIceCandidate", roomContext.roomId, JSON.stringify(event.candidate));
        }
    };
}

async function initiateCall() {
    if (!localStream) await initMedia();

    await createPeerConnection();

    const offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);

    connection.invoke("InitiateCall", roomContext.roomId, "video", offer.sdp);
}

async function handleOffer(offerSdp) {
    if (!localStream) await initMedia();

    await createPeerConnection();

    await peerConnection.setRemoteDescription(new RTCSessionDescription({ type: 'offer', sdp: offerSdp }));

    const answer = await peerConnection.createAnswer();
    await peerConnection.setLocalDescription(answer);

    connection.invoke("AcceptCall", roomContext.roomId, answer.sdp);
}

async function handleAnswer(answerSdp) {
    await peerConnection.setRemoteDescription(new RTCSessionDescription({ type: 'answer', sdp: answerSdp }));
}

function displayRemoteStream(stream) {
    const remoteVideo = document.createElement("video");
    remoteVideo.srcObject = stream;
    remoteVideo.autoplay = true;
    remoteVideo.playsInline = true;
    remoteVideo.className = "w-full h-full object-cover rounded-[2rem]";

    // Try multiple possible containers
    const containers = ["remoteVideoContainer", "mainVideoContainer"];
    let container = null;
    for (const id of containers) {
        container = document.getElementById(id);
        if (container) break;
    }

    if (container) {
        container.innerHTML = ""; // Clear placeholders
        container.appendChild(remoteVideo);
    }
}

// --- UI & Utils ---

function updateDebugStatus(status) {
    const el = document.getElementById("debug-status");
    if (el) el.innerText = status;
}

function setupUIListeners() {
    // Chat
    document.getElementById("sendBtn")?.addEventListener("click", () => sendMessage());
    document.getElementById("chatInput")?.addEventListener("keypress", (e) => {
        if (e.key === "Enter") sendMessage();
    });

    // File Upload
    const fileInput = document.getElementById("fileInput");
    const attachBtn = document.getElementById("attachBtn");
    if (attachBtn && fileInput) {
        attachBtn.addEventListener("click", () => fileInput.click());
        fileInput.addEventListener("change", handleFileUpload);
    }

    // Controls
    const muteBtn = document.querySelector(".fa-microphone")?.parentElement || document.querySelector("[data-lucide='mic']")?.parentElement;
    if (muteBtn) muteBtn.onclick = toggleMute;

    const videoBtn = document.querySelector(".fa-video")?.parentElement || document.querySelector("[data-lucide='video']")?.parentElement;
    if (videoBtn) videoBtn.onclick = toggleVideo;

    const endBtn = document.getElementById("leaveCall");
    if (endBtn) endBtn.onclick = () => {
        if (confirm("End Consultation?")) {
            connection.invoke("EndCall", roomContext.roomId);
            window.location.href = roomContext.isDoctor ? "/Doctor" : "/Dashboard";
        }
    };
}

function setupRoleBasedUI() {
    if (roomContext.isDoctor) {
        document.getElementById("doctorPrescription")?.classList.remove("hidden");
        document.getElementById("addMedication")?.addEventListener("click", addMedicationRow);
        document.getElementById("diagnosis")?.addEventListener("input", syncPrescription);
        document.getElementById("finalizePrescription")?.addEventListener("click", finalizeEHR);
    } else {
        document.getElementById("patientPrescription")?.classList.remove("hidden");
        const diag = document.getElementById("diagnosis");
        if (diag) diag.readOnly = true;
    }
}

function toggleMute() {
    if (localStream) {
        const track = localStream.getAudioTracks()[0];
        track.enabled = !track.enabled;
        // Optionally update UI icon style
    }
}

function toggleVideo() {
    if (localStream) {
        const track = localStream.getVideoTracks()[0];
        track.enabled = !track.enabled;
    }
}

// --- Chat Logic Same as Before ---
function sendMessage(content = null, type = "text", url = "", name = "") {
    const input = document.getElementById("chatInput");
    const msg = content || input.value.trim();
    if (!msg && !url) return;

    connection.invoke("SendMessage", roomContext.roomId, roomContext.userName, msg, type, url, name, roomContext.senderId, roomContext.receiverId)
        .catch(console.error);

    if (!content) input.value = "";
}

async function handleFileUpload(e) {
    const file = e.target.files[0];
    if (!file) return;

    const fd = new FormData();
    fd.append("file", file);

    try {
        const res = await fetch("/Dashboard/UploadConsultationFile", { method: "POST", body: fd });
        const data = await res.json();
        if (data.success) {
            sendMessage("Sent an attachment", data.type, data.url, data.name);
        }
    } catch (err) { console.error(err); }
    e.target.value = "";
}

function addMessageToChat(user, message, type, url, name, isMe) {
    const container = document.getElementById("chatMessages");
    const div = document.createElement("div");
    div.className = `flex ${isMe ? 'justify-end' : 'justify-start'} mb-4`;

    let contentHtml = `<p class="text-sm opacity-90">${message}</p>`;

    if (type === "image") {
        contentHtml = `<div class="mt-2 mb-1 rounded-lg overflow-hidden border border-white/20"><img src="${url}" class="max-w-[200px] object-cover"></div>`;
    } else if (type === "file" || type === "document") {
        contentHtml = `<a href="${url}" target="_blank" class="flex items-center gap-2 mt-2 p-2 bg-black/5 rounded-lg"><span class="font-bold underline">${name}</span></a>`;
    }

    div.innerHTML = `
        <div class="message-bubble ${isMe ? 'bg-blue-600 text-white rounded-tr-none' : 'bg-gray-100 text-gray-800 rounded-tl-none'} p-4 rounded-2xl shadow-sm max-w-[80%]">
            <p class="text-[10px] font-bold mb-1 opacity-70">${isMe ? 'You' : user}</p>
            ${contentHtml}
        </div>
    `;
    container.appendChild(div);
    container.scrollTop = container.scrollHeight;
}

// --- Prescription Logic ---
function addMedicationRow() {
    const list = document.getElementById("medicationList");
    const div = document.createElement("div");
    div.className = "flex items-center gap-2 p-2 bg-gray-50 rounded-xl mb-2";
    div.innerHTML = `
        <input type="text" class="med-name bg-transparent border-none text-xs font-bold flex-1" placeholder="Medication Name">
        <input type="text" class="med-dose bg-transparent border-none text-[10px] w-16" placeholder="Dose">
        <button class="remove-med text-rose-500 font-bold">×</button>
    `;
    list.appendChild(div);

    div.querySelector(".remove-med").onclick = () => { div.remove(); syncPrescription(); };
    div.querySelectorAll("input").forEach(i => i.oninput = syncPrescription);
}

function syncPrescription() {
    if (!roomContext.isDoctor) return;
    const diagnosis = document.getElementById("diagnosis").value;
    const meds = [];
    document.querySelectorAll("#medicationList > div").forEach(div => {
        const name = div.querySelector(".med-name").value;
        const dose = div.querySelector(".med-dose").value;
        meds.push({
            medicineName: name,
            name: name,
            dosage: dose,
            dose: dose
        });
    });
    connection.invoke("UpdatePrescription", roomContext.roomId, JSON.stringify({ diagnosis, medications: meds }));
}

function updatePrescriptionUI(data) {
    if (roomContext) {
        roomContext.initialData = data;
    }

    if (roomContext.isDoctor) {
        if (document.activeElement.id !== "diagnosis") {
            document.getElementById("diagnosis").value = data.diagnosis;
        }
    } else {
        document.getElementById("patientDiagnosisDisplay").innerText = data.diagnosis || "Pending...";
        const display = document.getElementById("patientMedicationDisplay");
        if (data.medications?.length) {
            display.innerHTML = data.medications.map(m => {
                const name = m.medicineName || m.name || "Medicine";
                const dose = m.dosage || m.dose || "N/A";
                return `
                    <div class="px-3 py-1 bg-white border border-blue-100 rounded-lg flex gap-2 items-center">
                        <span class="w-2 h-2 rounded-full bg-blue-500"></span>
                        <span class="font-bold text-xs">${name}</span>
                        <span class="text-[10px] text-gray-500">${dose}</span>
                    </div>
                `;
            }).join("");
        } else {
            display.innerHTML = `<span class="text-xs text-gray-400 italic">No medications</span>`;
        }
    }
}

function finalizeEHR() {
    if (confirm("Finalize Prescription?")) {
        sendMessage("System", "text", "", "", "Dr. has finalized the prescription.");
        // Logic to save to DB could go here via fetch
    }
}

function hideSkeletonChat() {
    document.querySelectorAll(".skeleton-chat").forEach(s => s.classList.add("hidden"));
}

function startTimer() {
    let s = 0;
    setInterval(() => {
        s++;
        const fmt = val => val.toString().padStart(2, '0');
        document.getElementById("callTimer").innerText = `${fmt(Math.floor(s / 3600))}:${fmt(Math.floor((s % 3600) / 60))}:${fmt(s % 60)}`;
    }, 1000);
}
