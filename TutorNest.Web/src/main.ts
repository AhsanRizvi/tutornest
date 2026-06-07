import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { environment } from './environments/environment';

bootstrapApplication(AppComponent, appConfig)
  .then(() => {
    if ('serviceWorker' in navigator) {
      if (environment.production) {
        navigator.serviceWorker.register('/sw.js')
          .then(reg => console.log('Service Worker registered successfully!', reg))
          .catch(err => console.error('Service Worker registration failed:', err));
      } else {
        // Unregister service worker in development mode to prevent local cache issues
        navigator.serviceWorker.getRegistrations().then(registrations => {
          for (const registration of registrations) {
            registration.unregister().then(success => {
              if (success) console.log('Service Worker unregistered in development mode.');
            });
          }
        });
      }
    }
  })
  .catch((err) => console.error(err));
