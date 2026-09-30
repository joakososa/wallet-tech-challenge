import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AltaResultStore } from './alta-result-store';

/** Pantalla posterior al alta: número de socio, vigencia y si el socio era nuevo o ya existía (ADR 006 y 009). */
@Component({
  selector: 'app-alta-result',
  imports: [DatePipe, RouterLink],
  templateUrl: './alta-result.html',
})
export class AltaResult {
  protected readonly result = inject(AltaResultStore).result;

  constructor() {
    // Si se recargó la página no queda nada para mostrar: se vuelve al listado.
    if (!this.result()) {
      void inject(Router).navigateByUrl('/');
    }
  }
}
