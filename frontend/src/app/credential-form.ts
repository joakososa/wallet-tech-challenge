import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AltaResultStore } from './alta-result-store';
import { CredentialsApi } from './credentials-api';
import { CATEGORIAS, Categoria, ProblemDetails } from './models';
import * as rules from './validators';

type FieldName = 'nombre' | 'apellido' | 'dni' | 'categoria' | 'foto';

const MESSAGES: Record<string, string> = {
  required: 'Este campo es obligatorio.',
  maxlength: 'El texto es demasiado largo.',
  dni: 'El DNI debe tener entre 7 y 9 dígitos (se admiten puntos).',
  url: 'Ingresá una URL completa que empiece con http:// o https://.',
};

@Component({
  selector: 'app-credential-form',
  imports: [ReactiveFormsModule],
  templateUrl: './credential-form.html',
})
export class CredentialForm {
  private readonly api = inject(CredentialsApi);
  private readonly router = inject(Router);
  private readonly store = inject(AltaResultStore);

  protected readonly categorias = CATEGORIAS;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    nombre: ['', [rules.notBlank, Validators.maxLength(100)]],
    apellido: ['', [rules.notBlank, Validators.maxLength(100)]],
    dni: ['', [rules.dni]],
    categoria: ['' as Categoria | '', [Validators.required]],
    foto: ['', [rules.httpUrl, Validators.maxLength(2048)]],
  });

  /** El mensaje de error de un campo, solo una vez que el usuario lo tocó o intentó enviar. */
  protected errorOf(name: FieldName): string | null {
    const control = this.form.controls[name];
    if (!control.invalid || !(control.touched || control.dirty)) {
      return null;
    }
    const errors = control.errors ?? {};
    if (typeof errors['server'] === 'string') {
      return errors['server'];
    }
    const key = Object.keys(errors).find((k) => k in MESSAGES);
    return key ? MESSAGES[key] : 'Valor inválido.';
  }

  protected submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const value = this.form.getRawValue();
    this.api
      .create({ ...value, categoria: value.categoria as Categoria })
      .subscribe({
        next: (response) => {
          this.store.result.set(response);
          void this.router.navigateByUrl('/resultado');
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.showError(error);
        },
      });
  }

  private showError(error: HttpErrorResponse): void {
    const problem = (error.error ?? {}) as ProblemDetails;

    if (error.status === 400 && problem.errors) {
      for (const [field, messages] of Object.entries(problem.errors)) {
        const control = this.form.get(field);
        control?.setErrors({ server: messages[0] });
        control?.markAsTouched();
      }
      this.errorMessage.set('Revisá los campos marcados.');
      return;
    }

    switch (problem.code) {
      case 'issuer_signing_failed':
        this.errorMessage.set('No se pudo firmar la credencial. No se guardó nada; intentá de nuevo.');
        break;
      case 'database_unavailable':
        this.errorMessage.set('La base de datos no está disponible. Intentá de nuevo en unos instantes.');
        break;
      case 'duplicate_dni':
        this.errorMessage.set('Otra alta del mismo DNI se procesó al mismo tiempo. Intentá de nuevo.');
        break;
      default:
        this.errorMessage.set(
          error.status === 0
            ? 'No se pudo conectar con el servidor.'
            : 'Ocurrió un error inesperado. Intentá de nuevo.',
        );
    }
  }
}
