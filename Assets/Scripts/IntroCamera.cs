using UnityEngine;
using UnityEngine.Video;

//Attached to screenCam

public class IntroCamera : MonoBehaviour
{
    private VideoPlayer introVideo;

    private void Start()
    {
        introVideo = GetComponent<VideoPlayer>();
        introVideo.prepareCompleted += readyToPlay;
        introVideo.loopPointReached += endReached;
    }
	

    void readyToPlay(VideoPlayer vp)
    {

    }


    void endReached(VideoPlayer vp)
    {
        vp.Play();
    }
}
