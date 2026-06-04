import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssignmentResponse, SubmissionResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/assignment`;
  }

  getAssignments(classId: string): Observable<AssignmentResponse[]> {
    return this.http.get<AssignmentResponse[]>(`${this.apiUrl}/class/${classId}`);
  }
  createAssignment(data: {
    title: string;
    description: string;
    dueDate: string;
    totalMarks: number;
    classId: string;
    type: string;
    configJson?: string | null;
  }): Observable<AssignmentResponse> {
    return this.http.post<AssignmentResponse>(this.apiUrl, data);
  }
  submitAssignment(assignmentId: string, answerText?: string | null, attachmentUrl?: string | null): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(`${this.apiUrl}/${assignmentId}/submit`, { answerText, attachmentUrl });
  }
  getSubmissions(assignmentId: string): Observable<SubmissionResponse[]> {
    return this.http.get<SubmissionResponse[]>(`${this.apiUrl}/${assignmentId}/submissions`);
  }
  gradeSubmission(submissionId: string, grade: number, feedback: string): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(`${this.apiUrl}/submission/${submissionId}/grade`, { grade, feedback });
  }
  getMySubmissions(): Observable<SubmissionResponse[]> {
    return this.http.get<SubmissionResponse[]>(`${this.apiUrl}/my-submissions`);
  }
  uploadFile(file: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${this.apiUrl}/upload`, formData);
  }
}
