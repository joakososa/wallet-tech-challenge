import { DatePipe, JsonPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CredentialsApi } from './credentials-api';
import { CredentialItem } from './models';

const PLACEHOLDER_PHOTO =
  'data:image/svg+xml;utf8,' +
  encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64"><rect width="64" height="64" fill="#d9dee7"/><circle cx="32" cy="24" r="11" fill="#9aa5b8"/><rect x="12" y="40" width="40" height="18" rx="9" fill="#9aa5b8"/></svg>',
  );

/** UC02: listado de credenciales, con el estado vacío y el detalle expandible con los campos técnicos. */
@Component({
  selector: 'app-credentials-list',
  imports: [DatePipe, JsonPipe, ReactiveFormsModule, RouterLink],
  templateUrl: './credentials-list.html',
})
export class CredentialsList {
  private readonly api = inject(CredentialsApi);

  protected readonly items = signal<CredentialItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly filtered = signal(false);

  protected readonly filters = inject(NonNullableFormBuilder).group({
    dni: [''],
    numeroSocio: [''],
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    const { dni, numeroSocio } = this.filters.getRawValue();
    this.filtered.set(!!(dni.trim() || numeroSocio.trim()));
    this.loading.set(true);
    this.failed.set(false);

    this.api.list({ dni, numeroSocio }).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  protected clearFilters(): void {
    this.filters.reset();
    this.load();
  }

  /** "Vencida" no es un estado de la API: se deriva de la fecha de vencimiento. */
  protected statusLabel(item: CredentialItem): string {
    if (item.status === 1) {
      return 'Revocada';
    }
    if (item.status === 2) {
      return 'Suspendida';
    }
    return Date.parse(item.validUntil) < Date.now() ? 'Vencida' : 'Activa';
  }

  protected onPhotoError(event: Event): void {
    const image = event.target as HTMLImageElement;
    if (image.src !== PLACEHOLDER_PHOTO) {
      image.src = PLACEHOLDER_PHOTO;
    }
  }
}
