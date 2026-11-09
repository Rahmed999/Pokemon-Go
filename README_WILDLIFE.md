# Wildlife Explorer - start here

Unity 6.6 (6000.6.3f1), AR Foundation/ARCore 6.6.2, Android.

Your existing project is `C:\Users\moham\Desktop\unity\pokemonGo`.

## What is finished

- Foreground GPS requests precise location and shows coordinates, accuracy and distance.
- A zone unlocks an encounter after three **different**, fresh readings within 50 metres with accuracy at most 20 metres.
- Three readings beyond 70 metres confirm exit. The 50–70 metre band prevents boundary flicker.
- Simulation includes Inside (10 m), Outside (100 m), Edge (60 m) and Poor accuracy.
- One animal per encounter; capture requires exit and re-entry before another encounter.
- An original fox placeholder prefab with a ground-level root and a collider.
- Horizontal surface detection, centre-screen raycast and an AR anchor for animal placement.
- UI controls, portrait layout, safe-area handling and a capture connection for your friend.
- A GPS-only scene that can preview the animal without claiming to perform AR tracking.

Your original scenes in `Assets/Scenes` remain available. Use the **completed** scenes below for this feature. The GPS script is shared, so its corrections also apply to scenes that already used it.

## 1. Open the correct scene

In Unity's Project panel, open **Assets > WildlifeExplorer > Scenes**.

- **GPS_Test_Complete**: simulation and GPS-only development. Includes a local animal preview, without ARCore.
- **WildlifeExplorer_AR_Complete**: the full AR flow for an ARCore-supported phone.

Do not run “Create completed demo scenes” again: it intentionally refuses to overwrite the completed scenes. You can edit these scenes normally in Unity.

## 2. Test the flow in the Editor

1. Double-click **GPS_Test_Complete**.
2. Open the Game tab. Select a portrait aspect ratio and reduce the Scale slider until the whole screen fits.
3. Press Play.
4. Keep **SIMULATION** checked and click **Start exploration**.
5. Click **Inside: 10 m** and wait for three new readings (approximately three seconds).
6. Confirm that **ENCOUNTER UNLOCKED** appears.
7. Click **Show animal preview**. A fox appears in the local 3D scene. This is deliberately labelled **LOCAL PREVIEW - not AR tracking**.
8. Click **Test capture - development only**. The fox disappears, and another encounter stays blocked while you remain inside.
9. Click **Outside: 100 m**, wait for three readings, then click **Inside: 10 m** and wait again.
10. Confirm you can show another animal.
11. Test **Poor accuracy**: placement must stay blocked until good readings return. Inside/Outside/Edge restore good simulated accuracy.
12. After entering the zone, **Edge: 60 m** keeps you inside; after exiting, the same 60 m position keeps you outside.
13. Press Play again to exit Play mode. Runtime UI changes are not saved as scene edits.

The generated positions test your logic, not your phone's physical GPS hardware. Unity Remote is unnecessary for this flow.

## 3. Test real GPS on your A16 4G

1. Open **GPS_Test_Complete**.
2. Choose **Wildlife Explorer > Build GPS Test APK (A12 or A16)** from Unity's top menu.
3. Wait for the build. Its file is **Builds/WildlifeExplorer_GPS_Test.apk** under your project folder. This build temporarily disables XR initialization so GPS works independently of ARCore.
4. Install the APK on your phone. Android may ask you to allow installation from the app used to open it.
5. Enable the phone's Location setting.
6. In the app, uncheck **SIMULATION**, then press **Start exploration**.
7. Choose **Precise location** and **While using the app** when Android requests permission.
8. Go outdoors and wait until accuracy is 20 metres or better. Acquiring a position can take time. If it times out, press Start again outdoors.
9. Stand in a safe test area and press **Set test zone here (real GPS)**. This deliberately saves your current position as the test zone centre on this phone. Example Inspector coordinates cannot unlock a real encounter until a zone is configured.
10. For an entry demonstration, walk more than 70 metres from that point, confirm exit, then return within 50 metres and wait for three different accurate readings.
11. Your A16 4G can test GPS and the local preview. It cannot validate real ARCore plane detection or anchoring.

If Android only grants approximate location, change the app's location permission in Android Settings to precise and press Start again. No background location permission is used. Leaving the app during active exploration stops the session; press Start when you return.

Only the chosen test-zone centre is saved locally. Camera images, audio and a location history are not saved or sent by the feature.

## 4. Test full AR on your friend's phone

The phone must support ARCore, have Google Play Services for AR installed/updated, and run Android 10 or newer with ARM64 support.

1. Open **WildlifeExplorer_AR_Complete**.
2. Choose **Wildlife Explorer > Build AR APK (supported phone)**.
3. Install **Builds/WildlifeExplorer_AR.apk** on your friend's phone.
4. Allow the camera when requested. Follow any request to install/update Google Play Services for AR.
5. First keep **SIMULATION** checked and test Inside entry. This separates AR problems from GPS problems.
6. Stop walking. Slowly scan a well-lit, textured floor or table.
7. Aim the centre **+** reticle at a detected surface.
8. Press **Place animal on surface**. The fox should stay attached to the surface as you move the phone.
9. Test duplicate placement and the development capture button.
10. Then switch to **REAL GPS**, grant precise location and set a real test-zone centre on that phone. The saved zone is local to each phone.
11. Repeat the outdoor exit-and-entry test, then place the fox in AR.

GPS unlocks the encounter. It does not put the animal at a precise geographical point. The raycast and anchor determine its nearby position on a real surface.

## 5. Connect Shake & Catch

Your friend implements the accelerometer, ball launcher, Rigidbody and projectile collision handler. You provide the placed target and the capture gate.

Inside the projectile's valid collision handler:

```csharp
ExplorerAnimal target = collision.collider.GetComponentInParent<ExplorerAnimal>();
if (target != null && target.TryCapture())
{
    // Award the capture once through your team's shared collection service.
    Destroy(gameObject); // The projectile.
}
```

Their projectile must also guard against resolving multiple times. Do not destroy the animal directly: `TryCapture()` checks identity/state and completes the encounter. The spawner removes the animal and anchor. Capture in AR is rejected while tracking is insufficient.

`WildlifeAnimalSpawner.CurrentAnimal` exposes the current target. Its **On Animal Placed** event passes the animal GameObject. **On Animal Captured** can trigger shared progression or effects. This feature does not implement the other teammates' sensors or a full collection system.

The animal prefab can be replaced later. Keep `ExplorerAnimal` on the root, retain an appropriate Collider, use metres for scale and keep its feet at local Y = 0. No downloaded Pokemon assets are required.

## 6. Verify the project

- **Wildlife Explorer > Validate completed scene wiring** checks scene references, missing scripts and persistent button bindings. It asks you to save any unsaved scene before opening scenes for inspection.
- **Wildlife Explorer > Run automated zone and encounter checks** runs the Edit Mode tests and writes **TestResults/WildlifeExplorer.xml**.
- Keep the Console free of red compiler errors before building.
- The generated setup has no need to modify your original manual scene's object hierarchy. The completed scenes are separate files.

### Hardware acceptance checklist

- Camera and precise-location permission allowed/refused without a crash.
- Three accurate, different positions unlock one encounter inside 50 m.
- Poor or stale data blocks new placement.
- 50–70 m does not cause state flicker.
- One placed animal; no duplicate capture.
- AR animal stays at the surface while moving the phone.
- Tracking loss prevents placement and capture.
- Pause/resume requires a fresh exploration session.
- No crash during a 15-minute session; aim for at least 30 FPS on the reference phone.

These physical-device checks still need to be performed with your friend. Editor tests cannot prove camera tracking, GPS accuracy or device performance.

## Troubleshooting

- **Wrong scene in a build:** use the custom Wildlife Explorer build menus. They explicitly include the completed scene, independent of the original template's Scene List.
- **No real encounter:** turn Simulation off, allow precise location, configure a real zone and wait for accurate fresh readings outdoors.
- **No new position while standing still:** Android controls GPS updates. Do not count repeated timestamps as new positions; wait or move a short distance safely.
- **No animal in AR:** confirm “Encounter unlocked”, wait for AR tracking and point the reticle at a detected horizontal surface.
- **Black camera:** check camera permission, ARCore compatibility and the existing URP AR Background Renderer Feature.
- **Build downloads fail:** Unity/Gradle may need internet access for the initial Android dependency download. Retry from the same menu once the connection is available.
- **Phone cannot install APK:** verify Android 10+ and ARM64 support. Your A16 4G is for GPS tests; use an ARCore-supported phone for AR.

## Files worth knowing

- `Scripts/WildlifeExplorerGPS.cs`: foreground location, permissions, simulation and encounter availability.
- `Scripts/ZoneRules.cs`: independently tested distance, confirmation and hysteresis rules.
- `Scripts/WildlifeAnimalSpawner.cs`: local preview, anchored AR placement and capture completion.
- `Scripts/ExplorerAnimal.cs`: capture bridge for the teammate.
- `Scripts/ExplorerUI.cs`: runtime control availability and safe-area handling.
- `Prefabs/Fox Animal.prefab`: original built-in placeholder.
- `Editor/ExplorerSetup.cs`: non-destructive scene setup, validation and APK builds.
- `Tests/Editor`: zone, encounter and controller regression checks.
