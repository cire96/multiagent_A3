using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class planner1 : MonoBehaviour {
    Graph mapGraph;
    int[,] nodeIdMatrix;
    public void start(){
        //mapGraph=GameObject.Find("GraphObj").GetComponent<TravGraph>().mapGraph;

    }

    public List<int> plan(Vector3 startPos, Vector3 goalPos, Color pathColor){
        mapGraph=GameObject.Find("GraphObj").GetComponent<TravGraph>().mapGraph;
        int start = mapGraph.FindClosestNode(startPos,mapGraph).getId();
        int goal = mapGraph.FindClosestNode(goalPos,mapGraph).getId();
        List<int> path = aStar(start,goal);
        for(int i=0;i<path.Count-1;i++){
            Debug.DrawLine(mapGraph.getNode(path[i]).getPosition(),mapGraph.getNode(path[i+1]).getPosition(),pathColor,1000);
        }
        return path;
    }




    public List<int> aStar (int start, int Goal) {
        List<int> openSet = new List<int> () { start };
        Dictionary<int, int> cameFrom = new Dictionary<int, int> ();

        // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
        float[] gScore = new float[mapGraph.getSize ()];
        float[] fScore = new float[mapGraph.getSize ()];

        for (int i = 0; i < mapGraph.getSize (); i++) {
            gScore[i] = 1000000.0f;
            fScore[i] = 1000000.0f;
        }
        gScore[start] = 0.0f;
        fScore[start] = cost (start, Goal);

        while (openSet.Count > 0) { //!openSet.Any()
            int current = helpCurrent (fScore, openSet);
            if (current == Goal) {
                return reconstruct_path (cameFrom, current);
            }
            openSet.Remove (current);
            foreach (int neighbor in mapGraph.getAdjList (current)) {
                // d(current,neighbor) is the weight of the edge from current to neighbor
                // tentative_gScore is the distance from start to the neighbor through current
                float tentative_gScore = gScore[current] + cost (current, neighbor);
                if (tentative_gScore < gScore[neighbor]) {
                    // This path to neighbor is better than any previous one. Record it!
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_gScore;
                    fScore[neighbor] = gScore[neighbor] + costUqil(neighbor, Goal);
                    if (openSet.Contains (neighbor) == false) {
                        openSet.Add (neighbor);
                    }
                }
            }
        }

        return new List<int> ();

    }

    public int helpCurrent (float[] fScore, List<int> openSet) {
        float lowestCost = 10000000000.0f;
        int current = 0;
        foreach (int id in openSet) {
            if (fScore[id] < lowestCost) {
                lowestCost = fScore[id];
                current = id;
            }
        }
        return current;
    }

    public float costUqil(int id, int goal){
        Node startNode = mapGraph.getNode(id);
        Node goalNode = mapGraph.getNode(goal);
        Vector3 startPos = startNode.getPosition();
        Vector3 goalPos = goalNode.getPosition();
        return Vector3.Distance (startPos, goalPos);
    } 

    public float cost (int id, int goal) {
        Node startNode = mapGraph.getNode(id);
        Node goalNode = mapGraph.getNode(goal);
        Vector3 startPos = startNode.getPosition();
        Vector3 goalPos = goalNode.getPosition();
        float DirCost = 0;
        Vector3 dirVec = (goalPos - startPos);
        dirVec.Normalize();
        if(startNode.getI()==goalNode.getI()){
            if(dirVec.z>0 ){
                if(0==goalNode.getI()%2){
                    DirCost = 0;
                }else{
                    DirCost = 100000;
                }
            }else{
                if(0==goalNode.getI()%2){
                    DirCost = 100000;
                }else{
                    DirCost = 0;
                }
            }
        }else if(startNode.getJ()==goalNode.getJ()){
            if(dirVec.x>0){
                if(0==goalNode.getJ()%2){
                    DirCost = 0;
                }else{
                    DirCost = 100000;
                }
            }else{
                if(0==goalNode.getJ()%2){
                    DirCost = 100000;
                }else{
                    DirCost = 0;
                }
            }
        }else if((startNode.getJ()+startNode.getI())!=(goalNode.getJ()+goalNode.getI())){
            if(dirVec.x>0 && dirVec.z>0){
                if(0==(goalNode.getJ()+goalNode.getI())%2){
                    DirCost = 0;
                }else{
                    DirCost = 100000;
                }
            }else if(dirVec.x<0 && dirVec.z<0){
                if(0==(goalNode.getJ()+goalNode.getI())%2){
                    DirCost = 100000;
                }else{
                    DirCost = 0;
                }
            }
        }else if((startNode.getJ()+startNode.getI())==(goalNode.getJ()+goalNode.getI())){
            if(dirVec.x<0 && dirVec.z>0){
                if(0==(goalNode.getJ()+goalNode.getI())%2){
                    DirCost = 0;
                }else{
                    DirCost = 100000;
                }
            }else if(dirVec.x>0 && dirVec.z<0){
                if(0==(goalNode.getJ()+goalNode.getI())%2){
                    DirCost = 100000;
                }else{
                    DirCost = 0;
                }
            }
        }



        return Vector3.Distance (startPos, goalPos) + DirCost + goalNode.getCost();
    }

    public List<int> reconstruct_path (Dictionary<int, int> cameFrom, int current) {
        foreach (KeyValuePair<int, int> kvp in cameFrom) {
            //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            //Debug.Log("Key = "+kvp.Key.ToString()+", Value = "+ kvp.Value.ToString());
        }
        List<int> total_path = new List<int> () { current };
        while (cameFrom.ContainsKey (current)) {
            Node currentNode=mapGraph.getNode(current);
            currentNode.setCost(currentNode.getCost()+0f);
            current = cameFrom[current];
            total_path.Insert (0, current);
        }
        return total_path;
    }

}