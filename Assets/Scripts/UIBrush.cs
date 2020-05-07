using UnityEngine;
using UnityEngine.UI;

//attached to all brushes

public class UIBrush : MonoBehaviour
{
    //set in Editor for each brush
    public Color brushColor;

    public Color theColor
    {
        get
        {
            return brushColor;
        }
    }
	
}
