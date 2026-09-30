import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AltaRequest, AltaResponse, CredentialFilters, CredentialItem } from './models';

/** Cliente de la API. Las rutas son relativas: el proxy de `ng serve` o nginx las reenvían al backend (ADR 009). */
@Injectable({ providedIn: 'root' })
export class CredentialsApi {
  private readonly http = inject(HttpClient);

  create(request: AltaRequest): Observable<AltaResponse> {
    return this.http.post<AltaResponse>('/api/credentials', request);
  }

  list(filters: CredentialFilters = {}): Observable<CredentialItem[]> {
    let params = new HttpParams();
    if (filters.dni?.trim()) {
      params = params.set('dni', filters.dni.trim());
    }
    if (filters.numeroSocio?.trim()) {
      params = params.set('numeroSocio', filters.numeroSocio.trim());
    }
    return this.http.get<CredentialItem[]>('/api/credentials', { params });
  }
}
