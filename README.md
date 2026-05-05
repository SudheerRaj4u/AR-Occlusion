# 📘 EE654 AR Occlusion Project — Complete Guide
### *"If you've never used Unity before, this guide is for you."*

**Project:** EE654 3D Vision & Augmented Reality — Final Project  
**Student:** Sudheer Raj Jonnalagadda | Maynooth University  
**Project Title:** Dynamic Occlusion Handling for Interactive AR Posters

---

## 🧭 WHAT IS THIS PROJECT ABOUT? (Plain English)

Imagine you print a poster and hold your phone camera up to it. The phone shows a video playing on top of the poster — like magic. That's called **Augmented Reality (AR)**.

Now imagine you walk in front of that poster. Normally, the video would awkwardly float **in front of you**, as if you're invisible. That looks wrong and breaks the illusion.

This project fixes that. It makes the phone "see" you as a real person and correctly shows you **in front of the video** — just like reality. The video appears to be playing on the poster, and you walk in front of it naturally.

**To do this, the project:**
1. Detects the poster using the phone camera
2. Runs an AI model to detect any person standing in front
3. Applies a graphics trick to hide the video pixels where the person is
4. Result: person appears in front, video appears behind

---

## 📦 WHAT YOU NEED (Shopping List)

Before starting, make sure you have all of these:

| Item | Where to get it | Cost |
|------|----------------|------|
| A Windows PC | You have this | Free |
| **Unity Hub** | unityhub.com | Free |
| **Unity 6** (version 6000.3.7f1) | Via Unity Hub | Free |
| An **Android phone** (with ARCore support) | You have this | Free |
| A **USB data cable** for your phone | Any electronics shop | ~€5 |
| A **printed poster** (A4, any photo) | Print shop or home printer | ~€1 |
| Google Chrome browser | chrome.google.com | Free |

> **ARCore support check:** Go to the Google Play Store on your phone and search "Google Play Services for AR". If it's installed or installable, your phone is compatible.

---

## 🗂️ YOUR PROJECT FILES

All scripts and guides are in:
```
C:\Users\sudhe\.gemini\antigravity\scratch\3D-VAR-Project\unity-scripts\
```

These are the 5 code files Unity needs:
| File | What it does |
|------|-------------|
| `OcclusionManager.cs` | The brain — coordinates everything |
| `SentisInferenceRunner.cs` | Runs the AI model to detect people |
| `ARTrackedImageHandler.cs` | Detects the poster and places the video on it |
| `DemoUIController.cs` | Controls the FPS display and ON/OFF toggle button |
| `OcclusionShader.shader` | The graphics trick that hides video behind real people |

---

## ✅ STEP-BY-STEP: FULL SETUP (Follow in Order)

---

### STEP 1 — Install Unity Hub and Unity 6 (30–45 min)

**Unity Hub** is the app that manages Unity versions. **Unity** is the game engine we build the AR app in.

1. Open Chrome → go to **https://unity.com/download**
2. Click **Download Unity Hub** → install it (Next, Next, Finish)
3. Open **Unity Hub**
4. On the left sidebar, click **Installs**
5. Click the blue **Install Editor** button (top right)
6. Find **Unity 6** (look for version starting with `6000`) → click **Install**
7. On the module screen that appears, scroll down and tick:
   - ✅ **Android Build Support**
   - ✅ (expand it) **Android SDK & NDK Tools**
   - ✅ (expand it) **OpenJDK**
8. Click **Install** → wait 20–40 minutes (big download)

> ✅ Done when Unity Hub shows Unity 6 listed under Installs with a green tick.

---

### STEP 2 — Create the Unity Project (5 min)

1. In Unity Hub → click **Projects** on the left
2. Click the blue **New project** button (top right)
3. In the template list, click **Universal 3D** (or "3D URP") — it must say URP
   - ⚠️ Do NOT pick the plain "3D" template
4. On the right side:
   - **Project name**: `AR-Occlusion-Demo`
   - **Location**: `C:\Users\sudhe\UnityProjects\`
5. Click **Create project** → Unity opens (takes 2–3 min first time)

> ✅ Done when you see the Unity editor with a 3D scene open.

---

### STEP 3 — Install the Required Packages (20 min)

Think of packages like apps for Unity — you need to add specific ones for AR and AI.

1. In Unity, go to top menu: **Window → Package Manager**
2. In the Package Manager window, click the **+** button (top left)
3. Click **"Add package by name..."**

**Install these one at a time** (type the name, click Add, wait for it to finish):

| # | Type this exactly | What it does |
|---|-------------------|-------------|
| 1 | `com.unity.xr.arfoundation` | AR tracking toolkit |
| 2 | `com.unity.xr.arcore` | Google's AR engine for Android |
| 3 | `com.unity.sentis` | Runs the AI model on-device |

> Each one takes 1–3 minutes. Wait for the spinning circle to stop before adding the next.

**Then configure Android settings:**
1. Go to **Edit → Project Settings → XR Plug-in Management**
2. Click the **Android tab** (icon with a little robot 🤖)
3. Tick **ARCore** ✅
4. Click **Project Validation** → click **Fix All** for any red/yellow items

**Then set Player settings:**
1. **Edit → Project Settings → Player → Android tab → Other Settings**
2. Find and set these three things:

| Setting | Set it to |
|---------|-----------|
| Minimum API Level | Android 8.0 (API 26) |
| Scripting Backend | IL2CPP |
| Target Architectures | ARM64 only (untick others) |

> ✅ Done when all three settings are correct and Project Validation shows no red items.

---

### STEP 4 — Download the AI Model Files (10 min)

The AI brain of this project is called **MobileSAM**. It needs two files:

**File 1 — The Encoder (detects features in the image):**
1. Open Chrome → go to: https://huggingface.co/Acly/MobileSAM/blob/main/mobile_sam_image_encoder.onnx
2. Click the **download** button (↓ icon)
3. Save to your Downloads folder
4. File size should be about **28 MB**

**File 2 — The Decoder (turns features into a person-shaped mask):**
1. Go to: https://huggingface.co/Acly/MobileSAM/blob/main/sam_mask_decoder_single.onnx
2. Click the **download** button
3. Save to your Downloads folder
4. File size should be about **16.5 MB**

**Import into Unity:**
1. In Unity, look at the **Project** panel (bottom of screen)
2. Right-click on `Assets` → **Create → Folder** → name it `Models`
3. Open File Explorer → go to your Downloads folder
4. Drag both `.onnx` files into Unity's `Assets/Models/` folder
5. Wait for the import bar to finish
6. Click each file → the Inspector should say type: **Sentis ModelAsset** ✅

> ✅ Done when both files show as ModelAsset in Unity's inspector.

---

### STEP 5 — Import the Project Scripts (5 min)

1. In Unity's **Project** panel, right-click `Assets` and create these folders:
   - `Scripts`
   - `Shaders`
   - `Prefabs`
   - `Videos`
   - `Textures`

2. Open **File Explorer** and go to:
   ```
   C:\Users\sudhe\.gemini\antigravity\scratch\3D-VAR-Project\unity-scripts\
   ```

3. Drag these **4 files** into Unity's `Assets/Scripts/` folder:
   - `OcclusionManager.cs`
   - `SentisInferenceRunner.cs`
   - `ARTrackedImageHandler.cs`
   - `DemoUIController.cs`

4. Drag this **1 file** into Unity's `Assets/Shaders/` folder:
   - `OcclusionShader.shader`

5. Wait for Unity to compile → look at the bottom of the screen
6. Open **Window → General → Console** → must show **0 red errors** ✅

> ✅ Done when Console shows zero errors.

---

### STEP 6 — Build the Scene (35 min)

This is where we assemble all the pieces in Unity like building with LEGO.

#### 6A — Clean the Default Scene
1. In the **Hierarchy** panel (left side), click **Main Camera** → press `Delete`
2. Click **Directional Light** → press `Delete`
3. Leave any "Volume" objects — don't delete those

#### 6B — Add AR Objects
1. Top menu: **GameObject → XR → AR Session** → click it (appears in Hierarchy)
2. Top menu: **GameObject → XR → XR Origin (Mobile AR)** → click it

#### 6C — Add the Occlusion Manager
1. In Hierarchy, expand `XR Origin → Camera Offset → Main Camera`
2. Click **Main Camera**
3. In Inspector (right side) → click **Add Component** → type `OcclusionManager` → click it
4. Set `Inference Frame Interval` to **2**
5. Leave `Inference Runner` empty for now

#### 6D — Add Poster Tracking
1. Click the top-level **XR Origin** in Hierarchy
2. **Add Component → AR Tracked Image Manager**
3. **Add Component → ARTrackedImageHandler**

#### 6E — Create the Poster Reference Library
1. In Project panel → right-click `Assets` → **Create → Folder** → name it `XR`
2. Right-click `Assets/XR/` → **Create → XR → Reference Image Library**
3. Name it `PosterReferenceLibrary`
4. In Inspector → click **Add Image**:
   - Import your poster photo into `Assets/Textures/` first (drag it from File Explorer)
   - Click the circle next to "Texture 2D" → pick your poster photo
   - Tick **Specify size** ✅ → Width: `0.21` (metres = A4 width)
5. Click **XR Origin** → find **AR Tracked Image Manager** → drag `PosterReferenceLibrary` into the **Serialized Library** field

#### 6F — Create the Video Render Texture
1. Right-click `Assets/Textures/` → **Create → Render Texture**
2. Inspector: Width `1920`, Height `1080`, Depth Buffer: `No depth buffer`
3. Name it `VideoRenderTexture`

#### 6G — Create the Video Plane
1. **GameObject → 3D Object → Plane** → rename it `ARVideoPlane`
2. Right-click its Transform → **Reset**
3. **Add Component → Video → Video Player**:
   - Render Mode: `Render Texture`
   - Target Texture: drag `VideoRenderTexture`
   - Tick **Play On Awake** ✅ and **Loop** ✅
   - Audio Output Mode: `None`
   - Drag a `.mp4` video file from your computer into `Assets/Videos/` then assign it to **Video Clip**
4. Right-click `Assets/` → **Create → Material** → name it `ARVideoMaterial`
5. In Inspector, click the **Shader dropdown** → pick **Custom → ARVideoOcclusion**
6. Drag `VideoRenderTexture` into the `_MainTex (Video Texture)` slot
7. On `ARVideoPlane` → expand **Mesh Renderer → Materials → Element 0** → drag `ARVideoMaterial` in
8. Drag `ARVideoPlane` from Hierarchy into `Assets/Prefabs/` (it turns blue = saved as Prefab ✅)
9. **Delete** `ARVideoPlane` from Hierarchy (only the Prefab copy is needed)

#### 6H — Add the UI (FPS counter + toggle button)
1. **GameObject → UI → Canvas**
2. Inspector → Render Mode: `Screen Space - Overlay`
3. Right-click Canvas → **UI → Button - TextMeshPro** → rename `SceneToggleButton`
   - Click child Text → set text: `Occlusion: ON`
4. Right-click Canvas → **UI → Text - TextMeshPro** → rename `SceneFpsLabel`
   - Text: `-- FPS`
5. **GameObject → Create Empty** → rename `UIManager`
6. **Add Component → DemoUIController**:
   - `Occlusion Manager`: drag `Main Camera`
   - `Toggle Button`: drag `SceneToggleButton`
   - `Toggle Button Label`: drag the Text child of `SceneToggleButton`
   - `Fps Label`: drag `SceneFpsLabel`

#### 6I — Add the AI Inference Runner
1. **GameObject → Create Empty** → rename `InferenceRunner`
2. **Add Component → SentisInferenceRunner**
3. Inspector:
   - **Encoder Model**: drag `mobile_sam_image_encoder.onnx` from `Assets/Models/`
   - **Decoder Model**: drag `sam_mask_decoder_single.onnx` from `Assets/Models/`
   - Encoder Input Width: `1024`, Height: `1024`
   - Mask Width: `256`, Height: `256`

#### 6J — Wire Everything Together
1. Click **Main Camera** → `OcclusionManager` → drag `InferenceRunner` into the `Inference Runner` field
2. Click **XR Origin** → `ARTrackedImageHandler`:
   - `Ar Video Plane Prefab`: drag `ARVideoPlane` from `Assets/Prefabs/`
   - `Occlusion Manager`: drag `Main Camera`

#### 6K — Save the Scene
1. **File → Save** (`Ctrl+S`)
2. Name it `ARScene`, save in `Assets/Scenes/`
3. Check Console → **0 red errors** ✅

---

### STEP 7 — Enable Developer Mode on Your Android Phone (5 min)

1. Go to **Settings → About Phone**
2. Tap **Build Number** exactly **7 times** → phone says *"You are now a developer!"*
3. Go back → **Settings → Developer Options** (now visible)
4. Turn on **USB Debugging** ✅
5. Turn on **Stay awake** ✅ (screen stays on while charging — helpful for testing)

---

### STEP 8 — Build and Deploy to Your Phone (15–20 min)

1. Plug your phone into your PC with the USB cable
2. On the phone: tap **Allow** when it asks about USB Debugging
3. In Unity: **File → Build Settings**
4. Click **Android** in the list → click **Switch Platform** (wait 2–4 min)
5. Click **Add Open Scenes** → `ARScene` should appear
6. In **Run Device** dropdown → your phone should appear (e.g. `Pixel 7`)
   - Not showing? Click **Refresh**, re-plug the cable
7. Click **Build And Run**
8. Save the APK file somewhere (e.g. Desktop)
9. Wait **8–12 minutes** for the first build
10. The app installs and opens on your phone automatically ✅

---

### STEP 9 — Test the App (10 min)

**Basic test:**
1. App opens → you see the live camera feed
2. Point your phone at your **printed poster**
3. Within 1–3 seconds, the video should appear overlaid on the poster ✅

**Occlusion test:**
1. With video visible on poster, put your **hand between the phone and poster**
2. ✅ PASS: Your hand appears IN FRONT of the video
3. ❌ FAIL: Video floats over your hand → see troubleshooting table below

**Toggle test:**
1. Tap **"Occlusion: ON"** button on screen
2. It changes to **"Occlusion: OFF"**
3. Put your hand in front → it should now go BEHIND the video
4. Tap again → hand appears in front again ✅

**Write down your FPS** (shown in top-left corner) — you'll need this for your report.

---

## ⚠️ COMMON PROBLEMS & FIXES

| Problem | Fix |
|---------|-----|
| "XR" missing from GameObject menu | Go back to Step 3 — XR Plug-in Management not done |
| Red errors in Console | Check that all 5 files were copied to Scripts/ and Shaders/ folders |
| ONNX file not showing as ModelAsset | Sentis package not installed — re-check Step 3 |
| Video doesn't appear on poster | Check PosterReferenceLibrary is assigned; poster needs clear photo (not plain text) |
| Hand appears BEHIND video (no occlusion working) | In Unity Inspector → SentisInferenceRunner → lower Mask Threshold to `0.3` → rebuild |
| Phone not detected in Build Settings | Try different USB cable; re-allow USB Debugging on phone; click Refresh |
| Build fails with Gradle error | Edit → Project Settings → Player → Android → Publishing Settings → tick all three Custom Templates |
| FPS below 20 | In OcclusionManager → set Inference Frame Interval to `3` → rebuild |
| App crashes on launch | Check Minimum API Level is set to 26, Scripting Backend is IL2CPP |

---


---

## 📞 QUICK REFERENCE CARD (Print This)

```
WHAT THE APP DOES:
  → Point phone at printed poster
  → Video appears on poster
  → Walk in front → you appear IN FRONT of the video (not behind it)
  → Toggle button: switch occlusion ON/OFF to compare

KEY NUMBERS TO REMEMBER:
  → MobileSAM encoder: 28 MB  |  decoder: 16.5 MB
  → Inference every 2 frames (not every frame)
  → Mask output: 256×256 pixels
  → Target FPS: ≥ 20 with occlusion active


*Guide written for EE654 3D Vision & AR Project — Maynooth University, 2026*
