import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ScannedInvoice,
  ConfirmScannedOrderRequest,
  ConfirmScannedOrderResponse
} from '../models/ai-scanner.model';

@Injectable({ providedIn: 'root' })
export class AiScannerService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/ai`;

  getGeminiApiKey(): string {
    return localStorage.getItem('gemini_api_key') || '';
  }

  setGeminiApiKey(key: string): void {
    if (key.trim()) {
      localStorage.setItem('gemini_api_key', key.trim());
    } else {
      localStorage.removeItem('gemini_api_key');
    }
  }

  scanInvoice(file?: File, sampleType?: string): Observable<ScannedInvoice> {
    const formData = new FormData();
    if (file) {
      formData.append('image', file, file.name);
    }
    if (sampleType) {
      formData.append('sampleType', sampleType);
    }

    let headers = new HttpHeaders();
    const apiKey = this.getGeminiApiKey();
    if (apiKey) {
      headers = headers.set('X-Gemini-Key', apiKey);
    }

    let params = new HttpParams();
    if (sampleType) {
      params = params.set('sampleType', sampleType);
    }

    return this.http.post<ScannedInvoice>(`${this.baseUrl}/scan`, formData, { headers, params });
  }

  confirmScannedOrder(req: ConfirmScannedOrderRequest): Observable<ConfirmScannedOrderResponse> {
    return this.http.post<ConfirmScannedOrderResponse>(`${this.baseUrl}/confirm`, req);
  }
}
