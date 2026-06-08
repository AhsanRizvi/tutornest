import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService } from '../../services/admin.service';
import { AuthService } from '../../services/auth.service';
import { SubscriptionService } from '../../services/subscription.service';
import { ReportService } from '../../services/report.service';
import { TeacherDetailsResponse, SubscriptionPlanResponse } from '../../models';

import { AppTourComponent, TourStep } from '../app-tour/app-tour.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AppTourComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  showTour = signal<boolean>(false);
  adminTourSteps: TourStep[] = [
    {
      title: 'Welcome to TutorNest Administrator Workspace!',
      content: 'Let us take a quick tour through the administrative dashboard to help you manage instructors, adjust packages, and download financial reports.',
      position: 'center'
    },
    {
      targetSelector: '.tour-nav-teachers',
      title: 'Tutor Accounts Manager',
      content: 'Create and register new instructor credentials, review active tutor details, delete inactive teachers, or upgrade subscription plans.',
      position: 'right'
    },
    {
      targetSelector: '.tour-nav-plans',
      title: 'Pricing Packages Configuration',
      content: 'Manage the platform subscription packages, edit boundaries like class limits, student count limits, and video storage space for instructors.',
      position: 'right'
    },
    {
      targetSelector: '.profile-menu-container',
      title: 'Admin Settings & Options',
      content: 'Export PDF summaries of platform telemetry, log out securely, or replay this administrator guide at any time.',
      position: 'top'
    }
  ];

  teachers = signal<TeacherDetailsResponse[]>([]);
  plans = signal<SubscriptionPlanResponse[]>([]);
  teacherForm: FormGroup;
  
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  
  adminName = signal<string>('Admin');
  showProfileDropdown = signal<boolean>(false);
  isSidebarOpen = signal<boolean>(false);
  activeTab = signal<string>('teachers');
  selectedPlan = signal<any | null>(null);
  isEditingPlan = signal<boolean>(false);
  planForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    public authService: AuthService,
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

    this.planForm = this.fb.group({
      name: ['', [Validators.required]],
      price: [0, [Validators.required, Validators.min(0)]],
      currency: ['USD', [Validators.required]],
      classLimit: [5, [Validators.required, Validators.min(1)]],
      studentLimit: [50, [Validators.required, Validators.min(1)]],
      storageLimitMb: [500, [Validators.required, Validators.min(1)]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.adminName.set(`${user.firstName} ${user.lastName}`);
      const hasSeen = localStorage.getItem(`seen_tour_${user.email}_admin`);
      if (!hasSeen) {
        setTimeout(() => this.showTour.set(true), 1200);
      }
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
    this.adminService.getPlans().subscribe({
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

  setTab(tab: string): void {
    this.activeTab.set(tab);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.selectedPlan.set(null);
    this.isEditingPlan.set(false);
    this.planForm.reset({
      name: '',
      price: 0,
      currency: 'USD',
      classLimit: 5,
      studentLimit: 50,
      storageLimitMb: 500,
      isActive: true
    });
    this.showProfileDropdown.set(false);
    this.isSidebarOpen.set(false);

    if (tab === 'teachers') {
      this.loadTeachers();
    } else if (tab === 'plans') {
      this.loadPlans();
    }
  }

  editPlan(plan: any): void {
    this.selectedPlan.set(plan);
    this.isEditingPlan.set(true);
    this.planForm.patchValue({
      name: plan.name,
      price: plan.price,
      currency: plan.currency,
      classLimit: plan.classLimit,
      studentLimit: plan.studentLimit,
      storageLimitMb: Math.round(plan.storageLimitBytes / (1024 * 1024)),
      isActive: plan.isActive
    });
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  cancelPlanEdit(): void {
    this.selectedPlan.set(null);
    this.isEditingPlan.set(false);
    this.planForm.reset({
      name: '',
      price: 0,
      currency: 'USD',
      classLimit: 5,
      studentLimit: 50,
      storageLimitMb: 500,
      isActive: true
    });
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  savePlan(): void {
    if (this.planForm.invalid) {
      this.planForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formValue = this.planForm.value;
    const planPayload = {
      name: formValue.name,
      price: formValue.price,
      currency: formValue.currency,
      classLimit: formValue.classLimit,
      studentLimit: formValue.studentLimit,
      storageLimitBytes: formValue.storageLimitMb * 1024 * 1024,
      isActive: formValue.isActive
    };

    if (this.isEditingPlan() && this.selectedPlan()) {
      const planId = this.selectedPlan()!.id;
      this.adminService.updatePlan(planId, planPayload).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Pricing package updated successfully!');
          this.cancelPlanEdit();
          this.loadPlans();
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to update plan.');
        }
      });
    } else {
      this.adminService.createPlan(planPayload).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set('Pricing package created successfully!');
          this.cancelPlanEdit();
          this.loadPlans();
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(err.error?.message || 'Failed to create plan.');
        }
      });
    }
  }

  toggleProfileDropdown(): void {
    this.showProfileDropdown.set(!this.showProfileDropdown());
  }

  toggleSidebar(): void {
    this.isSidebarOpen.set(!this.isSidebarOpen());
  }

  startTour(): void {
    this.showTour.set(false);
    setTimeout(() => {
      this.showTour.set(true);
    }, 100);
  }

  onTourCompletedOrSkipped(): void {
    this.showTour.set(false);
  }

  onAdminTourStepChanged(index: number): void {
    const tabMap: Record<number, string> = {
      1: 'teachers',
      2: 'plans'
    };
    const targetTab = tabMap[index];
    if (targetTab) {
      this.setTab(targetTab);
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
