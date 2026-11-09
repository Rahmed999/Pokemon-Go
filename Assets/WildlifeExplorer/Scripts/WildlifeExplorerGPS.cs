using System;
using System.Collections;
using TMPro;
using UnityEngine;
using WildlifeExplorer;
using System.Globalization;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class WildlifeExplorerGPS : MonoBehaviour
{
    [Header("Interface")]
    public TMP_Text statusText;

    [Header("Test mode")]
    public bool useSimulation = true;

    [Header("Zone centre - replace with your real test location")]
    public double zoneLatitude = 36.8;
    public double zoneLongitude = 10.18;
    public bool realZoneConfigured;
    public bool Running => running;
    public bool ExternalZoneBlocked;
    public bool ExternalPauseManagement;
    public bool PermissionPending {get;private set;}
    public double DistanceToZone => distance;
    public event Action<double,double> AccuratePosition;
    ZoneRules rules;
    public string Readout => statusText != null ? statusText.text : gpsMessage;

    [Header("Zone rules")]
    public float entryRadius = 50f;
    public float exitRadius = 70f;
    public float maximumAccuracy = 20f;
    public int requiredReadings = 3;

    public event Action ExplorationReset;

    public bool AnimalActive { get; private set; }
    public bool IsInsideZone { get; private set; }

    public bool CanPlaceAnimal =>
        running &&
        (useSimulation || realZoneConfigured) &&
        HasFreshPosition &&
        IsInsideZone &&
        !AnimalActive &&
        !waitingForExit && !ExternalZoneBlocked;

    public bool HasFreshPosition =>
        running &&
        rules != null && rules.HasFreshFix(Now);

    double latitude;
    double longitude;
    double accuracy;
    double distance;
    double lastTimestamp = -1;

    int confirmations;
    bool running;
    bool polling;
    bool lastReadingWasGood;
    bool waitingForExit;

    float simulatedDistance = 100f;
    float simulatedAccuracy = 5f;

    string gpsMessage = "Press Start Exploration.";

    static double Now =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    void Awake()
    {
        if(double.TryParse(PlayerPrefs.GetString("WildlifeExplorer.zoneLat", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
           double.TryParse(PlayerPrefs.GetString("WildlifeExplorer.zoneLon", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
           Finite(lat) && Finite(lon) && Math.Abs(lat)<=90 && Math.Abs(lon)<=180)
        {zoneLatitude=lat;zoneLongitude=lon;realZoneConfigured=true;}
    }

    void Update()
    {
        if (running && !HasFreshPosition) { confirmations = 0; rules?.BreakConfirmation(); }

        if (statusText == null)
            return;

        string position = lastTimestamp > 0
            ? $"Latitude: {latitude:F6}\n" +
              $"Longitude: {longitude:F6}\n" +
              $"Accuracy: {accuracy:F1} m\n" +
              $"Distance to zone: {distance:F1} m"
            : "Waiting for a position.";

        string encounter;

        if (AnimalActive)
            encounter = "Animal active.";
        else if (waitingForExit)
            encounter = "Encounter finished. Leave the zone to try again.";
        else if (!useSimulation && !realZoneConfigured)
            encounter = "Use Set test zone here after obtaining an accurate GPS fix.";
        else if (CanPlaceAnimal)
            encounter = "Encounter unlocked!";
        else if (!HasFreshPosition && running)
            encounter = "Waiting for a fresh, accurate position.";
        else
            encounter = $"Zone confirmation: {confirmations}/{requiredReadings}";

        statusText.text =
            $"{(useSimulation ? "SIMULATION" : "REAL GPS")}\n" +
            $"{gpsMessage}\n\n{position}\n\n{encounter}";
    }

    public void StartExploration()
    {
        StopExploration();

        lastTimestamp = -1;
        lastReadingWasGood = false;
        IsInsideZone = false;
        waitingForExit = false;
        confirmations = 0;
        try { rules = new ZoneRules(zoneLatitude, zoneLongitude, entryRadius, exitRadius, requiredReadings, maximumAccuracy); }
        catch(ArgumentOutOfRangeException) { gpsMessage="Invalid zone settings. Check coordinates and radii."; return; }
        running = true;
        StartCoroutine(LocationLoop());
    }

    public void StopExploration()
    {
        PermissionPending=false;
        StopAllCoroutines();
        Input.location.Stop();

        running = false;
        polling = false;
        AnimalActive = false;
        lastReadingWasGood = false;
        confirmations = 0;
        IsInsideZone = false;
        waitingForExit = false;
        lastTimestamp = -1;

        gpsMessage = "Exploration stopped.";
        ExplorationReset?.Invoke();
    }

    IEnumerator LocationLoop()
    {
        if (useSimulation)
        {
            polling = true;
            gpsMessage = "Simulation running.";

            while (running)
            {
                // North/south offset used only for simulated readings.
                double simulatedLatitude =
                    zoneLatitude + simulatedDistance / 111194.9266;

                ProcessReading(
                    simulatedLatitude,
                    zoneLongitude,
                    simulatedAccuracy,
                    Now);

                yield return new WaitForSecondsRealtime(1f);
            }

            yield break;
        }

#if UNITY_EDITOR
        gpsMessage = "Real GPS requires an Android phone. Enable simulation.";
        running = false;
        yield break;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(
                Permission.FineLocation))
        {
            bool answered = false;

            var callbacks = new PermissionCallbacks();

            callbacks.PermissionGranted += permission =>
            {
                if (permission == Permission.FineLocation)
                    answered = true;
            };

            callbacks.PermissionDenied += permission =>
            {
                if (permission == Permission.FineLocation)
                    answered = true;
            };

            callbacks.PermissionDeniedAndDontAskAgain += permission =>
            {
                if (permission == Permission.FineLocation)
                    answered = true;
            };

            PermissionPending=true;
            gpsMessage = "Allow precise location while using the app.";

            Permission.RequestUserPermissions(
                new[]
                {
                    Permission.CoarseLocation,
                    Permission.FineLocation
                },
                callbacks);

            float permissionDeadline =
                Time.realtimeSinceStartup + 60f;

            while (!answered &&
                   !Permission.HasUserAuthorizedPermission(
                       Permission.FineLocation) &&
                   Time.realtimeSinceStartup < permissionDeadline)
            {
                yield return null;
            }

            PermissionPending=false;
            if (!Permission.HasUserAuthorizedPermission(
                    Permission.FineLocation))
            {
                gpsMessage =
                    "Precise location is required. Enable it in " +
                    "Android app settings, then press Start again.";

                running = false;
                yield break;
            }
        }
#endif

        if (!Input.location.isEnabledByUser)
        {
            gpsMessage = "Enable Location in your phone settings.";
            running = false;
            yield break;
        }

        gpsMessage = "Starting GPS...";
        polling = true;
        Input.location.Start(10f, 0f);

        float deadline = Time.realtimeSinceStartup + 30f;

        while (Input.location.status ==
                   LocationServiceStatus.Initializing &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            gpsMessage =
                "GPS unavailable or timed out. Try outdoors and press Start.";

            Input.location.Stop();
            running = false;
            polling = false;
            yield break;
        }

        polling = true;
        gpsMessage = "GPS running.";

        while (running)
        {
            if (Input.location.status != LocationServiceStatus.Running)
            {
                gpsMessage = "GPS stopped. Press Start to retry.";
                break;
            }

            LocationInfo info = Input.location.lastData;

            ProcessReading(
                info.latitude,
                info.longitude,
                info.horizontalAccuracy,
                info.timestamp);

            yield return new WaitForSecondsRealtime(1f);
        }

        polling = false;
        running = false;
        lastReadingWasGood = false;
        Input.location.Stop();
    }

    void ProcessReading(
        double lat,
        double lon,
        double horizontalAccuracy,
        double timestamp)
    {
        double previousTimestamp=rules.LastFix.Timestamp;
        ZoneTransition change = rules.Process(new ZoneFix(lat, lon, horizontalAccuracy, timestamp), Now);
        IsInsideZone = rules.Inside;
        confirmations = rules.Confirmations;
        lastReadingWasGood = rules.HasFreshFix(Now);
        latitude = rules.LastFix.Latitude;
        longitude = rules.LastFix.Longitude;
        accuracy = rules.LastFix.Accuracy;
        lastTimestamp = rules.LastFix.Timestamp;
        distance = rules.Distance;
        if(lastReadingWasGood && lastTimestamp>previousTimestamp)AccuratePosition?.Invoke(distance,lastTimestamp);
        if (change == ZoneTransition.Exited) waitingForExit = false;
        gpsMessage = change == ZoneTransition.Rejected ? "Invalid, inaccurate or stale GPS reading." :
            useSimulation ? "Simulation running - no real GPS is being used." : "GPS running.";
    }

    public void MarkAnimalPlaced()
    {
        TryMarkAnimalPlaced();
    }

    public bool TryMarkAnimalPlaced() { if(!CanPlaceAnimal) return false; AnimalActive=true; return true; }
    public bool TryCompleteEncounter() { if(!AnimalActive) return false; CompleteEncounter(); return true; }
    public void SetSimulation(bool value) { StopExploration(); useSimulation=value; }
    public void SimulateEdge() { simulatedDistance=60; simulatedAccuracy=5; }
    public void SetTestZoneHere()
    {
        if(useSimulation || !HasFreshPosition || AnimalActive) {gpsMessage="Obtain an accurate real GPS fix first.";return;}
        zoneLatitude=latitude;zoneLongitude=longitude;realZoneConfigured=true;
        PlayerPrefs.SetString("WildlifeExplorer.zoneLat",latitude.ToString("R",CultureInfo.InvariantCulture));
        PlayerPrefs.SetString("WildlifeExplorer.zoneLon",longitude.ToString("R",CultureInfo.InvariantCulture));PlayerPrefs.Save();
        StartExploration();gpsMessage="Saved this position as the 50 m test zone centre.";
    }

    public void CompleteEncounter()
    {
        AnimalActive = false;

        // If the player already left while the animal was active,
        // the next confirmed entry can unlock another encounter.
        waitingForExit = IsInsideZone;
    }

    public void SimulateInside()
    {
        simulatedDistance = 10f;
        simulatedAccuracy = 5f;
    }

    public void SimulateOutside()
    {
        simulatedDistance = 100f;
        simulatedAccuracy = 5f;
    }

    public void SimulatePoorGPS()
    {
        simulatedAccuracy = 100f;
    }

    void OnApplicationPause(bool paused)
    {
        // Do not interrupt the initial location permission dialog.
        if (paused && polling && !ExternalPauseManagement)
            StopExploration();
    }

    void OnDisable()
    {
        StopExploration();
    }

    static bool Finite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);

    public static double DistanceMetres(
        double lat1, double lon1, double lat2, double lon2)
    {
        const double radians = Math.PI / 180.0;

        double deltaLat = (lat2 - lat1) * radians;
        double deltaLon = (lon2 - lon1) * radians;

        double a =
            Math.Pow(Math.Sin(deltaLat / 2), 2) +
            Math.Cos(lat1 * radians) *
            Math.Cos(lat2 * radians) *
            Math.Pow(Math.Sin(deltaLon / 2), 2);

        a = Math.Max(0, Math.Min(1, a));

        return 6371000.0 *
               2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
