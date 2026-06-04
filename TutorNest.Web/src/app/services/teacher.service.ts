import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ClassResponse, VideoResponse, StudentResponse, StudentProgressReport } from '../models';
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
  assignVideo(classId: string, videoId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/classes/${classId}/videos`, { videoId });
  }
  getClassVideos(classId: string): Observable<VideoResponse[]> {
    return this.http.get<VideoResponse[]>(`${this.apiUrl}/classes/${classId}/videos`);
  }
  getProgressReports(): Observable<StudentProgressReport[]> {
    return this.http.get<StudentProgressReport[]>(`${this.apiUrl}/progress`);
  }
}
