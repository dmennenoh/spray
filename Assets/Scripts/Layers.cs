using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Layers : MonoBehaviour
{
    private Vector3 fVec;
    private Vector3 uVec;

    //calculated in Main when the meshes are built
    //this is the normal to the mesh surface
    public Vector3 forward
    {
        get
        {
            return fVec;
        }
        set
        {
            fVec = value;
        }
    }


    //Backplane/original mesh that has the hit collider is at 0
    public Vector3 merge
    {
        get
        {
            return fVec * .01f;
        }
    }

    public Vector3 paint
    {
        get
        {
            return fVec * .015f;
        }
    }

    public Vector3 outline
    {
        get
        {
            return fVec * .02f;
        }
    }

    public Vector3 ui
    {
        get
        {
            return fVec * .045f;
        }
    }

    public Vector3 cursor
    {
        get
        {
            return fVec * .048f;
        }
    }
}
