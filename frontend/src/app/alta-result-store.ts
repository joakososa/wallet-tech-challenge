import { Injectable, signal } from '@angular/core';
import { AltaResponse } from './models';

/** Guarda el resultado del último alta para mostrarlo en la pantalla de resultado. Al recargar se pierde. */
@Injectable({ providedIn: 'root' })
export class AltaResultStore {
  readonly result = signal<AltaResponse | null>(null);
}
