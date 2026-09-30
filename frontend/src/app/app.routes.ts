import { Routes } from '@angular/router';
import { AltaResult } from './alta-result';
import { CredentialForm } from './credential-form';
import { CredentialsList } from './credentials-list';

export const routes: Routes = [
  { path: '', component: CredentialsList, title: 'Credenciales' },
  { path: 'alta', component: CredentialForm, title: 'Nueva credencial' },
  { path: 'resultado', component: AltaResult, title: 'Alta realizada' },
  { path: '**', redirectTo: '' },
];
