import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GoogleSheetStatus, SyncHistoryItem, ConnectGoogleSheetRequest } from '../models/google-sheets.model';

@Injectable({
  providedIn: 'root'
})
export class GoogleSheetsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/googlesheets`;

  getStatus(): Observable<GoogleSheetStatus> {
    return this.http.get<GoogleSheetStatus>(`${this.apiUrl}/status`);
  }

  getOAuthUrl(): Observable<{ url: string }> {
    return this.http.get<{ url: string }>(`${this.apiUrl}/connect-url`);
  }

  connect(request: ConnectGoogleSheetRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/connect`, request);
  }

  disconnect(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/disconnect`, {});
  }

  getHistory(count: number = 20): Observable<SyncHistoryItem[]> {
    return this.http.get<SyncHistoryItem[]>(`${this.apiUrl}/history?count=${count}`);
  }

  retry(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/retry/${id}`, {});
  }
}
