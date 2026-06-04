// Production environment — API_URL is replaced at build time by Vercel.
// Set the VITE_API_URL or API_URL env var in your Vercel project settings.
export const environment = {
  production: true,
  // Falls back to localhost if the env var is not set (shouldn't happen in production)
  apiUrl: (window as any).__API_URL__ || 'http://localhost:5299'
};
