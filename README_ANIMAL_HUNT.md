# AR Animal Hunt — beginner testing guide

This is the complete student demo: menu → explore → unlock → place fox → aim and shake → capture/escape → saved collection.

## Which file goes on which phone?

| File | Purpose |
| --- | --- |
| ShakeCatch_Test.apk | Standalone 3D test for your A16. Uses its real accelerometer, with simulated GPS and drag-to-aim. Does not require ARCore. |
| AnimalHunt_FullAR.apk | Full GPS and camera AR game for your friend's ARCore-supported Android phone. |
| AnimalHunt_Complete_Project.zip | Updated Unity source. Extract, then add the pokemonGo folder in Unity Hub using Unity 6000.6.3f1. |

Both APKs require Android 10 or newer and ARM64. They have different application IDs and separate collections. These are Development builds for the school demonstration, not store releases.

## 1. Try the A16 test first

1. Transfer **ShakeCatch_Test.apk** to your phone and install it. Android may ask you to allow installation from the app opening the file.
2. Open **Animal Hunt Test**. After the splash, press **Play**.
3. Open **Development controls**. The mode should say **SIMULATION**.
4. Press **Inside: 10 m**. Wait about three seconds, then press **Back to exploration**.
5. Confirm **A wild fox is nearby!**, then press **Open Camera**. This test uses a normal 3D camera; it does not show your real room.
6. Press **Place fox (local test)**.
7. The test camera initially aims at the fox. Drag across the open part of the scene to change aim. Align the **+** with the fox's body.
8. Hold still for the one-second calibration. Hold the phone securely, then make a deliberate back-and-forth shake. The detector requires two movement peaks, rather than a single tilt.
9. Confirm one ball launches and the attempts counter decreases. Wait for that ball to finish before throwing again.
10. A hit captures the fox and awards **50 XP**. Open **View Collection** to see the saved count and XP.
11. Close and reopen the app. **My Animals** should retain the collection.

If shakes are too difficult, return to the menu, open **Settings**, and choose **High** sensitivity. Low sensitivity requires stronger movement. Real sensor tuning must be checked on your phone.

The labelled **DEV: throw ball** button allows a comparison between input detection and physics. It does not validate the accelerometer. In the Unity Editor, Space also throws.

## 2. Test three misses and another encounter

1. After finishing an encounter, press **Continue Exploring**.
2. In Development controls choose **Outside: 100 m** and wait for three readings. Then choose **Inside: 10 m** and wait again.
3. Open the camera and place the fox.
4. Drag the view away from the fox and throw three balls. Missed balls resolve on surface collision or after four seconds.
5. Confirm **The fox escaped!** after the third miss. No XP should be awarded.
6. Opening the menu or restarting the app must not unlock the consumed zone. Exit and re-enter as above.

Simulation and real GPS have separate test-zone identities. Switching test modes is a development action, not part of normal gameplay.

## 3. Test AR on your friend's phone

1. Verify that Google Play Services for AR works on that phone, then install **AnimalHunt_FullAR.apk**.
2. Open **AR Animal Hunt** and press **Play**.
3. For the first AR test, use Development controls to switch to **SIMULATION**, then choose **Inside: 10 m** and wait for three readings.
4. Return to exploration and press **Open Camera**. Grant camera permission. Allow any ARCore installation/update requested by the phone.
5. Stop walking and slowly scan a well-lit, textured floor or table. Avoid blank reflective surfaces.
6. A marker appears where the centre of the camera detects a horizontal surface. Tap a detected surface away from UI buttons to place the fox.
7. Move the phone gently: the fox should remain attached to its surface.
8. Aim the centre reticle at its body, hold still for calibration, then shake back and forth.
9. Test a hit, a miss and three-miss escape. Check that only one ball and one fox exist at a time.
10. Cover the camera or point away until tracking is lost. Throwing should stop. An interrupted in-flight throw is refunded.

The accelerometer gesture is the same in both APKs. Full AR aiming uses the camera's physical movement; drag aiming is provided only in the standalone test.

## 4. Test actual GPS

1. In exploration, open Development controls and select **REAL GPS**.
2. Enable the phone's Location setting. Grant **Precise location** and **While using the app** when requested.
3. Wait outdoors for a fresh position with accuracy **20 m or better**.
4. Press **Set test zone here (real GPS)** at a safe test location. This saves the centre on this phone.
5. Return to exploration. Wait for three distinct accurate readings within **50 m** to unlock the encounter.
6. After capture or escape, walk beyond **70 m** and wait for three accurate readings. Return within **50 m** and wait for entry confirmation.
7. At **50–70 m**, the game retains its previous inside/outside state, avoiding boundary flicker.

GPS updates come from Android. Standing still may not produce three distinct timestamps immediately. Repeated readings are deliberately not counted as new readings.

If permission is denied or GPS times out, correct the phone setting and use **Retry GPS / refresh exploration**. This does not remove a consumed-zone lockout.

## 5. Pause, collection and settings checklist

- Pause the app during a throw, then return. It should return to exploration, reacquire GPS and preserve remaining attempts for the unfinished encounter during that session. The interrupted throw is refunded.
- Reopen the camera and place the fox again after resuming.
- Press Back during an encounter. **Stay and continue** keeps it; **Leave encounter** counts as escape.
- Verify sound and vibration can be disabled independently.
- Verify each capture adds one fox and exactly 50 XP, including repeated captures after valid exit/re-entry.
- Restart the app and confirm collection, XP, settings and consumed-zone state remain saved.
- Run for 15 minutes and check for crashes, accumulating balls or duplicate rewards.

Saves are local. No account, server, live map, camera-image storage or location history is implemented. Uninstalling the app removes its data. The existing saved test-zone centre is also local to each phone.

## 6. Open and work in Unity

The existing project on your PC is `C:\Users\moham\Desktop\unity\pokemonGo`.

Use the new **Animal Hunt** top menu:

- **Open Main Menu**: complete app flow. In the Editor's scene list, Play chooses the standalone scene when it is included.
- **Open Standalone Test**: jump to the non-AR development scene.
- **2 Run all automated checks**: EditMode zone, shake, round, persistence and regression checks.
- **Run end-to-end physics smoke test**: automatically exercises a real ball collision, rewards, zone reset and three timeout misses. It restores the prior Editor collection afterward.
- **3 Build both APKs**: produces the two APKs in the project's Builds folder. The first native build can take several minutes.

Do not run **1 Create full game scenes** again. It refuses to overwrite the generated scenes. Edit the existing scenes normally instead. Previous Wildlife Explorer reference scenes and your manual scenes remain available.

The interface is created at runtime by AnimalHuntView. Its spacing and text can be changed there. AnimalHuntGame controls the screen flow; GameRules holds the testable shake, attempt and reward rules.

## What still needs physical testing

Editor tests and a successful APK build do not prove Android permission behavior, accelerometer sensitivity, GPS accuracy, camera tracking or performance. Perform the phone checklist above before presenting the project.
