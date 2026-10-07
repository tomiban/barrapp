import { TRAINING_WEEKDAYS, WEEKDAY_LABELS } from '@/api/athleteProfile';

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
});
