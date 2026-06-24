import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateLiveClassRequest, LiveClassResponse, AgoraTokenResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class LiveClassService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/LiveClass`;
  }

  scheduleLiveClass(request: CreateLiveClassRequest): Observable<LiveClassResponse> {
    return this.http.post<LiveClassResponse>(this.apiUrl, request);
  }
  getClassLiveClasses(classId: string): Observable<LiveClassResponse[]> {
    return this.http.get<LiveClassResponse[]>(`${this.apiUrl}/class/${classId}`);
  }
  getUpcomingLiveClasses(): Observable<LiveClassResponse[]> {
    return this.http.get<LiveClassResponse[]>(`${this.apiUrl}/upcoming`);
  }
  uploadRecording(id: string, recordingUrl: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/recording`, { recordingUrl });
  }
  getLiveClassById(id: string): Observable<LiveClassResponse> {
    return this.http.get<LiveClassResponse>(`${this.apiUrl}/${id}`);
  }
  startInstantLiveClass(request: CreateLiveClassRequest): Observable<LiveClassResponse> {
    return this.http.post<LiveClassResponse>(`${this.apiUrl}/instant`, request);
  }
  getAgoraToken(id: string): Observable<AgoraTokenResponse> {
    return this.http.get<AgoraTokenResponse>(`${this.apiUrl}/${id}/token`);
  }
}
