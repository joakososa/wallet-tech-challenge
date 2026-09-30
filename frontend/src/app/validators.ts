import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Obligatorio, ignorando los espacios sobrantes. */
export function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim().length === 0 ? { required: true } : null;
}

/** Misma regla que el backend (ADR 006): sin puntos ni espacios, solo dígitos, de 7 a 9. */
export function dni(control: AbstractControl<string>): ValidationErrors | null {
  const normalized = control.value.replace(/[.\s]/g, '');
  return /^\d{7,9}$/.test(normalized) ? null : { dni: true };
}

/** URL absoluta http o https (ADR 007). */
export function httpUrl(control: AbstractControl<string>): ValidationErrors | null {
  try {
    const { protocol } = new URL(control.value.trim());
    return protocol === 'http:' || protocol === 'https:' ? null : { url: true };
  } catch {
    return { url: true };
  }
}
