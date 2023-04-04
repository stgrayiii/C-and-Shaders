//Stewart Gray III
//Point to Geometry Shader | Reads from the Kinect's Depth Buffer to create and store a point cloud as a mesh
//August 2017
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Kinect;
using UnityEngine;
using UnityEngine.Assertions;



public class PointCloudGenerator : MonoBehaviour
{
    //Default threshold for determining which set of pixels to be used in generating the point cloud
    [SerializeField]
    private float MIN_THRESHOLD = 580f;
    [SerializeField]
    private float MAX_THRESHOLD = 944f;
    [SerializeField]
    private int SCALE = 64;
    const int MAX_VERTS = 120000;
    public float CLIP = 2.8f;
    public float VIS_THRESH = 1.0f;
    public float INC = 0.1f;
    public float INC2 = 0.01f;

    //Initializing the Kinect
    private KinectSensor k2 = null;

    //Obtain the depth frame
    private DepthFrameSource depthSource = null;

    //Read from the depth frame
    private DepthFrameReader depthReader = null;

    //Store each pixel of the frame as a series of unsigned shorts
    private ushort[] depthArray;

    //Kinectv2 Camera Information
    static float cx = 254.878f;
    static float cy = 205.395f;
    static float fx = 365.456f;
    static float fy = 365.456f;

    //For generating points of the cloud as vertices in space
    private Mesh mesh;

    /* To contain a counter (value) for every pixel (key) in our vertex array. If the key for that corresponding
    pixel is above THRESH, add it to our mesh's final vertices. */
    private float[,] vismarks;

    //To send mesh and material data to our PointToGeometry Shader
    public Material mat;
    public Mesh currPointCloud;

    void Start()
    {
        //Use the Kinect Sensor
        k2 = KinectSensor.GetDefault();

        Assert.IsTrue(k2 != null, "A Kinect could not be found");
        //Start running the sensor
        if (k2 != null)
        {
            //Read depth data
            depthSource = k2.DepthFrameSource;
            depthReader = depthSource.OpenReader();
            var depthFrame = depthSource.FrameDescription;


            k2.Open();
            Assert.IsTrue(k2.IsOpen, "Kinect Sensor is offline");
            if (k2.IsOpen)
            {
                Debug.Log("Kinect Sensor Running");
                depthArray = new ushort[depthFrame.LengthInPixels];
               
            }
        }
    }

    //Add valid points to the point cloud, then proces them as a mesh
    Mesh generateCloud(int width, int height)
    {
        Mesh tmp = new Mesh();
        tmp.Clear();

        int nCount = 0;
        Vector3 point = new Vector3(0, 0, 0);

        //Iterate through all the pixels
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> inds = new List<int>();
        
        for (int x = 0; x < width; x += SCALE)
        {
            for (int y = 0; y < height; y += SCALE)
            {
                int offset = x + y * width;
                int z = depthArray[offset];

                Vector3 prevPoint = point;
                point = toCamera(x, y, z);
                float v = vismarks[x,y];

                if (z > MIN_THRESHOLD && z < MAX_THRESHOLD
                   &&  pixelDifference(prevPoint, point) < CLIP)
                {
                    v += INC;
                }
                else
                {
                    v -= INC2;
                }
                if (verts.Count < MAX_VERTS && v >= VIS_THRESH)
                {
                    verts.Add(point);
                    inds.Add(nCount);
                    uv.Add(new Vector2((v - VIS_THRESH) * 2.0f, 0));
                    nCount++;
                }
                vismarks[x, y] = Mathf.Clamp01(v);
            }
        }

        //Complete the creation of the mesh
        tmp.vertices = verts.ToArray();
        tmp.uv = uv.ToArray();
        tmp.SetIndices(inds.ToArray(), MeshTopology.Points, 0);
        return tmp;
    }


    void Update()
    {
        if (depthReader != null)
        {
            //To update and obtain all the pixels generated from the raw depth sensor as integers
            var cFrame = depthReader.AcquireLatestFrame();
            if (cFrame != null)
            {
                cFrame.CopyFrameDataToArray(depthArray);
                if (vismarks == null)
                {
                    vismarks = new float[cFrame.FrameDescription.Width, cFrame.FrameDescription.Height];
                }
                currPointCloud = generateCloud(cFrame.FrameDescription.Width, cFrame.FrameDescription.Height);
                cFrame.Dispose();
                cFrame = null;
            }
        }

        if (currPointCloud != null)
        {
            Graphics.DrawMesh(currPointCloud, Matrix4x4.identity, mat, 0);           
        }

    }

    //Convert depth map to camera space
    Vector3 toCamera(int x, int y, float depthValue)
    {
        Vector3 point = new Vector3();
        point.z = (depthValue);
        point.x = (x - cx) * point.z / fx;
        point.y = (y - cy) * point.z / fy;
        return point;
    }

    //Obtains the difference, in pixels, between the position of two vertices in the point cloud
    float pixelDifference(Vector3 p1, Vector3 p2)
    {
        float dif = 0;
        dif = Mathf.Abs((Vector3.Distance(p2, p1)));
        return dif;
    }

    //Clear and close the Kinect's buffer upon qutting the game. (Necessary to prevent a memory leak)
    void OnApplicationQuit()
    {
        if (depthReader != null)
        {
            depthReader.Dispose();
            depthReader = null;
        }

        if (k2 != null)
        {
            if (k2.IsOpen)
            {
                k2.Close();
            }

            k2 = null;
        }
    }

}
