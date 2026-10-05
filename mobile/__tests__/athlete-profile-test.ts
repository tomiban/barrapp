import {
  fetchAthleteProfile,
  saveAthleteProfile,
  validateAthleteProfileMeasurements,
} from '../src/api/athleteProfile';

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
      .mockResolvedValue(
        jsonResponse(200, { weightKilograms: 78, heightCentimeters: 181, trainingDays: 4 }),
      );
    global.fetch = fetchMock as unknown as typeof fetch;

    await expect(fetchAthleteProfile()).resolves.toEqual({
      weightKilograms: 78,
      heightCentimeters: 181,
      trainingDays: 4,
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
      .mockResolvedValue(
        jsonResponse(200, { weightKilograms: 80, heightCentimeters: 182, trainingDays: 5 }),
      );
    global.fetch = fetchMock as unknown as typeof fetch;

    await expect(
      saveAthleteProfile({ weightKilograms: 80, heightCentimeters: 182, trainingDays: 5 }),
    ).resolves.toEqual({ weightKilograms: 80, heightCentimeters: 182, trainingDays: 5 });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/profile'),
      expect.objectContaining({
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ weightKilograms: 80, heightCentimeters: 182, trainingDays: 5 }),
      }),
    );
  });

  it('throws when the API responds with an error', async () => {
    global.fetch = jest.fn().mockResolvedValue(jsonResponse(500, {})) as unknown as typeof fetch;

    await expect(fetchAthleteProfile()).rejects.toThrow('El API respondió 500');
  });

  it('surfaces the API problem detail so the UI can show it', async () => {
    const detail = 'El peso debe estar entre 30 y 200 kg.';
    global.fetch = jest
      .fn()
      .mockResolvedValue(jsonResponse(400, { detail })) as unknown as typeof fetch;

    await expect(
      saveAthleteProfile({ weightKilograms: 29.9, heightCentimeters: 180, trainingDays: 4 }),
    ).rejects.toThrow(detail);
  });
});

describe('validateAthleteProfileMeasurements', () => {
  it('accepts the range boundaries', () => {
    expect(validateAthleteProfileMeasurements('30', '120', 3)).toEqual({
      weightKilograms: 30,
      heightCentimeters: 120,
      trainingDays: 3,
      errors: {},
    });

    expect(validateAthleteProfileMeasurements('200,0', '220', 5)).toEqual({
      weightKilograms: 200,
      heightCentimeters: 220,
      trainingDays: 5,
      errors: {},
    });
  });

  it('rejects values outside the range with a clear message per field', () => {
    const result = validateAthleteProfileMeasurements('29.9', '220.1', 4);

    expect(result.weightKilograms).toBeNull();
    expect(result.heightCentimeters).toBeNull();
    expect(result.trainingDays).toBe(4);
    expect(result.errors.weight).toBe('El peso debe estar entre 30 y 200 kg.');
    expect(result.errors.height).toBe('La altura debe estar entre 120 y 220 cm.');
  });

  it('asks for a missing or non-numeric value', () => {
    const result = validateAthleteProfileMeasurements('', 'abc', 4);

    expect(result.weightKilograms).toBeNull();
    expect(result.heightCentimeters).toBeNull();
    expect(result.errors.weight).toBe('Introduce el peso en kg.');
    expect(result.errors.height).toBe('Introduce la altura en cm.');
  });

  it('rejects trailing junk instead of coercing it', () => {
    const result = validateAthleteProfileMeasurements('30kg', '120cm', 4);

    expect(result.weightKilograms).toBeNull();
    expect(result.heightCentimeters).toBeNull();
    expect(result.errors.weight).toBe('Introduce el peso en kg.');
    expect(result.errors.height).toBe('Introduce la altura en cm.');
  });

  it('rejects training days outside the 3–5 range with a clear message', () => {
    const below = validateAthleteProfileMeasurements('30', '120', 2);
    expect(below.trainingDays).toBeNull();
    expect(below.errors.trainingDays).toBe('Los días de entrenamiento deben estar entre 3 y 5.');

    const above = validateAthleteProfileMeasurements('30', '120', 6);
    expect(above.trainingDays).toBeNull();
    expect(above.errors.trainingDays).toBe('Los días de entrenamiento deben estar entre 3 y 5.');
  });
});
