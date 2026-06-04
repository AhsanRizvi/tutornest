import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { AdminDashboardComponent } from './components/admin-dashboard/admin-dashboard.component';
import { TeacherDashboardComponent } from './components/teacher-dashboard/teacher-dashboard.component';
import { StudentDashboardComponent } from './components/student-dashboard/student-dashboard.component';
import { authGuard, roleGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'admin', component: AdminDashboardComponent, canActivate: [authGuard, roleGuard(['Admin'])] },
  { path: 'teacher', component: TeacherDashboardComponent, canActivate: [authGuard, roleGuard(['Teacher'])] },
  { path: 'student', component: StudentDashboardComponent, canActivate: [authGuard, roleGuard(['Student'])] },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];
