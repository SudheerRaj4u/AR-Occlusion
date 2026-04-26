# EE654 Demo Video Script — 2 to 3 Minutes
# Real-Time Foreground Occlusion for Interactive AR Poster Experiences
# Sudheer Raj Jonnalagadda | Maynooth University

---

## 🎬 PRODUCTION NOTES (before you record)

- **Device**: Your Android tablet (the one in the demo photo)
- **Recording**: Use screen recorder on the tablet (Settings → Advanced Features → Screen Recorder)
  - OR use ADB screenrecord: `adb shell screenrecord /sdcard/demo.mp4`
- **Setup**: Place poster flat on a table, room well lit
- **What you need**: Poster printed, demo video running on app, your hand/arm ready
- **Target length**: 2 min 30 sec
- **Add captions** in post (any free editor: CapCut, DaVinci Resolve)

---

## 🎥 SEGMENT 1 — Title Card (0:00 – 0:10)

**On screen**: Black title card with white text

> **"Real-Time Foreground Occlusion for Interactive AR Poster Experiences"**
> **EE654 | Maynooth University | Sudheer Raj Jonnalagadda**

*No narration needed — just hold title for 8 seconds then fade out.*

---

## 🎥 SEGMENT 2 — The Problem (0:10 – 0:35)

**Shot**: Hold camera/tablet pointed away from poster, then slowly reveal the poster but place your hand BETWEEN the camera and poster.

**Narration (record voiceover):**
> "This is the standard AR occlusion problem. When a real object — in this case my hand — is physically between the camera and an AR video, the video incorrectly floats in front of it. This is called an occlusion error. My project solves this in real time, on-device, with no special hardware."

**Caption overlay**: `❌ WITHOUT OCCLUSION — Virtual video floats ABOVE real hand`

---

## 🎥 SEGMENT 3 — AR Poster Detection (0:35 – 0:55)

**Shot**: Point tablet camera at the printed poster from about 40–50 cm away.
Show the poster coming into frame → video snapping onto it.

**Narration:**
> "The system uses AR Foundation image tracking to detect a reference poster. Once recognised, a video plane is anchored precisely onto the physical poster — locked to its position even as you move."

**Caption overlay**: `AR Foundation Image Tracking — poster detected ✓`

*[Hold steady for 3 seconds showing video playing cleanly on the poster]*

---

## 🎥 SEGMENT 4 — Occlusion Demo: Hand (0:55 – 1:30)

**Shot**: Slowly move your hand into the frame, in front of the poster. Hold it there for a few seconds. Move it around. Try full arm.

**Narration:**
> "Now with the occlusion system active. ARCore's human segmentation detects my hand as a foreground object. The custom HLSL shader discards the video pixels wherever my hand is detected — revealing the real camera feed underneath. My hand correctly appears in front of the video."

**Caption overlay**: `✅ OCCLUSION ON — Hand correctly appears IN FRONT of video`

*[Move your hand slowly left to right across the poster — hold each position 1-2 seconds]*
*[Then try moving the camera closer and further — show it works at different distances]*

---

## 🎥 SEGMENT 5 — Toggle Comparison (1:30 – 2:00)

**Shot**: Hand is in front of poster. Tap the "Occlusion: ON" button on screen to toggle OFF, then ON again. Repeat 2-3 times.

**Narration:**
> "A runtime toggle lets us compare directly. Occlusion OFF — the video floats over my hand, breaking immersion. Occlusion ON — my hand is correctly in front. The difference is immediate and clear."

**Caption overlay**: First toggle: `❌ Occlusion OFF`  Second toggle: `✅ Occlusion ON`

*[Go slow on the toggles — let each state settle for 2 seconds before switching]*
*[Show FPS counter is visible in the corner — point to it briefly]*

---

## 🎥 SEGMENT 6 — Technical Summary + Close (2:00 – 2:30)

**Shot**: Lower the tablet, look at camera (or keep tablet visible in background with video running).

**Narration:**
> "This pipeline runs entirely on-device — no cloud, no depth sensor. It uses AR Foundation for image tracking, ARCore's native human segmentation for the occlusion mask, and a custom Universal Render Pipeline shader that discards video pixels using HLSL clip(). The system maintains real-time performance, demonstrating that hardware-agnostic foreground occlusion is practical for consumer AR applications today. Thank you."

**Caption overlay**: 
```
📱 AR Foundation + ARCore Human Segmentation
🎨 Custom URP HLSL Shader (clip-based discard)  
⚡ Real-time · On-device · No depth sensor required
```

*[Final 3 seconds: hold on the poster with video playing and hand in frame — clean ending shot]*

---

## 📋 POST-PRODUCTION CHECKLIST

- [ ] Add captions/subtitles for each segment (copy text above)
- [ ] Add a subtle background music track (royalty-free, low volume)
- [ ] Insert a split-screen at the toggle moment (left = OFF, right = ON)
- [ ] Export as MP4, 1080p or higher, < 100 MB
- [ ] Check total length is between 2:00 and 3:00

---

## 🎙️ RECORDING TIPS

1. **Narration**: Record audio separately on your laptop mic into Audacity (free), then sync in editor
2. **Lighting**: Make sure the poster is well-lit — avoid shadows across it
3. **Steady hands**: Rest your elbows on the table when pointing at poster
4. **Multiple takes**: Record each segment 2-3 times, use the best one
5. **FPS**: The FPS counter should be visible and ideally showing > 30 fps during demo

---

## ⏱️ TIMING SUMMARY

| Segment | Time | Key visual |
|---|---|---|
| 1 — Title | 0:00–0:10 | Title card |
| 2 — Problem | 0:10–0:35 | Hand in front, video floating over it (bad) |
| 3 — Poster Detection | 0:35–0:55 | Camera finds poster, video snaps on |
| 4 — Occlusion Demo | 0:55–1:30 | Hand correctly in front of video |
| 5 — Toggle Comparison | 1:30–2:00 | Button toggles ON/OFF clearly |
| 6 — Technical Close | 2:00–2:30 | Summary + clean ending shot |
| **Total** | **~2:30** | |
