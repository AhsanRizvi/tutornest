import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { AdminDashboardComponent } from './components/admin-dashboard/admin-dashboard.component';
import { TeacherDashboardComponent } from './components/teacher-dashboard/teacher-dashboard.component';
import { StudentDashboardComponent } from './components/student-dashboard/student-dashboard.component';
import { LandingPageComponent } from './components/landing-page/landing-page.component';
import { LiveClassRoomComponent } from './components/live-class-room/live-class-room.component';
import { authGuard, roleGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'admin', component: AdminDashboardComponent, canActivate: [authGuard, roleGuard(['Admin'])] },
  { path: 'teacher', component: TeacherDashboardComponent, canActivate: [authGuard, roleGuard(['Teacher'])] },
  { path: 'student', component: StudentDashboardComponent, canActivate: [authGuard, roleGuard(['Student'])] },
  { path: 'live-class/:id', component: LiveClassRoomComponent, canActivate: [authGuard] },
  { path: '', component: LandingPageComponent },
  { path: '**', redirectTo: '' }
];
