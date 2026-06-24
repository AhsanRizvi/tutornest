import { Component, OnInit, OnDestroy, signal, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LiveClassService } from '../../services/liveclass.service';
import { AuthService } from '../../services/auth.service';
import { LiveClassResponse } from '../../models';
import AgoraRTC, { IAgoraRTCClient, ILocalVideoTrack, ILocalAudioTrack, IAgoraRTCRemoteUser } from 'agora-rtc-sdk-ng';

@Component({
  selector: 'app-live-class-room',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './live-class-room.component.html',
  styleUrls: ['./live-class-room.component.scss']
})
export class LiveClassRoomComponent implements OnInit, OnDestroy {
  @ViewChild('localVideoContainer', { static: false }) localVideoContainer!: ElementRef;

  classId = '';
  liveClass = signal<LiveClassResponse | null>(null);
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);

  // Agora State
  agoraClient: IAgoraRTCClient | null = null;
  localAudioTrack: ILocalAudioTrack | null = null;
  localVideoTrack: ILocalVideoTrack | null = null;
  screenVideoTrack: any = null;
  
  isMuted = signal<boolean>(false);
  isCameraOff = signal<boolean>(false);
  isScreenSharing = signal<boolean>(false);
  isConnected = signal<boolean>(false);
  isSimulatorMode = signal<boolean>(false);

  // Participants lists
  remoteUsers = signal<IAgoraRTCRemoteUser[]>([]);
  isTeacher = signal<boolean>(false);
  localUserLabel = signal<string>('You');
  teacherUid = signal<number | null>(null);
  
  // Local stream for simulator mode
  localStream: MediaStream | null = null;

  // Chat state
  chatMessages = signal<{ sender: string, text: string, time: string }[]>([]);
  newMessageText = '';

  // Call duration counter
  elapsedTime = signal<string>('00:00');
  private timerInterval: any;
  private secondsElapsed = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private liveClassService: LiveClassService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.classId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.classId) {
      this.error.set('Invalid Live Class Session ID.');
      this.isLoading.set(false);
      return;
    }
    
    // Check if current user is teacher
    const role = this.authService.userRole();
    console.log('LiveClassRoom Component Init - User Role from AuthService:', role);
    this.isTeacher.set(role === 'Teacher');
    this.localUserLabel.set(this.isTeacher() ? 'You (Teacher)' : 'You (Student)');

    this.loadClassDetails();
  }

  ngOnDestroy(): void {
    this.cleanupCall();
  }

  loadClassDetails(): void {
    this.liveClassService.getLiveClassById(this.classId).subscribe({
      next: (data) => {
        this.liveClass.set(data);
        this.loadAgoraToken();
      },
      error: () => {
        this.error.set('Failed to load live class details. It may have been deleted or you do not have permission.');
        this.isLoading.set(false);
      }
    });
  }

  loadAgoraToken(): void {
    this.liveClassService.getAgoraToken(this.classId).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        this.teacherUid.set(res.teacherUid);
        if (res.appId && res.token) {
          this.initAgora(res.appId, res.channelName, res.token, res.uid);
        } else {
          this.isSimulatorMode.set(true);
          this.isConnected.set(true);
          this.startCallTimer();
          this.addSystemMessage('Agora configurations not found. Running in Virtual Simulator Mode.');
          this.initSimulatorCamera();
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.isSimulatorMode.set(true);
        this.isConnected.set(true);
        this.startCallTimer();
        this.addSystemMessage('Token API error. Running in Virtual Simulator Mode.');
        this.initSimulatorCamera();
      }
    });
  }

  async initAgora(appId: string, channel: string, token: string, uid: number): Promise<void> {
    try {
      this.agoraClient = AgoraRTC.createClient({ mode: 'rtc', codec: 'vp8' });
      
      this.agoraClient.on('user-published', async (user, mediaType) => {
        await this.agoraClient!.subscribe(user, mediaType);
        if (mediaType === 'video') {
          this.updateRemoteUsersList();
          setTimeout(() => {
            user.videoTrack?.play(`remote-player-${user.uid}`);
          }, 100);
        }
        if (mediaType === 'audio') {
          user.audioTrack?.play();
        }
      });

      this.agoraClient.on('user-unpublished', (user) => {
        this.updateRemoteUsersList();
      });

      this.agoraClient.on('user-left', (user) => {
        this.updateRemoteUsersList();
      });

      await this.agoraClient.join(appId, channel, token, uid);
      this.isConnected.set(true);
      this.startCallTimer();
      
      try {
        [this.localAudioTrack, this.localVideoTrack] = await AgoraRTC.createMicrophoneAndCameraTracks();
        this.localVideoTrack.play('local-player');
        await this.agoraClient.publish([this.localAudioTrack, this.localVideoTrack]);
      } catch (trackError) {
        console.warn('Microphone or Camera access blocked:', trackError);
        this.addSystemMessage('Camera/Mic access denied. You are joined as an audience member.');
      }

      this.addSystemMessage('Connected to Agora Live Room.');
    } catch (e: any) {
      console.error('Agora init failed:', e);
      this.error.set('Failed to connect to Agora servers: ' + e.message);
      this.isSimulatorMode.set(true);
      this.isConnected.set(true);
      this.startCallTimer();
      this.initSimulatorCamera();
    }
  }

  updateRemoteUsersList(): void {
    if (this.agoraClient) {
      this.remoteUsers.set(this.agoraClient.remoteUsers);
    }
  }

  async initSimulatorCamera(): Promise<void> {
    if (this.localStream) {
      this.localStream.getTracks().forEach(t => t.stop());
    }

    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
      const videoElement = document.createElement('video');
      videoElement.srcObject = this.localStream;
      videoElement.autoplay = true;
      videoElement.muted = true;
      videoElement.playsInline = true;
      videoElement.style.width = '100%';
      videoElement.style.height = '100%';
      videoElement.style.objectFit = 'cover';
      
      const container = document.getElementById('local-player');
      if (container) {
        container.innerHTML = '';
        container.appendChild(videoElement);
      }
    } catch (err) {
      console.warn('Failed to access camera in simulator mode:', err);
    }
  }

  async toggleAudio(): Promise<void> {
    if (this.isSimulatorMode()) {
      this.isMuted.set(!this.isMuted());
      if (this.localStream) {
        this.localStream.getAudioTracks().forEach(track => track.enabled = !this.isMuted());
      }
      return;
    }

    if (!this.localAudioTrack) {
      try {
        this.localAudioTrack = await AgoraRTC.createMicrophoneAudioTrack();
        this.isMuted.set(false);
        if (this.agoraClient) {
          await this.agoraClient.publish(this.localAudioTrack);
        }
        this.addSystemMessage('Microphone enabled successfully.');
      } catch (err) {
        console.error('Failed to enable microphone:', err);
        this.addSystemMessage('Unable to access microphone.');
      }
      return;
    }

    if (this.isMuted()) {
      await this.localAudioTrack.setEnabled(true);
      this.isMuted.set(false);
    } else {
      await this.localAudioTrack.setEnabled(false);
      this.isMuted.set(true);
    }
  }

  async toggleVideo(): Promise<void> {
    if (this.isSimulatorMode()) {
      this.isCameraOff.set(!this.isCameraOff());
      if (this.localStream) {
        this.localStream.getVideoTracks().forEach(track => track.enabled = !this.isCameraOff());
      }
      if (!this.isCameraOff()) {
        setTimeout(() => this.initSimulatorCamera(), 100);
      } else {
        const container = document.getElementById('local-player');
        if (container) container.innerHTML = '';
      }
      return;
    }

    if (!this.localVideoTrack) {
      try {
        this.localVideoTrack = await AgoraRTC.createCameraVideoTrack();
        this.isCameraOff.set(false);
        setTimeout(() => this.localVideoTrack?.play('local-player'), 100);
        if (this.agoraClient) {
          await this.agoraClient.publish(this.localVideoTrack);
        }
        this.addSystemMessage('Camera enabled successfully.');
      } catch (err) {
        console.error('Failed to enable camera:', err);
        this.addSystemMessage('Unable to access camera.');
      }
      return;
    }

    if (this.isCameraOff()) {
      await this.localVideoTrack.setEnabled(true);
      this.isCameraOff.set(false);
      setTimeout(() => this.localVideoTrack?.play('local-player'), 100);
    } else {
      await this.localVideoTrack.setEnabled(false);
      this.isCameraOff.set(true);
    }
  }

  async toggleScreenShare(): Promise<void> {
    if (this.isSimulatorMode()) {
      if (this.isScreenSharing()) {
        this.isScreenSharing.set(false);
        this.initSimulatorCamera();
      } else {
        try {
          const screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
          this.isScreenSharing.set(true);
          const videoElement = document.createElement('video');
          videoElement.srcObject = screenStream;
          videoElement.autoplay = true;
          videoElement.playsInline = true;
          videoElement.style.width = '100%';
          videoElement.style.height = '100%';
          videoElement.style.objectFit = 'cover';
          
          const container = document.getElementById('local-player');
          if (container) {
            container.innerHTML = '';
            container.appendChild(videoElement);
          }

          screenStream.getVideoTracks()[0].onended = () => {
            this.isScreenSharing.set(false);
            this.initSimulatorCamera();
          };
        } catch (err) {
          console.warn('Screen share cancelled in simulator:', err);
        }
      }
      return;
    }

    if (!this.agoraClient) return;

    try {
      if (this.isScreenSharing()) {
        if (this.screenVideoTrack) {
          await this.agoraClient.unpublish(this.screenVideoTrack);
          this.screenVideoTrack.close();
          this.screenVideoTrack = null;
        }
        if (this.localVideoTrack) {
          await this.agoraClient.publish(this.localVideoTrack);
          this.localVideoTrack.play('local-player');
        }
        this.isScreenSharing.set(false);
      } else {
        this.screenVideoTrack = await AgoraRTC.createScreenVideoTrack({}, "auto");
        
        if (this.localVideoTrack) {
          await this.agoraClient.unpublish(this.localVideoTrack);
        }
        
        await this.agoraClient.publish(this.screenVideoTrack);
        this.screenVideoTrack.play('local-player');
        this.isScreenSharing.set(true);

        this.screenVideoTrack.on("track-ended", () => {
          this.toggleScreenShare();
        });
      }
    } catch (err) {
      console.error("Failed to share screen:", err);
      this.addSystemMessage("Screen sharing cancelled.");
    }
  }

  leaveRoom(): void {
    this.cleanupCall();
    if (window.opener) {
      window.close();
    } else {
      this.router.navigate(['/']);
    }
  }

  private cleanupCall(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
    
    if (this.localStream) {
      this.localStream.getTracks().forEach(t => t.stop());
      this.localStream = null;
    }

    if (this.localAudioTrack) {
      this.localAudioTrack.stop();
      this.localAudioTrack.close();
      this.localAudioTrack = null;
    }
    if (this.localVideoTrack) {
      this.localVideoTrack.stop();
      this.localVideoTrack.close();
      this.localVideoTrack = null;
    }
    if (this.screenVideoTrack) {
      this.screenVideoTrack.stop();
      this.screenVideoTrack.close();
      this.screenVideoTrack = null;
    }

    if (this.agoraClient) {
      this.agoraClient.leave();
      this.agoraClient = null;
    }
    
    this.isConnected.set(false);
  }

  private startCallTimer(): void {
    this.secondsElapsed = 0;
    this.timerInterval = setInterval(() => {
      this.secondsElapsed++;
      const mins = Math.floor(this.secondsElapsed / 60).toString().padStart(2, '0');
      const secs = (this.secondsElapsed % 60).toString().padStart(2, '0');
      this.elapsedTime.set(`${mins}:${secs}`);
    }, 1000);
  }

  sendMessage(): void {
    if (!this.newMessageText.trim()) return;
    
    const now = new Date();
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    
    const newMsg = {
      sender: this.authService.currentUser()?.firstName || 'You',
      text: this.newMessageText.trim(),
      time: timeStr
    };
    
    this.chatMessages.set([...this.chatMessages(), newMsg]);
    this.newMessageText = '';

    if (this.isSimulatorMode()) {
      setTimeout(() => {
        const mockResponses = [
          "Good morning teacher! Yes, I can hear you clearly.",
          "Awesome screen share. Thanks!",
          "Can you explain the last part of this section again?",
          "Yes, the code is very clean."
        ];
        const randomResp = mockResponses[Math.floor(Math.random() * mockResponses.length)];
        const respMsg = {
          sender: 'Saman Perera',
          text: randomResp,
          time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
        };
        this.chatMessages.set([...this.chatMessages(), respMsg]);
      }, 2500);
    }
  }

  addSystemMessage(text: string): void {
    const timeStr = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    this.chatMessages.set([...this.chatMessages(), {
      sender: 'System',
      text: text,
      time: timeStr
    }]);
  }
}
