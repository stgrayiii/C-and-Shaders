//Stewart Gray III
//Unity Terrain Layers | This Script provides an interface for and drives the Shader of the same name. 
//September 2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainLayersTool : MonoBehaviour
{
    [Range (0.5f , 75f)]
    public float snowFalloff = 10f;

    [Range (0.5f , 5f)]
    public float snowFormationHeight = 1.35f;

    [Range(25f, 90f)]
    public float slopeAngleThreshold = 30f;

    public Texture ground, gravel, snow, cliff;

    private Material m_TerrainLayers;
    private Camera cam;
    const int RIGHT_BUTTON = 1;

    void Start()
    {   
        //If the material does not exist on this object, create it and assign our TerrainLayers shader to it 
        if(!m_TerrainLayers)
        m_TerrainLayers = new Material(Shader.Find("Custom/TerrainLayers"));
        GetComponent<Renderer>().material = m_TerrainLayers;

        //Obtain the main camera within the scene
        cam = Camera.main;
    }

    //Updates shader parameters within the assigned material
    void updateShader()
    {
        m_TerrainLayers.SetFloat("_fC", snowFalloff);
        m_TerrainLayers.SetFloat("_height0", snowFormationHeight);
        m_TerrainLayers.SetFloat("_slopeThresh", slopeAngleThreshold);

        m_TerrainLayers.SetTexture("_layer0", ground);
        m_TerrainLayers.SetTexture("_layer1", gravel);
        m_TerrainLayers.SetTexture("_layer2", snow);
        m_TerrainLayers.SetTexture("_layer3", cliff);
    }

    //Allows the terrain mesh to be rotated along the main camera's Y-Axis
    void rotateAlongAxis(Transform t, float middle)
    {
        if (Input.mousePosition.x > middle)
        {
            transform.RotateAround(t.position, new Vector3(0, cam.transform.localPosition.y, 0), -180 * Time.deltaTime * 0.6f);
        }
        else if (Input.mousePosition.x < middle)
        {
            transform.RotateAround(t.position, new Vector3(0, cam.transform.localPosition.y, 0), 180 * Time.deltaTime * 0.6f);
        }
    }

    void Update()
    {
        //Obtain the middle position of the screen
        float middleX = Screen.width / 2;

        
      /*Clicking the mouse to the right of the middle will rotate the terrain to the right
        Clicking the mouse to the left of the middle will rotate the terrain to the left  */
        if (Input.GetMouseButton(RIGHT_BUTTON))
        {
            rotateAlongAxis(gameObject.transform, middleX);
        }
        updateShader();
    }

}
