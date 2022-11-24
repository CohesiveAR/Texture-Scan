using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewTextureAdd : MonoBehaviour
{
    [SerializeField]
    Camera arCamera;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(gameObject.name);
    }
    bool TryGetTouchPosition(out Vector2 touchPosition){
        // System.TimeSpan ts = System.DateTime.UtcNow - startTime;
        if(Input.touchCount==1){
            touchPosition = Input.GetTouch(0).position;
            
            RaycastHit hit;
            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            if (Physics.Raycast(ray, out hit)&&hit.collider != null&&hit.collider.gameObject.name==gameObject.name) {
                Debug.Log("hit.collider.gameObject.name");
                return true;                            
            }
        }
        touchPosition  = default;
        return false;
    }
    // Update is called once per frame
    void Update()
    {
        if(!TryGetTouchPosition(out Vector2 touchPosition)){
            return;
        }

        gameObject.SetActive(false);
    }
}
