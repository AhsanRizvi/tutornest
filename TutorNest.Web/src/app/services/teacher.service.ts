import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ClassResponse, VideoResponse, StudentResponse, StudentProgressReport, CertificateResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class TeacherService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/teacher`;
  }

  getClasses(): Observable<ClassResponse[]> {
    return this.http.get<ClassResponse[]>(`${this.apiUrl}/classes`);
  }
  createClass(data: { name: string; description: string }): Observable<ClassResponse> {
    return this.http.post<ClassResponse>(`${this.apiUrl}/classes`, data);
  }
  getStudents(): Observable<StudentResponse[]> {
    return this.http.get<StudentResponse[]>(`${this.apiUrl}/students`);
  }
  enrollStudent(classId: string, studentId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/classes/${classId}/enroll`, { studentId });
  }
  getClassStudents(classId: string): Observable<StudentResponse[]> {
    return this.http.get<StudentResponse[]>(`${this.apiUrl}/classes/${classId}/students`);
  }
  getVideos(): Observable<VideoResponse[]> {
    return this.http.get<VideoResponse[]>(`${this.apiUrl}/videos`);
  }
  createVideo(data: { title: string; description: string; videoUrl: string }): Observable<VideoResponse> {
    return this.http.post<VideoResponse>(`${this.apiUrl}/videos`, data);
  }
  uploadVideoFile(file: File, title: string, description: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('title', title);
    formData.append('description', description);
    return this.http.post<any>(`${this.apiUrl}/videos/upload`, formData);
  }
  assignVideo(classId: string, videoId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/classes/${classId}/videos`, { videoId });
  }
  getClassVideos(classId: string): Observable<VideoResponse[]> {
    return this.http.get<VideoResponse[]>(`${this.apiUrl}/classes/${classId}/videos`);
  }
  getProgressReports(): Observable<StudentProgressReport[]> {
    return this.http.get<StudentProgressReport[]>(`${this.apiUrl}/progress`);
  }
  updateClass(classId: string, data: { name: string; description: string }): Observable<ClassResponse> {
    return this.http.put<ClassResponse>(`${this.apiUrl}/classes/${classId}`, data);
  }
  deleteClass(classId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/classes/${classId}`);
  }
  deleteVideo(videoId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/videos/${videoId}`);
  }
  updateStudent(studentId: string, data: { email: string; firstName: string; lastName: string; password?: string }): Observable<StudentResponse> {
    return this.http.put<StudentResponse>(`${this.apiUrl}/students/${studentId}`, data);
  }
  deleteStudent(studentId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/students/${studentId}`);
  }
  removeStudentFromClass(classId: string, studentId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/classes/${classId}/students/${studentId}`);
  }
  removeVideoFromClass(classId: string, videoId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/classes/${classId}/videos/${videoId}`);
  }
  getCertificates(): Observable<CertificateResponse[]> {
    return this.http.get<CertificateResponse[]>(`${this.apiUrl}/certificates`);
  }
  awardCertificate(data: {
    studentId: string;
    courseId?: string | null;
    classId?: string | null;
    customTitle?: string | null;
    customSubTitle?: string | null;
    customMessage?: string | null;
    logoUrl?: string | null;
  }): Observable<CertificateResponse> {
    return this.http.post<CertificateResponse>(`${this.apiUrl}/certificates`, data);
  }
  deleteCertificate(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/certificates/${id}`);
  }
  uploadCertificateLogo(file: File): Observable<{ logoUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ logoUrl: string }>(`${this.apiUrl}/certificates/upload-logo`, formData);
  }
}
