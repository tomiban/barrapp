import { fetchAthleteProfile, saveAthleteProfile } from '../src/api/athleteProfile';

/** Respuesta mínima con la forma que consume el módulo. */
function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    json: async () => body,
  } as unknown as Response;
}

describe('athleteProfile api', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('returns the saved profile from GET /profile', async () => {
    const fetchMock = jest
      .fn()
      .mockResolvedValue(jsonResponse(200, { weightKilograms: 78, heightCentimeters: 181 }));
    global.fetch = fetchMock as unknown as typeof fetch;

    await expect(fetchAthleteProfile()).resolves.toEqual({
      weightKilograms: 78,
      heightCentimeters: 181,
    });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/profile'),
      expect.objectContaining({ signal: undefined }),
    );
  });

  it('returns null when the profile has not been created yet (404)', async () => {
    global.fetch = jest.fn().mockResolvedValue(jsonResponse(404, {})) as unknown as typeof fetch;

    await expect(fetchAthleteProfile()).resolves.toBeNull();
  });

  it('sends a PUT with the profile body when saving', async () => {
    const fetchMock = jest
      .fn()
      .mockResolvedValue(jsonResponse(200, { weightKilograms: 80, heightCentimeters: 182 }));
    global.fetch = fetchMock as unknown as typeof fetch;

    await expect(
      saveAthleteProfile({ weightKilograms: 80, heightCentimeters: 182 }),
    ).resolves.toEqual({ weightKilograms: 80, heightCentimeters: 182 });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/profile'),
      expect.objectContaining({
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ weightKilograms: 80, heightCentimeters: 182 }),
      }),
    );
  });

  it('throws when the API responds with an error', async () => {
    global.fetch = jest.fn().mockResolvedValue(jsonResponse(500, {})) as unknown as typeof fetch;

    await expect(fetchAthleteProfile()).rejects.toThrow('El API respondió 500');
  });
});
