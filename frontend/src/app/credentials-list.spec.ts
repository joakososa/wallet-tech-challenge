import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CredentialsList } from './credentials-list';
import { CredentialItem } from './models';

function item(overrides: Partial<CredentialItem> = {}): CredentialItem {
  return {
    id: '8f14e45f-ceea-467e-9de1-93f5a5f4bfae',
    nombre: 'Juan',
    apellido: 'Pérez',
    dni: '30123456',
    numeroSocio: '000001',
    categoria: 'niño',
    foto: 'https://x.test/f.jpg',
    validFrom: '2026-09-30T08:00:00Z',
    validUntil: '2099-09-30T08:00:00Z',
    status: 0,
    credential: { id: 'https://credenciales.futbol.com.ar/x' },
    ...overrides,
  };
}

describe('CredentialsList', () => {
  let http: HttpTestingController;
  let fixture: ReturnType<typeof TestBed.createComponent<CredentialsList>>;
  let element: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CredentialsList);
    element = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => http.verify());

  async function respond(items: CredentialItem[]): Promise<void> {
    await fixture.whenStable();
    http.expectOne((r) => r.url === '/api/credentials').flush(items);
    await fixture.whenStable();
  }

  it('sin credenciales muestra el estado vacío', async () => {
    await respond([]);

    expect(element.textContent).toContain('Todavía no hay credenciales emitidas.');
  });

  it('con credenciales muestra nombre, número de socio, vigencia y estado', async () => {
    await respond([item()]);

    const text = element.textContent!;
    expect(text).toContain('Juan Pérez');
    expect(text).toContain('Socio 000001');
    expect(text).toContain('30/09/2026');
    expect(text).toContain('Activa');
  });

  it('una credencial activa pero pasada de fecha se muestra como vencida', async () => {
    await respond([item({ validUntil: '2020-01-01T00:00:00Z' })]);

    expect(element.querySelector('.badge')?.textContent).toContain('Vencida');
  });

  it('si la API falla muestra el error con la opción de reintentar', async () => {
    await fixture.whenStable();
    http
      .expectOne((r) => r.url === '/api/credentials')
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain('No se pudo cargar el listado');
  });
});
