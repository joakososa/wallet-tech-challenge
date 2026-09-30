import { FormControl } from '@angular/forms';
import { dni, httpUrl, notBlank } from './validators';

const control = (value: string) => new FormControl(value, { nonNullable: true });

describe('validators', () => {
  describe('notBlank', () => {
    it('acepta un texto con contenido', () => {
      expect(notBlank(control('Juan'))).toBeNull();
    });

    it.each(['', '   '])('rechaza el texto vacío o de solo espacios (%j)', (value) => {
      expect(notBlank(control(value))).toEqual({ required: true });
    });
  });

  describe('dni', () => {
    it.each(['30123456', '30.123.456', ' 30 123 456 ', '1234567', '123456789'])(
      'acepta un DNI válido (%j)',
      (value) => {
        expect(dni(control(value))).toBeNull();
      },
    );

    it.each(['', '123456', '1234567890', '3012345a', '-30123456'])(
      'rechaza un DNI inválido (%j)',
      (value) => {
        expect(dni(control(value))).toEqual({ dni: true });
      },
    );
  });

  describe('httpUrl', () => {
    it.each(['https://cdn.futbol.com.ar/a.jpg', 'http://localhost:8080/foto.png'])(
      'acepta una URL http o https (%j)',
      (value) => {
        expect(httpUrl(control(value))).toBeNull();
      },
    );

    it.each(['', 'foto.jpg', 'ftp://x.test/a.jpg', 'javascript:alert(1)'])(
      'rechaza lo que no es una URL http o https (%j)',
      (value) => {
        expect(httpUrl(control(value))).toEqual({ url: true });
      },
    );
  });
});
