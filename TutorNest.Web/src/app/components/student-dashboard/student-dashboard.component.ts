import { Component, OnInit, OnDestroy, signal, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { StudentService } from '../../services/student.service';
import { AuthService } from '../../services/auth.service';
import { AssignmentService } from '../../services/assignment.service';
import { AnnouncementService } from '../../services/announcement.service';
import { SubscriptionService } from '../../services/subscription.service';
import { LiveClassService } from '../../services/liveclass.service';
import { CourseService } from '../../services/course.service';
import { LanguageService } from '../../services/language.service';
import { ReportService } from '../../services/report.service';
import { NotificationsBellComponent } from '../notifications-bell/notifications-bell.component';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { 
  ClassResponse, StudentVideoResponse, AssignmentResponse, AnnouncementResponse, UserProfileResponse,
  LiveClassResponse, CourseResponse, CourseProgressResponse, CertificateResponse
} from '../../models';

import { AppTourComponent, TourStep } from '../app-tour/app-tour.component';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NotificationsBellComponent, AppTourComponent],
  templateUrl: './student-dashboard.component.html',
  styleUrls: ['./student-dashboard.component.scss']
})
export class StudentDashboardComponent implements OnInit, OnDestroy {
  showTour = signal<boolean>(false);
  studentTourSteps: TourStep[] = [
    {
      title: 'Welcome to TutorNest Student Workspace!',
      content: 'Let us guide you through your dashboard panel to access your classrooms, check syllabus courses, watch video lessons, do homework, and track your scores.',
      position: 'center'
    },
    {
      targetSelector: '.tour-nav-classes',
      title: 'Enrolled Classrooms',
      content: 'View your enrolled classes, watch assigned lectures, download assets, and complete grading assignments.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-live-classes',
      title: 'Live Streams & Classes',
      content: 'Access upcoming live lectures, stream directly with your instructors, and view archived streaming videos.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-courses',
      title: 'Curriculum Subjects',
      content: 'Explore registered subject courses and review syllabus material to track your completion progress.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-announcements',
      title: 'Announcements Board',
      content: 'Keep up to date with notices, global attachments, and warnings broadcasted by your instructors.',
      position: 'right'
    },
    {
      targetSelector: '.profile-menu-container',
      title: 'Settings & Account Options',
      content: 'Configure your profile details, edit fields, add photos, or restart this helper tour anytime.',
      position: 'top'
    }
  ];

  // Navigation states
  enrolledClasses = signal<ClassResponse[]>([]);
  selectedClass = signal<ClassResponse | null>(null);
  classVideos = signal<StudentVideoResponse[]>([]);
  activeVideo = signal<StudentVideoResponse | null>(null);

  // Phase 2 states
  selectedClassAssignments = signal<AssignmentResponse[]>([]);
  selectedAssignment = signal<AssignmentResponse | null>(null);
  announcements = signal<AnnouncementResponse[]>([]);
  activeTab = signal<string>('classes'); // 'classes' | 'announcements'
  leaderboard = signal<any[]>([]);
  isLoadingLeaderboard = signal<boolean>(false);

  // Phase 4 states
  upcomingLiveClasses = signal<LiveClassResponse[]>([]);
  myCourses = signal<CourseResponse[]>([]);
  courseProgress = signal<Record<string, CourseProgressResponse>>({});
  certificates = signal<CertificateResponse[]>([]);

  // Submission inputs
  mcqSelectedOption = signal<string>('');
  shortAnswerText = signal<string>('');
  uploadedFileUrl = signal<string>('');

  // Loading states
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  studentName = signal<string>('Student');
  showProfileDropdown = signal<boolean>(false);
  isSidebarOpen = signal<boolean>(false);

  // Profile settings (Phase 3)
  profile = signal<UserProfileResponse | null>(null);
  profileForm: FormGroup;

  // Video element query
  @ViewChild('videoPlayer') videoPlayer!: ElementRef<HTMLVideoElement>;

  // Security modals state (Phase 2 & 4)
  showAttachmentModal = signal<boolean>(false);
  viewingAttachmentName = signal<string>('');
  isAttachmentLoading = signal<boolean>(false);
  attachmentUrlSafe = signal<SafeResourceUrl | null>(null);
  attachmentImageSrc = signal<string | null>(null);

  showRecordingModal = signal<boolean>(false);
  viewingRecordingTitle = signal<string>('');
  viewingRecordingUrl = signal<string>('');

  private currentBlobUrl: string | null = null;

  // Heartbeat tracking
  private heartbeatInterval: any = null;
  private lastSentTime = 0;

  constructor(
    private fb: FormBuilder,
    private studentService: StudentService,
    public authService: AuthService,
    private assignmentService: AssignmentService,
    private announcementService: AnnouncementService,
    private subscriptionService: SubscriptionService,
    private liveClassService: LiveClassService,
    private courseService: CourseService,
    public langService: LanguageService,
    private reportService: ReportService,
    private router: Router,
    private sanitizer: DomSanitizer
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      bio: [''],
      subject: [''],
      profilePictureUrl: ['']
    });
  }

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.studentName.set(`${user.firstName} ${user.lastName}`);
      const hasSeen = localStorage.getItem(`seen_tour_${user.email}_student`);
      if (!hasSeen) {
        setTimeout(() => this.showTour.set(true), 1200);
      }
    }
    this.loadClasses();
    this.loadAnnouncements();

    // Hook security listeners to prevent inspection/sharing
    document.addEventListener('contextmenu', this.blockContextMenuListener);
    document.addEventListener('keydown', this.blockKeysListener);
  }

  ngOnDestroy(): void {
    this.clearHeartbeat();
    this.revokeBlobUrl();
    document.removeEventListener('contextmenu', this.blockContextMenuListener);
    document.removeEventListener('keydown', this.blockKeysListener);
  }

  private blockKeysListener = (e: KeyboardEvent) => {
    // Disable Ctrl+S / Cmd+S
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault();
      return false;
    }
    // Disable Ctrl+P / Cmd+P
    if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
      e.preventDefault();
      return false;
    }
    // Disable Ctrl+U / Cmd+U
    if ((e.ctrlKey || e.metaKey) && e.key === 'u') {
      e.preventDefault();
      return false;
    }
    // Disable F12 / Ctrl+Shift+I / Cmd+Opt+I (Developer Tools)
    if (e.key === 'F12' || 
        ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'I') || 
        ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'C')) {
      e.preventDefault();
      return false;
    }
    return true;
  };

  private blockContextMenuListener = (e: Event) => {
    // Allow standard inputs to have context menu if needed, but block on student pages
    const target = e.target as HTMLElement;
    if (target.tagName !== 'INPUT' && target.tagName !== 'TEXTAREA') {
      e.preventDefault();
    }
  };

  viewAttachment(url: string, name: string): void {
    this.showAttachmentModal.set(true);
    this.viewingAttachmentName.set(name || 'Attachment');
    this.isAttachmentLoading.set(true);
    this.attachmentUrlSafe.set(null);
    this.attachmentImageSrc.set(null);

    this.revokeBlobUrl();

    this.studentService.getProxyFile(url).subscribe({
      next: (blob) => {
        this.currentBlobUrl = URL.createObjectURL(blob);
        const ext = url.split('.').pop()?.toLowerCase() || '';
        const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'];

        if (imageExts.includes(ext)) {
          this.attachmentImageSrc.set(this.currentBlobUrl);
        } else {
          this.attachmentUrlSafe.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.currentBlobUrl));
        }
        this.isAttachmentLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching file proxy:', err);
        this.isAttachmentLoading.set(false);
      }
    });
  }

  closeAttachmentModal(): void {
    this.showAttachmentModal.set(false);
    this.revokeBlobUrl();
  }

  private revokeBlobUrl(): void {
    if (this.currentBlobUrl) {
      URL.revokeObjectURL(this.currentBlobUrl);
      this.currentBlobUrl = null;
    }
  }

  viewRecording(url: string, title: string): void {
    this.viewingRecordingTitle.set(title || 'Class Recording');
    this.viewingRecordingUrl.set(url);
    this.showRecordingModal.set(true);
  }

  closeRecordingModal(): void {
    this.showRecordingModal.set(false);
    this.viewingRecordingUrl.set('');
  }

  setTab(tab: string): void {
    this.activeTab.set(tab);
    this.showProfileDropdown.set(false);
    this.isSidebarOpen.set(false);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.selectedClass.set(null);
    this.selectedAssignment.set(null);
    this.activeVideo.set(null);
    this.clearHeartbeat();

    if (tab === 'classes') {
      this.loadClasses();
    } else if (tab === 'announcements') {
      this.loadAnnouncements();
    } else if (tab === 'profile') {
      this.loadProfile();
    } else if (tab === 'live-classes') {
      this.loadLiveClasses();
    } else if (tab === 'courses') {
      this.loadCoursesAndCertificates();
    }
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.subscriptionService.getProfile().subscribe({
      next: (data) => {
        this.profile.set(data);
        this.profileForm.patchValue({
          firstName: data.firstName,
          lastName: data.lastName,
          bio: data.bio || '',
          subject: data.subject || '',
          profilePictureUrl: data.profilePictureUrl || ''
        });
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load profile details.')
    });
  }

  updateProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.subscriptionService.updateProfile(this.profileForm.value).subscribe({
      next: () => {
        this.successMessage.set('Profile updated successfully!');
        this.isSubmitting.set(false);
        this.loadProfile();
        this.studentName.set(`${this.profileForm.get('firstName')?.value} ${this.profileForm.get('lastName')?.value}`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to update profile.');
        this.isSubmitting.set(false);
      }
    });
  }

  onProfilePictureUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.assignmentService.uploadFile(file).subscribe({
        next: (res) => {
          this.profileForm.patchValue({ profilePictureUrl: res.url });
          this.successMessage.set('Profile picture uploaded successfully!');
        },
        error: () => this.errorMessage.set('Profile photo upload failed.')
      });
    }
  }

  loadClasses(): void {
    this.isLoading.set(true);
    this.studentService.getClasses().subscribe({
      next: (data) => {
        this.enrolledClasses.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load enrolled classes.')
    });
  }

  loadAnnouncements(): void {
    this.announcementService.getStudentAnnouncements().subscribe({
      next: (data) => this.announcements.set(data)
    });
  }

  selectClass(cls: ClassResponse): void {
    this.selectedClass.set(cls);
    this.activeVideo.set(null);
    this.selectedAssignment.set(null);
    this.clearHeartbeat();
    this.loadVideosAndAssignments(cls.id);
  }

  loadVideosAndAssignments(classId: string): void {
    this.isLoading.set(true);
    this.studentService.getClassVideos(classId).subscribe({
      next: (data) => {
        this.classVideos.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load class videos.')
    });

    this.assignmentService.getAssignments(classId).subscribe({
      next: (data) => this.selectedClassAssignments.set(data)
    });

    this.loadLeaderboard(classId);
  }

  loadLeaderboard(classId: string): void {
    this.isLoadingLeaderboard.set(true);
    this.studentService.getClassLeaderboard(classId).subscribe({
      next: (data) => {
        this.leaderboard.set(data);
        this.isLoadingLeaderboard.set(false);
      },
      error: () => {
        this.isLoadingLeaderboard.set(false);
        this.errorMessage.set('Failed to load class leaderboard.');
      }
    });
  }

  clearActiveContent(): void {
    this.clearHeartbeat();
    this.activeVideo.set(null);
    this.selectedAssignment.set(null);
    // Reload leaderboard to show fresh ranking data
    const clsId = this.selectedClass()?.id;
    if (clsId) {
      this.loadLeaderboard(clsId);
    }
  }

  playVideo(video: StudentVideoResponse): void {
    this.clearHeartbeat();
    this.selectedAssignment.set(null); // Clear active assignment view if playing video
    this.activeVideo.set(video);
    this.lastSentTime = video.watchTimeSeconds;
    
    setTimeout(() => {
      if (this.videoPlayer && this.videoPlayer.nativeElement) {
        const player = this.videoPlayer.nativeElement;
        
        if (video.watchTimeSeconds > 0 && !video.isCompleted) {
          player.currentTime = video.watchTimeSeconds;
        }

        player.onplay = () => this.startHeartbeat();
        player.onpause = () => this.sendProgressUpdate(false);
        player.onended = () => {
          this.clearHeartbeat();
          this.sendProgressUpdate(true);
        };
      }
    }, 100);
  }

  private startHeartbeat(): void {
    this.clearHeartbeat();
    this.heartbeatInterval = setInterval(() => {
      this.sendProgressUpdate(false);
    }, 5000);
  }

  private clearHeartbeat(): void {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = null;
    }
  }

  private sendProgressUpdate(isCompleted: boolean): void {
    const video = this.activeVideo();
    if (!video || !this.videoPlayer || !this.videoPlayer.nativeElement) return;

    const player = this.videoPlayer.nativeElement;
    const currentTime = player.currentTime;
    const duration = player.duration || video.durationSeconds || 1;

    if (Math.abs(currentTime - this.lastSentTime) < 1 && !isCompleted) {
      return;
    }

    this.lastSentTime = currentTime;

    this.studentService.updateProgress(video.id, {
      watchTimeSeconds: currentTime,
      durationSeconds: duration,
      isCompleted: isCompleted || (currentTime / duration >= 0.9)
    }).subscribe({
      next: (res) => {
        this.classVideos.update(vids => 
          vids.map(v => v.id === video.id ? { 
            ...v, 
            watchTimeSeconds: res.watchTimeSeconds, 
            durationSeconds: res.durationSeconds, 
            isCompleted: res.isCompleted 
          } : v)
        );
        if (isCompleted || res.isCompleted) {
          const clsId = this.selectedClass()?.id;
          if (clsId) {
            this.loadLeaderboard(clsId);
          }
        }
      }
    });
  }

  // Phase 2 Assignment Methods
  openAssignment(asg: AssignmentResponse): void {
    this.clearHeartbeat();
    this.activeVideo.set(null); // Close active video
    this.selectedAssignment.set(asg);
    
    // Clear submission input caches
    this.mcqSelectedOption.set('');
    this.shortAnswerText.set('');
    this.uploadedFileUrl.set('');
  }

  getParsedMcqConfig(configJson?: string | null): { options: string[], correctAnswer?: string } {
    try {
      return configJson ? JSON.parse(configJson) : { options: [] };
    } catch {
      return { options: [] };
    }
  }

  onFileUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.isSubmitting.set(true);
      this.errorMessage.set(null);

      this.assignmentService.uploadFile(file).subscribe({
        next: (res) => {
          this.uploadedFileUrl.set(res.url);
          this.isSubmitting.set(false);
          this.successMessage.set('File uploaded successfully!');
        },
        error: () => {
          this.isSubmitting.set(false);
          this.errorMessage.set('File upload failed.');
        }
      });
    }
  }

  submitAssignmentAnswer(): void {
    const asg = this.selectedAssignment();
    const classId = this.selectedClass()?.id;
    if (!asg || !classId) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    let answerText: string | null = null;
    let attachmentUrl: string | null = null;

    if (asg.type === 'MultipleChoice') {
      if (!this.mcqSelectedOption()) {
        this.errorMessage.set('Please select an option.');
        this.isSubmitting.set(false);
        return;
      }
      answerText = this.mcqSelectedOption();
    } else if (asg.type === 'ShortAnswer') {
      if (!this.shortAnswerText().trim()) {
        this.errorMessage.set('Please type an answer.');
        this.isSubmitting.set(false);
        return;
      }
      answerText = this.shortAnswerText();
    } else if (asg.type === 'FileUpload') {
      if (!this.uploadedFileUrl()) {
        this.errorMessage.set('Please select and upload a file first.');
        this.isSubmitting.set(false);
        return;
      }
      attachmentUrl = this.uploadedFileUrl();
    }

    this.assignmentService.submitAssignment(asg.id, answerText, attachmentUrl).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Assignment submitted successfully!');
        this.selectedAssignment.set(null);
        this.loadVideosAndAssignments(classId); // Reload
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Submission failed.');
      }
    });
  }

  // Phase 2 Announcement Methods
  readNotice(notice: AnnouncementResponse): void {
    if (!notice.isRead) {
      this.announcementService.markAsRead(notice.id).subscribe({
        next: () => {
          // Toggle read state locally
          this.announcements.update(list => 
            list.map(a => a.id === notice.id ? { ...a, isRead: true } : a)
          );
        }
      });
    }
  }

  get unreadAnnouncementsCount(): number {
    return this.announcements().filter(a => !a.isRead).length;
  }

  backToClasses(): void {
    this.selectedClass.set(null);
    this.activeVideo.set(null);
    this.selectedAssignment.set(null);
    this.clearHeartbeat();
    this.loadClasses();
  }

  t(key: string): string {
    return this.langService.translate(key);
  }

  switchLanguage(lang: string): void {
    this.langService.setLanguage(lang);
  }

  loadLiveClasses(): void {
    this.isLoading.set(true);
    this.liveClassService.getUpcomingLiveClasses().subscribe({
      next: (data) => {
        this.upcomingLiveClasses.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load upcoming live classes.')
    });
  }

  loadCoursesAndCertificates(): void {
    this.isLoading.set(true);
    this.courseService.getCourses().subscribe({
      next: (courseList) => {
        this.myCourses.set(courseList);
        
        const progressMap: Record<string, CourseProgressResponse> = {};
        let requestsCompleted = 0;
        
        if (courseList.length === 0) {
          this.isLoading.set(false);
        } else {
          courseList.forEach(course => {
            this.courseService.getStudentProgress(course.id).subscribe({
              next: (prog) => {
                progressMap[course.id] = prog;
                requestsCompleted++;
                if (requestsCompleted === courseList.length) {
                  this.courseProgress.set(progressMap);
                  this.isLoading.set(false);
                }
              },
              error: () => {
                requestsCompleted++;
                if (requestsCompleted === courseList.length) {
                  this.courseProgress.set(progressMap);
                  this.isLoading.set(false);
                }
              }
            });
          });
        }
      },
      error: () => this.handleError('Failed to load courses.')
    });

    this.courseService.getMyCertificates().subscribe({
      next: (data) => this.certificates.set(data)
    });
  }

  downloadCertificatePdf(certId: string): void {
    this.isLoading.set(true);
    this.reportService.downloadCertificatePdf(certId).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `certificate_${certId}.pdf`);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to download certificate PDF.')
    });
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  toggleProfileDropdown(): void {
    this.showProfileDropdown.set(!this.showProfileDropdown());
  }

  toggleSidebar(): void {
    this.isSidebarOpen.set(!this.isSidebarOpen());
  }

  startTour(): void {
    this.showTour.set(false);
    setTimeout(() => {
      this.showTour.set(true);
    }, 100);
  }

  onTourCompletedOrSkipped(): void {
    this.showTour.set(false);
  }

  onStudentTourStepChanged(index: number): void {
    const tabMap: Record<number, string> = {
      1: 'classes',
      2: 'live-classes',
      3: 'courses',
      4: 'announcements',
      5: 'profile'
    };
    const targetTab = tabMap[index];
    if (targetTab) {
      this.setTab(targetTab);
    }
  }

  logout(): void {
    this.clearHeartbeat();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private handleError(msg: string): void {
    this.errorMessage.set(msg);
    this.isLoading.set(false);
  }
}
