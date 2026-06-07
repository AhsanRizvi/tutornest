import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ClassResponse, StudentVideoResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class StudentService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/student`;
  }

  getClasses(): Observable<ClassResponse[]> {
    return this.http.get<ClassResponse[]>(`${this.apiUrl}/classes`);
  }
  getClassVideos(classId: string): Observable<StudentVideoResponse[]> {
    return this.http.get<StudentVideoResponse[]>(`${this.apiUrl}/classes/${classId}/videos`);
  }
  updateProgress(videoId: string, progress: { watchTimeSeconds: number; durationSeconds: number; isCompleted: boolean }): Observable<any> {
    return this.http.post(`${this.apiUrl}/videos/${videoId}/progress`, progress);
  }
  getClassLeaderboard(classId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/classes/${classId}/leaderboard`);
  }
}
