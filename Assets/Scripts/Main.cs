using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.SceneManagement;


public class Main : MonoBehaviour
{
    public Texture2D blankBG;
    public Texture2D cityBG;
    public Texture2D overlay;

    private GameObject screenCam;
    private GameObject renderCam;    

    private GameObject mergeLayer;//the combined paint layer
    private GameObject paintLayer;//has the render texture
    private GameObject outlineLayer; //the outline layer    

    private Layers layers;//ref to the layers class attached to Main go

    private GameObject hitParent;//used for scaling the hit mesh to prevent edge stoppage

    private FileScript fileScript; //for reading the screenLoc.txt file

    private Vector3 midPoint;//calculated midpoint of the mesh

    public RenderTexture renderTex;

    //keeps track of app state - used by ControllerInput
    // 0:intro playing, 1:canvas selection, 2:painting allowed, -1:not accepting input
    private int appMode;
    
    private VideoPlayer introVideo;

    //UI
    private GameObject screenCanvases;
    private GameObject screenTools;
    private GameObject screenQR;
    private GameObject endTransition;

    private string currentCanvas; //name of the selected canvas
    private string GUID;


    private int brushCount;//for sorting the brush sprites
    private string lastHoverName;

    private Sprite[] rollovers;

    private GameObject instructionsCanvas;



    void Start()
    {
        Cursor.visible = false;

        screenCam = GameObject.Find("screenCam");
        renderCam = GameObject.Find("renderCam");

        mergeLayer = GameObject.Find("mergeLayer");//mergeMesh
        paintLayer = GameObject.Find("paintLayer");//paintMesh built in here
        outlineLayer = GameObject.Find("outlineLayer");//outlineMesh built in here

        instructionsCanvas = GameObject.Find("instCanvas");

        layers = GetComponent<Layers>();

        hitParent = new GameObject("hitParent");

        fileScript = GetComponent<FileScript>();

        introVideo = GameObject.Find("screenCam").GetComponent<VideoPlayer>();

        screenCanvases = GameObject.Find("screenCanvases");
        screenTools = GameObject.Find("toolKit");
        screenQR = GameObject.Find("QRHolder");
        endTransition = GameObject.Find("endTran");

        currentCanvas = "";
        appMode = -1; //no inout until isntructions are finished
        //introVideo.Play();

        screenTools.SetActive(false);
        screenQR.SetActive(false);
        screenCanvases.SetActive(false);
        endTransition.GetComponent<Animator>().enabled = false;
        endTransition.SetActive(false);

        brushCount = 0;
        lastHoverName = "";

        rollovers = new Sprite[9];
        rollovers[0] = Resources.Load<Sprite>("can0_color");
        rollovers[1] = Resources.Load<Sprite>("can1_color");
        rollovers[2] = Resources.Load<Sprite>("can2_color");
        rollovers[3] = Resources.Load<Sprite>("can3_color");
        rollovers[4] = Resources.Load<Sprite>("can4_color");
        rollovers[5] = Resources.Load<Sprite>("can5_color");
        rollovers[6] = Resources.Load<Sprite>("can6_color");
        rollovers[7] = Resources.Load<Sprite>("can7_color");
        rollovers[8] = Resources.Load<Sprite>("can3_color_sp");

        //Show instructions screen before video
        CanvasGroup inst = GameObject.Find("instCanvas").GetComponent<CanvasGroup>();
        LeanTween.value(hitParent, fillInstructions, 0f, 1f, 10f);
        LeanTween.alphaCanvas(inst, 0, 1).setDelay(10f).setOnComplete(playVid);
    }
    

    void playVid()
    {
        instructionsCanvas.SetActive(false);
        appMode = 0; //intro playing
        introVideo.Play();
        Invoke("loadConfig", 1);
    }


    //called 1sec after playVid() to allow video to start playing before building the meshes
    void loadConfig()
    { 
        Vector3[] points = fileScript.readConfigFile();
       buildScreen(points);
    }


    void fillInstructions(float val)
    {
        GameObject.Find("instCircle").GetComponent<Image>().fillAmount = val;
    }
    
    //Called from ControllerInput.Update()
    public int mode
    {
        get
        {
            return appMode;
        }
    }


    /**
     * Called from controllers whenever a controller ray is intersecting the main mesh
     * it's assumed if the controller is being held in front of the monitor - ie the ray is intersecting the mesh
     * that the app is being used - if controller is put down ray will no longer intersect
     * resets the timer in the fileScript to prevent a timeout - app reset
     */
    public void userMove()
    {
        fileScript.resetTime();
    }


    //called by ControllerInput to get sort order for the brush sprites
    public int sortIndex()
    {
        brushCount++;
       
        if(brushCount > 1200)
        {
            mergeTex();
            brushCount = 0;
        }

       return brushCount;        
    }


    //called from ControllerInput when trigger down on intro - appMode = 0
    public void introComplete()
    {
        screenTools.SetActive(false);
        screenQR.SetActive(false);
        screenCanvases.SetActive(true);

        if (fileScript.spanish)
        {
            GameObject.Find("can3").GetComponent<Image>().sprite = Resources.Load<Sprite>("canvas_4_blank_sp");
        }
        else
        {
            GameObject.Find("can3").GetComponent<Image>().sprite = Resources.Load<Sprite>("canvas_4_blank");
        }

        appMode = 1;//canvas selection
        introVideo.Stop();
    }


    public void canvasHover(string canvasName)
    {
        if(lastHoverName != canvasName)
        {          
            switch (canvasName)
            {
                case "can0":
                    GameObject.Find("can0").GetComponent<Image>().overrideSprite = rollovers[0];
                    break;
                case "can1":
                    GameObject.Find("can1").GetComponent<Image>().overrideSprite = rollovers[1];
                    break;
                case "can2":
                    GameObject.Find("can2").GetComponent<Image>().overrideSprite = rollovers[2];
                    break;
                case "can3":
                    if (fileScript.spanish)
                    {
                        GameObject.Find("can3").GetComponent<Image>().overrideSprite = rollovers[8];
                    }
                    else
                    {
                        GameObject.Find("can3").GetComponent<Image>().overrideSprite = rollovers[3];
                    }                    
                    break;
                case "can4":
                    GameObject.Find("can4").GetComponent<Image>().overrideSprite = rollovers[4];
                    break;
                case "can5":
                    GameObject.Find("can5").GetComponent<Image>().overrideSprite = rollovers[5];
                    break;
                case "can6":
                    GameObject.Find("can6").GetComponent<Image>().overrideSprite = rollovers[6];
                    break;
                case "can7":
                    GameObject.Find("can7").GetComponent<Image>().overrideSprite = rollovers[7];
                    break;
            }

            lastHoverName = canvasName;
        }        
    }


    public void noHover()
    {
        //revert to originals
        for(int i = 0; i < 9; i++)
        {
            GameObject.Find("can" + i.ToString()).GetComponent<Image>().overrideSprite = null;
        }
        lastHoverName = "";
    }


    //called from controllerInput when a canvas has been selected
    public void canvasSelect(string canvasName)
    {
        appMode = -1;//don't allow control input while this happens

        screenTools.SetActive(true);
        screenCanvases.SetActive(false);

        //Blank Canvas
        Texture2D blank = Instantiate(blankBG) as Texture2D;

        //is a new canvas selected?
        if (currentCanvas != canvasName)
        {
            doTrash();

            if (canvasName == "can8")
            {
                //blank canvas - hide outline
                outlineLayer.SetActive(false);
                //outlineLayer.GetComponent<Renderer>().material.mainTexture = blankTex;
                mergeLayer.GetComponent<Renderer>().material.mainTexture = blank;// Resources.Load("BGRegular") as Texture;

                mergeLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 1f, 1f, 1));
                paintLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 1f, 1f, 1));
            }
            else if (canvasName == "can4")
            {
                outlineLayer.SetActive(true);
                outlineLayer.GetComponent<Renderer>().material.mainTexture = Resources.Load(canvasName) as Texture;

                //Blank Canvas - City
                Texture2D city = Instantiate(cityBG) as Texture2D;
                mergeLayer.GetComponent<Renderer>().material.mainTexture = city;
                //slightly darken the material so the city behind the glass doesn't look too bright
                mergeLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(.815686f, .815686f, .815686f, 1));
                paintLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(.815686f, .815686f, .815686f, 1));
            }
            else if(canvasName == "can3" && fileScript.spanish)
            {                
                outlineLayer.SetActive(true);
                outlineLayer.GetComponent<Renderer>().material.mainTexture = Resources.Load("can8") as Texture;
                mergeLayer.GetComponent<Renderer>().material.mainTexture = blank;// Resources.Load("BGRegular") as Texture;

                mergeLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 1f, 1f, 1));
                paintLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 1f, 1f, 1));                
            }
            else
            {
                outlineLayer.SetActive(true);
                outlineLayer.GetComponent<Renderer>().material.mainTexture = Resources.Load(canvasName) as Texture;
                mergeLayer.GetComponent<Renderer>().material.mainTexture = blank;// Resources.Load("BGRegular") as Texture;

                mergeLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 1f, 1f, 1));
                paintLayer.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 1f, 1f, 1));
            }

            currentCanvas = canvasName;
        }

        Invoke("doModeTwo", 1);
    }

    void doModeTwo()
    {
        appMode = 2;
    }
 

    //Called from ControllerInput if the canvases selection button is clicked
    public void showCanvases()
    {
        screenTools.SetActive(false);
        screenCanvases.SetActive(true);
        appMode = 1; //canvas select - will call canvasSelect with the canvas number
    }


    /**
     * Builds the screen mesh given the four corner points 
     */
    private void buildScreen(Vector3[] clickPoints)
    {
        Mesh mesh = new Mesh();
        Mesh mergeMesh = new Mesh();
        Mesh paintMesh = new Mesh();
        Mesh outlineMesh = new Mesh();

        GetComponent<MeshFilter>().mesh = mesh;
        mergeLayer.GetComponent<MeshFilter>().mesh = mergeMesh;
        paintLayer.GetComponent<MeshFilter>().mesh = paintMesh;
        outlineLayer.GetComponent<MeshFilter>().mesh = outlineMesh;

        //clickPoints has 5 Vector3's - last one is UI scale
        Vector3[] verts = new Vector3[4];
        verts[0] = clickPoints[0];
        verts[1] = clickPoints[1];
        verts[2] = clickPoints[2];
        verts[3] = clickPoints[3];

        mesh.vertices = verts;
        mergeMesh.vertices = verts;
        paintMesh.vertices = verts;
        outlineMesh.vertices = verts;

        mesh.triangles = new int[] { 0, 1, 3, 1, 2, 3 };
        mergeMesh.triangles = new int[] { 0, 1, 3, 1, 2, 3 };
        paintMesh.triangles = new int[] { 0, 1, 3, 1, 2, 3 };
        outlineMesh.triangles = new int[] { 0, 1, 3, 1, 2, 3 };

        mesh.uv = new Vector2[4] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        mergeMesh.uv = new Vector2[4] { new Vector2(0,1), new Vector2(1,1), new Vector2(1,0), new Vector2(0,0) };
        paintMesh.uv = new Vector2[4] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) };
        outlineMesh.uv = new Vector2[4] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) };

        //this is only needed to catch the ray on the expanded base/background mesh
        GetComponent<MeshCollider>().sharedMesh = mesh;
       
        //use topleft and topRight corners to calculate the mid point vector
        Vector3 topMid = verts[0] + ((verts[1] - verts[0]) * .5f);

        //and then move down to midway between topRight and bottomRight 
        midPoint = topMid + ((verts[2] - verts[1]) * .5f);        

        //calculate normal vector to the surface
        Vector3 c1 = verts[1] - verts[0];
        Vector3 c2 = verts[2] - verts[0];
        Vector3 norml = Vector3.Cross(c1, c2).normalized;

        //calculate the aspect ratio based on the rect size
        float wide = Vector3.Distance(verts[0], verts[1]);
        float high = Vector3.Distance(verts[1], verts[2]);
        float asp = wide / high;

        screenCam.GetComponent<Camera>().aspect = asp;
        renderCam.GetComponent<Camera>().aspect = asp;

        //set the forward vector in layers - for layer offsets
        layers.forward = norml;

        //Move camera away from surface by some arbitrary amount...
        Vector3 result = midPoint + layers.forward * 3f;        
        screenCam.transform.position = result;
        renderCam.transform.position = result;

        //Point the camera at the center screen
        screenCam.transform.LookAt(midPoint);
        renderCam.transform.LookAt(midPoint);

        //Calculate required FOV to make the mesh full screen
        Vector3 from = (result - midPoint).normalized;
        Vector3 to = (result - topMid).normalized;
        float ang = Vector3.Angle(from, to);

        screenCam.GetComponent<Camera>().fieldOfView = ang * 2;
        renderCam.GetComponent<Camera>().fieldOfView = ang * 2;

        //offset the layers - these are just the forward/mesh normal
        //vector multiplied by a small scalar
        //transform.position += layers.cursor;
        mergeLayer.transform.position += layers.merge;
        paintLayer.transform.position += layers.paint;
        outlineLayer.transform.position += layers.outline;

        //scale the hit layer so we don't have edge stoppage
        //use hitParent to scale from center   
        hitParent.transform.position = midPoint;
        hitParent.transform.LookAt(result);
        transform.parent = hitParent.transform;
        hitParent.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);  
       
        //UI
        GameObject t = GameObject.Find("UICanvas");
        Vector3 uip = midPoint + layers.ui;        
        t.transform.position = uip;
        t.transform.rotation = Quaternion.LookRotation(-layers.forward);
        t.transform.localScale = clickPoints[4];
    }


    public Vector3 mid
    {
        get
        {
            return midPoint;
        }
    }


    //called from ControllerInput when the share button is clicked
    public void saveImage()
    {
        appMode = 3;//stops painting

        //hide cursors
        GameObject.Find("redRing").SetActive(false);
        GameObject.Find("redRingLeft").SetActive(false);

        //generates the QRCode, and saves the image
        //StartCoroutine(takeScreenshot(showEndTransition));
        showEndTransition();

        screenQR.SetActive(false);
        screenTools.SetActive(false);
        screenCanvases.SetActive(false);
    }


    IEnumerator takeScreenshot(Action callback)
    {
        // We should only read the screen buffer after rendering is complete
        yield return new WaitForEndOfFrame();

        GUID = System.Guid.NewGuid().ToString();

        int width = Screen.width;
        int height = Screen.height;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);//read from screen buffer

        //Need to apply the overlay
        Texture2D over = Instantiate(overlay) as Texture2D;

        //they should be the same size - but in case they are not
        if(over.width != width)
        {
            over = scaleTexture(over, width, height);
        }

        Color[] topData = over.GetPixels();
        Color[] bottomData = screenShot.GetPixels();

        int c = bottomData.Length;

        for (int i = 0; i < c; i++)
        {
            float rOut = (topData[i].r * topData[i].a) + (bottomData[i].r * (1 - topData[i].a));
            float gOut = (topData[i].g * topData[i].a) + (bottomData[i].g * (1 - topData[i].a));
            float bOut = (topData[i].b * topData[i].a) + (bottomData[i].b * (1 - topData[i].a));
            float aOut = topData[i].a + (bottomData[i].a * (1 - topData[i].a));

            bottomData[i] = new Color(rOut, gOut, bOut, aOut);
        }

        screenShot.SetPixels(bottomData);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + "/" + GUID + ".png", bytes);

        if (callback != null) callback();//call showEndTransition
    }


    /**
     * Scales the source texture to the target width x height
     */
    private Texture2D scaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
       
        for (int i = 0; i < result.height; ++i)
        {
            for (int j = 0; j < result.width; ++j)
            {
                Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }
        result.Apply();
        return result;
    }


    //callback from takeScreenShot
    void showEndTransition()
    {
        endTransition.SetActive(true);
        endTransition.GetComponent<Animator>().enabled = true;

        //THE LAB - don't show the qr code - just restart
        //Invoke("showQR", 1);//let anim finish
        Invoke("labRestart", 1);
    }


    void labRestart()
    {
        SceneManager.LoadScene(0);//this was in FileScript
    }


    void showQR()
    { 
        screenQR.SetActive(true);
        screenQR.GetComponent<CanvasGroup>().alpha = 0f;
        LeanTween.alphaCanvas(screenQR.GetComponent<CanvasGroup>(), 1f, 2f);

        //generates a 740x740 image
        Texture2D qrTex = QRGenerator.EncodeString(GUID, new Color(.77255f, .09109f, .2f, 1), new Color(1, 1, 1, 1));
        screenQR.GetComponent<RawImage>().texture = qrTex;

        //add to the queue - send to NowPik
        fileScript.addFile(GUID);
    }



    /*
     * Merges the active render texture into the current merge layer and then removes
     * the brush sprites. Allows painting to continue forever
     */
    private void mergeTex()
    {
        //sets the active renderTex in the system - this is where readPixels reads from
        RenderTexture.active = renderTex;
        int width = renderTex.width;
        int height = renderTex.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);//reads from the active renderTexture
        tex.Apply();
        RenderTexture.active = null;

        //get current texture from the merge layer
        Texture2D pTex = mergeLayer.GetComponent<Renderer>().material.mainTexture as Texture2D;

        if (pTex)
        {
            Color[] topData = tex.GetPixels();//renderTexture - current brush strokes
            Color[] bottomData = pTex.GetPixels();//texture from the merge layer - add a to this

            int c = bottomData.Length;
            
            for (int i = 0; i < c; i++)
            {                
                float rOut = (topData[i].r * topData[i].a) + (bottomData[i].r * (1 - topData[i].a));
                float gOut = (topData[i].g * topData[i].a) + (bottomData[i].g * (1 - topData[i].a));
                float bOut = (topData[i].b * topData[i].a) + (bottomData[i].b * (1 - topData[i].a));
                float aOut = topData[i].a + (bottomData[i].a * (1 - topData[i].a));

                bottomData[i] = new Color(rOut, gOut, bOut, aOut);                
            }
            
            pTex.SetPixels(bottomData);
            pTex.Apply();
            
            mergeLayer.GetComponent<Renderer>().material.mainTexture = pTex;
        }
        else
        {
            //first time merging - just assign the renderTex
            mergeLayer.GetComponent<Renderer>().material.mainTexture = tex;
        }          

        killBrushes();
    }


    //Destroys all the brush sprites from in front of renderCam
    //called from mergeTex() and doTrash()
    public void killBrushes()
    {
        brushCount = 0;

        GameObject[] theBrushes = GameObject.FindGameObjectsWithTag("brush");
        foreach (GameObject aBrush in theBrushes)
        {
            Destroy(aBrush);
        }
    }


    //Called from ControllerInput when the trash icon is clicked
    public void doTrash()
    {
        killBrushes();

        //clear render texture
        GL.Clear(true, true, new Color(0, 0, 0, 0));       

        if (currentCanvas == "can4")
        {
            Texture2D city = Instantiate(cityBG) as Texture2D;
            mergeLayer.GetComponent<Renderer>().material.mainTexture = city;
        }
        else
        {           
            Texture2D blank = Instantiate(blankBG) as Texture2D;
            mergeLayer.GetComponent<Renderer>().material.mainTexture = blank;
        }
    }
}
