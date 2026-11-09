using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplorerUI : MonoBehaviour
{
    public WildlifeExplorerGPS explorer;
    public WildlifeAnimalSpawner spawner;
    public TMP_Text encounter,modeLabel;
    public Toggle simulationToggle;
    public Button placeButton,setZoneButton,captureButton;
    public GameObject simulationControls;
    public RectTransform safeArea;
    Rect lastSafeArea;
    void Start()
    {
        simulationToggle.SetIsOnWithoutNotify(explorer.useSimulation);
        simulationToggle.onValueChanged.AddListener(explorer.SetSimulation);
    }
    void Update()
    {
        encounter.text=spawner.Status;
        modeLabel.text=explorer.useSimulation?"SIMULATION":"REAL GPS";
        simulationControls.SetActive(explorer.useSimulation);
        setZoneButton.interactable=!explorer.useSimulation&&explorer.HasFreshPosition&&!explorer.AnimalActive;
        placeButton.interactable=explorer.CanPlaceAnimal&&!spawner.Placing;
        captureButton.gameObject.SetActive(Application.isEditor||Debug.isDebugBuild);
        captureButton.interactable=spawner.CurrentAnimal!=null;
        if(Screen.width>0&&Screen.height>0&&lastSafeArea!=Screen.safeArea)
        {
            lastSafeArea=Screen.safeArea;
            safeArea.anchorMin=new Vector2(lastSafeArea.xMin/Screen.width,lastSafeArea.yMin/Screen.height);
            safeArea.anchorMax=new Vector2(lastSafeArea.xMax/Screen.width,lastSafeArea.yMax/Screen.height);
        }
    }
}
