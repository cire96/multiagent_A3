using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravGraph : MonoBehaviour {


    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;
    TerrainInfo terrainInfo;
    public Graph mapGraph;
    public int[,] nodeIdMatrix;
    private float stepx;
    private float stepz;

    // Use this for early initialization
    void Start () {
        //makeMap();
            
    }
    public void makeMap(){
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        print(terrain_manager);
        terrainInfo = terrain_manager.myInfo;
        print(terrainInfo);
        float[, ] traversability = terrainInfo.traversability;
        terrainInfo = terrain_manager.myInfo;
        float tileXSize = (terrainInfo.x_high - terrainInfo.x_low) / terrainInfo.x_N;
        float tileZSize = (terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N;

        float factor = 3f;
        float[, ] newTerrain = new float[(int) Mathf.Floor (tileXSize * tileXSize / factor), (int) Mathf.Floor (tileZSize * tileZSize / factor)];
        stepx = (terrainInfo.x_high - terrainInfo.x_low) / newTerrain.GetLength (0);
        stepz = (terrainInfo.z_high - terrainInfo.z_low) / newTerrain.GetLength (1);
        for (int i = 0; i < newTerrain.GetLength (0); i++) {
            float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
            for (int j = 0; j < newTerrain.GetLength (1); j++) {
                float posz = terrainInfo.z_low + stepz / 2 + stepz * j;
                newTerrain[i, j] = terrainInfo.traversability[terrainInfo.get_i_index (posx), terrainInfo.get_j_index (posz)];
            }
        }
        
        
        
        int xLen = newTerrain.GetLength(0);
        int zLen = newTerrain.GetLength(1);
        nodeIdMatrix = new int[xLen, zLen];
        mapGraph = new Graph();
        int nodeId;
        for (int i = 0; i < xLen; i++) {
            float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
            for (int j = 0; j < zLen; j++) {
                float posz = terrainInfo.z_low + stepz / 2 + stepz * j;
                if (newTerrain[i,j]==0.0f && 0.0f==checkTrav(posx,posz+3.0f) && 0.0f==checkTrav(posx,posz-3.0f) && 0.0f==checkTrav(posx+3.0f,posz) && 0.0f==checkTrav(posx-3.0f,posz)){
                    /*GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
                    Collider c = cube.GetComponent<Collider> ();
                    c.enabled = false;
                    cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
                    cube.transform.position = new Vector3 (posx, 0, posz);*/
                    nodeIdMatrix[i,j] = mapGraph.addNode(new Node (posx, posz));
                    mapGraph.getNode(nodeIdMatrix[i,j]).setIndex(i,j);
                
                }else{
                    nodeIdMatrix[i,j] = -1;
                }
            }
        }
        float timeDraw = 1; 
        LayerMask mask = LayerMask.GetMask("Wall");
        RaycastHit hit;
        for (int i = 0; i < xLen; i++){
            for (int j = 0; j < zLen; j++){
                if(nodeIdMatrix[i,j] != -1){
                    nodeId = nodeIdMatrix[i,j];
                    if(nodeIdMatrix[i,j+1] != -1){
                        Debug.DrawLine(mapGraph.getNode(nodeId).getPosition(),mapGraph.getNode(nodeIdMatrix[i,j+1]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i,j+1]);}
                    if(nodeIdMatrix[i,j-1] != -1){
                        Debug.DrawLine(mapGraph.getNode(nodeId).getPosition(),mapGraph.getNode(nodeIdMatrix[i,j-1]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i,j-1]);}
                    if(nodeIdMatrix[i+1,j] != -1){
                        Debug.DrawLine(mapGraph.getNode(nodeId).getPosition(),mapGraph.getNode(nodeIdMatrix[i+1,j]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i+1,j]);}
                    if(nodeIdMatrix[i-1,j] != -1){
                        Debug.DrawLine(mapGraph.getNode(nodeId).getPosition(),mapGraph.getNode(nodeIdMatrix[i-1,j]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i-1,j]);}
                    
                    Vector3 from = mapGraph.getNode(nodeId).getPosition();
                    
                    /*
                    if(nodeIdMatrix[i-1,j-1] != -1 && !Physics.Raycast(from , mapGraph.getNode(nodeIdMatrix[i-1,j-1]).getPosition()-from, out hit,Vector3.Distance(mapGraph.getNode(nodeIdMatrix[i-1,j-1]).getPosition(),from) , mask)){
                        Debug.DrawLine(from,mapGraph.getNode(nodeIdMatrix[i-1,j-1]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i-1,j-1]);}
                    
                    
                    if(nodeIdMatrix[i-1,j+1] != -1 && !Physics.Raycast(from , mapGraph.getNode(nodeIdMatrix[i-1,j+1]).getPosition()-from, out hit,Vector3.Distance(mapGraph.getNode(nodeIdMatrix[i-1,j+1]).getPosition(),from) , mask)){
                        Debug.DrawLine(from,mapGraph.getNode(nodeIdMatrix[i-1,j+1]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i-1,j+1]);}

                        
                    if(nodeIdMatrix[i+1,j-1] != -1 && !Physics.Raycast(from , mapGraph.getNode(nodeIdMatrix[i+1,j-1]).getPosition()-from, out hit,Vector3.Distance(mapGraph.getNode(nodeIdMatrix[i+1,j-1]).getPosition(),from) , mask)){
                        Debug.DrawLine(from,mapGraph.getNode(nodeIdMatrix[i+1,j-1]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i+1,j-1]);}

                    
                    if(nodeIdMatrix[i+1,j+1] != -1 && !Physics.Raycast(from , mapGraph.getNode(nodeIdMatrix[i+1,j+1]).getPosition()-from, out hit,Vector3.Distance(mapGraph.getNode(nodeIdMatrix[i+1,j+1]).getPosition(),from) , mask)){
                        Debug.DrawLine(from,mapGraph.getNode(nodeIdMatrix[i+1,j+1]).getPosition(),Color.cyan,timeDraw);
                        mapGraph.addEdge(nodeId,nodeIdMatrix[i+1,j+1]);}
                    */

                }
            }
        }
    }

    public float checkTrav(float posx, float posz){
        return terrainInfo.traversability[terrainInfo.get_i_index (posx), terrainInfo.get_j_index (posz)];
    }
}