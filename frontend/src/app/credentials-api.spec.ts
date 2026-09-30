import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CredentialsApi } from './credentials-api';

describe('CredentialsApi', () => {
  let api: CredentialsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(CredentialsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create envía el alta por POST a /api/credentials', () => {
    const request = {
      nombre: 'Juan',
      apellido: 'Pérez',
      dni: '30123456',
      categoria: 'niño' as const,
      foto: 'https://x.test/f.jpg',
    };

    api.create(request).subscribe();

    const call = http.expectOne('/api/credentials');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(request);
    call.flush({});
  });

  it('list sin filtros no agrega parámetros', () => {
    api.list().subscribe();

    const call = http.expectOne('/api/credentials');
    expect(call.request.method).toBe('GET');
    expect(call.request.params.keys()).toEqual([]);
    call.flush([]);
  });

  it('list con filtros los envía sin espacios y omite los vacíos', () => {
    api.list({ dni: ' 30123456 ', numeroSocio: '  ' }).subscribe();

    const call = http.expectOne((r) => r.url === '/api/credentials');
    expect(call.request.params.get('dni')).toBe('30123456');
    expect(call.request.params.has('numeroSocio')).toBe(false);
    call.flush([]);
  });
});
