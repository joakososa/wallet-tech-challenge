import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AltaResultStore } from './alta-result-store';
import { CredentialForm } from './credential-form';
import { AltaResponse } from './models';

const RESPONSE: AltaResponse = {
  numeroSocio: '000001',
  isNewSocio: true,
  validFrom: '2026-09-30T08:00:00Z',
  validUntil: '2027-09-30T08:00:00Z',
  credential: {},
};

describe('CredentialForm', () => {
  let http: HttpTestingController;
  let element: HTMLElement;
  let fixture: ReturnType<typeof TestBed.createComponent<CredentialForm>>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CredentialForm);
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  function fill(values: Record<string, string>): void {
    for (const [id, value] of Object.entries(values)) {
      const input = element.querySelector<HTMLInputElement | HTMLSelectElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event(input.tagName === 'SELECT' ? 'change' : 'input'));
    }
  }

  const submit = async () => {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  };

  const VALID = {
    nombre: 'Juan',
    apellido: 'Pérez',
    dni: '30.123.456',
    categoria: 'niño',
    foto: 'https://x.test/f.jpg',
  };

  it('si el formulario es inválido, muestra los errores y no llama a la API', async () => {
    await submit();

    expect(element.querySelectorAll('.error').length).toBe(5);
    http.expectNone('/api/credentials');
  });

  it('si es válido, envía el alta, guarda el resultado y navega a la pantalla de resultado', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fill(VALID);

    await submit();
    http.expectOne('/api/credentials').flush(RESPONSE);

    expect(TestBed.inject(AltaResultStore).result()).toEqual(RESPONSE);
    expect(navigate).toHaveBeenCalledWith('/resultado');
  });

  it('un 400 del backend marca los campos con el mensaje recibido', async () => {
    fill(VALID);

    await submit();
    http
      .expectOne('/api/credentials')
      .flush({ errors: { dni: ['El DNI ya no es válido.'] } }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();

    expect(element.textContent).toContain('El DNI ya no es válido.');
    expect(element.querySelector('[role=alert]')?.textContent).toContain('Revisá los campos');
  });

  it('una falla de firma muestra que no se guardó nada y habilita reintentar', async () => {
    fill(VALID);

    await submit();
    http
      .expectOne('/api/credentials')
      .flush({ code: 'issuer_signing_failed' }, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain('No se guardó nada');
    expect(element.querySelector<HTMLButtonElement>('button[type=submit]')!.disabled).toBe(false);
  });
});
