import { DEFAULT_TRAINING_WEEKDAYS, TRAINING_WEEKDAYS, WEEKDAY_LABELS } from '@/api/athleteProfile';

describe('días de entrenamiento (#94)', () => {
  it('ofrece los siete días de la semana, de lunes a domingo, con su nombre para la UI', () => {
    expect(TRAINING_WEEKDAYS).toHaveLength(7);
    expect(TRAINING_WEEKDAYS[0]).toBe('monday');
    expect(TRAINING_WEEKDAYS[6]).toBe('sunday');
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

  it('tiene días por defecto para cada frecuencia admitida, y son tantos como la frecuencia', () => {
    for (const trainingDays of [3, 4, 5]) {
      const weekdays = DEFAULT_TRAINING_WEEKDAYS[trainingDays];

      expect(weekdays).toBeDefined();
      expect(weekdays).toHaveLength(trainingDays);
      expect(new Set(weekdays).size).toBe(trainingDays);
      expect(weekdays.every((day) => TRAINING_WEEKDAYS.includes(day))).toBe(true);
    }
  });
});