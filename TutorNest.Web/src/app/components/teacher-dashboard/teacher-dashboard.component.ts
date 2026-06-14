import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TeacherService } from '../../services/teacher.service';
import { AuthService } from '../../services/auth.service';
import { AssignmentService } from '../../services/assignment.service';
import { AnnouncementService } from '../../services/announcement.service';
import { AnalyticsService } from '../../services/analytics.service';
import { SubscriptionService } from '../../services/subscription.service';
import { PaymentService } from '../../services/payment.service';
import { ReportService } from '../../services/report.service';
import { LiveClassService } from '../../services/liveclass.service';
import { CourseService } from '../../services/course.service';
import { LanguageService } from '../../services/language.service';
import { NotificationsBellComponent } from '../notifications-bell/notifications-bell.component';
import { 
  ClassResponse, VideoResponse, StudentResponse, StudentProgressReport, 
  AssignmentResponse, SubmissionResponse, AnnouncementResponse,
  ClassProgressDto, VideoWatchCountDto, StudentEngagementDto, TopPerformerDto,
  SubscriptionPlanResponse, TeacherSubscriptionResponse, PaymentHistoryResponse, UserProfileResponse,
  CreateLiveClassRequest, LiveClassResponse, CreateCourseRequest, CourseResponse, CertificateResponse
} from '../../models';

import { AppTourComponent, TourStep } from '../app-tour/app-tour.component';

@Component({
  selector: 'app-teacher-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NotificationsBellComponent, AppTourComponent],
  templateUrl: './teacher-dashboard.component.html',
  styleUrls: ['./teacher-dashboard.component.scss']
})
export class TeacherDashboardComponent implements OnInit {
  activeTab = signal<string>('classes');

  showTour = signal<boolean>(false);
  teacherTourSteps: TourStep[] = [
    {
      title: 'Welcome to TutorNest!',
      content: 'Let us take a quick tour to guide you through your new premium instructor workspace. Click Next to begin.',
      position: 'center'
    },
    {
      targetSelector: '.tour-nav-classes',
      title: 'Classrooms Manager',
      content: 'Here you can view, edit, and create classroom groups, configure students, assign learning videos, and post assignments.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-live-classes',
      title: 'Live Streams & Classes',
      content: 'Schedule interactive live lessons, enter meeting URLs, invite students, and upload recordings afterwards.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-courses',
      title: 'Curriculum Courses',
      content: 'Create, update, and organize structural subject courses to group class files and materials under syllabus subjects.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-students',
      title: 'Referrals & Student Roster',
      content: 'Track student list details, register new student accounts, or delete inactive profiles from the platform.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-videos',
      title: 'Video Library & Lectures',
      content: 'Maintain a repository of recorded video lectures. Add URLs or upload video files directly to allocate them to your classes.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-announcements',
      title: 'Notice board Announcements',
      content: 'Broadcast important notifications, schedules, updates, and files globally or to specific classroom groups.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-analytics',
      title: 'Analytics Reports',
      content: 'View comprehensive telemetry charts on student engagement, class progress, video watch statistics, and top performers.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-billing',
      title: 'Billing Settings',
      content: 'Check your subscription plans, active pricing features, limits, and your past payment history details.',
      position: 'right'
    },
    {
      targetSelector: '.tour-certificates-nav',
      title: 'Custom Certificates Hub',
      content: 'Award completion certificates directly to your students here. Design custom title templates, presentation subtitles, and messages.',
      position: 'right'
    },
    {
      targetSelector: '.profile-menu-container',
      title: 'Instructor Profile Settings',
      content: 'Click here to customize your teacher bio, update display fields, load avatars, or replay this tour anytime.',
      position: 'top'
    }
  ];
  
  // Core lists
  classes = signal<ClassResponse[]>([]);
  students = signal<StudentResponse[]>([]);
  videos = signal<VideoResponse[]>([]);
  progressReports = signal<StudentProgressReport[]>([]);
  
  // Phase 2 Lists
  assignments = signal<AssignmentResponse[]>([]);
  submissions = signal<SubmissionResponse[]>([]);
  announcements = signal<AnnouncementResponse[]>([]);

  // Analytics Signals
  classProgress = signal<ClassProgressDto[]>([]);
  mostWatchedVideos = signal<VideoWatchCountDto[]>([]);
  studentEngagement = signal<StudentEngagementDto[]>([]);
  topPerformers = signal<TopPerformerDto[]>([]);

  // Selected details
  selectedClass = signal<ClassResponse | null>(null);
  selectedClassStudents = signal<StudentResponse[]>([]);
  selectedClassVideos = signal<VideoResponse[]>([]);
  selectedClassAssignments = signal<AssignmentResponse[]>([]);
  
  selectedAssignment = signal<AssignmentResponse | null>(null);
  selectedSubmission = signal<SubmissionResponse | null>(null);
  activeVideoToWatch = signal<VideoResponse | null>(null);

  // Forms
  classForm: FormGroup;
  studentForm: FormGroup;
  profileForm: FormGroup;

  // Phase 3 Signals
  subscriptionStatus = signal<TeacherSubscriptionResponse | null>(null);
  plans = signal<SubscriptionPlanResponse[]>([]);
  billingHistory = signal<PaymentHistoryResponse[]>([]);
  profile = signal<UserProfileResponse | null>(null);
  videoForm: FormGroup;
  assignmentForm: FormGroup;
  announcementForm: FormGroup;
  gradingForm: FormGroup;
  
  // Loading indicators
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  teacherName = signal<string>('Teacher');

  // Modal displays
  showClassModal = signal<boolean>(false);
  showStudentModal = signal<boolean>(false);
  showVideoModal = signal<boolean>(false);
  selectedVideoFile = signal<File | null>(null);
  videoUploadType = signal<'link' | 'file'>('link');
  showProfileDropdown = signal<boolean>(false);
  isSidebarOpen = signal<boolean>(false);
  showAssignmentModal = signal<boolean>(false);
  showAnnouncementModal = signal<boolean>(false);
  showGradingModal = signal<boolean>(false);
  editingAnnouncementId = signal<string | null>(null);

  // Edit states
  isEditingClass = signal<boolean>(false);
  selectedClassToEdit = signal<ClassResponse | null>(null);
  isEditingStudent = signal<boolean>(false);
  selectedStudentToEdit = signal<StudentResponse | null>(null);

  // Phase 4 Signals & Forms
  upcomingLiveClasses = signal<LiveClassResponse[]>([]);
  courses = signal<CourseResponse[]>([]);
  selectedCourse = signal<CourseResponse | null>(null);
  selectedLiveClassForRecording = signal<LiveClassResponse | null>(null);
  recordingUploadType = signal<'link' | 'file'>('link');
  selectedRecordingFile = signal<File | null>(null);
  csvFile = signal<File | null>(null);
  csvUploadProgress = signal<string | null>(null);
  csvErrors = signal<string[]>([]);
  courseClassIds = signal<string[]>([]);

  liveClassForm: FormGroup;
  courseForm: FormGroup;
  recordingForm: FormGroup;
  certificateForm: FormGroup;

  showLiveClassModal = signal<boolean>(false);
  showCourseModal = signal<boolean>(false);
  showRecordingModal = signal<boolean>(false);
  showAssignCourseModal = signal<boolean>(false);
  showCertificateModal = signal<boolean>(false);
  certificates = signal<CertificateResponse[]>([]);

  showEnrollDropdown = signal<boolean>(false);
  showAssignVideoDropdown = signal<boolean>(false);

  selectedEnrollStudentId = signal<string>('');
  selectedAssignVideoId = signal<string>('');

  // MCQ Options helper
  mcqOptions = signal<string[]>(['Option A', 'Option B']);
  newOptionText = signal<string>('');

  constructor(
    private fb: FormBuilder,
    private teacherService: TeacherService,
    public authService: AuthService,
    private assignmentService: AssignmentService,
    private announcementService: AnnouncementService,
    private analyticsService: AnalyticsService,
    private subscriptionService: SubscriptionService,
    private paymentService: PaymentService,
    private reportService: ReportService,
    private liveClassService: LiveClassService,
    private courseService: CourseService,
    public langService: LanguageService,
    private router: Router
  ) {
    this.classForm = this.fb.group({
      name: ['', [Validators.required]],
      description: ['', [Validators.required]]
    });

    this.studentForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]]
    });

    this.videoForm = this.fb.group({
      title: ['', [Validators.required]],
      description: ['', [Validators.required]],
      videoUrl: ['', [Validators.required, Validators.pattern(/^(http|https):\/\/[^\s$.?#].[^\s]*$/)]]
    });

    this.assignmentForm = this.fb.group({
      title: ['', [Validators.required]],
      description: ['', [Validators.required]],
      dueDate: ['', [Validators.required]],
      totalMarks: [100, [Validators.required, Validators.min(1)]],
      type: ['ShortAnswer', [Validators.required]],
      mcqCorrect: ['']
    });

    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      bio: [''],
      subject: [''],
      profilePictureUrl: ['']
    });

    this.announcementForm = this.fb.group({
      title: ['', [Validators.required]],
      content: ['', [Validators.required]],
      classId: [''],
      attachmentUrl: ['']
    });

    this.gradingForm = this.fb.group({
      grade: [0, [Validators.required, Validators.min(0)]],
      feedback: ['', [Validators.required]]
    });

    this.liveClassForm = this.fb.group({
      title: ['', [Validators.required]],
      description: ['', [Validators.required]],
      scheduledStartTime: ['', [Validators.required]],
      durationMinutes: [60, [Validators.required, Validators.min(5)]],
      meetingLink: ['', [Validators.required, Validators.pattern(/^(http|https):\/\/[^\s$.?#].[^\s]*$/)]],
      classId: ['', [Validators.required]]
    });

    this.courseForm = this.fb.group({
      title: ['', [Validators.required]],
      description: ['', [Validators.required]]
    });

    this.recordingForm = this.fb.group({
      recordingUrl: ['', [Validators.required, Validators.pattern(/^(http|https):\/\/[^\s$.?#].[^\s]*$/)]]
    });

    this.certificateForm = this.fb.group({
      studentId: ['', [Validators.required]],
      courseId: [null],
      classId: [null],
      customTitle: ['CERTIFICATE OF COMPLETION'],
      customSubTitle: ['This certificate is proudly presented to'],
      customMessage: ['for successfully completing the curriculum requirements for']
    });
  }

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.teacherName.set(`${user.firstName} ${user.lastName}`);
      const hasSeen = localStorage.getItem(`seen_tour_${user.email}_teacher`);
      if (!hasSeen) {
        setTimeout(() => this.showTour.set(true), 1200);
      }
    }
    this.loadClasses();
    this.loadStudents();
    this.loadVideos();
    this.loadProfile(); // Load profile immediately to display the thumbnail avatar
  }

  toggleProfileDropdown(): void {
    this.showProfileDropdown.set(!this.showProfileDropdown());
  }

  toggleSidebar(): void {
    this.isSidebarOpen.set(!this.isSidebarOpen());
  }

  setTab(tab: string): void {
    this.activeTab.set(tab);
    this.showProfileDropdown.set(false);
    this.isSidebarOpen.set(false);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.selectedClass.set(null);
    this.selectedAssignment.set(null);

    if (tab === 'classes') this.loadClasses();
    else if (tab === 'students') { this.loadStudents(); this.loadProfile(); }
    else if (tab === 'videos') this.loadVideos();
    else if (tab === 'progress') this.loadProgressReports();
    else if (tab === 'announcements') this.loadAnnouncements();
    else if (tab === 'analytics') this.loadAnalytics();
    else if (tab === 'billing') this.loadBilling();
    else if (tab === 'profile') this.loadProfile();
    else if (tab === 'live-classes') this.loadLiveClasses();
    else if (tab === 'courses') this.loadCourses();
    else if (tab === 'certificates') this.loadCertificates();
  }

  // Load Operations
  loadClasses(): void {
    this.isLoading.set(true);
    this.teacherService.getClasses().subscribe({
      next: (data) => {
        this.classes.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load classes.')
    });
  }

  loadStudents(): void {
    this.isLoading.set(true);
    this.teacherService.getStudents().subscribe({
      next: (data) => {
        this.students.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load students.')
    });
  }

  loadVideos(): void {
    this.isLoading.set(true);
    this.teacherService.getVideos().subscribe({
      next: (data) => {
        this.videos.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load videos.')
    });
  }

  loadProgressReports(): void {
    this.isLoading.set(true);
    this.teacherService.getProgressReports().subscribe({
      next: (data) => {
        this.progressReports.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load progress reports.')
    });
  }

  loadAnnouncements(): void {
    this.isLoading.set(true);
    this.announcementService.getTeacherAnnouncements().subscribe({
      next: (data) => {
        this.announcements.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load announcements.')
    });
  }

  loadAnalytics(): void {
    this.isLoading.set(true);
    this.analyticsService.getTeacherAnalytics().subscribe({
      next: (data) => {
        this.classProgress.set(data.classProgress);
        this.mostWatchedVideos.set(data.mostWatchedVideos);
        this.studentEngagement.set(data.studentEngagement);
        this.topPerformers.set(data.topPerformers);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load analytics statistics.')
    });
  }

  // Class Details View
  viewClassDetails(cls: ClassResponse): void {
    this.selectedClass.set(cls);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.loadClassDetails(cls.id);
  }

  loadClassDetails(classId: string): void {
    this.teacherService.getClassStudents(classId).subscribe({
      next: (stds) => this.selectedClassStudents.set(stds)
    });
    this.teacherService.getClassVideos(classId).subscribe({
      next: (vids) => this.selectedClassVideos.set(vids)
    });
    this.assignmentService.getAssignments(classId).subscribe({
      next: (asg) => this.selectedClassAssignments.set(asg)
    });
  }

  // Creations & CRUD
  openNewClassModal(): void {
    this.isEditingClass.set(false);
    this.selectedClassToEdit.set(null);
    this.classForm.reset({ name: '', description: '' });
    this.showClassModal.set(true);
  }

  editClass(c: ClassResponse): void {
    this.selectedClassToEdit.set(c);
    this.isEditingClass.set(true);
    this.classForm.patchValue({
      name: c.name,
      description: c.description
    });
    this.showClassModal.set(true);
  }

  saveClass(): void {
    if (this.classForm.invalid) {
      this.classForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);

    if (this.isEditingClass() && this.selectedClassToEdit()) {
      const classId = this.selectedClassToEdit()!.id;
      this.teacherService.updateClass(classId, this.classForm.value).subscribe({
        next: (updatedClass) => {
          this.isSubmitting.set(false);
          this.successMessage.set('Class updated successfully!');
          this.classForm.reset();
          this.showClassModal.set(false);
          this.isEditingClass.set(false);
          this.selectedClassToEdit.set(null);
          this.loadClasses();
          if (this.selectedClass()?.id === classId) {
            this.selectedClass.set(updatedClass);
          }
        },
        error: (err) => this.handleSubmittingError(err)
      });
    } else {
      this.teacherService.createClass(this.classForm.value).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Class created successfully!');
          this.classForm.reset();
          this.showClassModal.set(false);
          this.loadClasses();
        },
        error: (err) => this.handleSubmittingError(err)
      });
    }
  }

  deleteClass(classId: string, event: Event): void {
    event.stopPropagation();
    if (confirm('Are you sure you want to delete this class? This will also remove student enrollments.')) {
      this.isLoading.set(true);
      this.teacherService.deleteClass(classId).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.successMessage.set('Class deleted successfully!');
          this.loadClasses();
          if (this.selectedClass()?.id === classId) {
            this.selectedClass.set(null);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to delete class.');
        }
      });
    }
  }

  openNewStudentModal(): void {
    this.isEditingStudent.set(false);
    this.selectedStudentToEdit.set(null);
    this.studentForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.studentForm.get('password')?.updateValueAndValidity();
    this.studentForm.reset({ firstName: '', lastName: '', email: '', password: '' });
    this.showStudentModal.set(true);
  }

  editStudent(student: StudentResponse): void {
    this.selectedStudentToEdit.set(student);
    this.isEditingStudent.set(true);
    this.studentForm.get('password')?.clearValidators();
    this.studentForm.get('password')?.setValidators([Validators.minLength(6)]);
    this.studentForm.get('password')?.updateValueAndValidity();
    this.studentForm.patchValue({
      firstName: student.firstName,
      lastName: student.lastName,
      email: student.email,
      password: ''
    });
    this.showStudentModal.set(true);
  }

  saveStudent(): void {
    if (this.studentForm.invalid) {
      this.studentForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);

    if (this.isEditingStudent() && this.selectedStudentToEdit()) {
      const studentId = this.selectedStudentToEdit()!.id;
      this.teacherService.updateStudent(studentId, this.studentForm.value).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Student updated successfully!');
          this.studentForm.reset();
          this.showStudentModal.set(false);
          this.isEditingStudent.set(false);
          this.selectedStudentToEdit.set(null);
          this.loadStudents();
          if (this.selectedClass()) {
            this.viewClassDetails(this.selectedClass()!);
          }
        },
        error: (err) => this.handleSubmittingError(err)
      });
    } else {
      this.authService.registerStudent(this.studentForm.value).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Student registered successfully!');
          this.studentForm.reset();
          this.showStudentModal.set(false);
          this.loadStudents();
        },
        error: (err) => this.handleSubmittingError(err)
      });
    }
  }

  deleteStudent(studentId: string): void {
    if (confirm('Are you sure you want to delete this student? All their enrollments, progress, and assignments will be deleted.')) {
      this.isLoading.set(true);
      this.teacherService.deleteStudent(studentId).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.successMessage.set('Student deleted successfully!');
          this.loadStudents();
          if (this.selectedClass()) {
            this.viewClassDetails(this.selectedClass()!);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to delete student.');
        }
      });
    }
  }

  removeStudentFromClass(studentId: string): void {
    const classId = this.selectedClass()?.id;
    if (!classId) return;

    if (confirm('Are you sure you want to remove this student from this class?')) {
      this.isLoading.set(true);
      this.teacherService.removeStudentFromClass(classId, studentId).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.successMessage.set('Student removed from class successfully!');
          this.viewClassDetails(this.selectedClass()!);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to remove student from class.');
        }
      });
    }
  }

  setVideoUploadType(type: 'link' | 'file'): void {
    this.videoUploadType.set(type);
    const urlControl = this.videoForm.get('videoUrl');
    if (type === 'file') {
      urlControl?.clearValidators();
    } else {
      urlControl?.setValidators([Validators.required, Validators.pattern(/^(http|https):\/\/[^\s$.?#].[^\s]*$/)]);
    }
    urlControl?.updateValueAndValidity();
  }

  onVideoFileSelected(event: any): void {
    const file = event.target.files?.[0];
    if (file) {
      this.selectedVideoFile.set(file);
    }
  }

  watchVideo(video: VideoResponse): void {
    this.activeVideoToWatch.set(video);
  }

  uploadVideo(): void {
    if (this.videoForm.invalid) {
      this.videoForm.markAllAsTouched();
      return;
    }

    const isLink = this.videoUploadType() === 'link';
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    if (isLink) {
      this.teacherService.createVideo(this.videoForm.value).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Video link added to library successfully!');
          this.videoForm.reset();
          this.showVideoModal.set(false);
          this.loadVideos();
        },
        error: (err) => this.handleSubmittingError(err)
      });
    } else {
      const file = this.selectedVideoFile();
      if (!file) {
        this.isSubmitting.set(false);
        this.errorMessage.set('Please select a video file to upload.');
        return;
      }

      this.teacherService.uploadVideoFile(file, this.videoForm.value.title, this.videoForm.value.description).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.limitExceeded) {
            this.successMessage.set('Video uploaded, but storage limit is exceeded! Please upgrade your plan.');
          } else {
            this.successMessage.set('Video uploaded and added to library successfully!');
          }
          this.videoForm.reset();
          this.selectedVideoFile.set(null);
          this.showVideoModal.set(false);
          this.loadVideos();
        },
        error: (err) => this.handleSubmittingError(err)
      });
    }
  }

  deleteVideo(videoId: string, event: Event): void {
    event.stopPropagation();
    if (confirm('Are you sure you want to delete this video? This will remove it from all assigned classrooms.')) {
      this.isLoading.set(true);
      this.teacherService.deleteVideo(videoId).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.successMessage.set('Video deleted successfully!');
          this.loadVideos();
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to delete video.');
        }
      });
    }
  }

  // Phase 2 Creations
  createAssignment(): void {
    if (this.assignmentForm.invalid) {
      this.assignmentForm.markAllAsTouched();
      return;
    }

    const classId = this.selectedClass()?.id;
    if (!classId) return;

    this.isSubmitting.set(true);
    
    // Configure MCQ JSON
    let configJson: string | null = null;
    const formVal = this.assignmentForm.value;
    if (formVal.type === 'MultipleChoice') {
      configJson = JSON.stringify({
        options: this.mcqOptions(),
        correctAnswer: formVal.mcqCorrect || this.mcqOptions()[0]
      });
    }

    this.assignmentService.createAssignment({
      title: formVal.title,
      description: formVal.description,
      dueDate: formVal.dueDate,
      totalMarks: formVal.totalMarks,
      classId: classId,
      type: formVal.type,
      configJson: configJson
    }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Assignment posted successfully!');
        this.assignmentForm.reset({ totalMarks: 100, type: 'ShortAnswer' });
        this.mcqOptions.set(['Option A', 'Option B']);
        this.showAssignmentModal.set(false);
        this.loadClassDetails(classId);
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  openNewAnnouncementModal(): void {
    this.editingAnnouncementId.set(null);
    this.announcementForm.reset({ title: '', content: '', classId: '', attachmentUrl: '' });
    this.showAnnouncementModal.set(true);
  }

  openEditAnnouncementModal(announcement: AnnouncementResponse): void {
    this.editingAnnouncementId.set(announcement.id);
    this.announcementForm.patchValue({
      title: announcement.title,
      content: announcement.content,
      classId: announcement.classId || '',
      attachmentUrl: announcement.attachmentUrl || ''
    });
    this.showAnnouncementModal.set(true);
  }

  saveAnnouncement(): void {
    if (this.announcementForm.invalid) {
      this.announcementForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const formVal = this.announcementForm.value;
    const data = {
      title: formVal.title,
      content: formVal.content,
      classId: formVal.classId ? formVal.classId : null,
      attachmentUrl: formVal.attachmentUrl ? formVal.attachmentUrl : null
    };

    const editId = this.editingAnnouncementId();
    if (editId) {
      this.announcementService.updateAnnouncement(editId, data).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Notice updated successfully!');
          this.announcementForm.reset();
          this.showAnnouncementModal.set(false);
          this.editingAnnouncementId.set(null);
          this.loadAnnouncements();
        },
        error: (err) => this.handleSubmittingError(err)
      });
    } else {
      this.announcementService.createAnnouncement(data).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Notice broadcasted successfully!');
          this.announcementForm.reset();
          this.showAnnouncementModal.set(false);
          this.loadAnnouncements();
        },
        error: (err) => this.handleSubmittingError(err)
      });
    }
  }

  deleteAnnouncement(announcementId: string): void {
    if (confirm('Are you sure you want to delete this notice broadcast? This action cannot be undone.')) {
      this.isLoading.set(true);
      this.announcementService.deleteAnnouncement(announcementId).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.successMessage.set('Notice deleted successfully!');
          this.loadAnnouncements();
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to delete notice.');
        }
      });
    }
  }

  // File Attachment Upload (Teacher Announcements Helper)
  onAnnouncementAttachmentUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.assignmentService.uploadFile(file).subscribe({
        next: (res) => {
          this.announcementForm.patchValue({ attachmentUrl: res.url });
          this.successMessage.set('Attachment uploaded successfully!');
        },
        error: () => this.errorMessage.set('Attachment upload failed.')
      });
    }
  }

  // MCQ Helpers
  addMcqOption(): void {
    const txt = this.newOptionText().trim();
    if (txt) {
      this.mcqOptions.update(opts => [...opts, txt]);
      this.newOptionText.set('');
    }
  }

  removeMcqOption(index: number): void {
    this.mcqOptions.update(opts => opts.filter((_, i) => i !== index));
  }

  // Class Details Mappings
  enrollStudent(): void {
    const classId = this.selectedClass()?.id;
    const studentId = this.selectedEnrollStudentId();
    if (!classId || !studentId) return;

    this.teacherService.enrollStudent(classId, studentId).subscribe({
      next: () => {
        this.successMessage.set('Student enrolled successfully!');
        this.selectedEnrollStudentId.set('');
        this.showEnrollDropdown.set(false);
        this.loadClassDetails(classId);
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'Failed to enroll student.')
    });
  }

  assignVideo(): void {
    const classId = this.selectedClass()?.id;
    const videoId = this.selectedAssignVideoId();
    if (!classId || !videoId) return;

    this.teacherService.assignVideo(classId, videoId).subscribe({
      next: () => {
        this.successMessage.set('Video assigned successfully!');
        this.selectedAssignVideoId.set('');
        this.showAssignVideoDropdown.set(false);
        this.loadClassDetails(classId);
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'Failed to assign video.')
    });
  }

  // Submission Reviews
  viewSubmissions(asg: AssignmentResponse): void {
    this.selectedAssignment.set(asg);
    this.isLoading.set(true);
    this.assignmentService.getSubmissions(asg.id).subscribe({
      next: (subs) => {
        this.submissions.set(subs);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load submissions.')
    });
  }

  openGradingModal(sub: SubmissionResponse): void {
    this.selectedSubmission.set(sub);
    this.gradingForm.reset({
      grade: sub.grade || 0,
      feedback: sub.feedback || ''
    });
    this.showGradingModal.set(true);
  }

  submitGrade(): void {
    const sub = this.selectedSubmission();
    const asg = this.selectedAssignment();
    if (!sub || !asg) return;

    if (this.gradingForm.invalid) {
      this.gradingForm.markAllAsTouched();
      return;
    }

    const formVal = this.gradingForm.value;
    if (formVal.grade > asg.totalMarks) {
      this.errorMessage.set(`Grade cannot exceed maximum marks (${asg.totalMarks}).`);
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.assignmentService.gradeSubmission(sub.id, formVal.grade, formVal.feedback).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Submission graded and feedback sent!');
        this.showGradingModal.set(false);
        this.viewSubmissions(asg); // Refresh list
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  // Unenrolled & Unassigned helpers
  get unenrolledStudents() {
    const enrolledIds = this.selectedClassStudents().map(s => s.id);
    return this.students().filter(s => !enrolledIds.includes(s.id));
  }

  get unassignedVideos() {
    const assignedIds = this.selectedClassVideos().map(v => v.id);
    return this.videos().filter(v => !assignedIds.includes(v.id));
  }

  // Phase 3 Billing & Profile Methods
  loadBilling(): void {
    this.isLoading.set(true);
    this.subscriptionService.getMyStatus().subscribe({
      next: (data) => this.subscriptionStatus.set(data),
      error: () => this.handleError('Failed to load subscription status.')
    });

    this.subscriptionService.getPlans().subscribe({
      next: (data) => this.plans.set(data),
      error: () => this.handleError('Failed to load pricing plans.')
    });

    this.subscriptionService.getBillingHistory().subscribe({
      next: (data) => {
        this.billingHistory.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load billing history.')
    });
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
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  subscribeToPlan(planId: string): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const successUrl = window.location.origin + '/billing-success';
    const cancelUrl = window.location.href;

    this.paymentService.createCheckoutSession(planId, successUrl, cancelUrl).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.sessionUrl) {
          window.location.href = res.sessionUrl; // redirect
        }
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  exportClassProgressPdf(classId: string): void {
    this.isLoading.set(true);
    this.reportService.downloadClassProgressPdf(classId).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `class_progress_report_${classId}.pdf`);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to download class progress report.')
    });
  }

  exportAssignmentResultsPdf(assignmentId: string): void {
    this.isLoading.set(true);
    this.reportService.downloadAssignmentResultsPdf(assignmentId).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `assignment_results_report_${assignmentId}.pdf`);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to download homework grades report.')
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

  loadCourses(): void {
    this.isLoading.set(true);
    this.courseService.getCourses().subscribe({
      next: (data) => {
        this.courses.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load courses.')
    });
  }

  createLiveClass(): void {
    if (this.liveClassForm.invalid) {
      this.liveClassForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.liveClassService.scheduleLiveClass(this.liveClassForm.value).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Live class scheduled successfully!');
        this.liveClassForm.reset({ durationMinutes: 60 });
        this.showLiveClassModal.set(false);
        this.loadLiveClasses();
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  createCourse(): void {
    if (this.courseForm.invalid) {
      this.courseForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.courseService.createCourse(this.courseForm.value).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Course created successfully!');
        this.courseForm.reset();
        this.showCourseModal.set(false);
        this.loadCourses();
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  deleteCourse(courseId: string, event: Event): void {
    event.stopPropagation();
    if (confirm('Are you sure you want to delete this course? Classes assigned to this course will be unlinked.')) {
      this.isLoading.set(true);
      this.errorMessage.set(null);
      this.successMessage.set(null);
      this.courseService.deleteCourse(courseId).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.successMessage.set('Course deleted successfully!');
          this.loadCourses();
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to delete course.');
        }
      });
    }
  }

  openRecordingModal(lc: LiveClassResponse): void {
    this.selectedLiveClassForRecording.set(lc);
    this.recordingUploadType.set('link');
    this.selectedRecordingFile.set(null);
    this.recordingForm.reset({
      recordingUrl: lc.recordingUrl || ''
    });
    this.showRecordingModal.set(true);
  }

  setRecordingUploadType(type: 'link' | 'file'): void {
    this.recordingUploadType.set(type);
    const urlControl = this.recordingForm.get('recordingUrl');
    if (type === 'file') {
      urlControl?.clearValidators();
    } else {
      urlControl?.setValidators([Validators.required, Validators.pattern(/^(http|https):\/\/[^\s$.?#].[^\s]*$/)]);
    }
    urlControl?.updateValueAndValidity();
  }

  onRecordingFileSelected(event: any): void {
    const file = event.target.files?.[0];
    if (file) {
      this.selectedRecordingFile.set(file);
    }
  }

  submitRecording(): void {
    const lc = this.selectedLiveClassForRecording();
    if (!lc) return;

    if (this.recordingUploadType() === 'link') {
      if (this.recordingForm.invalid) {
        this.recordingForm.markAllAsTouched();
        return;
      }

      this.isSubmitting.set(true);
      this.errorMessage.set(null);
      this.successMessage.set(null);

      this.liveClassService.uploadRecording(lc.id, this.recordingForm.value.recordingUrl).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Recording URL updated successfully!');
          this.showRecordingModal.set(false);
          this.loadLiveClasses();
        },
        error: (err) => this.handleSubmittingError(err)
      });
    } else {
      const file = this.selectedRecordingFile();
      if (!file) {
        this.errorMessage.set('Please select a video recording file first.');
        return;
      }

      this.isSubmitting.set(true);
      this.errorMessage.set(null);
      this.successMessage.set(null);

      // Upload the recording file via the teacher video upload API (automatically handles storage quota and R2 tracking)
      this.teacherService.uploadVideoFile(file, `Recording: ${lc.title}`, lc.description || '').subscribe({
        next: (res) => {
          const videoUrl = res.video?.videoUrl;
          if (!videoUrl) {
            this.isSubmitting.set(false);
            this.errorMessage.set('Failed to retrieve uploaded video URL.');
            return;
          }

          // Link the uploaded video URL to the live class recording
          this.liveClassService.uploadRecording(lc.id, videoUrl).subscribe({
            next: () => {
              this.isSubmitting.set(false);
              if (res.limitExceeded) {
                this.errorMessage.set('Recording uploaded, but storage limit exceeded. Please upgrade your plan.');
              } else {
                this.successMessage.set('Recording file uploaded and linked successfully!');
              }
              this.showRecordingModal.set(false);
              this.loadLiveClasses();
              this.loadVideos(); // Reload teacher video library too
            },
            error: (err) => this.handleSubmittingError(err)
          });
        },
        error: (err) => this.handleSubmittingError(err)
      });
    }
  }

  openAssignCourseModal(course: CourseResponse): void {
    this.selectedCourse.set(course);
    const assignedIds = this.classes()
      .filter(c => c.courseId === course.id)
      .map(c => c.id);
    this.courseClassIds.set(assignedIds);
    this.showAssignCourseModal.set(true);
  }

  toggleCourseClass(classId: string): void {
    const ids = [...this.courseClassIds()];
    const idx = ids.indexOf(classId);
    if (idx > -1) {
      ids.splice(idx, 1);
    } else {
      ids.push(classId);
    }
    this.courseClassIds.set(ids);
  }

  assignClassesToCourse(): void {
    const course = this.selectedCourse();
    if (!course) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.courseService.assignClasses(course.id, this.courseClassIds()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Classes assigned to course curriculum successfully!');
        this.showAssignCourseModal.set(false);
        this.loadCourses();
        this.loadClasses();
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  onCsvFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.csvFile.set(input.files[0]);
      this.csvErrors.set([]);
      this.csvUploadProgress.set(null);
    }
  }

  bulkUploadClassId = signal<string>('');

  processBulkCsvUpload(): void {
    const file = this.csvFile();
    const classId = this.bulkUploadClassId();

    if (!file) {
      this.errorMessage.set('Please select a CSV file first.');
      return;
    }
    if (!classId) {
      this.errorMessage.set('Please select a Classroom to enroll the students into.');
      return;
    }

    this.isSubmitting.set(true);
    this.csvErrors.set([]);
    this.csvUploadProgress.set('Reading CSV file...');
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const reader = new FileReader();
    reader.onload = async (e: any) => {
      const text = e.target.result;
      const lines = text.split(/\r?\n/);
      const studentsToRegister: { firstName: string; lastName: string; email: string }[] = [];

      for (let i = 0; i < lines.length; i++) {
        const line = lines[i].trim();
        if (!line) continue;

        if (i === 0 && (line.toLowerCase().includes('email') || line.toLowerCase().includes('first'))) {
          continue;
        }

        const parts = line.split(',');
        if (parts.length < 3) {
          this.csvErrors.update(errs => [...errs, `Row ${i + 1}: Invalid format. Expected FirstName,LastName,Email`]);
          continue;
        }

        const firstName = parts[0].trim();
        const lastName = parts[1].trim();
        const email = parts[2].trim();

        if (!firstName || !lastName || !email) {
          this.csvErrors.update(errs => [...errs, `Row ${i + 1}: Missing name or email`]);
          continue;
        }

        studentsToRegister.push({ firstName, lastName, email });
      }

      if (studentsToRegister.length === 0) {
        this.csvUploadProgress.set(null);
        this.isSubmitting.set(false);
        this.errorMessage.set('No valid student rows found in CSV.');
        return;
      }

      this.csvUploadProgress.set(`Found ${studentsToRegister.length} students. Registering...`);
      let successCount = 0;
      let failureCount = 0;

      for (let index = 0; index < studentsToRegister.length; index++) {
        const student = studentsToRegister[index];
        this.csvUploadProgress.set(`Registering ${index + 1}/${studentsToRegister.length}: ${student.email}...`);

        try {
          const regRes: any = await this.authService.registerStudent({
            email: student.email,
            password: 'Student123!',
            firstName: student.firstName,
            lastName: student.lastName
          }).toPromise();

          if (regRes && regRes.studentId) {
            await this.teacherService.enrollStudent(classId, regRes.studentId).toPromise();
            successCount++;
          } else {
            failureCount++;
            this.csvErrors.update(errs => [...errs, `Failed to register ${student.email}: response empty`]);
          }
        } catch (err: any) {
          failureCount++;
          const errorMsg = err.error?.message || err.message || 'Unknown error';
          this.csvErrors.update(errs => [...errs, `Failed to process ${student.email}: ${errorMsg}`]);

          if (err.status === 403) {
            this.csvErrors.update(errs => [...errs, 'Aborted early: Subscription student limit reached.']);
            break;
          }
        }
      }

      this.csvUploadProgress.set(null);
      this.isSubmitting.set(false);
      this.csvFile.set(null);

      this.loadStudents();
      this.loadClassDetails(classId);

      if (successCount > 0) {
        this.successMessage.set(`Successfully registered and enrolled ${successCount} students!`);
      }
      if (failureCount > 0) {
        this.errorMessage.set(`Completed with errors. ${failureCount} rows failed.`);
      }
    };

    reader.readAsText(file);
  }

  loadCertificates(): void {
    this.isLoading.set(true);
    this.teacherService.getCertificates().subscribe({
      next: (data) => {
        this.certificates.set(data);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to load certificates.')
    });
  }

  openCertificateModal(): void {
    this.certificateForm.reset({
      studentId: '',
      courseId: null,
      classId: null,
      customTitle: 'CERTIFICATE OF COMPLETION',
      customSubTitle: 'This certificate is proudly presented to',
      customMessage: 'for successfully completing the curriculum requirements for'
    });
    this.showCertificateModal.set(true);
  }

  awardCertificate(): void {
    if (this.certificateForm.invalid) {
      this.certificateForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formVal = this.certificateForm.value;
    const payload = {
      studentId: formVal.studentId,
      courseId: formVal.courseId || null,
      classId: formVal.classId || null,
      customTitle: formVal.customTitle || null,
      customSubTitle: formVal.customSubTitle || null,
      customMessage: formVal.customMessage || null
    };

    this.teacherService.awardCertificate(payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Certificate awarded successfully!');
        this.showCertificateModal.set(false);
        this.loadCertificates();
      },
      error: (err) => this.handleSubmittingError(err)
    });
  }

  deleteCertificate(id: string, event: Event): void {
    event.stopPropagation();
    if (confirm('Are you sure you want to delete this certificate? This will invalidate the certificate code.')) {
      this.isLoading.set(true);
      this.teacherService.deleteCertificate(id).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.successMessage.set('Certificate deleted successfully!');
          this.loadCertificates();
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to delete certificate.');
        }
      });
    }
  }

  downloadCertificatePdf(id: string, event: Event): void {
    event.stopPropagation();
    this.isLoading.set(true);
    this.reportService.downloadCertificatePdf(id).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `certificate_${id}.pdf`);
        this.isLoading.set(false);
      },
      error: () => this.handleError('Failed to download certificate PDF.')
    });
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

  onTeacherTourStepChanged(index: number): void {
    const tabMap: Record<number, string> = {
      1: 'classes',
      2: 'live-classes',
      3: 'courses',
      4: 'students',
      5: 'videos',
      6: 'announcements',
      7: 'analytics',
      8: 'billing',
      9: 'certificates',
      10: 'profile'
    };
    const targetTab = tabMap[index];
    if (targetTab) {
      this.setTab(targetTab);
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private handleError(msg: string): void {
    this.errorMessage.set(msg);
    this.isLoading.set(false);
  }

  private handleSubmittingError(err: any): void {
    this.errorMessage.set(err.error?.message || 'An error occurred while submitting.');
    this.isSubmitting.set(false);
  }
}
