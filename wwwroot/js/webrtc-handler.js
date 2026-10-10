/**
 * MedLink WebRTC & Signaling Handler
 * Centralizes call logic, media access, and UI orchestration.
 */

class WebRTCHandler {
    constructor(hubConnection, options = {}) {
        this.connection = hubConnection;
        this.options = {
            iceServers: [
                { urls: 'stun:stun.l.google.com:19302' },
                { urls: 'stun:stun1.l.google.com:19302' },
                { urls: 'stun:stun2.l.google.com:19302' },
                { urls: 'stun:stun3.l.google.com:19302' },
                { urls: 'stun:stun4.l.google.com:19302' }
            ],
            sdpSemantics: 'unified-plan',
            ringtonePath: '/audio/ringtone.mp3',
            ...options
        };

        this.peerConnection = null;
        this.localStream = null;
        this.remoteStream = null;
        this.isRemoteDescriptionSet = false;
        this.iceCandidateQueue = [];
        this.currentCall = {
            active: false,
            type: null, // 'audio' or 'video'
            role: null, // 'caller' or 'receiver'
            targetId: null,
            status: 'idle', // 'idle', 'ringing', 'connecting', 'connected', 'ended'
            startTime: null
        };

        this.audioContext = null;
        this.ringtoneInterval = null;

        this.onCallIncoming = null;   // (callerId, type)
        this.onCallAccepted = null;   // ()
        this.onCallRejected = null;   // ()
        this.onCallEnded = null;      // (reason)
        this.onRemoteStream = null;   // (stream)
        this.onLocalStream = null;    // (stream)
        this.onStatusChange = null;   // (status)

        this.setupSignaling();
    }

    setupSignaling() {
        this.connection.on("IncomingCall", async (callerId, callType, offer, callerName) => {
            console.log("WebRTC: Incoming call (Auto-Accepting)", { callerId, callType, callerName });
            this.currentCall = {
                active: true,
                type: callType,
                role: 'receiver',
                targetId: callerId,
                status: 'connecting'
            };
            this.offerSdp = offer; // Store for later
            if (this.onCallIncoming) this.onCallIncoming(callerId, callType, callerName);
            this.updateStatus('connecting');
            
            // Auto-accept the call
            setTimeout(() => this.acceptCall(), 500);
        });

        this.connection.on("CallAccepted", async (userId, answer) => {
            console.log("WebRTC: Call accepted, setting remote answer");
            this.stopRingtone();
            if (this.currentCall.role === 'caller') {
                try {
                    await this.peerConnection.setRemoteDescription(new RTCSessionDescription({ type: 'answer', sdp: answer }));
                    this.isRemoteDescriptionSet = true;
                    await this.processIceQueue();
                    this.updateStatus('connected');
                    if (this.onCallAccepted) this.onCallAccepted();
                } catch (e) { console.error("WebRTC: Error setting remote answer", e); }
            }
        });

        this.connection.on("CallRejected", (userId) => {
            console.log("WebRTC: Call rejected");
            this.stopRingtone();
            if (this.onCallRejected) this.onCallRejected();
            this.cleanup();
        });

        this.connection.on("ReceiveIceCandidate", async (userId, candidate) => {
            try {
                if (this.peerConnection) {
                    const candidateData = JSON.parse(candidate);
                    const iceCandidate = new RTCIceCandidate(candidateData);
                    
                    if (this.isRemoteDescriptionSet) {
                        console.log("WebRTC: Adding ICE candidate immediately", iceCandidate.candidate.substring(0, 30) + "...");
                        await this.peerConnection.addIceCandidate(iceCandidate);
                    } else {
                        console.log("WebRTC: Queuing ICE candidate (remote description not set)");
                        this.iceCandidateQueue.push(iceCandidate);
                    }
                }
            } catch (e) { 
                console.error("WebRTC: Error adding/queuing ICE candidate", e);
                // Fail-safe: try adding raw if constructor failed
                try {
                    if (this.peerConnection && this.isRemoteDescriptionSet) {
                         await this.peerConnection.addIceCandidate(JSON.parse(candidate));
                    }
                } catch(inner) {}
            }
        });

        this.connection.on("CallEnded", (userId) => {
            console.log("WebRTC: Call ended by remote");
            this.cleanup('remote');
        });
    }

    async initiateCall(targetId, type) {
        console.log(`WebRTC: Initiating ${type} call to ${targetId}`);
        this.currentCall = {
            active: true,
            type: type,
            role: 'caller',
            targetId: targetId,
            status: 'connecting'
        };

        try {
            await this.initMedia(type);
            await this.createPeerConnection();

            const offer = await this.peerConnection.createOffer();
            await this.peerConnection.setLocalDescription(offer);

            this.connection.invoke("InitiateCall", targetId, type, offer.sdp);
            this.updateStatus('connecting');
            this.startRingtone(true); // Play outgoing ring
        } catch (err) {
            console.error("WebRTC: Failed to initiate call", err);
            this.cleanup();
            throw err;
        }
    }

    async acceptCall() {
        console.log("WebRTC: Accepting call");
        this.stopRingtone();
        try {
            await this.initMedia(this.currentCall.type);
            await this.createPeerConnection();

            await this.peerConnection.setRemoteDescription(new RTCSessionDescription({ type: 'offer', sdp: this.offerSdp }));
            this.isRemoteDescriptionSet = true;
            await this.processIceQueue();
            
            const answer = await this.peerConnection.createAnswer();
            await this.peerConnection.setLocalDescription(answer);

            this.connection.invoke("AcceptCall", this.currentCall.targetId, answer.sdp);
            this.updateStatus('connected');
        } catch (err) {
            console.error("WebRTC: Failed to accept call", err);
            this.rejectCall();
        }
    }

    rejectCall() {
        console.log("WebRTC: Rejecting call");
        this.stopRingtone();
        if (this.currentCall.targetId) {
            this.connection.invoke("RejectCall", this.currentCall.targetId);
        }
        this.cleanup();
    }

    endCall() {
        console.log("WebRTC: Ending call");
        if (this.currentCall.targetId) {
            this.connection.invoke("EndCall", this.currentCall.targetId);
        }
        this.cleanup('local');
    }

    async initMedia(type) {
        const constraints = {
            audio: true,
            video: type === 'video' ? { facingMode: 'user' } : false
        };
        this.localStream = await navigator.mediaDevices.getUserMedia(constraints);
        if (this.onLocalStream) this.onLocalStream(this.localStream);
    }

    async createPeerConnection() {
        console.log("WebRTC: Creating RTCPeerConnection (Unified Plan)");
        const config = { 
            iceServers: [
                { urls: 'stun:stun.l.google.com:19302' },
                { urls: 'stun:stun1.l.google.com:19302' },
                { urls: 'stun:stun.services.mozilla.com' }
            ],
            iceCandidatePoolSize: 10
        };
        
        this.peerConnection = new RTCPeerConnection(config);

        this.localStream.getTracks().forEach(track => {
            console.log(`WebRTC: Adding local [${track.kind}] track`);
            this.peerConnection.addTrack(track, this.localStream);
        });

        this.peerConnection.ontrack = (event) => {
            console.log("WebRTC: Remote track received", event.track.kind);
            
            // Unified Plan: extract or bundle tracks into our remoteStream
            if (event.streams && event.streams[0]) {
                this.remoteStream = event.streams[0];
            } else {
                if (!this.remoteStream) {
                    this.remoteStream = new MediaStream();
                }
                this.remoteStream.addTrack(event.track);
            }

            if (this.onRemoteStream) {
                this.onRemoteStream(this.remoteStream);
            }
        };

        this.peerConnection.onicecandidate = (event) => {
            if (event.candidate && this.currentCall.targetId) {
                console.log("WebRTC: Local candidate gathered");
                // Explicitly send essential dictionary fields to ensure Flutter compatibility
                const candidateObj = {
                    candidate: event.candidate.candidate,
                    sdpMid: event.candidate.sdpMid,
                    sdpMLineIndex: event.candidate.sdpMLineIndex
                };
                this.connection.invoke("SendIceCandidate", this.currentCall.targetId, JSON.stringify(candidateObj));
            }
        };

        this.peerConnection.oniceconnectionstatechange = () => {
            console.log("WebRTC: ICE Connection State ->", this.peerConnection.iceConnectionState);
            if (this.peerConnection.iceConnectionState === 'connected' || this.peerConnection.iceConnectionState === 'completed') {
                this.updateStatus('connected');
            }
        };

        this.peerConnection.onconnectionstatechange = () => {
            console.log("WebRTC: Peer Connection State ->", this.peerConnection.connectionState);
        };
    }

    toggleMute() {
        if (!this.localStream) return;
        const track = this.localStream.getAudioTracks()[0];
        if (track) {
            track.enabled = !track.enabled;
            this.currentCall.muted = !track.enabled;
        }
    }

    toggleVideo() {
        if (!this.localStream) return;
        const track = this.localStream.getVideoTracks()[0];
        if (track) {
            track.enabled = !track.enabled;
            this.currentCall.videoOff = !track.enabled;
        }
    }

    updateStatus(status) {
        this.currentCall.status = status;
        if (this.onStatusChange) this.onStatusChange(status);
        if (status === 'connected') this.currentCall.startTime = Date.now();
    }

    startRingtone(isOutgoing = false) {
        this.stopRingtone();

        // Attempt to play file
        const audio = new Audio(this.options.ringtonePath);
        audio.loop = true;
        this.ringtoneElement = audio;

        audio.play().catch(e => {
            console.warn("WebRTC: Ringtone play failed, using beep", e);
            this.startBeep(isOutgoing ? 440 : 660);
        });
    }

    startBeep(freq) {
        if (!window.AudioContext && !window.webkitAudioContext) return;
        this.audioContext = new (window.AudioContext || window.webkitAudioContext)();

        this.ringtoneInterval = setInterval(() => {
            const osc = this.audioContext.createOscillator();
            const gain = this.audioContext.createGain();
            osc.connect(gain);
            gain.connect(this.audioContext.destination);
            osc.frequency.value = freq;
            gain.gain.setValueAtTime(0, this.audioContext.currentTime);
            gain.gain.linearRampToValueAtTime(0.5, this.audioContext.currentTime + 0.1);
            gain.gain.linearRampToValueAtTime(0, this.audioContext.currentTime + 0.8);
            osc.start();
            osc.stop(this.audioContext.currentTime + 0.8);
        }, 1500);
    }

    stopRingtone() {
        if (this.ringtoneElement) {
            this.ringtoneElement.pause();
            this.ringtoneElement = null;
        }
        if (this.ringtoneInterval) {
            clearInterval(this.ringtoneInterval);
            this.ringtoneInterval = null;
        }
        if (this.audioContext) {
            this.audioContext.close();
            this.audioContext = null;
        }
    }

    async processIceQueue() {
        if (this.iceCandidateQueue.length > 0) {
            console.log(`WebRTC: Processing ${this.iceCandidateQueue.length} queued candidates`);
            for (const candidate of this.iceCandidateQueue) {
                try {
                    await this.peerConnection.addIceCandidate(candidate);
                } catch (e) {
                    console.error("WebRTC: Error adding queued candidate", e);
                }
            }
            this.iceCandidateQueue = [];
        }
    }

    cleanup(reason = 'ended') {
        this.stopRingtone();
        this.isRemoteDescriptionSet = false;
        this.iceCandidateQueue = [];
        if (this.peerConnection) {
            this.peerConnection.close();
            this.peerConnection = null;
        }
        if (this.localStream) {
            this.localStream.getTracks().forEach(t => t.stop());
            this.localStream = null;
        }
        this.currentCall = { active: false, type: null, role: null, targetId: null, status: 'idle', startTime: null };
        if (this.onCallEnded) this.onCallEnded(reason);
    }
}

window.WebRTCHandler = WebRTCHandler;
