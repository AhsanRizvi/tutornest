import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateCourseRequest, CourseResponse, CourseProgressResponse, CertificateResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class CourseService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/Course`;
  }

  createCourse(request: CreateCourseRequest): Observable<CourseResponse> {
    return this.http.post<CourseResponse>(this.apiUrl, request);
  }
  getCourses(): Observable<CourseResponse[]> {
    return this.http.get<CourseResponse[]>(this.apiUrl);
  }
  getCourseById(id: string): Observable<CourseResponse> {
    return this.http.get<CourseResponse>(`${this.apiUrl}/${id}`);
  }
  assignClasses(id: string, classIds: string[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/classes`, { classIds });
  }
  getStudentProgress(id: string): Observable<CourseProgressResponse> {
    return this.http.get<CourseProgressResponse>(`${this.apiUrl}/${id}/progress`);
  }
  getMyCertificates(): Observable<CertificateResponse[]> {
    return this.http.get<CertificateResponse[]>(`${this.apiUrl}/certificates`);
  }
  getCertificate(id: string): Observable<CertificateResponse> {
    return this.http.get<CertificateResponse>(`${this.apiUrl}/certificates/${id}`);
  }
}
