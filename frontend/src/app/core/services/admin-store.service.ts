import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminStore, CreateStoreRequest, UpdateStoreRequest, SwitchStoreResponse } from '../models/admin-store.model';

@Injectable({ providedIn: 'root' })
export class AdminStoreService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/stores`;

  getAll(): Observable<AdminStore[]> {
    return this.http.get<AdminStore[]>(this.baseUrl);
  }

  create(req: CreateStoreRequest): Observable<AdminStore> {
    return this.http.post<AdminStore>(this.baseUrl, req);
  }

  update(id: string, req: UpdateStoreRequest): Observable<AdminStore> {
    return this.http.put<AdminStore>(`${this.baseUrl}/${id}`, req);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  switchStore(storeId: string): Observable<SwitchStoreResponse> {
    return this.http.post<SwitchStoreResponse>(`${this.baseUrl}/${storeId}/switch`, {});
  }
}
