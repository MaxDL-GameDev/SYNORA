# SYNORA — GDD M5: Combate no letal (v0.1)

> Documento de diseño de juego del hito **M5 — Combate no letal**. Cubre la fantasía jugable, la experiencia, el flujo, el tono y las reglas del enfrentamiento con el **Verak Alterado**. No modifica la Biblia, el GDD del prototipo ni documentos anteriores. Autoridad creativa y de canon: el Director.

## 1. Fantasía jugable

Hasta M4, el mundo de SYNORA se **observaba**. En M5 aparece, por primera vez, algo que **rompe** esa calma: un **Verak Alterado**, una versión corrompida de la criatura ambiental que el jugador aprendió a contemplar. El jugador no la caza ni la destruye: la **contiene**. La fantasía es la del **guardián que restablece el equilibrio sin matar** — enfrentar una amenaza y neutralizarla (`Subdued`), no eliminarla, coherente con un mundo que se cuida en lugar de explotarse.

El contraste es deliberado: el mismo animal que en Idle apenas respiraba, alterado, **persigue y golpea**. Y aun así el objetivo no es la violencia final, sino **detenerlo**.

## 2. Naturaleza del combate (canon)

- **No letal.** No hay muerte, loot, experiencia, captura, restauración ni vinculación. No hay persistencia.
- **Objetivo:** contener temporalmente una amenaza.
- **Verak normal** sigue siendo pacífico y observable (M3/M4 intactos). **No** combate.
- **Verak Alterado** es la única entidad de combate de M5.
- **Resultado:** el Verak Alterado queda **`Subdued`** (contenido, presente en escena, ni muerto ni restaurado). El jugador, si cae, sufre **derrota temporal** — nunca un Game Over definitivo.

## 3. Experiencia objetivo

1. El jugador explora ClaroExterior (o el área de M5).
2. Encuentra un **Verak Alterado** (distinguible del normal por su aspecto corrompido).
3. El Verak Alterado **detecta al jugador y lo persigue** (Chase).
4. En rango, **ataca** (Attack): el jugador puede recibir daño.
5. El jugador **ataca en melee frontal** (tecla de Attack), en la dirección que mira.
6. Cada golpe conectado reduce la salud del Verak Alterado; al recibir daño reacciona (Hurt).
7. Al agotar su salud, el Verak Alterado queda **`Subdued`**: se detiene, inerte, sin volver a atacar.
8. Si la salud del jugador se agota antes, **derrota temporal**: breve pérdida de control y recuperación, sin fin de partida.

## 4. Flujo de estados (experiencia)

```
Explorando ─▶ Verak Alterado detecta ─▶ Chase ──(en rango)──▶ Attack
                                          ▲                     │
                                          └──(fuera de rango)───┘
   (recibe golpe del jugador) ─▶ Hurt ─▶ vuelve a Chase/Attack
   (salud del Verak = 0) ─────────────▶ Subdued  (terminal)

   (salud del jugador = 0) ─▶ Derrota temporal ─▶ recupera control
```

## 5. El Verak Alterado

- **Qué es:** una entidad **nueva**, variante corrompida del Verak. Comparte la base de criatura (movimiento, sensor, animación) pero con comportamiento **hostil acotado**: perseguir, atacar, reaccionar y ser contenido.
- **Qué NO es:** no es el Verak normal (que sigue pacífico), no muere, no se captura, no se restaura ni se vincula en M5.
- **Aspecto:** debe leerse de un vistazo como "Verak, pero mal" (corrupción visual). El arte definitivo es autoridad de Gemini bajo brief del Director; M5 puede usar placeholders.
- **Contención (`Subdued`):** al ser contenido queda pasivo y presente en la escena — un gancho para la **restauración de M6**, que queda **fuera de M5**.

## 6. Combate del jugador

- **Ataque melee frontal**, en las **cuatro direcciones** de `PlayerOrientation`.
- **Cooldown** entre ataques; **una ventana de golpe** por ataque; **un impacto máximo por objetivo**.
- **Sin** combos, proyectiles, stamina, armas múltiples ni habilidades.
- El ataque **no mueve** al jugador (el movimiento sigue siendo de `PlayerMotor`).
- **No se puede atacar** mientras un panel de observación está abierto (control bloqueado, `Time.timeScale`=1).

## 7. Salud y derrota

- Salud **simple**, por instancia, **sin** críticos, resistencias, elementos ni armadura.
- **Verak Alterado a 0:** `Subdued` (contenido).
- **Jugador a 0:** **derrota temporal** — bloqueo breve + recuperación a un estado seguro. Sin pantalla de fin, sin pérdida de progreso (no hay progreso persistente en M5). El mecanismo se mantiene mínimo.

## 8. Feedback

- **Visual:** animación de ataque del jugador; reacción (Hurt) y estado contenido (Subdued) del Verak Alterado; indicación de daño recibido por el jugador. Arte/animaciones nuevas bajo brief del Director; placeholders admitidos en el MVP.
- **UI de salud:** mínima; solo si el jugador puede recibir daño (se muestra su salud). Sin HUD complejo.
- **Audio:** fuera del MVP salvo indicación.
- El mundo **no se pausa** (`Time.timeScale`=1) durante el combate.

## 9. Relación con M2–M4

- **Observación (M4):** intacta. El Verak **normal** sigue examinable (Watchful in-scene). El Verak **Alterado no es examinable** en M5.
- **Interacción (M2):** `Interact` y `Attack` son acciones distintas; con panel abierto no hay ataque ni movimiento.
- **Criaturas (M3):** el Verak normal conserva Idle/Patrol/Alert y su no-hostilidad; el combate vive en la entidad Alterada.

## 10. Tono

- Coherente con SYNORA: **contención, no exterminio.** El combate es una alteración del equilibrio que el jugador **restablece**, no una cacería.
- Sin regodeo en la violencia; el lenguaje y el feedback evitan la brutalidad. La meta emocional es **aliviar** una amenaza, anticipando la restauración de M6.

## 11. Restricciones (fuera de alcance)

Restauración · vinculación · inventario · crafting · loot · experiencia · progresión · guardado/persistencia · múltiples armas · habilidades · combos · proyectiles · stamina · críticos/resistencias · nuevas especies (aparte del Verak Alterado) · diálogos · misiones · cinemáticas · IA de grupo. Nada de esto entra en M5.

## 12. Deuda aceptable (MVP)

- **Arte/animaciones placeholder** para el Verak Alterado y el ataque del jugador, hasta la pasada de arte del Director.
- **UI de salud mínima** (sin HUD elaborado).
- **Recuperación de derrota simple** (reinicio a estado seguro), sin secuencia elaborada.
- **Un solo tipo de enemigo** (Verak Alterado) y un solo ataque del jugador.
- `Subdued` sin efectos posteriores (la restauración es M6).

## 13. Relación con el canon

El Verak Alterado y el combate no letal se introducen bajo **autoridad del Director**. M5 **no** afirma lore mayor sobre el origen de la corrupción ni sobre la restauración/vinculación (M6/M7); solo establece la mecánica de contención. Cualquier ampliación narrativa queda fuera de M5 y sujeta a aprobación.
