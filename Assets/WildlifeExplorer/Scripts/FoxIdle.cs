using UnityEngine;
public class FoxIdle:MonoBehaviour {
    Vector3 initial;float phase;
    void Start(){initial=transform.localScale;phase=Random.value*6;}
    void Update(){float pulse=1+Mathf.Sin(Time.time*2+phase)*.018f;transform.localScale=new Vector3(initial.x,initial.y*pulse,initial.z);}
}
