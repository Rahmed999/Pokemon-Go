using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WildlifeExplorer;
public class AnimalHuntView:MonoBehaviour {
    public AnimalHuntGame game;
    Canvas canvas;RectTransform safe,body,artwork;Image flash;float artStart;
    TMP_Text eggText;Slider eggBar;Button eggCollect,eggHatch;int eggRevision=-1;float eggFill;
    TMP_Text readout,notice,attempts;RectTransform reticle;Button open,devThrow;string screen;bool dev;
    readonly Color ink=new Color(.035f,.08f,.105f),card=new Color(.07f,.15f,.18f,.96f),teal=new Color(.08f,.56f,.43f),white=new Color(.94f,.97f,.91f),muted=new Color(.63f,.77f,.76f);
    TMP_FontAsset font;
    void Ensure(){
        if(canvas!=null)return;font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var c=new GameObject("Animal Hunt Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas=c.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scale=c.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1080,1920);scale.matchWidthOrHeight=.5f;
        safe=Rect("Safe area",c.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        body=Rect("Screen",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var fx=Rect("Feedback flash",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);flash=fx.gameObject.AddComponent<Image>();flash.color=Color.clear;flash.raycastTarget=false;
    }
    RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    RectTransform Area(string name,float y,float h,float width=944,float x=0){return Rect(name,body,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(x,-y-h/2),new Vector2(width,h));}
    TMP_Text Label(string name,string value,float y,float h=80,int size=36,Color? color=null){var r=Area(name,y,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=color??white;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;t.alignment=TextAlignmentOptions.TopLeft;return t;}
    Button Action(string label,float y,UnityEngine.Events.UnityAction action,float h=100){var r=Area(label,y,h);var image=r.gameObject.AddComponent<Image>();image.color=teal;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);
        var t=Rect("Label",r,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-24,-12)).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=label;t.fontSize=34;t.color=white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return b;}
    void Begin(string name,bool transparent=false){
        Ensure();screen=name;eggText=null;eggBar=null;eggCollect=eggHatch=null;eggRevision=-1;readout=notice=attempts=null;reticle=null;open=devThrow=null;
        foreach(Transform child in body)Destroy(child.gameObject);
        var bg=body.GetComponent<Image>()??body.gameObject.AddComponent<Image>();bg.color=transparent?Color.clear:ink;bg.raycastTarget=!transparent;
    }
    void Header(string title,string subtitle){Label("Brand","AR ANIMAL HUNT",50,70,28,muted);Label("Title",title,145,150,64);Label("Subtitle",subtitle,310,130,32,muted);}
    void Footer(){Label("Footer",game.localPreview?"LOCAL TEST / no ARCore required":"GPS + AR / student demo",1740,90,26,muted);}
    void Icon(float y,float size=260){var r=Area("Fox artwork",y,size,size);r.gameObject.AddComponent<FoxArtwork>().raycastTarget=false;artwork=r;artStart=Time.unscaledTime;}
    public void ShowSplash(){Begin("splash");Header("AR Animal Hunt","Explore your world. Meet wildlife.");Icon(650,400);}
    public void ShowMenu(){Begin("menu");Header("Explore. Aim. Catch.","A wildlife adventure built with GPS and AR.");Icon(470,270);Action("Play",830,game.Play);Action("My Animals",960,game.ShowCollection);Action("My Egg",1090,game.ShowMyEgg);Action("Settings",1220,game.ShowSettings);Action("Quit",1350,game.Quit);Label("Save warning",game.Save.Warning,1480,150,28,muted);Footer();}
    public void ShowExplore(){Begin("explore");Header("Wildlife Explorer","Find a wildlife zone within 50 metres.");
        readout=Label("GPS status","Waiting for location...",440,420,31);
        notice=Label("Guidance","",875,120,30);EggWidget(1010,150);
        Action("My Egg / Walk & Hatch",1190,game.ShowMyEgg,75);open=Action("Open Camera",1280,game.OpenCamera);
        Action("Retry GPS / refresh exploration",1410,game.StartExploring);Action("Main Menu",1540,game.ReturnMenu);
        if(game.Development)Action("Development controls",1670,()=>{dev=!dev;ShowExplore();},70);
        if(dev&&game.Development){
            // A separate opaque panel keeps test controls out of the player interface.
            Begin("dev");Header("Development controls","Simulation is labelled and does not use real GPS.");
            Action("Mode: "+(game.gps.useSimulation?"SIMULATION":"REAL GPS"),480,()=>{game.ChangeSimulation();ShowDevelopment();});
            Action("Inside: 10 m",610,()=>game.gps.SimulateInside());Action("Outside: 100 m",740,()=>game.gps.SimulateOutside());Action("Edge: 60 m",870,()=>game.gps.SimulateEdge());Action("Poor GPS accuracy",1000,()=>game.gps.SimulatePoorGPS());
            Action("Set test zone here (real GPS)",1130,()=>game.gps.SetTestZoneHere());readout=Label("GPS status","",1270,350,27);Action("Back to exploration",1640,()=>{dev=false;ShowExplore();});
        }Footer();Refresh();
    }
    void ShowDevelopment(){dev=true;ShowExplore();}
    public void ShowEncounter(){Begin("encounter",true);
        var strip=Area("Encounter header",30,255);strip.gameObject.AddComponent<Image>().color=card;
        Label("Encounter",game.localPreview?"SHAKE & CATCH / LOCAL TEST":"SHAKE & CATCH / AR",55,70,30);attempts=Label("Attempts","",135,85,42);
        var cross=Label("Aim reticle","+",790,100,64);cross.alignment=TextAlignmentOptions.Center;reticle=cross.rectTransform;reticle.anchorMin=reticle.anchorMax=new Vector2(.5f,.5f);reticle.sizeDelta=new Vector2(100,100);
        var bottom=Area("Instructions panel",1260,450);bottom.gameObject.AddComponent<Image>().color=card;
        notice=Label("Encounter guidance","",1300,240,33);
        if(game.localPreview&&game.State==HuntState.Scanning)Action("Place fox (local test)",1540,game.PlaceLocal);
        else if(game.Development){devThrow=Action("DEV: throw ball",1540,game.ThrowBall);}
        Action("Back",1740,game.Back,80);Refresh();
    }
    public void ShowResult(){Begin("result");Header(game.State==HuntState.Captured?"Caught!":"Escaped",game.ResultText);Icon(540,330);
        Label("XP","Total XP: "+game.Save.Data.xp,930,100,46);Label("Save warning",game.Save.Warning,1050,180,28,muted);
        Action("View Collection",1260,game.ShowCollection);Action("Continue Exploring",1390,game.ContinueExploring);Action("Main Menu",1520,game.ReturnMenu);Footer();}
    public void ShowCollection(){Begin("collection");Header("My Animals","Your discoveries are saved on this phone.");Icon(480,300);
        Label("Species","FOX",840,100,58);Label("Collection totals","Fox captured: "+game.Save.Data.CountAnimal("fox")+"\nBaby Fox hatched: "+game.Save.Data.CountAnimal("baby_fox")+"\n\nTotal animals: "+game.Save.Data.TotalAnimals+"\nTotal XP: "+game.Save.Data.xp,980,350,42);
        Action("Back",1490,game.CloseOverlay);Footer();}
    void EggWidget(float y,float h){
        var panel=Area("Egg progress panel",y,h);panel.gameObject.AddComponent<Image>().color=card;
        eggText=Label("Egg progress","",y+10,h-45,29);
        var track=Area("Egg progress bar",y+h-25,16);track.gameObject.AddComponent<Image>().color=new Color(.15f,.28f,.29f);
        eggBar=track.gameObject.AddComponent<Slider>();eggBar.interactable=false;eggBar.minValue=0;eggBar.maxValue=1;eggBar.transition=Selectable.Transition.None;
        var fill=Rect("Fill",track,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);fill.gameObject.AddComponent<Image>().color=new Color(.3f,.9f,.6f);eggBar.fillRect=fill;
        eggRevision=-1;
    }
    public void ShowEggInventory(){Begin("egg");Header("Walk & Hatch","Collect a Mystery Egg, then walk 100 foreground steps.");
        var art=Area("Mystery Egg artwork",480,230,190);art.gameObject.AddComponent<EggArtwork>().raycastTarget=false;
        EggWidget(760,290);
        eggCollect=Action(game.menuOnly?"Play / return to egg zone":"Collect Egg in AR",1100,game.CollectEgg);
        eggHatch=Action("Hatch",1220,game.HatchEgg);
        Action("Enable step counter",1340,game.RequestSteps,80);
        if(game.Development&&game.Progress!=null){
            if(game.Progress.Eggs.Egg.state==EggState.Discovered)Action("DEV step input: "+(game.Progress.Eggs.Egg.simulated?"SIMULATED":"REAL PEDOMETER"),1440,game.ToggleEggSimulation,80);
            if(game.Progress.Eggs.Egg.simulated&&game.Progress.Eggs.Egg.state==EggState.Incubating){
                Action("DEV: +1 step",1440,()=>game.DeveloperSteps(1),65);Action("DEV: +10 steps",1515,()=>game.DeveloperSteps(10),65);
                Action("DEV: near target / ready",1590,()=>game.DeveloperSteps(game.Progress.Eggs.Egg.steps<game.Progress.Eggs.Egg.target-1?game.Progress.Eggs.Egg.target-1-game.Progress.Eggs.Egg.steps:1),65);
            }
        }
        Action("Back",1710,game.CloseOverlay,80);Refresh();
    }
    public void ShowEggEncounter(){Begin("egg-ar",true);
        var header=Area("Egg AR header",30,215);header.gameObject.AddComponent<Image>().color=card;
        Label("Egg AR title",game.EggHatchMode?"HATCH YOUR EGG":"MYSTERY EGG",60,90,44);
        Label("Egg AR tracking",game.localPreview?"LOCAL PREVIEW - no AR tracking":"AR camera / tap a horizontal surface",155,65,28);
        notice=Label("Egg AR guidance","",1310,230,32);
        if(game.localPreview&&game.State==HuntState.EggScanning)Action("Place egg (local test)",1560,game.EggAR.PlaceLocal);
        if(game.localPreview&&game.State==HuntState.EggCollecting)Action("Collect egg (local test)",1560,()=>game.EggAR.TryCollect());
        Action("Back to My Egg",1730,game.CancelEggFlow,80);Refresh();
    }
    public void ShowEggResult(){Begin("egg-result");Header("Hatched!","Congratulations! You hatched a Baby Fox!");Icon(530,330);
        Label("Baby fox reward","Baby Fox added to My Animals.\nThe incubation slot is free.",960,210,38);
        Action("View Collection",1290,game.ShowCollection);Action("Continue Exploring",1420,game.ContinueExploring);Action("Main Menu",1550,game.ReturnMenu);Footer();}
    void RefreshEgg(){
        if(game.Progress==null)return;
        if(eggCollect!=null)eggCollect.interactable=game.Progress.Eggs.Egg.state==EggState.Discovered&&(game.menuOnly||game.CanCollectEgg);
        if(eggHatch!=null)eggHatch.interactable=game.Progress.Eggs.Egg.state==EggState.ReadyToHatch&&!game.CameraStarting;
        if(eggRevision==game.Progress.Revision)return;eggRevision=game.Progress.Revision;var egg=game.Progress.Eggs.Egg;
        if(eggText!=null){
            eggFill=egg.Percent;
            eggText.text=egg.state==EggState.None?"MY EGG / No egg yet. Enter a wildlife zone.":egg.state==EggState.Discovered?"MYSTERY EGG / Discovered\nReturn to its zone and collect it through the camera.":"MYSTERY EGG / "+(egg.state==EggState.ReadyToHatch?"Ready to Hatch":egg.state==EggState.Hatching?"Hatching":"Incubating")+"\n"+egg.steps+" / "+egg.target+" steps  -  "+Mathf.RoundToInt(egg.Percent*100)+"%";
            if(screen=="egg")eggText.text+="\n"+game.Progress.Steps.Status+(!string.IsNullOrEmpty(game.Save.Warning)?"\n"+game.Save.Warning:"");
        }
    }
    public void ShowSettings(){Begin("settings");Header("Settings","Tune your catching experience.");Action("Sound: "+(game.Save.Data.sound?"ON":"OFF"),540,game.ToggleSound);Action("Vibration: "+(game.Save.Data.vibration?"ON":"OFF"),700,game.ToggleVibration);
        Action("Shake sensitivity: "+new[]{"Low","Normal","High"}[game.Save.Data.sensitivity],860,game.CycleSensitivity);Label("Help","Hold your phone securely. Aim first, then use a deliberate back-and-forth shake.\n\nHigher sensitivity needs less movement.",1050,300,32,muted);Action("Back",1490,game.CloseOverlay);Footer();}
    public void ShowAbandon(){Begin("abandon");Header("Leave this encounter?","Leaving makes the fox escape. You must exit the zone and return for another encounter.");Action("Stay and continue",750,game.CloseOverlay);Action("Leave encounter",910,game.Abandon);}
    public void Refresh(){
        if(reticle!=null){RectTransformUtility.ScreenPointToLocalPointInRectangle(body,new Vector2(Screen.width*.5f,Screen.height*.5f),null,out Vector2 centre);reticle.anchoredPosition=centre;}
        if(game==null||game.Save==null)return;
        RefreshEgg();
        if(readout!=null)readout.text=game.gps.Readout+(game.Consumed?"\n\nZONE COMPLETE: exit beyond 70 m, then return.":game.CanOpen?"\n\nA wild fox is nearby!":"");
        if(open!=null)open.interactable=game.CanOpen&&!game.CameraStarting;
        if(attempts!=null)attempts.text="Attempts remaining: "+game.Round.Attempts+" / 3";
        if(devThrow!=null)devThrow.interactable=game.Ready;
        if(notice!=null){notice.text=game.Notice;
            if(screen=="explore"&&game.Progress!=null&&!string.IsNullOrEmpty(game.Progress.EggNotice))notice.text+="\n"+game.Progress.EggNotice;
            if(screen=="egg-ar")notice.text=game.Tracking?game.EggAR.Message:"Tracking lost. Scan the surface again; your egg is preserved.";
            if(screen=="encounter")notice.text=game.State==HuntState.Scanning?game.Notice:!game.Tracking?"Tracking lost. Scan the surface again.":game.State==HuntState.BallInFlight?"Ball in flight":game.Notice+"\n"+(!game.HasSensor?"No accelerometer detected. Development throw is available for testing.":!game.Calibrated?"Hold still for one-second calibration.":(game.localPreview?"Drag the scene to aim; then shake.":"Accelerometer ready. Aim and shake."));
        }
    }
    void Update(){if(eggBar!=null)eggBar.SetValueWithoutNotify(Mathf.MoveTowards(eggBar.value,eggFill,Time.unscaledDeltaTime*1.5f));if(artwork!=null)artwork.localScale=Vector3.one*Mathf.Lerp(.8f,1,Mathf.SmoothStep(0,1,(Time.unscaledTime-artStart)/.4f));if(safe!=null&&Screen.width>0&&Screen.height>0){var r=Screen.safeArea;safe.anchorMin=new Vector2(r.xMin/Screen.width,r.yMin/Screen.height);safe.anchorMax=new Vector2(r.xMax/Screen.width,r.yMax/Screen.height);}Refresh();}
    public void SetFlash(float alpha,bool success){if(flash!=null)flash.color=new Color(success?.2f:1,success?1:.6f,success?.5f:.2f,Mathf.Clamp01(alpha));}
    void OnDestroy(){if(canvas!=null){if(Application.isPlaying)Destroy(canvas.gameObject);else DestroyImmediate(canvas.gameObject);}}
}
[RequireComponent(typeof(CanvasRenderer))]
public class FoxArtwork:Graphic {
    protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();
        Poly(vh,new[]{new Vector2(-.43f,.5f),new Vector2(-.37f,-.1f),new Vector2(.37f,-.1f),new Vector2(.43f,.5f),new Vector2(.15f,.3f),new Vector2(-.15f,.3f)},new Color(1,.42f,.12f));
        Poly(vh,new[]{new Vector2(-.43f,.12f),new Vector2(0,-.45f),new Vector2(.43f,.12f),new Vector2(0,.3f)},new Color(.96f,.32f,.08f));
        Poly(vh,new[]{new Vector2(-.4f,.03f),new Vector2(0,-.45f),new Vector2(-.06f,-.08f)},new Color(1,.94f,.77f));
        Poly(vh,new[]{new Vector2(.4f,.03f),new Vector2(0,-.45f),new Vector2(.06f,-.08f)},new Color(1,.94f,.77f));
        Poly(vh,new[]{new Vector2(-.08f,-.25f),new Vector2(0,-.35f),new Vector2(.08f,-.25f)},Color.black);
        Eye(vh,-.19f,.04f);Eye(vh,.19f,.04f);
    }
    void Eye(VertexHelper vh,float x,float y){Poly(vh,new[]{new Vector2(x-.025f,y-.035f),new Vector2(x+.025f,y-.035f),new Vector2(x+.025f,y+.035f),new Vector2(x-.025f,y+.035f)},Color.black);}
    void Poly(VertexHelper vh,Vector2[] points,Color colour){int start=vh.currentVertCount;var r=rectTransform.rect;foreach(var p in points)vh.AddVert(new Vector3(r.center.x+p.x*r.width,r.center.y+p.y*r.height),colour,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
}


[RequireComponent(typeof(CanvasRenderer))]
public class EggArtwork:Graphic {
    protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;vh.AddVert(r.center,new Color(.98f,.95f,.77f),Vector2.zero);for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;float y=Mathf.Sin(a);vh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width*.48f*(1-y*.15f),y*r.height*.48f),i%7<3?new Color(.58f,.9f,.73f):new Color(.98f,.95f,.77f),Vector2.zero);if(i>0)vh.AddTriangle(0,i,i+1);}}
}
