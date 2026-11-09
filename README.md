# Pokemon-Go
unity project game

## Open the project

1. Clone this repository and add the cloned folder in Unity Hub.
2. Open it with Unity **6000.6.3f1**, including Android Build Support, SDK/NDK and OpenJDK.
3. Let Unity import the project. Use **Animal Hunt → Open Main Menu** to open the game.
4. For an Android phone with ARCore, use **Walk & Hatch → 3 Build updated APKs**. The full AR build is `Builds/AnimalHunt_WalkHatch_AR.apk`; it needs camera and precise location permission. Google Play Services for AR must be available on the phone.
5. For a non-ARCore phone, `Builds/WalkHatch_Test.apk` uses a normal-camera preview with simulated GPS.

See [README_WALK_HATCH.md](README_WALK_HATCH.md) and [README_ANIMAL_HUNT.md](README_ANIMAL_HUNT.md) for detailed testing steps. Actual phone sensors, GPS and AR tracking need testing on the device. Unity recreates the excluded Library and other generated folders during import.
