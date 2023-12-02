using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using UnityEngine.Windows.WebCam;
using System.Collections.Generic;

public class TestClick : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;

    public GameObject PhotoPrefab;

    Resolution cameraResolution;

    float ratio = 1.0f;

    AudioSource shutterSound;

    // Use this for initialization
    void Start()
    {
        shutterSound = GetComponent<AudioSource>() as AudioSource;

        //Debug.Log("File path " + Application.persistentDataPath);

        cameraResolution = PhotoCapture
            .SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();


        ratio = (float)cameraResolution.height / (float)cameraResolution.width;


        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
            photoCaptureObject = captureObject;
            Debug.Log("camera ready to take picture");
        });
    }

    public void StopCamera()
    {
        // Deactivate our camera

        photoCaptureObject?.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    public void TakePicture()
    {
        TCPServerConnection.instance.PauseServer();

        StartCoroutine(Wait());
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(1f);

        CameraParameters cameraParameters = new CameraParameters();

        cameraParameters.hologramOpacity = 0.0f;

        cameraParameters.cameraResolutionWidth = cameraResolution.width / 5;
        cameraParameters.cameraResolutionHeight = cameraResolution.height / 5;

        cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

        // Activate the camera
        if (photoCaptureObject != null)
        {
            if (shutterSound != null)
            {
                shutterSound.Play();
            }

            photoCaptureObject.StartPhotoModeAsync(cameraParameters,
                delegate (PhotoCapture.PhotoCaptureResult result)
                {
                    Debug.Log("start photo mode aync");
                    // Take a picture
                    photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);

                    TCPServerConnection.instance.ResumeServer();
                });
        }
    }

    public RawImage rawIamge;


    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

            photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);

            Debug.Log(filePath);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("Saved Photo to disk!");

            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
        else
        {
            Debug.Log("Failed to save Photo to disk");
        }
    }

    void OnCapturedPhotoToMemory2(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {

        // Copy the raw image data into our target texture
        var targetTexture = new Texture2D(cameraResolution.width / 5, cameraResolution.height/5);


        photoCaptureFrame.UploadImageDataToTexture(targetTexture);


        rawIamge.texture = targetTexture;

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, 
        PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            Debug.Log("Success to capture");

            //List<byte> imageBufferList = new List<byte>();
            //// Copy the raw IMFMediaBuffer data into our empty byte list.
            //photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            var targetTexture = new Texture2D(cameraResolution.width/5 , cameraResolution.height / 5);


            photoCaptureFrame.UploadImageDataToTexture(targetTexture);

            //byte[] pngBytes = targetTexture.EncodeToPNG();

            // PNG 형식으로 텍스처 인코딩
            byte[] pngBytes = targetTexture.EncodeToPNG();

            // 바이트 배열을 List<byte>로 변환
            List<byte> pngBytesList = new List<byte>(pngBytes);

            //Debug.Log(imageBufferList.Count);

            StartCoroutine(Wait2(pngBytesList));
            // In this example, we captured the image using the BGRA32 format.
            // So our stride will be 4 since we have a byte for each rgba channel.
            // The raw image data will also be flipped so we access our pixel data
            // in the reverse order.

            //int stride = 4;
            //float denominator = 1.0f / 255.0f;

            //List<Color> colorArray = new List<Color>();
            //for (int i = imageBufferList.Count - 1; i >= 0; i -= stride)
            //{
            //    float a = (int)(imageBufferList[i - 0]) * denominator;
            //    float r = (int)(imageBufferList[i - 1]) * denominator;
            //    float g = (int)(imageBufferList[i - 2]) * denominator;
            //    float b = (int)(imageBufferList[i - 3]) * denominator;

            //    colorArray.Add(new Color(r, g, b, a));
            //}

            //// Now we could do something with the array such as texture.SetPixels() or run image processing on the list
        }

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

        
    }

    IEnumerator Wait2(List<byte> imageBufferList)
    {
        yield return new WaitForSeconds(1f);
        

        if (TCPServerConnection.instance.PythonConnected == true)
        {
            TCPServerConnection.instance.
                SendData2PythonClientImageBuffer(imageBufferList);
        }
        
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown our photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ClickTest();
        }
    }

    public void ClickTest()
    {
        Debug.Log("CLick!! by Hololens");

        TakePicture();

    }
}
