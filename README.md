# Gamgak 
**`Seamless MR → VR Art Experience`**

#### A seamless MR/VR exhibition that lets visitors step “into” Monet’s Impression, Sunrise and form their own sensory interpretation.

<img width="500" height="816" alt="Screenshot 2025-11-05 at 9 33 28 PM" src="https://github.com/user-attachments/assets/5a490c46-9aea-44be-ba6a-c027f205ae11" />
<img width="500" alt="Screenshot 2025-11-05 at 9 29 15 PM" src="https://github.com/user-attachments/assets/015a653e-af83-4137-8a09-56151c3f066d" />


🎥 Demo: https://www.youtube.com/watch?v=kwojRil0k7o

### What it is

Gamgak combines scene-anchored MR curation with a fade-through VR space that recreates light, fog, color, and sound at dawn. Visitors first get lightweight MR guidance near the painting, then transition—without loading screens—into a living VR scene to experience the work’s atmosphere and narrative.

### My role (VR Engineer)
- **VR Space Design**: Reconstructed Monet’s harbor scene for a 25-second sunrise sequence.
- **Hand Interactions**:
  - Left hand controls musical phrasing (dynamic scale).
  - Right hand controls time flow; Clench gesture to pause the moment (“catch an impression”).
- **3D Spatial Audio**: Harbor ambience (waves, seagulls, horn) placed in 3D; music adapts to light changes.

### Key features
- Scene Anchor (Meta XR SDK): Detects the physical artwork; spawns MR curation UI on approach.
- MR Curation UI: Title/artist/basic context; no heavy interaction—keeps focus on the piece.
- Seamless MR Space Transition: Dual-passthrough trick + render queue blending for smooth MR→VR fade.
- Real-Time Atmospherics: Unity GI, dynamic sun path, fog, and luminance shifts tied to time of day.
- Agency via Hands: Music = left hand; Time = right hand (with clench to hold a frame of feeling).

### Tech stack
- Engine: Unity 6000.1.10f
- Languages/SDKs: C# (Unity), Meta XR SDK, OpenXR
- Collab: GitHub, Notion
- Target: Meta Quest

### How it works (flow)
1. Approach painting → Scene Anchor recognizes it and shows MR curation UI.
2. Choose “Experience” → Seamless fade into VR harbor at dawn.
3. Interact → Shape music with left hand; scrub/hold time with right hand.
4. Exit anytime → Turn away; passthrough fades in for safe return to MR.

### Team
- PM/Pipeline: Yoonseo Choi
- UX/Docs: Yerim Lee
- 3D/Interaction/UIUX: Seojin Cho
- Seamless MR R&D, UI/UX: Chaeyoon Lim
- VR (space, hands, audio): Joni Park
