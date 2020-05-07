using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;


/*
 * Attached to Main in Main Scene
 */
public class FileScript : MonoBehaviour
{
    //public TextAsset TextFile;
    private string projectFolder;
    private string queueFilePath;

    //List of string arrays- each array has 3 items -file name, uploaded flag, num tries uploading
    private List<string[]> currentQueue;

    private int currentUploadIndex; //index of current upload in currentQueue - set in findNextUpload()

    private bool isUploading;//true when uploading

    private Main mainRef;//reference to Main class

    private float elapsedTime;

    private bool isSpanish;


    void Start()
    {
        isUploading = false;
        currentUploadIndex = -1;
        elapsedTime = 0;
        isSpanish = false;

        mainRef = GetComponent<Main>();

        //this is the root app folder
        projectFolder = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        queueFilePath = Path.Combine(projectFolder, "videoQueue.txt");

        readQueueFile();//populates currentQueue
        findNextUpload();//populates currentUpload  
    }


    void Update()
    {
        if (!isUploading && currentUploadIndex != -1)
        {
            UploadFile();
        }

        int m = mainRef.mode;

        //if BA presses mouse button during painting - erase it (mouse button 0 is left, 1 right, 2 middle)
        if (Input.GetKeyDown(KeyCode.PageUp) && m == 2)
        {
            mainRef.doTrash();
        }

        //mode 3 is QR code showing
        if (Input.GetKeyDown(KeyCode.PageDown) && m == 3)
        {
            restart();
        }

        //if canvas select or painting
        if (m == 1 || m == 2)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime > 120)
            {
                //3 minutes with no activity and not uploading an image
                restart();
            }
        }
    }


    //Called from Main.userMove whenever a user does something 
    public void resetTime()
    {
        elapsedTime = 0;
    }



    void restart()
    {
        //mode 3 - showing QR Code
        //BA pressed the mouse - restart
        writeQueueToFile();
        SceneManager.LoadScene(0);//reload sets mode back to 0
    }



    /*
     * In Editor sent when Stop is pressed
     * 
     * In app sent before the application exits

     * Writes the currentQueue to the queue text file 
     */
    void OnApplicationQuit()
    {        
        writeQueueToFile();        
    }


    void writeQueueToFile()
    {
        //append a new data line to the queue -file name, successful upload flag, number of times uploading
        string content = "";
        if (currentQueue.Count > 0)
        {
            for (int i = 0; i < currentQueue.Count; i++)
            {
                content = content + currentQueue[i][0] + "," + currentQueue[i][1] + "," + currentQueue[i][2] + System.Environment.NewLine;
            }
            File.WriteAllText(queueFilePath, content);
        }
    }


    /*
     * Called from Main.showQR()
     * Adds filename to the currentQueue List
     */
    public void addFile(string fileNameGuid)
    {
        string[] line = new string[3];       
        line[0] = fileNameGuid;//just the guid - no file extension
        line[1] = "0";//uploaded flag
        line[2] = "0";//num tries uploading
        currentQueue.Add(line);//add to the List object

        findNextUpload();
    }


    private void UploadFile()
    {
        isUploading = true;
        StartCoroutine(UploadFileCo());
    }


    IEnumerator UploadFileCo()
    {
        //First get the Auth token from NowPik
        string json = "{\"userName\":\"niss@nart\", \"password\":\"dave\", \"latitude\":42, \"longitude\":-88}";

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Content-Type", "application/json");

        byte[] postData = Encoding.ASCII.GetBytes(json);

        WWW www = new WWW("http://api.nowpik.com/api/authorize/validateuser", postData, headers);

        yield return www;

        AuthData auth = JsonUtility.FromJson<AuthData>(www.text);

        if (auth.Status != 1)
        {
            isUploading = false;
            yield break; // stop the coroutine here
        }

        //FILE
        WWW localFile = new WWW("file:///" + Application.persistentDataPath + "/" + currentQueue[currentUploadIndex][0] + ".png");//guid,uloaded flag, num tries
        yield return localFile;
        if (localFile.error != null)
        {
            isUploading = false;
            yield break; // stop the coroutine here
        }

        //got token and file
        WWWForm postForm = new WWWForm();

        byte[] by = intToByteArray(localFile.bytes.Length);

        byte[] conc = new byte[by.Length + localFile.bytes.Length];

        System.Buffer.BlockCopy(by, 0, conc, 0, by.Length);
        System.Buffer.BlockCopy(localFile.bytes, 0, conc, by.Length, localFile.bytes.Length);

        postForm.AddField("accessToken", auth.ResponseObject);
        postForm.AddField("json", JSON(currentQueue[currentUploadIndex][0]));
        postForm.AddBinaryData("binary", conc, currentQueue[currentUploadIndex][0] + ".png");

        WWW upload = new WWW("http://api.nowpik.com/api/interaction/interactionResponseV3", postForm);

        yield return upload;

        if (upload.error == null)
        {
            //Mark upload complete flag in queue
            currentQueue[currentUploadIndex][1] = "1";

            //increment the num tries entry
            int inc = int.Parse(currentQueue[currentUploadIndex][2]) + 1;
            currentQueue[currentUploadIndex][2] = inc.ToString();
        }
        else
        {
            //increment the num tries entry
            int inc = int.Parse(currentQueue[currentUploadIndex][2]) + 1;
            currentQueue[currentUploadIndex][2] = inc.ToString();

            //delete this line in the currentQueue and then add it to the end
            string[] temp = currentQueue[currentUploadIndex];
            currentQueue.RemoveAt(currentUploadIndex);
            currentQueue.Add(temp);
        }

        isUploading = false;

        findNextUpload();//See if there's a new file to uplaod        
    }


    private byte[] intToByteArray(int integer)
    {
        var result = new byte[4];

        for (int i = 3; i > -1; i--)
        {
            result[i] = (byte)(integer % 256);
            integer = integer / 256;
        }

        return result;
    }


    /*
     * Refreshes the currentQueue List
     */
    private void readQueueFile()
    {
        currentQueue = new List<string[]>();

        if (File.Exists(queueFilePath))
        {

            string[] fullFile = File.ReadAllLines(queueFilePath, Encoding.UTF8);

            char[] splitchar = { ',' };
            for (int i = 0; i < fullFile.Length; i++)
            {
                string[] line = new string[3];
                line = fullFile[i].Split(splitchar);
                currentQueue.Add(line);
            }
        }
    }


    /*
     * Iterates the currentQueue List and finds the next item with a false uploaded flag
     * Sets currentUploadIndex
     */
    private void findNextUpload()
    {
        currentUploadIndex = -1;

        for (int i = 0; i < currentQueue.Count; i++)
        {
            if (currentQueue[i][1] == "0")
            {
                //will be picked up in Update()
                currentUploadIndex = i;
                break;
            }
        }
    }


    //Reads the config file - lines 1-4 are the corner points, line 5 is the localScale of the UICanvas
    //line 6 is e or s for english or spanish
    public Vector3[] readConfigFile()
    {
        StreamReader sr;

        try
        {
            sr = File.OpenText(Path.Combine(projectFolder, "screenLoc.txt"));
        }catch
        {
            throw new Exception("Config file [screenLoc.txt] not found.");            
        }

        Vector3[] points = new Vector3[5];

        string line;
        int index = 0;

        while (index < 5 && (line = sr.ReadLine()) != null)
        {
            string[] p = line.Split(','); //x,y,z
            points[index] = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            index++;
        }

        //get line 6 - eng/span
        line = sr.ReadLine();
        isSpanish = line == "s" ? true : false;

        sr.Dispose();
        return points;
    }


    public bool spanish
    {
        get
        {
            return isSpanish;
        }
    }


    /**
     * Returns a unique device identifier
     */
    private string deviceID()
    {
        string id;
       
        if (!File.Exists("deviceID.txt"))
        {
            id = System.Guid.NewGuid().ToString();
            StreamWriter sr = File.CreateText("deviceID.txt");
            sr.WriteLine(id);            
            sr.Close();
        }
        else
        {
            StreamReader sr = File.OpenText("deviceID.txt");
            id = sr.ReadLine();
            sr.Close();
        }

        return id;        
    }


    //returns the JSON string sent to NowPik API
    private string JSON(string fileGUID)
    {        
        DateTime n = DateTime.Now;

        string month = n.Month.ToString();
        if(month.Length < 2)
        {
            month = "0" + month;
        }

        string day = n.Day.ToString();
        if (day.Length < 2) {
            day = "0" + day;
        }
        
        string hour = n.Hour.ToString();
        if (hour.Length < 2)
        {
            hour = "0" + hour;
        }

        string min = n.Minute.ToString();
        if(min.Length < 2)
        {
            min = "0" + min;
        }

        string sec = n.Second.ToString();
        if(sec.Length < 2)
        {
            sec = "0" + sec;
        }

        string ms = n.Millisecond.ToString();
        if(ms.Length < 3)
        {
            if(ms.Length < 2)
            {
                ms = "00" + ms;
            }
            else
            {
                ms = "0" + ms;
            }
        }

        //date format: "2016-11-09T23:05:50.201Z"
        string timeStamp = n.Year.ToString() + "-" + month + "-" + day + "T" + hour + ":" + min + ":" + sec + "." + ms + "Z";

        string json = "{ \"deviceId\":\"" + deviceID() + "\", \"deviceResponseId\": \"" + System.Guid.NewGuid().ToString() + "\", \"interactionId\": 611, \"responseDate\": \"" + timeStamp + "\",";        

        json += "\"fieldResponses\": [{\"fieldId\":5439, \"response\": \"" + fileGUID + "\", \"optionId\": null, \"attachments\": null, \"processCommand\": null }, ";

        json += "{ \"fieldId\":5433, \"response\":null, \"optionId\": null, \"attachments\":[\"binary://0\"], \"processCommand\":null }],";

        json += "\"latitude\":42, \"longitude\":-88}";

        return json;
    }

}



[Serializable]
public class AuthData
{
    public int Status;
    public string Message;
    public string ResponseObject;
}
