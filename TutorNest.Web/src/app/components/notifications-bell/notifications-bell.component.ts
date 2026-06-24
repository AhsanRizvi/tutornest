import { Component, OnInit, signal, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../services/notification.service';
import { AuthService } from '../../services/auth.service';
import { NotificationResponse } from '../../models';
import { Router } from '@angular/router';

@Component({
  selector: 'app-notifications-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications-bell.component.html',
  styleUrls: ['./notifications-bell.component.scss']
})
export class NotificationsBellComponent implements OnInit {
  isOpen = signal<boolean>(false);

  constructor(
    public notificationService: NotificationService,
    private authService: AuthService,
    private eRef: ElementRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.notificationService.loadNotifications();
    
    // Poll notifications every 30 seconds for live updates
    setInterval(() => {
      this.notificationService.loadNotifications();
    }, 30000);
  }

  toggleDropdown(): void {
    this.isOpen.update(val => !val);
    if (this.isOpen()) {
      this.notificationService.loadNotifications();
    }
  }

  markAsRead(item: NotificationResponse, event: MouseEvent): void {
    event.stopPropagation(); // Avoid closing dropdown
    if (!item.isRead) {
      this.notificationService.markAsRead(item.id).subscribe();
    }
    if (item.type === 'LiveClass') {
      sessionStorage.setItem('preferred_tab', 'live-classes');
      if (this.router.url.includes('/student')) {
        window.location.href = '/student';
      } else if (this.router.url.includes('/teacher')) {
        window.location.href = '/teacher';
      } else {
        const role = this.authService.userRole();
        if (role === 'Student') {
          this.router.navigate(['/student']);
        } else if (role === 'Teacher') {
          this.router.navigate(['/teacher']);
        }
      }
      this.isOpen.set(false);
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe();
  }

  // Close dropdown when clicking outside
  @HostListener('document:click', ['$event'])
  clickout(event: MouseEvent) {
    if (!this.eRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  getIcon(type: string): string {
    switch (type) {
      case 'Assignment': return 'fa-file-signature text-cyan';
      case 'Announcement': return 'fa-bullhorn text-purple';
      case 'Grade': return 'fa-award text-emerald';
      case 'LiveClass': return 'fa-tower-broadcast text-rose';
      default: return 'fa-bell text-secondary';
    }
  }
}
