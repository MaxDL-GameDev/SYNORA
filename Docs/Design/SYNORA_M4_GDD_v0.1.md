# SYNORA — GDD M4: Observación de Criaturas (v0.1)

> Documento de diseño de juego del hito **M4 — Observación de Criaturas**. Cubre la fantasía jugable, la experiencia, el tono y los textos de Verak. No modifica la Biblia, el GDD del prototipo ni documentos anteriores. La autoridad creativa y de canon reside en el Director; este documento propone contenido para su aprobación.

## 1. Fantasía jugable

El jugador no solo atraviesa SYNORA: **la mira**. M4 convierte a las criaturas de fondo en algo que se puede **observar de cerca** — detenerse frente a un Verak, prestarle atención y recibir una lectura breve de lo que está haciendo en ese momento. La criatura no reacciona a la mirada como a una amenaza ni como a un premio; simplemente **sigue viva** mientras el jugador la contempla.

La fantasía es la del **naturalista silencioso**: acercarse sin perturbar, entender un poco más del mundo por el solo hecho de mirar con cuidado. No hay captura, no hay lucha, no hay recompensa mecánica. La recompensa es **saber**.

## 2. Experiencia objetivo

1. El jugador encuentra a Verak en `ClaroExterior`.
2. Se acerca hasta el rango permitido.
3. Se orienta hacia la criatura.
4. Aparece el prompt: **`[E] Examinar`**.
5. Pulsa la interacción.
6. Se abre el panel de observación (el mismo de M2).
7. El Player queda inmovilizado.
8. `Time.timeScale` sigue en 1 — **el mundo no se pausa**.
9. Verak continúa su comportamiento normal (patrulla, se detiene, se aleja).
10. El panel muestra **nombre + descripción según el estado observado al abrir**.
11. El jugador cierra el panel.
12. Recupera el control.
13. Verak nunca recibió una orden ni cambió de estado por culpa de la UI.

El detalle importante de la experiencia: **el mundo sigue corriendo detrás del panel**. Si el jugador abre el examen mientras Verak patrulla, al cerrar puede que la criatura ya no esté donde estaba. Eso es intencional y refuerza que se observa algo vivo, no una ficha estática.

## 3. Flujo de estados (experiencia)

```
Explorando ─ (en rango + de frente) ─▶ Prompt "[E] Examinar"
     ▲                                        │  (pulsa E)
     │                                        ▼
     └────────── (pulsa E) ────────── Panel abierto / Player inmóvil
                                              │  Verak sigue vivo detrás
                                              │  texto fijo (capturado al abrir)
                                              ▼
                                       (pulsa E) cierra → recupera control
```

## 4. Tono

- Evocador, breve, natural. **No** enciclopédico.
- Sin estadísticas, sin números, sin lenguaje de combate.
- Describe **aspecto y presencia**, no lore mayor no establecido.
- Coherente con SYNORA: un mundo que se contempla y se cuida, no que se explota.
- Segunda voz observacional, en presente: se describe lo que el jugador ve **ahora**.

## 5. Textos de Verak (contenido congelado)

**Nombre / título de panel:** `Verak`

**Descripción base** (fallback; aspecto y presencia, sin narrativa no establecida):
> Una criatura acorazada de porte bajo, cubierta de placas pétreas de tono pardo. A lo largo del lomo le crecen espinas cristalinas de un verde azulado que atrapan la luz. Se mueve sin prisa, tan parte del paisaje como las rocas entre las que habita.

### Variantes por estado (para revisión — se congela **V1** de cada una)

**Calm** (observable de `Idle` — calma, escucha, reposo)
- **V1 (congelada):** *Permanece quieto, casi confundido con el terreno. Solo el lento subir y bajar de su costado delata que está vivo.*
- V2: Reposa con los párpados entornados; su respiración es apenas un temblor entre las placas.
- V3: Descansa inmóvil, con las espinas del lomo brillando tenues, al ritmo de algo que no tiene prisa.

**Roaming** (observable de `Patrol` — desplazamiento territorial, vigilancia ambiental)
- **V1 (congelada):** *Recorre su tramo de siempre con paso medido, revisando los límites de un territorio que solo él parece conocer.*
- V2: Avanza en un rondín silencioso; cada vuelta repite la anterior, atento a su rincón del claro.

**Watchful** (observable de `Alert` — ha detectado una presencia y permanece atento, **sin agresión**)
- **V1 (congelada):** *Se ha detenido. Las espinas del lomo se le erizan apenas y su mirada ámbar sigue algo cercano. No amenaza: solo observa, tan atento como quien lo observa.*
- V2: Inmóvil y tenso, ha notado una presencia. Espera, midiendo, sin dar un paso.

> Mapeo de presentación: `Idle → Calm`, `Patrol → Roaming`, `Alert → Watchful`. Si el estado no puede resolverse, se muestra la **descripción base**.

## 6. Feedback

- **Prompt:** reutiliza el `InteractionPromptPresenter` de M2 (`[E] Examinar`). Aparece/desaparece por los mismos criterios de rango y orientación.
- **Panel:** reutiliza `ObservationPanelPresenter` (título + cuerpo). Sin elementos nuevos de UI.
- **Sin** feedback de audio, partículas ni reacción de la criatura en M4 (fuera de alcance).
- El único "feedback vivo" es que **Verak sigue animándose y moviéndose** detrás del panel.

## 7. Restricciones de diseño

- El examen **no** altera a Verak: ni estado, ni facing, ni movimiento, ni animación, ni sensor.
- El mundo **no** se pausa (`Time.timeScale` = 1).
- El texto se **captura al abrir** y no se actualiza mientras el panel está abierto.
- El panel se cierra **solo** por acción del jugador (o por teardown/target perdido, que cierra limpio).
- Dos Verak son **independientes**: se examinan por separado, con su propio contenido, sin compartir referencias.
- Solo Verak. Ninguna especie nueva.

## 8. Deuda aceptable (MVP)

- **Contenido por estado = 3 estados** (Calm/Roaming/Watchful). No hay sub-variantes por hora del día, clima ni historia.
- **Sin memoria:** examinar no deja registro (no hay códex ni "ya observado"). Es intencional en M4.
- **Texto no reactivo:** aunque Verak cambie de estado con el panel abierto, el texto no cambia. Aceptado para preservar la semántica de "instantánea" y evitar acoplar UI↔Brain.
- **Sin línea de visión:** se usa el criterio frontal de M2; un obstáculo delgado entre Player y Verak no bloquea el examen (igual que el resto de examinables de M2).

## 9. Relación con el canon

Verak ya es canon como criatura ambiental de M3. M4 **no** añade lore mayor: describe lo observable (aspecto, presencia, conducta visible) sin afirmar origen, biología ni relación con la restauración/vinculación, que pertenecen a hitos futuros y a la autoridad del Director. Cualquier ampliación de lore de Verak queda fuera de M4 y sujeta a aprobación.
