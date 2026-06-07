import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TeacherDetailsResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/admin`;
  }

  getTeachers(): Observable<TeacherDetailsResponse[]> {
    return this.http.get<TeacherDetailsResponse[]>(`${this.apiUrl}/teachers`);
  }
  suspendUser(userId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/users/${userId}/suspend`, {});
  }
  unsuspendUser(userId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/users/${userId}/unsuspend`, {});
  }
  getPlans(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/plans`);
  }
  createPlan(plan: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/plans`, plan);
  }
  updatePlan(planId: string, plan: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/plans/${planId}`, plan);
  }
  getRevenueReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/revenue-report`);
  }
}
