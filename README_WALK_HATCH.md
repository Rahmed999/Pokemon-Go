# Walk & Hatch — testing guide

Unity 6000.6.3f1 · Android · student demo · version 1.1.0 (code 2)

## What is included

One Mystery Egg at a time, a 100-step foreground target, a saved progress bar, AR egg collection and hatching, and a Baby Fox reward in My Animals. Wildlife Explorer and Shake & Catch stay available. Hatch rewards add an animal without extra XP; normal fox captures still award 50 XP.

## Choose your APK

- **WalkHatch_Test.apk**: simulated GPS and a normal 3D camera. Use this on the A16 to test a real pedometer, real accelerometer, collection and persistence without ARCore. Local placement buttons replace AR placement.
- **AnimalHunt_WalkHatch_AR.apk**: real GPS, AR camera, plane detection and anchors. Use an ARCore-compatible phone for the complete outdoor experience.

Both are development builds with explicitly labelled DEV controls. They use the same package IDs as the previous standalone/full-game builds, with a higher version code so installing over a matching signed build preserves its save. Test and AR apps have separate collections. Uninstalling clears that app's local progress. The source archive excludes generated Library, Temp and native build caches; Unity recreates them.

## First test on the A16

1. Install **WalkHatch_Test.apk**, open it and press **Play**.
2. Use the development **Inside zone** GPS control. Three distinct simulated readings unlock the encounter and discover a Mystery Egg.
3. Open **My Egg**. Keep **REAL PEDOMETER** selected for this test.
4. Press **Collect Egg in AR**. In the standalone app this opens local preview. Press **Place egg (local test)**, then **Collect egg (local test)**.
5. Allow Android **Physical activity** when requested. If denied, open My Egg and press **Enable step counter** again; if Android no longer prompts, enable Physical activity in the app's Android permission settings.
6. Keep the app visible and the screen awake. Walk safely. The first fresh reading establishes a baseline; earlier steps are not credited. Sensor updates may arrive in batches and take about ten seconds.
7. Check that the bar increases toward **100 / 100**, then says **Ready to Hatch**. The exact hardware update rate and step-count accuracy must be tested on your phone.
8. Press **Hatch**, then **Place egg (local test)**. Watch the wiggle, shell fragments and Baby Fox reveal.
9. Open **My Animals**: Baby Fox count must increase by exactly one. The egg slot is empty. Reopening the result or restarting the app must not grant another reward.

If the app says **No pedometer detected**, your device has not exposed a StepCounter to Unity. Real incubation is unavailable on that device. There is no accelerometer or GPS-distance fallback. The other game features still work. Use a different phone for real step acceptance, or a labelled simulated egg for the demo.

## Developer simulation without mixing step sources

Before collecting a discovered egg, open My Egg and press the DEV input button to choose **SIMULATED**. Collect it normally. Open My Egg and use **+1**, **+10**, or **near target / ready**. The egg stays labelled simulated until it is hatched; real sensor readings never advance it. A collected real egg cannot be switched to simulation midway. Exit the zone beyond 70 m and return to discover another egg after the slot is free.

Editor mouse/keyboard input cannot prove a phone's pedometer works. Unity Remote is not the acceptance test for native AR/sensor behavior; use the APK.

## Full AR test on your friend's phone

1. Install **AnimalHunt_WalkHatch_AR.apk** on an ARCore-supported Android phone with Google Play Services for AR available.
2. Press Play. Allow precise location and turn on Location in Android settings.
3. Outdoors, use **Set test zone here** at a safe location. Wait for fresh, accurate GPS readings within 50 m. If previously consumed, first go beyond 70 m until reset, then return.
4. Open My Egg and **Collect Egg in AR**. Allow Camera. Slowly scan a textured horizontal surface in good light.
5. When the placement marker appears, tap the ground to place one egg. Tap the egg itself to collect it. Taps on interface controls do not place objects.
6. Allow Physical activity and walk with the app visible until the target is reached. You can still explore/catch while incubating. Only this egg's step progress is affected.
7. At Ready, press Hatch anywhere: returning to the GPS discovery zone is unnecessary. Scan a horizontal surface and tap to place the egg. Watch the reveal, then check My Animals.
8. To obtain another egg, the slot must be free and that discovery zone must reset by three accurate readings beyond 70 m before re-entry. The egg discovery lock is independent of the normal fox encounter lock.

## Pause, restart and failure checks

- Collect a real egg and reach some progress. Background the app, walk, then resume. The first fresh sample is a new baseline; background steps must not be added. Existing progress remains.
- Close and reopen during incubation. Saved steps remain; no closed-app steps are added. A phone counter reset rebases without subtracting progress.
- Interrupt hatching before reward commit by pausing or losing tracking. The egg returns to Ready and can be retried. Once reward is committed, it is saved exactly once even if presentation is interrupted.
- Open a ready egg from Main Menu, start hatching and cancel. Return from My Egg; exploration and GPS must work again.
- Deny Physical activity: show permission guidance without blocking exploration/catching. Deny Camera: preserve the egg and allow retry. Poor GPS prevents collection; a ready egg can still hatch elsewhere.
- A malformed save is retained as a timestamped unreadable backup and starts a fresh collection with a message. Back up the app data before deliberately corrupting a save.
- Run for 15 minutes: no accumulating eggs, anchors, balls or duplicate rewards. Test three misses/escape and one successful physical fox hit as a regression.

## Unity project entry points

Open the existing project with Unity 6000.6.3f1. Use **Animal Hunt → Open Main Menu**, then Play. Avoid recreating completed scenes.

**Walk & Hatch → 1 Prepare egg assets (preserve scenes)** creates missing new assets only. **2 Run all automated checks** runs the EditMode suite. **Run egg and pedometer smoke test** uses injected Unity StepCounter events in local preview, verifies baseline/progress/hatch, then restores the Editor save. **Animal Hunt → Run end-to-end physics smoke test** checks catching. **3 Build updated APKs** builds both Android variants.

Main components: EggRules (pure state/counting), EggManager (transactions), StepCounterService (permission and foreground input), HuntProgress (one store across scenes), EggARFlow (shared AR placement/reveal), and AnimalHuntGame/View (navigation/UI). New Resources/WalkHatch assets are original placeholders; the original fox and reference scenes are retained.

## Verification status

Automated tests and Editor smoke checks are recorded in TestResults and docs/WALK_HATCH_PROGRESS.md. Injected events verify Unity integration, not real hardware. Physical pedometer accuracy, Android permission dialogs, actual AR tracking and outdoor GPS acceptance remain phone tests for you and your friend.
