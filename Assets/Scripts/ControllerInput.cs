using UnityEngine;
using UnityEngine.UI;


public class ControllerInput : MonoBehaviour
{
    private SteamVR_Controller.Device controller;
    private SteamVR_TrackedObject trackedObj;   
   
    private Quaternion modAngle;
    private bool isPainting;   //true when trigger is pressed

    public GameObject cursor;   //redRing or redRingLeft

    private Layers layers;

    private RaycastHit mainRay;
    private RaycastHit buttonRay;

    private Color tintColor;

    private Main mainRef;


    void Start()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();        

        layers = GameObject.Find("Main").GetComponent<Layers>();
        isPainting = false;        

        //modifies the forward vector of the controller
        modAngle = Quaternion.Euler(60, 0, 0);

        mainRef = GameObject.Find("Main").GetComponent<Main>();

        cursor.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0);
    }


    void Update()
    {
        controller = SteamVR_Controller.Input((int)trackedObj.index);

        int m = mainRef.mode;

        if (controller == null)
        {
            return;
        }

        //mode 0 is playing intro video
        if (m == 0)
        {            
            //call intro complete with just a trigger down
            if (controller.GetHairTriggerDown())
            {
                mainRef.introComplete();
            }
        }
        else
        {
            Vector3 forward = transform.TransformDirection(modAngle * Vector3.forward);

            Ray theRay = new Ray(transform.position, forward);
            RaycastHit[] hits = Physics.RaycastAll(theRay, 2f);

            //Debug.DrawRay(transform.position, forward);

            //should never be more than two hits - a button and/or the main bg hit mesh
            mainRay = new RaycastHit();
            buttonRay = new RaycastHit();//so the colliders are null

            foreach (RaycastHit r in hits)
            {
                if (r.transform.name == "Main")
                {
                    mainRay = r;                    
                }
                else
                {
                    buttonRay = r;
                }
            }

            if (mainRay.collider != null)
            {
                //prevents timeout reset
                mainRef.userMove();

                //Move the cursor
                float cursorScale = mainRay.distance * .06f;

                if (cursorScale < .005f)
                {
                    cursorScale = .005f;
                    if (isPainting)
                    {
                        isPainting = false;
                    }
                    cursor.SetActive(false);
                }
                else
                {
                    if (!cursor.activeSelf)
                    {
                        cursor.SetActive(true);
                    }
                }
                if (cursorScale > .1f)
                {
                    cursorScale = .1f;
                }

                Vector3 cLoc = mainRay.point;

                cursor.transform.localScale = new Vector3(cursorScale, cursorScale, 1);
                cursor.transform.position = cLoc + layers.cursor;
                cursor.transform.rotation = Quaternion.LookRotation(layers.forward);

                //only paint if appMode is 2
                if (isPainting && m == 2)
                {
                    //trigger press based alpha
                    float axis = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
                    float alph = axis < .75f ? axis * .5f : axis;//lower alpha on light trigger press                     

                    //Add the brush sprite
                    GameObject newBrush = Instantiate(Resources.Load("baseBrush")) as GameObject;
                    tintColor.a = alph;
                    newBrush.layer = 8;//the paint layer - only seen by renderCam
                    newBrush.GetComponent<SpriteRenderer>().color = tintColor;
                    newBrush.GetComponent<SpriteRenderer>().sortingOrder = mainRef.sortIndex();//so all new paint is in front of older paint
                    newBrush.transform.position = cLoc + layers.paint;
                    newBrush.transform.localScale = new Vector3(cursorScale * .9f, cursorScale * .9f, 1);
                    newBrush.transform.rotation = Quaternion.LookRotation(layers.forward);
                }
            }//mainRay.collider != null


            //BUTTONS
            if (buttonRay.collider != null)
            {
                string n = buttonRay.collider.gameObject.name;

                //canvas selection mode and trigger not down
                if (m == 1 && !isPainting)
                {                    
                    mainRef.canvasHover(n);
                }

                //if !isPainting then the trigger was released - makes it so menu items can't be selected while painting
                if (!isPainting && controller.GetHairTriggerDown())
                {
                    //isPainting = false;

                    if(m == 1)
                    {
                        //canvas selection                       
                        mainRef.canvasSelect(n);

                    }else if(m == 2)
                    {
                        //painting mode
                        if (n == "garbage")
                        {
                            mainRef.doTrash();
                        }
                        else if (n == "share")
                        {
                            mainRef.saveImage();
                        }
                        else if(n == "canvases")
                        {
                            mainRef.showCanvases();
                        }
                        else
                        {
                            //Clicked a brush
                            tintColor = buttonRay.collider.gameObject.GetComponent<UIBrush>().theColor;
                            cursor.GetComponent<SpriteRenderer>().color = tintColor;
                        }
                    }
                }
            }
            else
            {
                //buttonRay.collider is null
                if(m == 1)
                {
                    mainRef.noHover();
                }
            }

            if (controller.GetHairTriggerUp())
            {
                isPainting = false;
            }

            if (controller.GetHairTriggerDown())
            {
                isPainting = true;
            }

        }//mode

    }//Update()
}