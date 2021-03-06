﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Math; 
using NPBehave;



namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends; // use these to avoid collisions

        public GameObject my_goal_object;
        public Root behaviorTree;
        public Vector3 goal_pos;
        public Blackboard blackboard;
        public List<int> ownPath;
        public int targetIndex=0;
        public float avoidRange=10.0f;

        public planner1 planner;
        public Graph mapGraph;

        public int backingCounter;
        float capSpeed=20f;

        public int Aggressiveness;
        public int oldAggressiveness;
        public Color my_color;

        public GameObject carHit;
        bool starting = true;
        public int temp = 0;
        public float newSpeed;
        public float newSteer;
        public float handBrake;
        public int delay;
        public float ourDis;
        public int idelFrames; 
        public bool colliding = false;
        int collidingTime = 0;
        public int waitTimer = 0;
        int moveCounter = 0;








        private void Start()
        {
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();

            Vector3 start_pos = transform.position; // terrain_manager.myInfo.start_pos;
            goal_pos = my_goal_object.transform.position;
            planner = GameObject.Find("GraphObj").GetComponent<planner1>();
            ownPath = planner.plan(start_pos,goal_pos,my_color);
            mapGraph=GameObject.Find("GraphObj").GetComponent<TravGraph>().mapGraph;


            friends = GameObject.FindGameObjectsWithTag("Player");

            oldAggressiveness = Aggressiveness;

            behaviorTree = new Root(
                new Selector(
                    new BlackboardCondition("NotOnGoal", Operator.IS_EQUAL, true, Stops.SELF,
                        new Selector(
                            new BlackboardCondition("Wall",Operator.IS_EQUAL, true ,Stops.SELF,
                                new Action(()=>rePlan())
                            ), 
                
                            new BlackboardCondition("Car",Operator.IS_EQUAL,true,Stops.SELF,
                                new Sequence(
                                    new Action(()=>calculateBearing()),
                                    new Action(()=>Avoid())
                                )
                            ),
                        
                            new Action(()=>followPath())

                        )
                    ),
                    new Action(()=>goalAdjust())
                )

            );
            blackboard = behaviorTree.Blackboard;

            
#if UNITY_EDITOR
            Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
            debugger.BehaviorTree = behaviorTree;
#endif
            
            //behaviorTree.Start();
            

            
        }

        void rePlan(){
            Debug.Log("replan");
        }

        void calculateBearing(){
            CarAI otherCarAI=carHit.GetComponent<CarAI>(); 
            ourDis=ownPath.Count-targetIndex;//-(0.1f*temp);//Vector3.Distance(transform.position,goal_pos);
            float otherDis = otherCarAI.ownPath.Count-otherCarAI.targetIndex;//-(0.1f*otherCarAI.temp);//Vector3.Distance(carHit.transform.position,otherCarAI.goal_pos);
            if(Aggressiveness<otherCarAI.Aggressiveness && !pathsCross(otherCarAI)){   //ourDis>otherDis
                temp++;
                waitTimer = 50;
                Vector3 vel = m_Car.GetComponent<Rigidbody>().velocity;
                if(transform.InverseTransformDirection(vel).z>0.01){
                    newSpeed=-1;
                    handBrake=1;
                }else if(transform.InverseTransformDirection(vel).z<-0.01){
                    newSpeed=1;
                    handBrake=1;
                }else{
                    newSpeed=0;
                    handBrake=1;
                }  
                m_Car.Move(0, newSpeed, newSpeed, handBrake);

            
            }
            
            else{
                followPath();
            }

            
        }

        bool pathsCross(CarAI otherCarAI){
            bool cross = false;
            int nextNPoints = 6;
            int prevNPoints = 4;

            List<int> myNextPoints = new List<int>();
            List<int> otherNextPoints = new List<int>();
            

            if(targetIndex+nextNPoints < ownPath.Count && targetIndex-prevNPoints >= 0){
                myNextPoints = ownPath.GetRange(targetIndex-prevNPoints, nextNPoints);
            }

            if(otherCarAI.targetIndex+nextNPoints < otherCarAI.ownPath.Count && otherCarAI.targetIndex-prevNPoints >= 0){
                otherNextPoints = otherCarAI.ownPath.GetRange(otherCarAI.targetIndex-prevNPoints, nextNPoints);
            }

            for(int i = 0; i<myNextPoints.Count; i++){
                if(otherNextPoints.Contains(myNextPoints[i])){
                    cross = true;
                }
            }
            return cross;
        }

        void Avoid(){
            //Debug.Log("Avoid");
        }

        void goalAdjust(){
            Vector3 target = goal_pos;
            Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
            float newSteer = (carToTarget.x / carToTarget.magnitude);
            float newSpeed; 
            float handBrake = 0f;
            float infrontOrbehind = (carToTarget.z / carToTarget.magnitude);
            if (moveCounter > 0){
                newSpeed = -1;
                newSteer = -1;
                moveCounter--;
            }

            else if (infrontOrbehind < 0) {
                newSpeed = -1;
                if (newSteer < 0) {
                    newSteer = 1;
                } else {
                    newSteer = -1;
                }
            } else { newSpeed = 1f; }



            Debug.DrawLine(transform.position,target,Color.black);
            m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);

        }

        void followPath(){
            int lookAhead = 4;
            int lookBack = 3;
            temp=0;
            Vector3 vel = m_Car.GetComponent<Rigidbody>().velocity;
            RaycastHit rayHit;
            RaycastHit finishHit;
            LayerMask wallMask = LayerMask.GetMask("Wall");
            LayerMask finishMask = LayerMask.GetMask("Finished");
            LayerMask carMask = LayerMask.GetMask("Car");
            LayerMask bothMask = LayerMask.GetMask("Wall", "Car");
            Vector3 target = mapGraph.getNode(ownPath[targetIndex]).getPosition();

            if(6f>Vector3.Distance(transform.position,target) && targetIndex<ownPath.Count-1){
                Vector3 tempTarget=mapGraph.getNode(ownPath[targetIndex+1]).getPosition();
                float tempDistance = Vector3.Distance(transform.position,tempTarget);
                bool tempHit = Physics.SphereCast(transform.position,1.8f,tempTarget-transform.position,out rayHit,tempDistance, wallMask);
                if(!tempHit){
                    targetIndex++;
                }
            }

            float minDis = 10000;
            int minIndex = targetIndex;
            for(int i=targetIndex;i<ownPath.Count;i++){
                Vector3 minTarget=mapGraph.getNode(ownPath[i]).getPosition();
                float tempDistance = Vector3.Distance(transform.position,minTarget);
                bool tempHit = Physics.SphereCast(transform.position,1.8f,minTarget-transform.position,out rayHit,tempDistance, wallMask);
                if(tempDistance < minDis && !tempHit){
                    minDis = tempDistance;
                    minIndex = i;
                }
            }
            targetIndex = minIndex;

            target = mapGraph.getNode(ownPath[targetIndex]).getPosition();
            Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
            newSteer = (carToTarget.x / carToTarget.magnitude);
             
            handBrake = 0f;


            if(backingCounter>0){
                backingCounter--;
                if(0.3f>transform.InverseTransformDirection(vel).z){
                    newSteer = -1f*Mathf.Sign(newSteer);
                }
                
                
                
                newSpeed = -1;
                if(capSpeed<m_Car.CurrentSpeed){
                    newSpeed=0;
                }
                //print"backing");
                m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);
                return;
            }

            float infrontOrbehind = (carToTarget.z / carToTarget.magnitude);
            if (infrontOrbehind < 0) {
                newSpeed = -1;
                if (newSteer < 0) {
                    newSteer = 1;
                } else {
                    newSteer = -1;
                }
            } else { newSpeed = 1f; }
            //if(infrontOrbehind<0 && Mathf.Abs(newSteer)<0.1){newSteer =1;}

            


            

            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            Vector3 steeringPointLeft = (transform.rotation * new Vector3(-1,0,1));
            bool hit = Physics.SphereCast(transform.position,2.3f,steeringPoint,out rayHit,3.0f, wallMask);
            bool hitTurn = Physics.SphereCast(transform.position,2.3f,steeringPoint,out rayHit,10.0f, wallMask);
            bool hitFinished = Physics.SphereCast(transform.position,2.3f,steeringPoint,out finishHit,6.0f, finishMask);
            bool hitCar = Physics.SphereCast(transform.position,2.3f,steeringPoint,out rayHit,3.0f, carMask);
            bool hitCarLeft = Physics.Raycast(transform.position,transform.TransformDirection(Vector3.forward+new Vector3(-1,0,0)),out rayHit,6.0f, carMask);

            if(hitFinished){
                carHit = finishHit.collider.transform.root.gameObject;
                carHit.GetComponent<CarAI>().moveCounter = 30;
                
            }
            if(hitCar){
                //backingCounter = 2;
            }

        
            if(hit){
                backingCounter = 20;
            }
            /*
            else if(hitTurn){
                newSteer = Mathf.Sign(newSteer);    
            }*/
            if(Mathf.Abs(newSteer) > 0.3f){
                newSteer = Mathf.Sign(newSteer);
            }  
            
            if(ownPath.Count>targetIndex+lookAhead && 0<targetIndex-lookBack){
                for(int i=targetIndex-lookBack;i<targetIndex+lookAhead-2;i++){
                Vector3 firstNode=mapGraph.getNode(ownPath[i]).getPosition();
                Vector3 secondNode=mapGraph.getNode(ownPath[i+1]).getPosition();
                Vector3 thirdNode=mapGraph.getNode(ownPath[i+2]).getPosition();
                float curveAngle = Vector3.Angle(firstNode-secondNode,thirdNode-secondNode);
                
                if(1.0f<curveAngle && 179.0f>curveAngle){
                    if(5<transform.InverseTransformDirection(vel).z){
                        newSpeed=-1;
                        handBrake=1;
                    }
                    break;
                }

                
            }

            }
            

            
            

            if(capSpeed<m_Car.CurrentSpeed){
                newSpeed=0;
            }
            if(newSpeed != 0 && 0.5>m_Car.CurrentSpeed){
                idelFrames++;
                if(idelFrames>30){
                    idelFrames=0;
                    backingCounter=100;
                }
            }

            else{
                idelFrames = 0;
            }
            

            Debug.DrawLine(transform.position,target,Color.black);
            m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);







        }

        public void SetLayerRecursively(GameObject obj,int newLayer ){
        obj.layer = newLayer;
   
            foreach(Transform child in obj.transform)
            {
                SetLayerRecursively( child.gameObject, newLayer );
            }
        }




        private void FixedUpdate(){
            if(delay > 0){
                delay--;
                return;
            }

            if(starting){
                blackboard["NotOnGoal"]=true;
                blackboard["Wall"]=false;
                blackboard["Car"]=false;
                starting=false;
                behaviorTree.Start();
            }

            if(colliding){
                collidingTime = 0;
            }

            else{
                collidingTime++;
            }

            if(collidingTime > 100){
                Aggressiveness = oldAggressiveness;
            }

            if(waitTimer > 0){
                Vector3 vel = m_Car.GetComponent<Rigidbody>().velocity;
                if(transform.InverseTransformDirection(vel).z>0.01){
                    newSpeed=-1;
                    handBrake=1;
                }else if(transform.InverseTransformDirection(vel).z<-0.01){
                    newSpeed=1;
                    handBrake=1;
                }else{
                    newSpeed=0;
                    handBrake=1;
                }
                waitTimer --;
                m_Car.Move(0, newSpeed, newSpeed, handBrake);

            }

            if(10>Vector3.Distance(transform.position,goal_pos)){
                blackboard["NotOnGoal"]=false;
                SetLayerRecursively(gameObject,12);
                /*Transform Colliders=transform.FindChild("Colliders");
                Colliders.FindChild("ColliderBody").gameObject.layer=12;
                Colliders.FindChild("ColliderBottom").gameObject.layer=12;
                Colliders.FindChild("ColliderBottom").gameObject.layer=12;*/

                //LayerMask.NameToLayer("Finished")

            }
            avoidRange = m_Car.CurrentSpeed;
            LayerMask mask = LayerMask.GetMask("Car");
            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            Vector3 steeringPointLeft = (transform.rotation * new Vector3(-1,0,1));
            Vector3 steeringPointRight = (transform.rotation * new Vector3(1,0,1));
            RaycastHit rayHit,rayHitLeft,rayHitRight;
            bool hit = Physics.SphereCast(transform.position,2.0f,steeringPoint,out rayHit,avoidRange, mask);
            //bool hitLeft = Physics.SphereCast(transform.position,2.0f,steeringPointLeft,out rayHitLeft,avoidRange, mask);
            bool hitLeft = false;
            bool hitRight = Physics.SphereCast(transform.position,2.0f,steeringPointRight,out rayHitRight,avoidRange, mask);
            if(hit){
                Debug.DrawRay(transform.position, steeringPoint * rayHit.distance, Color.yellow);
                carHit = rayHit.collider.transform.root.gameObject;
                if(carHit.GetComponent<CarAI>().delay > 0){
                    newSteer = Mathf.Sign(newSteer);
                }
                blackboard["Car"]=true;
            }
            /*else if(hitLeft){
                Debug.DrawRay(transform.position, steeringPointLeft * rayHit.distance, Color.yellow);
                carHit = rayHitLeft.collider.transform.root.gameObject;
                blackboard["Car"]=true;
                }
            */
            else if(hitRight){
                Debug.DrawRay(transform.position, steeringPointRight * rayHit.distance, Color.yellow);
                carHit = rayHitRight.collider.transform.root.gameObject;
                blackboard["Car"]=true;
            }
            
            else{
                Debug.DrawRay(transform.position, steeringPoint * avoidRange, Color.yellow);
                //Debug.DrawRay(transform.position, steeringPointLeft * avoidRange, Color.yellow);
                Debug.DrawRay(transform.position, steeringPointRight * avoidRange, Color.yellow);
                blackboard["Car"]=false;
                carHit = null;
            }

            

        }

        private void OnCollisionStay(Collision collision){
            if(collision.gameObject.tag == "Player"){
                RaycastHit rayHit;
                LayerMask mask = LayerMask.GetMask("Car");
                bool hit = Physics.SphereCast(transform.position,2.0f,transform.TransformDirection(Vector3.forward),out rayHit,6.0f, mask);

                if(hit){
                    carHit = rayHit.collider.transform.root.gameObject;

                    if(collision.gameObject == carHit){ // If the car can see the other car it is colliding with in front (i.e., it is the car running into the other car, and not the car being run into.)

                        int colliderAggressiveness=collision.gameObject.GetComponent<CarAI>().Aggressiveness; // Extract the aggressiveness of the car being hit.
                        if( Aggressiveness>1 && colliderAggressiveness<=Aggressiveness && newSpeed>0){ // If the aggressiveness of the car being hit is larger than 1, and the aggressiveness of the other car is smaller than this car. Only if car is actively going forward into the other car.
                            Aggressiveness--;
                        }
                        /*
                        else if(colliderAggressiveness>Aggressiveness){
                            Aggressiveness++;
                            }
                        */
                        }
                        

                    }
                }

                
        }

        private void OnCollisionEnter(Collision collision){
            if(collision.gameObject.tag == "Player"){
                colliding = true;
            }
            
        }

        private void OnCollisionExit(Collision collision){
            if(collision.gameObject.tag == "Player"){
                colliding = false;
            }
        }

    }
}
