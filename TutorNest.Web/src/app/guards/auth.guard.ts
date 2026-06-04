import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/login']);
  return false;
};

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const role = authService.userRole();

    if (authService.isAuthenticated() && role && allowedRoles.includes(role)) {
      return true;
    }

    if (role === 'Admin') {
      router.navigate(['/admin']);
    } else if (role === 'Teacher') {
      router.navigate(['/teacher']);
    } else if (role === 'Student') {
      router.navigate(['/student']);
    } else {
      router.navigate(['/login']);
    }
    
    return false;
  };
};
