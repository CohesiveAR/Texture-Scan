using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine.Networking;

public class CameraImageExample : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager m_CameraManager;
    [SerializeField]
    RawImage rawImage;
    bool init = false;
    ARPlaneManager planeManager;
     /// <summary>
    /// Get or set the <c>ARCameraManager</c>.
    /// </summary>
    public ARCameraManager cameraManager
    {
        get { return m_CameraManager; }
        set { m_CameraManager = value; }
    }

    
    void OnEnable()
    {
        if (m_CameraManager != null)
        {
            planeManager = gameObject.GetComponent<ARPlaneManager>();
            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }
 unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // if (!init)
        // {   
        //     for(int i=0;i<cameraManager.GetConfigurations(Allocator.Temp).Length;i++)
        //     cameraManager.subsystem.currentConfiguration = cameraManager.GetConfigurations(Allocator.Temp)[1]; //In my case 0=640*480, 1= 1280*720, 2=1920*1080
        //     init = true;
        // }

        // if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        //     return;
        // var conversionParams = new XRCpuImage.ConversionParams
        // {
        //     // Get the entire image.
        //     inputRect = new RectInt(0, 0, image.width, image.height),

        //     // not Downsample by 2. 
        //     outputDimensions = new Vector2Int(image.width, image.height)/2,

        //     // Choose RGBA format.
        //     outputFormat = TextureFormat.RGBA32,

        //     // Flip across the vertical axis (mirror image).
        //     transformation = XRCpuImage.Transformation.MirrorX
        // };

        // // See how many bytes you need to store the final image.
        // int size = image.GetConvertedDataSize(conversionParams);

        // // Allocate a buffer to store the image.
        // var buffer = new NativeArray<byte>(size, Allocator.Temp);

        // // Extract the image data
        // image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

        // // The image was converted to RGBA32 format and written into the provided buffer
        // // so you can dispose of the XRCpuImage. You must do this or it will leak resources.
        // image.Dispose();

        // // At this point, you can process the image, pass it to a computer vision algorithm, etc.
        // // In this example, you apply it to a texture to visualize it.

        // // You've got the data; let's put it into a texture so you can visualize it.
        // m_Texture = new Texture2D(
        //     conversionParams.outputDimensions.x,
        //     conversionParams.outputDimensions.y,
        //     conversionParams.outputFormat,
        //     false);

        // m_Texture.LoadRawTextureData(buffer);
        // m_Texture.Apply();
        StartCoroutine(CoroutineScreenshot());
        Debug.Log("started ss");
    }
    private IEnumerator CoroutineScreenshot() {
        //Remove UI from screenshot
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }
        rawImage.gameObject.transform.parent.gameObject.SetActive(false);//canvas
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;
        Texture2D screenshotTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        UnityEngine.Rect rect = new UnityEngine.Rect(0, 0, width, height);
        screenshotTexture.ReadPixels(rect, 0, 0);
        screenshotTexture.Apply();    
        
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }        
        rawImage.gameObject.transform.parent.gameObject.SetActive(true);//canvas

        StartCoroutine(perspectiveWarpApply(screenshotTexture));
        this.enabled = false;          
    }
    IEnumerator perspectiveWarpApply(Texture2D originalTexture){      
        int width = 400;
        int height = 400;

        Mat img = Mat.zeros(new Size(originalTexture.width,originalTexture.height),CvType.CV_8UC3);  
        Utils.texture2DToMat(originalTexture, img);

        MatOfPoint2f  src_mat = pointsInResolution(originalTexture.width, originalTexture.height,Screen.width, Screen.height);
        //MatOfPoint2f  dst_mat = new MatOfPoint2f(new Point(width,0), new Point(width,height), new Point(0,height),new Point(0,0));
        MatOfPoint2f  dst_mat = new MatOfPoint2f(new Point(0,0), new Point(width,0), new Point(0,height), new Point(width,height));
        
        Mat outputMat = Mat.zeros(new Size(width,height),CvType.CV_8UC3);        
        Mat matrix = Imgproc.getPerspectiveTransform(src_mat, dst_mat);
        
        Imgproc.warpPerspective(img, outputMat, matrix, new Size(width,height));
        Texture2D outputTexture = new Texture2D(outputMat.cols(), outputMat.rows(), TextureFormat.RGBA32, false);

        Utils.matToTexture2D(outputMat, outputTexture);
        rawImage.texture = outputTexture;    

        yield return null;
    }
    

    MatOfPoint2f pointsInResolution(int srcW=1080, int srcH=2340, int dstW=360, int dstH=640){
        Vector3[] inVertices = gameObject.GetComponent<ARTapToPlaceObject>().textureScreenVertices;
        Point[] outPoints = new Point[4];
        
        int[] order = {2,1,3,0};
        int j=0;
        foreach(int i in order){
            var v =  inVertices[i]; 

            float x = v.x*dstW/srcW;
            if(x<0) x=0;
            if(x>dstW) x=dstW-1;
            float y = dstH - (v.y*dstH/srcH);
            if(y<0) y=0;
            if(y>dstH) x=dstH-1;
            outPoints[j++] = new Point(x,y);
        }
        
        return new MatOfPoint2f(outPoints);
    }
    Texture2D m_Texture;
}