import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AnnouncementResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class AnnouncementService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/announcement`;
  }

  createAnnouncement(data: {
    title: string;
    content: string;
    attachmentUrl?: string | null;
    classId?: string | null;
  }): Observable<AnnouncementResponse> {
    return this.http.post<AnnouncementResponse>(this.apiUrl, data);
  }
  getStudentAnnouncements(): Observable<AnnouncementResponse[]> {
    return this.http.get<AnnouncementResponse[]>(`${this.apiUrl}/student`);
  }
  getTeacherAnnouncements(): Observable<AnnouncementResponse[]> {
    return this.http.get<AnnouncementResponse[]>(`${this.apiUrl}/teacher`);
  }
  markAsRead(announcementId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${announcementId}/read`, {});
  }
  updateAnnouncement(announcementId: string, data: {
    title: string;
    content: string;
    attachmentUrl?: string | null;
    classId?: string | null;
  }): Observable<AnnouncementResponse> {
    return this.http.put<AnnouncementResponse>(`${this.apiUrl}/${announcementId}`, data);
  }
  deleteAnnouncement(announcementId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${announcementId}`);
  }
}
