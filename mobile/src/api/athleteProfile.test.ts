import {
  TRAINING_WEEKDAYS,
  WEEKDAY_LABELS,
  todayStartDate,
  validateStartDate,
  validateTrainingWeekdaySelection,
} from '@/api/athleteProfile';

describe('training weekdays (#94)', () => {
  it('offers the seven days of the week, monday to sunday, with their UI label', () => {
    expect(TRAINING_WEEKDAYS).toEqual([
      'monday',
      'tuesday',
      'wednesday',
      'thursday',
      'friday',
      'saturday',
      'sunday',
    ]);
    expect(TRAINING_WEEKDAYS.map((day) => WEEKDAY_LABELS[day])).toEqual([
      'Lunes',
      'Martes',
      'Miércoles',
      'Jueves',
      'Viernes',
      'Sábado',
      'Domingo',
    ]);
  });

  it('accepts a selection of three to five days and returns it monday to sunday', () => {
    expect(validateTrainingWeekdaySelection(['saturday', 'monday', 'thursday'])).toEqual({
      selection: ['monday', 'thursday', 'saturday'],
    });

    expect(
      validateTrainingWeekdaySelection(['friday', 'monday', 'wednesday', 'sunday', 'tuesday'])
        .selection,
    ).toEqual(['monday', 'tuesday', 'wednesday', 'friday', 'sunday']);
  });

  it('rejects fewer than three or more than five days with a clear message', () => {
    expect(validateTrainingWeekdaySelection(['monday', 'wednesday'])).toEqual({
      selection: null,
      error: 'Elige entre 3 y 5 días de entrenamiento.',
    });

    expect(
      validateTrainingWeekdaySelection([
        'monday',
        'tuesday',
        'wednesday',
        'thursday',
        'friday',
        'saturday',
      ]).error,
    ).toBe('Elige entre 3 y 5 días de entrenamiento.');
  });

  it('rejects a repeated day', () => {
    expect(validateTrainingWeekdaySelection(['tuesday', 'tuesday', 'thursday'])).toEqual({
      selection: null,
      error: 'No puedes repetir un día de la semana.',
    });
  });
});

describe('validateStartDate (#94)', () => {
  it('treats an empty value as not chosen yet', () => {
    expect(validateStartDate('')).toEqual({ value: null });
    expect(validateStartDate('   ')).toEqual({ value: null });
  });

  it('accepts a real calendar date in yyyy-mm-dd and trims it', () => {
    expect(validateStartDate('2026-10-20')).toEqual({ value: '2026-10-20' });
    expect(validateStartDate(' 2026-03-02 ')).toEqual({ value: '2026-03-02' });
    expect(validateStartDate('2028-02-29')).toEqual({ value: '2028-02-29' });
  });

  it('rejects a malformed or impossible date with a clear message', () => {
    const message = 'Introduce la fecha de inicio como AAAA-MM-DD.';

    expect(validateStartDate('20-10-2026').error).toBe(message);
    expect(validateStartDate('2026/10/20').error).toBe(message);
    expect(validateStartDate('2026-02-30').error).toBe(message);
    expect(validateStartDate('2027-02-29').error).toBe(message);
    expect(validateStartDate('2026-13-01').error).toBe(message);
  });
});

describe('todayStartDate (#94)', () => {
  it('formats a date as yyyy-mm-dd in local time', () => {
    expect(todayStartDate(new Date(2026, 0, 5))).toBe('2026-01-05');
    expect(todayStartDate(new Date(2026, 11, 31))).toBe('2026-12-31');
  });
});
