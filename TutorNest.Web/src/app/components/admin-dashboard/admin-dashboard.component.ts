import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService } from '../../services/admin.service';
import { AuthService } from '../../services/auth.service';
import { SubscriptionService } from '../../services/subscription.service';
import { ReportService } from '../../services/report.service';
import { TeacherDetailsResponse, SubscriptionPlanResponse } from '../../models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  teachers = signal<TeacherDetailsResponse[]>([]);
  plans = signal<SubscriptionPlanResponse[]>([]);
  teacherForm: FormGroup;
  
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  
  adminName = signal<string>('Admin');

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private authService: AuthService,
    private subscriptionService: SubscriptionService,
    private reportService: ReportService,
    private router: Router
  ) {
    this.teacherForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.adminName.set(`${user.firstName} ${user.lastName}`);
    }
    this.loadTeachers();
    this.loadPlans();
  }

  loadTeachers(): void {
    this.isLoading.set(true);
    this.adminService.getTeachers().subscribe({
      next: (data: any) => {
        this.teachers.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set('Failed to load teachers.');
        this.isLoading.set(false);
      }
    });
  }

  loadPlans(): void {
    this.subscriptionService.getPlans().subscribe({
      next: (data) => {
        this.plans.set(data);
      },
      error: () => {
        this.errorMessage.set('Failed to load subscription plans.');
      }
    });
  }

  upgradePlan(teacherId: string, planId: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    
    this.subscriptionService.adminUpgradeTeacher(teacherId, planId).subscribe({
      next: (res) => {
        this.successMessage.set(res.message || 'Teacher subscription upgraded successfully.');
        this.loadTeachers(); // Refresh list to reflect changes
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to upgrade teacher plan.');
        this.isLoading.set(false);
      }
    });
  }

  downloadPlatformReport(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.reportService.downloadAdminPlatformPdf().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'platform_telemetry_report.pdf';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.isLoading.set(false);
        this.successMessage.set('Report downloaded successfully.');
      },
      error: () => {
        this.errorMessage.set('Failed to download platform telemetry report.');
        this.isLoading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.teacherForm.invalid) {
      this.teacherForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authService.registerTeacher(this.teacherForm.value).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Teacher account created successfully!');
        this.teacherForm.reset();
        this.loadTeachers(); // Refresh list
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to create teacher account.');
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
