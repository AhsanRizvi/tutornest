import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CheckoutResponse } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private apiUrl: string;
  constructor(private http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.apiUrl = `${baseUrl}/api/payment`;
  }

  createCheckoutSession(planId: string, successUrl: string, cancelUrl: string): Observable<CheckoutResponse> {
    return this.http.post<CheckoutResponse>(`${this.apiUrl}/checkout`, { planId, successUrl, cancelUrl });
  }
  mockCheckout(planId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/mock-checkout`, { planId });
  }
}
