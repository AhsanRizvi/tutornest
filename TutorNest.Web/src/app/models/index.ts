export interface AuthResponse {
  token: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  expiration: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface ClassResponse {
  id: string;
  name: string;
  description: string;
  createdAt: string;
  teacherId: string;
  studentCount: number;
  courseId?: string | null;
}

export interface VideoResponse {
  id: string;
  title: string;
  description: string;
  videoUrl: string;
  createdAt: string;
  teacherId: string;
}

export interface StudentResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface StudentProgressReport {
  studentId: string;
  studentName: string;
  studentEmail: string;
  videoId: string;
  videoTitle: string;
  watchTimeSeconds: number;
  durationSeconds: number;
  isCompleted: boolean;
  lastWatchedAt: string;
}

export interface StudentVideoResponse {
  id: string;
  title: string;
  description: string;
  videoUrl: string;
  createdAt: string;
  watchTimeSeconds: number;
  durationSeconds: number;
  isCompleted: boolean;
  lastWatchedAt?: string;
}

export interface AssignmentResponse {
  id: string;
  title: string;
  description: string;
  dueDate: string;
  totalMarks: number;
  type: string;
  configJson?: string | null;
  classId: string;
  isSubmitted?: boolean;
  scoreEarned?: number | null;
  isGraded?: boolean;
  feedback?: string | null;
}

export interface SubmissionResponse {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  answerText?: string | null;
  attachmentUrl?: string | null;
  grade?: number | null;
  feedback?: string | null;
  submittedAt: string;
  gradedAt?: string | null;
}

export interface AnnouncementResponse {
  id: string;
  title: string;
  content: string;
  attachmentUrl?: string | null;
  teacherId: string;
  teacherName: string;
  classId?: string | null;
  className?: string | null;
  createdAt: string;
  isRead: boolean;
}

export interface NotificationResponse {
  id: string;
  userId: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

export interface ClassProgressDto {
  className: string;
  averageWatchTimeSeconds: number;
  completionRatePercentage: number;
  activeStudentsCount: number;
  assignmentsCount: number;
}

export interface VideoWatchCountDto {
  videoTitle: string;
  totalWatchTracks: number;
  averageCompletionPercentage: number;
}

export interface StudentEngagementDto {
  studentName: string;
  studentEmail: string;
  totalWatchTimeHours: number;
  completedVideosCount: number;
  submittedAssignmentsCount: number;
}

export interface TopPerformerDto {
  studentName: string;
  studentEmail: string;
  averageScorePercentage: number;
  gradedAssignmentsCount: number;
}

export interface TeacherAnalyticsResponse {
  classProgress: ClassProgressDto[];
  mostWatchedVideos: VideoWatchCountDto[];
  studentEngagement: StudentEngagementDto[];
  topPerformers: TopPerformerDto[];
}

export interface AdminAnalyticsResponse {
  totalTeachers: number;
  totalStudents: number;
  totalClasses: number;
  totalVideos: number;
  totalAssignments: number;
  totalSubmissions: number;
}

export interface SubscriptionPlanResponse {
  id: string;
  name: string;
  price: number;
  currency: string;
  classLimit: number;
  studentLimit: number;
  storageLimitBytes: number;
  isActive: boolean;
}

export interface TeacherSubscriptionResponse {
  id: string;
  planId: string;
  planName: string;
  price: number;
  currency: string;
  status: string;
  startDate: string;
  endDate: string;
  storageUsedBytes: number;
  storageLimitBytes: number;
  classCount: number;
  classLimit: number;
  studentCount: number;
  studentLimit: number;
}

export interface PaymentHistoryResponse {
  id: string;
  planName: string;
  amount: number;
  currency: string;
  status: string;
  paymentProvider: string;
  transactionId: string;
  paymentDate: string;
}

export interface CheckoutResponse {
  sessionUrl: string;
  success: boolean;
}

export interface UserProfileResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  bio?: string | null;
  subject?: string | null;
  profilePictureUrl?: string | null;
  role: string;
  referralCode?: string | null;
  referredTutors?: string[] | null;
}

export interface TeacherDetailsResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  subscription: TeacherSubscriptionResponse;
}

export interface CreateLiveClassRequest {
  title: string;
  description: string;
  scheduledStartTime: string;
  durationMinutes: number;
  meetingLink: string;
  classId: string;
}

export interface LiveClassResponse {
  id: string;
  title: string;
  description: string;
  scheduledStartTime: string;
  durationMinutes: number;
  meetingLink: string;
  recordingUrl?: string | null;
  classId: string;
  className: string;
  teacherId: string;
  teacherName: string;
  createdAt: string;
}

export interface UploadRecordingRequest {
  recordingUrl: string;
}

export interface CreateCourseRequest {
  title: string;
  description: string;
}

export interface CourseResponse {
  id: string;
  title: string;
  description: string;
  teacherId: string;
  teacherName: string;
  classesCount: number;
  createdAt: string;
}

export interface AssignClassesRequest {
  classIds: string[];
}

export interface CourseProgressResponse {
  completionPercentage: number;
  certificateIssued: boolean;
  certificateCode?: string | null;
  certificateId?: string | null;
}

export interface CertificateResponse {
  id: string;
  studentName: string;
  studentEmail: string;
  courseId?: string | null;
  courseTitle?: string | null;
  classId?: string | null;
  className?: string | null;
  certificateCode: string;
  issuedAt: string;
}

