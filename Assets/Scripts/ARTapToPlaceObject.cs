using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

[RequireComponent(typeof(ARRaycastManager))]
public class ARTapToPlaceObject : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Camera arCamera;
    public GameObject patchGO;
    public Vector3[] textureScreenVertices = new Vector3[4];
    private GameObject patch;
    private ARRaycastManager aRRaycastManager;
    private Vector2 touchPosition;
    // private System.DateTime startTime;
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private void Awake()
    {
        aRRaycastManager = GetComponent<ARRaycastManager>();
        canvas.gameObject.SetActive(false);
        gameObject.GetComponent<CameraImageExample>().enabled = false;
    }

    bool TryGetTouchPosition(out Vector2 touchPosition){
        // System.TimeSpan ts = System.DateTime.UtcNow - startTime;
        if(Input.touchCount==1){
            touchPosition = Input.GetTouch(0).position;
            
            RaycastHit hit;
            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            if (Physics.Raycast(ray, out hit)&&hit.collider != null&&(hit.collider.gameObject.name.StartsWith("Patch")||hit.collider.gameObject.name.StartsWith("PreviewTexture"))) {
                touchPosition  = default;
                return false;                            
            }
            return true;
        }
        touchPosition  = default;
        return false;
    }

    void Update()
    {
        if(!TryGetTouchPosition(out Vector2 touchPosition)){
            return;
        }

        if(aRRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon)){
            var hitPose = hits[0].pose;   
            // System.TimeSpan ts = System.DateTime.UtcNow - startTime;

            if(patch==null) {
                patch = Instantiate(patchGO, hitPose.position, hitPose.rotation);
                canvas.gameObject.SetActive(true);
                canvas.GetComponentInChildren<RawImage>().enabled = false;
                canvas.GetComponentInChildren<Button>().enabled = true;
            }
            
        }      

    }

    public void scanTexture(){
        Matrix4x4 localToWorld = patch.transform.localToWorldMatrix;
        MeshFilter mf = patch.GetComponent<MeshFilter>();
        for(int i = 12; i<16; ++i) {
            Vector3 world_v = localToWorld.MultiplyPoint3x4(mf.mesh.vertices[i]);
            Vector3 screenPos = gameObject.GetComponentInChildren<Camera>().WorldToScreenPoint(world_v);
            textureScreenVertices[i-12] = screenPos;
        }
        
        Destroy(patch);
        canvas.GetComponentInChildren<Button>().enabled = false;
        canvas.GetComponentInChildren<RawImage>().enabled = true;
        gameObject.GetComponent<CameraImageExample>().enabled = true;   
    }

}
