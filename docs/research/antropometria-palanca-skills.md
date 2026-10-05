# Antropometría y palanca en calistenia — verificación de fuentes

Investigación para decidir si/cómo el peso corporal (masa) y la altura deben influir en el
plan generado por el motor determinista, distinguiendo habilidades *levered* (planche, front
lever) de ejercicios *non-levered* (push-up, pull-up, squat).

Fecha de la investigación: 2026-10-05.

---

## Resumen ejecutivo

- **Hipótesis 1 (non-levered = fuerza/peso, altura irrelevante):** **CONFIRMADA a nivel del
  ejercicio**, con una salvedad. La fuerza requerida por un push-up/pull-up/squat es un
  porcentaje de la masa corporal, independiente de la altura (evidencia empírica directa:
  Ebben 2011). La salvedad: la *ratio fuerza/peso* de un individuo **sí** depende del tamaño
  corporal (escalado alométrico: la fuerza escala con la sección transversal ∝L², la masa ∝L³),
  así que un atleta más alto tiene sistemáticamente peor fuerza relativa — pero eso es un
  efecto muscular, no de mecánica de palanca del ejercicio.
- **Hipótesis 2 (levered = también escala con la longitud):** **CONFIRMADA cualitativamente,
  con evidencia primaria fina.** El torque de planche/front lever es τ = m·g·d (masa × brazo de
  palanca); la ecuación segmentada está publicada (Wang & Shan 2023). El número concreto
  "palanca efectiva ≈ 22,5% de la altura" es **de un blog de practicante, no revisado por pares**.
- **El campo está casi sin estudiar.** El propio paper de 2023 (MDPI *Bioengineering*) documenta
  que una búsqueda "Planche + biomechanics" en Web of Science (marzo 2023) devolvió **cero
  registros**; el front lever no tiene paper biomecánico dedicado en PubMed/Crossref.
- **Caveat de alcance/ética:** el paper MDPI usa **sexo y raza como proxies antropométricos**
  (proporciones segmentales). Nuestra app solo debe usar medidas físicas (altura, peso, y
  opcionalmente envergadura/entrepierna), nunca categorías demográficas. Ver §6.

---

## (a) Torque de planche/front lever escala como masa × longitud de palanca

**Veredicto: la ecuación de torque está verificada (revisada por pares); el número "22,5%"
es de practicante.**

La fuente primaria que posee la ecuación es:

- **Wang, X., & Shan, G. (2023).** "Insights from a Nine-Segment Biomechanical Model and Its
  Simulation for Anthropometrical Influence on Individualized Planche Learning and Training in
  Gymnastics." *Bioengineering (Basel)*, 10(7):761.
  - DOI: [10.3390/bioengineering10070761](https://doi.org/10.3390/bioengineering10070761)
  - PMCID: [PMC10376746](https://pmc.ncbi.nlm.nih.gov/articles/PMC10376746/), PMID 37508787
  - **Peer-reviewed** (revista indexada; modelo validado mecánicamente, pero *simulación*, no
    medición real).
  - Ecuaciones de torque segmental (Eq. 1–8), reproducidas del original:
    - T1 = (x − L1 − D1·cosβ)·m1·g
    - T2 = (x + D2·cosβ)·m2·g
    - T3 = (x + L2 + D3·cosβ)·m3·g
    - T4 = (x + L2 + L3 + L4 − D4·cosβ)·m4·g
    - T5 = (x + L2 + L3 + L4 + L5 − D5·cosβ)·m5·g
    - T6 = (x + L2 + L3 + L4 + L5 + L6 − D6·cosβ)·m6·g
    - T7 = x·m7·g·(D7 + L8)/(L7 + L8)
    - T8 = x·m8·g·D8/(L7 + L8)
  - donde Ti = torque segmental, x = coordenada horizontal del hombro, mi = masa segmental,
    Li = longitud segmental, Di = posición del centro de gravedad (COG) segmental, g = gravedad.
  - Equilibrio: Σ Ti = 0 (Eq. 9). Cada término es literalmente **masa × gravedad × distancia
    (brazo de palanca)**: τ = m·g·d. Confirma que el torque requerido escala con masa **y** con
    la distancia horizontal del segmento al hombro (longitud de palanca), no solo con masa.

**Sobre el "brazo de palanca efectivo ≈ 22,5% de la altura":**

- El número **22,5% de la altura como distancia COG–hombro** proviene de un **blog de practicante**,
  no de una fuente revisada por pares:
  - **Urbanski, M. (2024).** "A Scientific Approach to Mastering the Planche: Quantifying Progress
    and Setting Clear Goals." *risewithmarcus.com* (29 nov 2024).
    - URL: <https://risewithmarcus.com/blog/a-scientific-approach-to-mastering-the-planche--quantifying-progress-and-setting-clear-goals>
    - **Practicante/anécdota** (automedición de una sola persona: 102 kg, 1,87 m).
    - Cita literal: "recalculated based on CG-to-shoulder distance—about 42 cm or 22.5% of height."
    - Fórmula que usa: `Torque per arm = Effective Lever Length (m) × Weight (kg) × g × cos(angle)`.
  - **No pude verificar** ningún valor revisado por pares que afirme "el brazo efectivo es un X %
    de la altura" para planche/front lever. El modelo MDPI no usa un único porcentaje: calcula el
    COG segmento a segmento a partir de datos de regresión antropométrica.

**Fuente de los datos antropométricos (masas/longitudes/COG segmentales) que alimentan el modelo:**

- **Shan, G., & Bohn, C. (2003).** "Anthropometrical data and coefficients of regression related
  to gender and race." *Applied Ergonomics*, 34(4):327–337.
  - DOI: [10.1016/S0003-6870(03)00040-1](https://doi.org/10.1016/S0003-6870(03)00040-1)
  - **Peer-reviewed.** Base de las regresiones que convierten masa/altura/sexo/raza en masas y
    longitudes segmentales y en la posición del COG (usada por el modelo de 9 segmentos).

**Tratamiento practicante temprano (profesional, no académico):**

- **Katrichis, N. E., & Moca, A. (1992).** "Sports performance series: The planche."
  *Strength & Conditioning Journal (NSCA)*, 14(6):6–9.
  - DOI: [10.1519/0744-0049(1992)014<0006:TP>2.3.CO;2](https://doi.org/10.1519/0744-0049\(1992\)014%3C0006:TP%3E2.3.CO;2)
  - **Practitioner/professional** (revista de entrenadores de la NSCA, no peer-review académico).

---

## (b) Ejercicios non-levered: dificultad independiente de la altura

**Veredicto: confirmado empíricamente para el push-up; la mecánica (F = m·g) es independiente de
la altura; salvedad alométrica sobre la ratio fuerza/peso.**

- **Ebben, W. P., Wurm, B., VanderZanden, T. L., Spadavecchia, M. L., Durocher, J. J.,
  Bickham, C. T., & Petushek, E. J. (2011).** "Kinetic analysis of several variations of
  push-ups." *Journal of Strength and Conditioning Research*, 25(10):2891–2894.
  - DOI: [10.1519/JSC.0b013e31820c8587](https://doi.org/10.1519/JSC.0b013e31820c8587), PMID 21873902
  - **Peer-reviewed.**
  - Hallazgo clave (cita literal del abstract): *"subject height was not related to the GRF for
    any of the push-up conditions (p > 0.05) other than the condition where hands were elevated on
    a 60.96-cm box (p ≤ 0.05; r = 0.63)."*
  - Es decir: la carga del push-up (medida como coeficiente de la masa corporal) **no correlaciona
    con la altura** en 5 de 6 variantes. Esto es la verificación empírica más directa de la
    hipótesis "el peso ya está dentro de la carga y la altura es irrelevante".

- **Mecánica general (libro de texto):** para un ejercicio vertical de peso corporal (pull-up,
  squat) la fuerza a vencer es F = m·g, que no depende de la altura; y en la ecuación de torque
  segmental de Wang & Shan (2023), cuando el cuerpo está vertical el brazo horizontal d ≈ 0, luego
  el torque gravitatorio tiende a 0. Fuente de libro de texto (mecánica de palancas del cuerpo):
  - *Body Physics: Motion to Metabolism* (Open Oregon, adaptado de OpenStax *College Physics*),
    cap. "Body Levers" — torque = fuerza × brazo de palanca, equilibrio estático.
    - URL: <https://openoregon.pressbooks.pub/bodyphysics/chapter/body-levers>
    - **Libro de texto educativo** (no investigación revisada por pares), útil para la definición
      formal τ = F·d.

**Salvedad importante (alométrica):** la afirmación "la dificultad es solo fuerza/peso" es cierta
*para la carga del ejercicio*, pero la **ratio fuerza/peso de la persona** depende del tamaño
corporal. La fuerza muscular escala con la sección transversal fisiológica (∝ L²) mientras la masa
escala con L³, de modo que la fuerza relativa (fuerza/masa) decrece con la altura/el tamaño. Por
tanto, un atleta más alto tendrá un rep-max relativo a su peso sistemáticamente menor — un efecto
fisiológico, distinto de la mecánica de palanca del ejercicio.

- **Zoeller, R. F., et al. (2008).** "Allometric scaling of isometric biceps strength in adult
  females and the effect of body mass index." *European Journal of Applied Physiology*, 104(3):455–461.
  - DOI: [10.1007/s00421-008-0819-2](https://doi.org/10.1007/s00421-008-0819-2), PMID 18648848
  - **Peer-reviewed.** Documenta escalado alométrico de la fuerza (exponentes ≠ 1 respecto a masa/
    CSA), es decir, la fuerza no escala 1:1 con la masa corporal.

**No verificado:** no encontré un valor publicado revisado por pares que fije "el pull-up requiere
exactamente el 100% del peso y el squat el X% del peso corporal" como constantes universales; son
identidades mecánicas (F = m·g) más que hallazgos empíricos. El número concreto de % de masa en
push-up sí está medido por Ebben 2011 (lo reporta como coeficiente de masa corporal), pero el
abstract no da el % exacto; **no lo invento**.

---

## (c) Paper MDPI 2023 — qué concluye realmente

Fuente: **Wang & Shan (2023)**, *Bioengineering* 10(7):761 (ver §a para DOI/PMCID). **Peer-reviewed.**

Conclusiones reales del paper (no la lectura que suele circular):

1. **El tipo corporal importa para la habilidad innata de hacer planche**, y la afectan masa,
   altura, sexo y raza (usados como proxies de proporciones segmentales). Concluye que se
   necesita un plan personalizado por tipo corporal, no "one-size-fits-all".
2. **Ratio tronco/pierna y COG:** "para una altura dada, individuos con piernas relativamente más
   largas y tronco más corto (características de los europeos frente a los asiáticos) estarían más
   aptos para el planche"; es decir, **un COG más alto es ventajoso**. Datos (Tabla 1 del paper):
   - Longitud de tronco (% altura): hombre asiático 39,53 vs europeo 37,92.
   - Longitud de piernas (% altura): hombre asiático 48,15 vs europeo 49,83.
   - Conclusión: europeos (piernas largas + tronco corto) aventajados frente a asiáticos.
3. **Tabla de ventaja/desventaja por grupo (Tabla 3):** por ejemplo, hombre asiático "advantaged:
   alto y pesado/peso normal", hombre europeo "advantaged: bajo/normal y pesado", etc.
4. **CRÍTICO — limitación reconocida por los propios autores** (cita literal del Discussion):
   *"Mechanically, it is well known that tall and/or heavy individuals need more muscle strength to
   perform the Planche… Therefore, the purely anthropometric results obtained from the current study,
   such as the advantageous body type for Asian males being tall and heavy/normal weight, may not be
   realistic. A logical hypothesis is that the balancing ability of tall and heavy individuals is
   limited by their muscle strength."*
   - Es decir: el modelo es **solo de equilibrio/antropometría, ignora la fuerza muscular**, y los
     propios autores advierten que sus resultados "alto+pesado = ventaja" probablemente **no son
     realistas** porque la fuerza es el factor limitante. Esto es central para nuestra decisión:
     el paper no debe usarse para decir "más alto = mejor para planche"; al contrario, su propia
     discusión respalda que altura/masa **aumentan** el requerimiento de fuerza.

**Caveat de alcance/ética (ver §6):** el paper usa sexo y raza como proxies. No replicar.

---

## (d) Handstand (habilidad/equilibrio) vs pistol squat (fuerza de pierna)

**Handstand — dominado por equilibrio/habilidad, no por torque (por tanto ~independiente de la
altura en fuerza).**

- **MacDonald, M., Baker, J. S., Gu, Y., & Ugbolue, U. C. (2025).** "Biomechanical analyses of the
  handstand: a systematic review." *Frontiers in Sports and Active Living*.
  - DOI: [10.3389/fspor.2025.1694648](https://doi.org/10.3389/fspor.2025.1694648), PMID 41473027
  - **Peer-reviewed** (revisión sistemática de 21 estudios).
  - El handstand se estudia dominantemente como **control de equilibrio**: 31% de los estudios
    analizan estrategias de balance; el hallazgo principal es la "estrategia de muñeca" (wrist
    strategy) y estrategias de control mixtas (muñeca/hombro/cadera/codo). Los propios autores
    anotan "Gymnasts with greater strength possess better balance control", pero el fenómeno
    caracterizado es **equilibrio/habilidad**, no fuerza de palanca.
- **Argumento mecánico (libro de texto):** en el handstand el cuerpo está vertical, el COG cae
  directamente sobre la base de apoyo (las manos), el brazo de palanca horizontal ≈ 0, por lo que
  no hay un gran torque de hombro como en planche/front lever (ver §a, τ = m·g·d con d ≈ 0). La
  dificultad es mantener el COG sobre una base pequeña (equilibrio), no generar torque. La altura
  no cambia la fuerza requerida (solo la inercia/dinámica del balanceo).
- Otras fuentes revisadas por pares que tratan el handstand como control postural/equilibrio
  (coinciden con la caracterización): Rohleder & Vogt 2019 (*Science of Gymnastics Journal*,
  "wrist strategy coaching… skill-related motor tasks"); Kochanowicz et al. 2015 (*Baltic J Health
  Phys Act*, "level of body balance in a handstand").

**Pistol squat — fuerza de pierna (unilateral), categorización de coaching.**

- **No pude verificar** ninguna fuente revisada por pares que mida específicamente la carga del
  "pistol squat": PubMed devolvió **0 resultados** para "pistol squat … kinetic/load". La
  biomecánica existente es sobre "single-leg squat" genérico (mayormente clínico/rehabilitación,
  p. ej. control de rodilla en osteoartritis).
- La caracterización "el pistol squat es fuerza de pierna unilateral (carga ≈ el peso corporal
  sobre una sola pierna)" proviene de **fuentes de coaching**, p. ej. *Overcoming Gravity* (Steven
  Low) y los programas de Antranik Kizirian, que lo clasifican como progresión de fuerza de
  piernas (no como habilidad de palanca). **Etiqueta: practicante/coach, no revisado por pares.**
- Implicación mecánica coherente con (b): el pistol squat es un ejercicio de cadena vertical
  (F = m·g sobre una pierna), luego la altura no entra en la fuerza requerida (salvo el mayor
  recorrido/trabajo y el mayor momento de inercia de piernas largas).

---

## (e) Normas de coaching: cuánto más lento progresan los altos/pesados en palancas

**Fuente principal disponible (coach): Antranik Kizirian.** *No pude acceder al libro* Overcoming
Gravity *de Steven Low en sí; su artículo público sobre palancas trata de programación de mesetas,
no cuantifica la altura/peso.*

- **Kizirian, A.** "Don't be fooled: Why the best performers aren't automatically the best
  teachers." *antranik.org*.
  - URL: <https://antranik.org/dont-be-fooled/>
  - **Practicante/coach** (el autor lo etiqueta explícitamente: *"these are my sincere
    observations"*).
  - Normas concretas que reporta (observación personal sobre cientos de personas):
    - **"5'9" (175 cm) seems to be the tipping point"** — por encima, todo se vuelve
      significativamente más difícil y el progreso se vuelve "exponentially slower".
    - **"someone who is 5'3" & 120 lbs might take 6 months to achieve a front lever while someone
      6'3" & 190 lbs might take 3 years."**
    - **"Just a few pounds makes a noticeable difference"**: fluctuar 175→182 lbs (~4 lbs/1,8 kg)
      le causa "a massive hit" en el front lever.
    - Los altos/pesados **"will need to figure out the intermediary progressions to bridge the gap,
      use solid programming and have extra mental fortitude"** — es decir, más progresiones
      intermedias y programación más granular.
    - El front lever "requires an excellent strength to weight ratio"; quienes lo logran suelen ser
      bajos y magros (<170 cm).

- **Low, S.** "How to program for advanced isometric movements after a plateau."
  *stevenlow.org* (8 ago 2018).
  - URL: <https://stevenlow.org/how-to-program-for-advanced-isometric-movements-after-a-plateau/>
  - **Practicante/coach** (autor de *Overcoming Gravity*; DPT, ex-gimnasta).
  - Lo que sí aporta (aplicable a altos/pesados, aunque sin números de altura/peso): descomponer
    planche/front lever en **primary + secondary + supplemental exercises**, atacar debilidad
    (fuerza primaria, escápula, control postural, bloqueo de codo), y trabajar múltiples rangos de
    repeticiones (3–5 / 5–15 / 20–50) para fuerza + hipertrofia. Su ecuación declarada:
    **Strength = neurological adaptations × cross-sectional area of muscle** (la hipertrofia
    compensa con creces el peso extra ganado).
  - **No pude verificar** una cifra específica de Low sobre "cuánto más lento progresa un alto"
    porque el libro no está abierto y el artículo citado no cuantifica altura/peso.

**Nota sobre el libro:** *Overcoming Gravity* (Steven Low) es la referencia de coaching más citada
para este tema, pero su contenido sobre proporciones/palanca no está libremente accesible online;
**no lo cito de memoria** (evito inventar páginas/cifras). Si se necesita, se puede adquirir la 2ª
edición para extraer citas exactas.

---

## 6. Caveat de alcance y ética (obligatorio)

- El paper MDPI de 2023 usa **sexo y raza como proxies** de proporciones segmentales (via Shan &
  Bohn 2003), y su abstract/conclusiones contienen afirmaciones tipo "European body types are
  naturally more advanced than Asian body types" que son **reduccionistas y éticamente
  problemáticas** si se trasladan a individuos.
- **Nuestra app no debe usar jamás categorías demográficas (sexo/raza/etnia).** Solo debe usar
  **medidas físicas directas**: altura, peso corporal, y opcionalmente envergadura (arm span) y/o
  entrepierna (inseam) para aproximar proporciones segmentales (ratio tronco/pierna, longitud de
  brazo). La física relevante (masa y longitudes de segmento) se captura con estas medidas; las
  categorías demográficas son solo un proxy innecesario y con sesgo.
- Además, el propio paper es una **simulación** (no medición de atletas), y sus autores reconocen
  que ignorar la fuerza muscular invalida parte de sus conclusiones (ver §c punto 4).

---

## 7. Tabla de confianza por fuente

| Fuente | Tipo | Confianza para la decisión |
|---|---|---|
| Wang & Shan 2023, *Bioengineering* (MDPI) | Peer-reviewed (simulación) | Alta para la ecuación de torque y la dirección del efecto de altura/masa; baja como ranking de "tipos corporales". |
| Shan & Bohn 2003, *Applied Ergonomics* | Peer-reviewed | Alta (datos antropométricos de COG/segmentos). |
| Ebben et al. 2011, *JSCR* | Peer-reviewed (medición) | Alta para "push-up: altura irrelevante". |
| Zoeller et al. 2008, *Eur J Appl Physiol* | Peer-reviewed | Alta para "fuerza no escala 1:1 con masa". |
| MacDonald et al. 2025, *Front Sports Act Living* | Peer-reviewed (revisión sistemática) | Alta para "handstand = equilibrio/habilidad". |
| Urbanski 2024, risewithmarcus.com | Practicante (automedición) | Baja; origen del "22,5% de la altura". No usar como verdad revisada. |
| Katrichis & Moca 1992, *NSCA SCJ* | Profesional/practicante | Baja-media (histórico). |
| Kizirian (Antranik.org) | Coach/practicante | Media (heurística explícitamente anecdotaria; útil para "más progresiones intermedias"). |
| Low, stevenlow.org | Coach (autor OG) | Media (marco de programación sólido, sin números de altura/peso). |

---

## 8. Implicaciones para el motor determinista (orientativo, no prescriptivo)

- Para **exercises non-levered** (pull-up, push-up, squat): usar solo masa (peso) como entrada; la
  altura no debe ajustar la dificultad de la *carga*. Si acaso, la altura podría informar un
  ajuste *fisiológico* (fuerza relativa esperada) y el mayor trabajo por recorrido — pero no un
  ajuste de palanca.
- Para **levered** (planche, front lever): la dificultad escala con **masa × brazo de palanca
  (altura/proporciones)**, así que altura y (mejor) medidas de proporción segmental sí deben
  informar la prescripción (más progresiones intermedias, más volumen, expectativa de progreso más
  lenta). El "22,5% de la altura" puede usarse como **heurística de practicante**, no como constante
  verificada; lo riguroso es estimar el COG con tablas antropométricas (Shan & Bohn 2003 / de Leva /
  Winter) o medir envergadura/entrepierna.
- **No usar categorías demográficas.** Usar solo medidas físicas.

---

## Referencias (orden de aparición)

1. Wang X, Shan G. *Insights from a Nine-Segment Biomechanical Model… Planche.* Bioengineering
   (Basel). 2023;10(7):761. doi:10.3390/bioengineering10070761. PMC10376746.
2. Urbanski M. *A Scientific Approach to Mastering the Planche.* risewithmarcus.com; 2024.
   <https://risewithmarcus.com/blog/a-scientific-approach-to-mastering-the-planche--quantifying-progress-and-setting-clear-goals>
3. Shan G, Bohn C. *Anthropometrical data and coefficients of regression related to gender and
   race.* Appl Ergon. 2003;34(4):327–337. doi:10.1016/S0003-6870(03)00040-1.
4. Katrichis NE, Moca A. *Sports performance series: The planche.* Strength Cond J. 1992;14(6):6–9.
   doi:10.1519/0744-0049(1992)014<0006:TP>2.3.CO;2.
5. Ebben WP, Wurm B, VanderZanden TL, et al. *Kinetic analysis of several variations of push-ups.*
   J Strength Cond Res. 2011;25(10):2891–2894. doi:10.1519/JSC.0b013e31820c8587. PMID 21873902.
6. *Body Physics: Motion to Metabolism* (Open Oregon / OpenStax). Body Levers.
   <https://openoregon.pressbooks.pub/bodyphysics/chapter/body-levers>
7. Zoeller RF, et al. *Allometric scaling of isometric biceps strength… BMI.* Eur J Appl Physiol.
   2008;104(3):455–461. doi:10.1007/s00421-008-0819-2. PMID 18648848.
8. MacDonald M, Baker JS, Gu Y, Ugbolue UC. *Biomechanical analyses of the handstand: a systematic
   review.* Front Sports Act Living. 2025. doi:10.3389/fspor.2025.1694648. PMID 41473027.
9. Kizirian A. *Don't be fooled…* antranik.org. <https://antranik.org/dont-be-fooled/>
10. Low S. *How to program for advanced isometric movements after a plateau.* stevenlow.org; 2018.
    <https://stevenlow.org/how-to-program-for-advanced-isometric-movements-after-a-plateau/>
