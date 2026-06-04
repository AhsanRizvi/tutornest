import { Injectable, Inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { NotificationResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private apiUrl: string;

  readonly notifications = signal<NotificationResponse[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/notification`;
  }

  loadNotifications(): void {
    this.http.get<NotificationResponse[]>(this.apiUrl).subscribe({
      next: (data) => this.notifications.set(data),
      error: () => console.error('Failed to load notifications')
    });
  }
  markAsRead(notificationId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${notificationId}/read`, {}).pipe(
      tap(() => this.notifications.update(list =>
        list.map(n => n.id === notificationId ? { ...n, isRead: true } : n)
      ))
    );
  }
  markAllAsRead(): Observable<any> {
    return this.http.post(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => this.notifications.update(list => list.map(n => ({ ...n, isRead: true }))))
    );
  }
}
