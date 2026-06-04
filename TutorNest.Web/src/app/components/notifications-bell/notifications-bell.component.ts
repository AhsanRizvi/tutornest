import { Component, OnInit, signal, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../services/notification.service';
import { NotificationResponse } from '../../models';

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
    private eRef: ElementRef
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
      default: return 'fa-bell text-secondary';
    }
  }
}
