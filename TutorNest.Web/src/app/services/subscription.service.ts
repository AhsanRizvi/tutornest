import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SubscriptionPlanResponse,
  TeacherSubscriptionResponse,
  PaymentHistoryResponse,
  UserProfileResponse
} from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/subscription`;
  }

  getPlans(): Observable<SubscriptionPlanResponse[]> {
    return this.http.get<SubscriptionPlanResponse[]>(`${this.apiUrl}/plans`);
  }
  getMyStatus(): Observable<TeacherSubscriptionResponse> {
    return this.http.get<TeacherSubscriptionResponse>(`${this.apiUrl}/my-status`);
  }
  getBillingHistory(): Observable<PaymentHistoryResponse[]> {
    return this.http.get<PaymentHistoryResponse[]>(`${this.apiUrl}/billing-history`);
  }
  getProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.apiUrl}/profile`);
  }
  updateProfile(data: {
    firstName: string;
    lastName: string;
    bio?: string | null;
    subject?: string | null;
    profilePictureUrl?: string | null;
  }): Observable<any> {
    return this.http.post(`${this.apiUrl}/profile`, data);
  }
  adminUpgradeTeacher(teacherId: string, planId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/admin/upgrade-teacher`, { teacherId, planId });
  }
}
