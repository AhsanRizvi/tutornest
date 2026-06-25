import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.tutornest.app',
  appName: 'TutorNest',
  webDir: 'dist/tutor-nest.web/browser',
  plugins: {
    CapacitorHttp: {
      enabled: true
    }
  }
};

export default config;
