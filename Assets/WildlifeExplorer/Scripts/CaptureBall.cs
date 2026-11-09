using System;
using UnityEngine;
public class CaptureBall:MonoBehaviour {
    public Action<bool> Resolved;
    bool done;float deadline;
    public void Launch(Vector3 velocity){deadline=Time.unscaledTime+4;GetComponent<Rigidbody>().linearVelocity=velocity;}
    void Update(){if(!done&&deadline>0&&Time.unscaledTime>=deadline)Finish(false);}
    void OnCollisionEnter(Collision collision){
        if(done)return;var animal=collision.collider.GetComponentInParent<ExplorerAnimal>();
        bool success=animal!=null&&animal.TryCapture();Finish(success);
    }
    public void Cancel(){if(done)return;done=true;Resolved=null;Destroy(gameObject);}
    void Finish(bool hit){if(done)return;done=true;var callback=Resolved;Resolved=null;callback?.Invoke(hit);Destroy(gameObject);}
}
