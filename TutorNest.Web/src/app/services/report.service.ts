import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/report`;
  }

  downloadClassProgressPdf(classId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/class/${classId}/pdf`, { responseType: 'blob' });
  }
  downloadAssignmentResultsPdf(assignmentId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/assignment/${assignmentId}/pdf`, { responseType: 'blob' });
  }
  downloadAdminPlatformPdf(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/admin/platform/pdf`, { responseType: 'blob' });
  }
  downloadCertificatePdf(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/certificate/${id}/pdf`, { responseType: 'blob' });
  }
  downloadAdminRevenuePdf(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/admin/revenue/pdf`, { responseType: 'blob' });
  }
}
