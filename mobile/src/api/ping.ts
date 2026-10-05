import { getApiBaseUrl } from '@/api/client';

export type PingResponse = {
  message: string;
  serverTimeUtc: string;
};

/** Consulta `GET /ping` del API. Lanza si la respuesta no es correcta. */
export async function fetchPing(signal?: AbortSignal): Promise<PingResponse> {
  const response = await fetch(`${getApiBaseUrl()}/ping`, { signal });

  if (!response.ok) {
    throw new Error(`El API respondió ${response.status} ${response.statusText}`.trim());
  }

  return (await response.json()) as PingResponse;
}
