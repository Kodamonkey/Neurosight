# NeuroSight – README

> 1. Instala Unity 6.1 LTS con Android Build Support.
> 2. Clona este repo y ejecuta `git lfs install && git lfs pull`.
> 3. Abre el proyecto con Unity Hub y pulsa **Build & Run** (Android).
> 4. Trabaja siempre en _feature-branches_, haz _pull requests_ contra **dev** y deja **main** solo para versiones demo.

---

## 1 · Tech Stack

| Capa                     | Versión                                                                             | Rol                                                        |
| ------------------------ | ----------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| **Motor 3-D/AR**         | Unity 6.1 LTS + Android Build Support                                               | Renderizado, compila a Android/iOS/HoloLens 2/Magic Leap 2 |
| **AR Móvil**             | AR Foundation 5.1 + AR Core XR Plug-in 5.1 (Android)<br>AR Kit XR Plug-in 5.1 (iOS) | Acceso unificado a sensores (tracking, depth, planos)      |
| **Interacción**          | XR Interaction Toolkit 2.5 + MRTK 3                                                 | Gestos, hand-tracking, UI holográfica                      |
| **Entrada**              | Input System 1.7                                                                    | Táctil, mandos, manos                                      |
| **Medicina**             | fo-DICOM (.NET)                                                                     | Lectura de estudios DICOM                                  |
| **Control de versiones** | Git + Git LFS (GitHub)                                                              | Código y assets grandes (FBX, PNG, meshes)                 |
| **CI/CD**                | GitHub Actions (Android workflow)                                                   | Compila APK y sube artefacto                               |

---

## 2 · ¿Por qué este stack?

- **Prototipado rápido** – Unity + C# tienen curva de aprendizaje moderada y gigante comunidad médica.
- **Multiplataforma real** – Misma base corre en Android, iOS y visores MR.
- **Precisión clínicamente útil** – Error 2 – 3 mm en HoloLens 2 / Magic Leap 2, suficiente para entrenamiento.
- **Ecosistema existente** – Plugins DICOM, sombreadores volumétricos, ejemplos quirúrgicos.
- **Coste accesible** – Unity Personal/Education = gratis; nada de licencias Enterprise.

---

## 3 · Requisitos previos

| Software                                       | Min. versión           | Notas                                                                          |
| ---------------------------------------------- | ---------------------- | ------------------------------------------------------------------------------ |
| **Unity Hub**                                  | 3.x                    | Inicia sesión con cuenta _edu_                                                 |
| **Unity Editor**                               | **6.1 LTS**            | Instalar módulos “Android Build Support” (Android SDK 34, NDK r26, OpenJDK 17) |
| **Visual Studio 2022** _o_ **JetBrains Rider** | —                      | Workloads: “Game development with Unity”, “Mobile (.NET)”                      |
| **Git**                                        | 2.44+                  | `git lfs install` una vez                                                      |
| **Node.js** (opcional)                         | 20+                    | Requerido por algunas tools MRTK 3                                             |
| **Dispositivo Android**                        | Android 10+ con ARCore | Instala **Google Play Services for AR** y activa _USB debugging_               |

---

## 4 · Instalación paso a paso

1. **Clona** el repositorio

   ```bash
   git clone https://github.com/NeuroSight/neurosight.git
   cd neurosight
   git lfs install       # sólo la primera vez
   git lfs pull          # descarga los assets grandes
   ```

2. **Abre** el proyecto en **Unity Hub**  
   _Add project → neurosight → abrir con Unity 6.1 LTS_.

3. **Resuelve dependencias**  
   El editor descargará automáticamente los paquetes del _Package Manager_.  
   Si aparece un diálogo “Project upgrade?”, acepta la actualización.

---

## 5 · Flujo de trabajo Git

### 5.1 Ramas

```
main   ←  estable, solo demos/versiones
│
└─ dev ←  integración diaria
     ├─ feature/HU-3-alinear-fantoma
     ├─ feature/HU-5-ui-menus
     └─ hotfix/v0.3.1-gradle
```

- **main**: desplegable en todo momento. Protegida - no _push_ directo.
- **dev**: rama de integración; builds nocturnos de CI.
- **feature/\***: rama por Historia de Usuario o tarea (< 3-4 días).
- **hotfix/\***: correcciones urgentes desde _main_ (se cherry-pickean a _dev_).
- Para pasar a main solo sera a traves de PR.
