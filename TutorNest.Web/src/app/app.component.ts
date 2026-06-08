import { Component, effect } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'TutorNest.Web';

  constructor(
    private authService: AuthService,
    private themeService: ThemeService
  ) {
    // Automatically load theme when authenticated, or default to classic indigo when logged out
    effect(() => {
      if (this.authService.isAuthenticated()) {
        this.themeService.loadTheme().subscribe({
          error: () => this.themeService.applyTheme('default')
        });
      } else {
        this.themeService.applyTheme('default');
      }
    });
  }
}
