export const CATEGORIAS = ['adulto', 'juvenil', 'niño'] as const;
export type Categoria = (typeof CATEGORIAS)[number];

/** El documento completo de la VC, tal como lo emitió el Issuer (incluye `proof`). */
export type VerifiableCredential = Record<string, unknown>;

export interface AltaRequest {
  nombre: string;
  apellido: string;
  dni: string;
  categoria: Categoria;
  foto: string;
}

/** Respuesta del alta (ADR 016). */
export interface AltaResponse {
  numeroSocio: string;
  isNewSocio: boolean;
  validFrom: string;
  validUntil: string;
  credential: VerifiableCredential;
}

/** 0 = activa, 1 = revocada, 2 = suspendida (enunciado 4.1.2). */
export type CredentialStatus = 0 | 1 | 2;

/** Una fila del listado (ADR 016). */
export interface CredentialItem {
  id: string;
  nombre: string;
  apellido: string;
  dni: string;
  numeroSocio: string;
  categoria: Categoria;
  foto: string;
  validFrom: string;
  validUntil: string;
  status: CredentialStatus;
  credential: VerifiableCredential;
}

export interface CredentialFilters {
  dni?: string;
  numeroSocio?: string;
}

/** Error de la API (ProblemDetails, ADR 007). */
export interface ProblemDetails {
  title?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
}
