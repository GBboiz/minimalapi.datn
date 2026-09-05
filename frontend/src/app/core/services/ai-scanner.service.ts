import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

  scanInvoice(file?: File): Observable<ScannedInvoice> {
    const formData = new FormData();
    if (file) {
      formData.append('image', file, file.name);
    }
    return this.http.post<ScannedInvoice>(`${this.baseUrl}/scan`, formData);
  }

  confirmScannedOrder(req: ConfirmScannedOrderRequest): Observable<ConfirmScannedOrderResponse> {
    return this.http.post<ConfirmScannedOrderResponse>(`${this.baseUrl}/confirm`, req);
  }
}
