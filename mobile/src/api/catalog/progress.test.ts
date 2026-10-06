import { advanceSkillStage, fetchSkillProgress } from './progress';

/**
 * El cliente de avance habla con `POST /catalog/progress/{skillId}/advance` y la lectura de
 * progreso con `GET /catalog/progress`. Se mockea `fetch` en la frontera del sistema; la URL base
 * se fija con `EXPO_PUBLIC_API_URL` para no depender del host del dev server.
 */

const BASE = 'http://api.test';

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? 'OK' : 'Bad Request',
    json: async () => body,
  } as Response;
}

const originalEnv = process.env.EXPO_PUBLIC_API_URL;

beforeEach(() => {
  process.env.EXPO_PUBLIC_API_URL = BASE;
  jest.restoreAllMocks();
});

afterAll(() => {
  if (originalEnv === undefined) {
    delete process.env.EXPO_PUBLIC_API_URL;
  } else {
    process.env.EXPO_PUBLIC_API_URL = originalEnv;
  }
});

describe('advanceSkillStage', () => {
  it('envía un POST al avance del skill y devuelve la etapa nueva al avanzar', async () => {
    const response = {
      skillId: 'handstand',
      stageOrder: 2,
      advanced: true,
    };
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(response));

    const result = await advanceSkillStage('handstand');

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe(`${BASE}/catalog/progress/handstand/advance`);
    expect(init.method).toBe('POST');
    expect(result).toEqual({ skillId: 'handstand', stageOrder: 2, advanced: true });
  });

  it('devuelve la etapa actual sin avanzar cuando el criterio no se cumple', async () => {
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse({ skillId: 'handstand', stageOrder: 1, advanced: false }));

    const result = await advanceSkillStage('handstand');

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(result.advanced).toBe(false);
    expect(result.stageOrder).toBe(1);
  });

  it('propaga el mensaje del API cuando el skill no existe', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ detail: 'El skill indicado no existe en el catálogo.' }, 400),
      );

    await expect(advanceSkillStage('ghost')).rejects.toThrow(
      'El skill indicado no existe en el catálogo.',
    );
  });
});

describe('fetchSkillProgress', () => {
  it('hace un GET a /catalog/progress y devuelve la lista filtrada', async () => {
    const entries = [{ skillId: 'handstand', stageOrder: 2 }];
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(entries));

    const result = await fetchSkillProgress();

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).toBe(`${BASE}/catalog/progress`);
    expect(result).toEqual(entries);
  });

  it('descarta entradas con forma inválida', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse([{ skillId: 'handstand', stageOrder: 2 }, { stageOrder: 1 }]),
      );

    const result = await fetchSkillProgress();

    expect(result).toEqual([{ skillId: 'handstand', stageOrder: 2 }]);
  });
});
