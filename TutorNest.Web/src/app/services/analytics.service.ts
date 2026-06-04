import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TeacherAnalyticsResponse, AdminAnalyticsResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/analytics`;
  }

  getTeacherAnalytics(): Observable<TeacherAnalyticsResponse> {
    return this.http.get<TeacherAnalyticsResponse>(`${this.apiUrl}/teacher`);
  }
  getAdminAnalytics(): Observable<AdminAnalyticsResponse> {
    return this.http.get<AdminAnalyticsResponse>(`${this.apiUrl}/admin`);
  }
}
