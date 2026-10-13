import { getApiBaseUrl } from '../src/api/client';

jest.mock('expo-constants', () => ({
  get expoConfig() {
    return mockExpoConfig();
  },
}));

const mockExpoConfig = jest.fn();

describe('getApiBaseUrl', () => {
  const originalApiUrl = process.env.EXPO_PUBLIC_API_URL;

  beforeEach(() => {
    mockExpoConfig.mockReset();
  });

  afterEach(() => {
    if (originalApiUrl === undefined) {
      delete process.env.EXPO_PUBLIC_API_URL;
    } else {
      process.env.EXPO_PUBLIC_API_URL = originalApiUrl;
    }
  });

  it('uses EXPO_PUBLIC_API_URL when defined, trimming trailing slashes', () => {
    process.env.EXPO_PUBLIC_API_URL = 'http://192.168.0.249:5213///';

    expect(getApiBaseUrl()).toBe('http://192.168.0.249:5213');
  });

  it('falls back to the dev server host and the API port when the env var is not set', () => {
    delete process.env.EXPO_PUBLIC_API_URL;
    mockExpoConfig.mockReturnValue({ hostUri: '192.168.0.249:8081' });

    expect(getApiBaseUrl()).toBe('http://192.168.0.249:5213');
  });

  it('falls back to localhost when there is neither env var nor dev server host', () => {
    delete process.env.EXPO_PUBLIC_API_URL;
    mockExpoConfig.mockReturnValue(undefined);

    expect(getApiBaseUrl()).toBe('http://localhost:5213');
  });
});
