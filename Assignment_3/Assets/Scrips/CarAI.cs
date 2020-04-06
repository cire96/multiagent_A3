using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public planner1 planner;
        public Graph mapGraph;

        int backingCounter;
        public int Aggressivness;
        public Color my_color;

        public GameObject carHit;



        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();

            // Plan your path here
            // Replace the code below that makes a random path
            // ...

            Vector3 start_pos = transform.position; // terrain_manager.myInfo.start_pos;
            goal_pos = my_goal_object.transform.position;
            planner = GameObject.Find("GraphObj").GetComponent<planner1>();
            ownPath = planner.plan(start_pos,goal_pos,my_color);
            mapGraph=GameObject.Find("GraphObj").GetComponent<TravGraph>().mapGraph;


            friends = GameObject.FindGameObjectsWithTag("Player");
            


            behaviorTree = new Root(
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

                    )//fix action to adjust closer to goal
                )

            );
            blackboard = behaviorTree.Blackboard;

            
#if UNITY_EDITOR
            Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
            debugger.BehaviorTree = behaviorTree;
#endif

            behaviorTree.Start();
            

            
        }

        void rePlan(){
            Debug.Log("replan");
        }

        void calculateBearing(){
            Debug.Log("calculateBearing");
        }

        void Avoid(){
            Debug.Log("Avoid");
        }

        void followPath(){
            //Debug.Log("followPath");
            Vector3 target = mapGraph.getNode(ownPath[targetIndex]).getPosition();

            if(6f>Vector3.Distance(transform.position,target) && targetIndex<ownPath.Count-1){
                targetIndex++;
                target = mapGraph.getNode(ownPath[targetIndex]).getPosition();
            }

            Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
            float newSteer = (carToTarget.x / carToTarget.magnitude);
            float newSpeed; 
            float handBrake = 0f;

            if(backingCounter>0){
                backingCounter--;
                newSteer = -1f*newSteer;
                newSpeed = -1;
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

            


            LayerMask wallMask = LayerMask.GetMask("Wall");
            LayerMask carMask = LayerMask.GetMask("Car");

            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            RaycastHit rayHit;
            bool hit = Physics.SphereCast(transform.position,2.3f,steeringPoint,out rayHit,6.0f, wallMask);
            bool hitTurn = Physics.SphereCast(transform.position,2.3f,steeringPoint,out rayHit,12.0f, wallMask);
            bool hitCar = Physics.SphereCast(transform.position,2.3f,steeringPoint,out rayHit,6.0f, carMask);
            if(hitCar){
                newSpeed = -1;
            }
            if(hit){
                backingCounter = 5;
            }else if(hitTurn){
                newSteer = newSteer*10;
            }

            

            if(10<m_Car.CurrentSpeed){
                newSpeed=0;
            }

            Debug.DrawLine(transform.position,target,Color.magenta);
            m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);






        }




        private void FixedUpdate(){

            blackboard["NotOnGoal"]=true;
            blackboard["Wall"]=false;
            blackboard["Car"]=false;

            if(10>Vector3.Distance(transform.position,goal_pos)){
                blackboard["NotOnGoal"]=false;
            }
            LayerMask mask = LayerMask.GetMask("Car");
            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            RaycastHit rayHit;
            bool hit = Physics.Raycast(transform.position,steeringPoint,out rayHit,20.0f, mask);
            if(hit){
                Debug.DrawRay(transform.position, steeringPoint * rayHit.distance, Color.yellow);
                carHit = rayHit.collider.transform.root.gameObject;
                blackboard["Car"]=true;
                Debug.Log("Hit car");
            }else{
                Debug.DrawRay(transform.position, steeringPoint * 20.0f, Color.yellow);
                blackboard["Car"]=false;
            }

            

        }
    }
}
