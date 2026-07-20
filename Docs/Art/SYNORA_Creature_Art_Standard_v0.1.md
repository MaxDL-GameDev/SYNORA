# SYNORA — Estándar de arte de criaturas (v0.1)

> Estándar técnico-artístico para sprites de criaturas (pixel art, cenital ¾). Brief para producción de arte (Gemini) bajo aprobación del Director. Alcance funcional pequeño: cubre lo necesario para M3 (Verak) y fija convenciones reutilizables.
>
> SPEC técnica: [`../Technical/SYNORA_SPEC_M3_Criaturas_v0.1.md`](../Technical/SYNORA_SPEC_M3_Criaturas_v0.1.md)

## 1. Conjuntos de animación

| Conjunto | Obligatorio M3 | Notas |
|---|---|---|
| **Turnaround** | Sí (referencia) | Hoja de referencia de la criatura en sus direcciones; no se anima. |
| **Idle** | Sí | Postura en reposo / "respiración". |
| **Walk** | Sí | Ciclo de caminado de patrulla. |
| **Alert** | Sí | Postura de detección: detenido, atento al Player. |
| **Reaction** | Sí (en estándar) | **Evento visual puntual** (one-shot). No es estado de IA; ver SPEC §13. Puede ser de uno o pocos frames. |

## 2. Direcciones

- **Down**, **Up**, **Side (Left)** como base.
- **Right = `flipX` de Side** cuando la criatura es **simétrica** (`isSymmetric == true`).
- Para criaturas **asimétricas** (`isSymmetric == false`): se producen sprites **Right independientes** (no espejo).
- La orientación en juego es instantánea (sin rotación); no se requieren diagonales en M3.

## 3. Frames

- La **cantidad de frames** por clip es **recomendación, no restricción universal**: Idle 2–4, Walk 4–8, Alert 1–3, Reaction 1–4, según la criatura.
- Rejilla **uniforme** por clip: todos los frames de un clip comparten tamaño de celda.

## 4. Celda, pivot y escala

- **Tamaño de celda** constante por especie (potencia de 2 recomendada, p. ej. 32×32 o 48×48).
- **Pivot centro-inferior** (pies) para orden de dibujo y alineación cenital ¾ coherente.
- **PPU = 32** (`SpritePixelsToUnits`), consistente con el preset del proyecto.
- Escala fina en juego vía `spriteScale` del `CreatureIdentity`, no reimportando.

## 5. Importación en Unity

- Usar `Assets/Settings/PixelArtTexture.preset`: **Point Filter**, **sin mipmaps**, `TextureType = Sprite`, wrap Clamp, PPU 32.
- **PNG con transparencia real** (canal alfa); **fondo eliminado**.
- **Sin marcas de agua**, **sin artefactos** de generación, sin halos ni bordes semitransparentes espurios.

## 6. Slicing y nomenclatura

- Corte de frames limpio y uniforme (mismo origen y tamaño de celda).
- **Carpetas:** `Assets/Art/Creatures/<Especie>/` (p. ej. `Assets/Art/Creatures/Verak/`).
- **Nomenclatura de clips:** `<Especie>_<Conjunto>_<Dirección>` (p. ej. `Verak_Walk_Side`, `Verak_Idle_Down`); Reaction puede omitir dirección si es única (`Verak_Reaction`).
- **Animator Controller:** `Assets/Animation/Creatures/<Especie>.controller`.

## 7. Consistencia

- **Anatomía** y **silueta** consistentes entre conjuntos y direcciones (misma criatura reconocible).
- Paleta y volumen coherentes con el pixel art del proyecto.

## 8. Estados de madurez del asset (categorías)

Distinción obligatoria para el pipeline:

1. **Arte conceptual/generado** — salida cruda (p. ej. de Gemini); puede tener fondo, marcas o artefactos; **no** importable directamente.
2. **Sprite limpio listo para Unity** — fondo eliminado, transparencia real, frames cortados, celda/pivot uniformes; importable.
3. **Placeholder técnico** — arte provisional (incluye graybox) que **desbloquea** la implementación sin ser definitivo.
4. **Asset definitivo aprobado** — versión final validada por el Director.

**Regla:** los assets definitivos **no bloquean** la implementación. Placeholders técnicos quedan aprobados; la Fase 5 integra lo definitivo disponible y placeholders para lo faltante. Sustituir placeholder por definitivo **no** debe requerir cambios en `CreatureBrain`, `CreatureMovement` ni `CreatureSensor`.

## 9. Estado actual de Verak

- **Parcialmente disponible / utilizable:** turnaround; Idle Down; Idle Up; Idle Left/Side (v1).
- **Faltante o a refinar:** Idle Right (si no se usa `flipX`); Walk; Alert; Reaction; limpieza de fondo; transparencia real; eliminación de artefactos/marcas; corte final de frames; uniformidad de celda y pivot.

## 10. Criterios de aprobación

Un conjunto se considera **definitivo aprobado** cuando: fondo transparente real, sin artefactos/marcas, celda y pivot uniformes, direcciones coherentes (Down/Up/Side + Right por flipX si simétrica), nomenclatura y carpeta correctas, importado con el preset y validado en juego por el Director.
