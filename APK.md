# NeuroSight - Rama ARCORE

Pasos necesarios para configurar el proyecto y generar el APK que se instalará en un dispositivo Android compatible con ARCore.

## Requisitos Previos

1. **Sistema Operativo**: Windows 10 (o macOS) con al menos 8 GB de RAM.  
2. **Unity Hub**: versión más reciente (`https://unity.com/download`).  
3. **Cuenta de Unity**: para autorizar el uso de Unity Hub.  
4. **Tablet o Smartphone Android (API ≥ 26) compatible con ARCore**:  
      - Debe tener instalado y actualizado “Google Play Services for AR”.  
      - Activar “Opciones de desarrollador”, “Depuración USB” e “Instalar vía USB” en el dispositivo.  
5. **Cable USB con transferencia de datos** (para instalar el APK).  

---

## Instalación de Unity 6.1 LTS con Android Build Support

1. Descarga e instala **Unity Hub** desde [unity.com/download](https://unity.com/download).  
2. Abre **Unity Hub**, inicia sesión o crea una cuenta gratuita.  
3. En la pestaña **Installs**, haz clic en **Add** (o **Install Editor**) y elige la versión **6.1.0 LTS**.  
4. Marca los siguientes módulos **IMPRESCINDIBLES**:  
   - **Android Build Support**  
     - Android SDK & NDK Tools  
     - OpenJDK
   - *(Opcional)* iOS Build Support (si más adelante quieres compilar a iOS)  
5. Desmarca la instalación de **Visual Studio Community** si prefieres usar **Visual Studio Code**.  
6. Haz clic en **Install** y espera a que Unity Hub descargue e instale el editor y los SDKs.

---

## Abrir el Proyecto NeuroSight

1. Clona (o actualiza) la rama **ARCORE** de este repositorio:  
   ```bash
   git clone https://github.com/Kodamonkey/Neurosight.git
   cd Neurosight
   git checkout ARCORE

---

## Configuración de Builds Profiles / Build Settings

1. Pulsa Ctrl + Shift + B para abrir la ventana Build Profiles.
2. Si aún no existe, haz clic en Add Build Profile. Ponle un nombre descriptivo como Android-AR.
3. En la sección Platforms, marca Android y desmarca cualquier otra plataforma.
4. Abajo, verifica que aparezca tu dispositivo en Run Device (p. ej. “Xiaomi Pad 5”). Si tu dispositivo no aparece, pulsa Refresh.
5. Marca Override Global Scene List y añade solo la escena AR (por defecto suele ser SampleScene). Reemplázala o añádela así:
      - Abre la carpeta Assets/Scenes/, selecciona tu archivo de escena AR (p. ej. AR_Scene.unity) y haz clic en Add Open Scene.
      - Elimina la SampleScene de la lista si no la vas a usar y asegurate de marcar la escena que quieras construir.
7. Haz clic en Player Settings (arriba a la derecha en Build Profiles) y verifica lo siguiente en Android → Other Settings:
      - Scripting Backend = IL2CPP
      - Target Architectures = ARM64
      - Minimum API Level ≥ 26
8. En el mismo Player Settings, selecciona XR Plug-in Management, pestaña Android y marca Google ARCore. Dentro de ARCore, puedes dejar Requirement = Required y Depth = Optional.

## Generar el APK en dispositivo android

1. Conecta tu tablet/celular Android en modo depuración USB al PC.
2. Asegúrate de que has aceptado la clave ADB en el dispositivo.
3. En Unity, abre la ventana Build Profiles (Ctrl + Shift + B) y confirma que tu perfil Android esté activo y tu dispositivo aparezca en Run Device.
4. Haz clic en Build And Run.
      - Unity mostrará un diálogo para elegir carpeta y nombre del APK (p. ej. ARPlane.apk).
      - Selecciona o crea la carpeta Builds/Android/ dentro del proyecto y pulsa Guardar.
5. Unity compilará el APK, lo instalará en la tablet y lanzará la aplicación automáticamente.
6. En tu tablet, cuando la app se abra por primera vez, acepta el permiso de la cámara.
7. Apunta la cámara a una superficie plana (suelo, mesa) para ver los quads semitransparentes, o apunta a la imagen impresa (marcador) para que se instancie el objeto 3D.

## Probar en el Editor (Simulación)

# Importante: no se ha probado levantar las escenas en simulación todavía.

1. En Window → Package Manager, dentro de AR Foundation, importa el AR Simulation sample.
2. Ve a Edit → Project Settings → AR Simulation y marca Enable Simulation.
3. Abre Window → XR → AR Simulation.
4. Mientras estés en Play Mode, en la ventana de simulación:
      - Para probar detección de planos: haz clic en Add Plane y mueve la cámara virtual (WASD+mouse) hasta “ver” el plano.
      - Para probar detección de imágenes: en “Image” asigna tu PNG de referencia y haz clic en Add Image. Luego mueve la cámara virtual hasta “ver” esa imagen simulada.
5. Observa en la Game View cómo se instancian los quads semitransparentes (planos) y los objetos 3D al detectar la imagen.

## Escenas implementadas en Assets/Scenes

1. SampleScene: Escena por defecto de Unity.
2. ARPlane: La cámara detecta planos (horizontales y verticales) y proyecta quads semitransparentes sobre el plano que detecta.
      - Tiene errores, no proyecta correctamente los quads sobre los planos que detecta. Es impreciso.
3. ARQR: La cámara trackea una imágen QR almacenada en Assets/ReferenceImages y luego proyecta un Cubo sobre el QR de referencia.
      - Es batante más preciso, se puede mover la cámara y el cubo se mantiene en su posición. Pero aún falta presición y ubicación de la imágen.
