import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Promotion, CreatePromotionRequest, UpdatePromotionRequest } from '../models/promotion.model';

@Injectable({
  providedIn: 'root'
})
export class PromotionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/promotions`;

  getAll(onlyActive?: boolean): Observable<Promotion[]> {
    let params = new HttpParams();
    if (onlyActive !== undefined) {
      params = params.set('onlyActive', onlyActive.toString());
    }
    return this.http.get<Promotion[]>(this.apiUrl, { params });
  }

  create(data: CreatePromotionRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, data);
  }

  update(id: string, data: UpdatePromotionRequest): Observable<string> {
    return this.http.put<string>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
