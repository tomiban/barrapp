import * as SQLite from 'expo-sqlite';

import type { Plan } from '@/api/plan';

/** Superficie de base de datos que usa la caché del plan; estructural para permitir un fake en tests. */
export interface PlanCacheDatabase {
  runAsync(source: string, ...params: unknown[]): Promise<unknown>;
  getFirstAsync<T>(source: string, ...params: unknown[]): Promise<T | null>;
}

/** Nombre de la base de datos local de la app en el dispositivo. */
const DATABASE_NAME = 'barrapp.db';

/** La caché vive en una única fila fija. */
const CACHE_ROW_ID = 1;

const SCHEMA_SQL = `
  PRAGMA journal_mode = WAL;
  CREATE TABLE IF NOT EXISTS plan_cache (
    id INTEGER PRIMARY KEY NOT NULL,
    plan TEXT NOT NULL,
    saved_at TEXT NOT NULL
  );
`;

/** Almacén local del plan: guarda la caché y la devuelve para leerla sin conexión. */
export type PlanStore = {
  savePlan(plan: Plan): Promise<void>;
  loadPlan(): Promise<Plan | null>;
};

/** Crea el almacén sobre una base de datos ya abierta (y con el esquema garantizado). */
export function createPlanStore(db: PlanCacheDatabase): PlanStore {
  return {
    async savePlan(plan: Plan): Promise<void> {
      await db.runAsync(
        `INSERT INTO plan_cache (id, plan, saved_at) VALUES (?, ?, ?)
         ON CONFLICT(id) DO UPDATE SET plan = excluded.plan, saved_at = excluded.saved_at;`,
        CACHE_ROW_ID,
        JSON.stringify(plan),
        new Date().toISOString(),
      );
    },

    async loadPlan(): Promise<Plan | null> {
      const row = await db.getFirstAsync<{ plan: string }>(
        'SELECT plan FROM plan_cache WHERE id = ?;',
        CACHE_ROW_ID,
      );
      return row ? (JSON.parse(row.plan) as Plan) : null;
    },
  };
}

/** Abre (y crea si hace falta) el almacén local del plan en el dispositivo. */
export async function openPlanStore(): Promise<PlanStore> {
  const db = await SQLite.openDatabaseAsync(DATABASE_NAME);
  await db.execAsync(SCHEMA_SQL);
  return createPlanStore(db);
}
