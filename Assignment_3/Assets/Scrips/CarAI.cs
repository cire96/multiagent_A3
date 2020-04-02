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

        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();

            // Plan your path here
            // Replace the code below that makes a random path
            // ...

            Vector3 start_pos = transform.position; // terrain_manager.myInfo.start_pos;
            goal_pos = terrain_manager.myInfo.goal_pos;

            friends = GameObject.FindGameObjectsWithTag("Player");
            //ownBlackBoard = new Blackboard();

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
                    new Action(()=>{
                        //behaviorTree.Stop()
                    })

                    

                )
            );
            blackboard = behaviorTree.Blackboard;

            blackboard["NotOnGoal"]=true;
            blackboard["Wall"]=false;
            blackboard["Car"]=false;
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
            Debug.Log("followPath");
        }




        private void FixedUpdate()
        {

            if(10>Vector3.Distance(transform.position,goal_pos)){
                blackboard["NotOnGoal"]=false;
            }
            LayerMask mask = LayerMask.GetMask("Default");
            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            RaycastHit rayHit;
            bool hit = Physics.Raycast(transform.position,steeringPoint,out rayHit,20.0f, mask);
            if(hit){
                Debug.DrawRay(transform.position, steeringPoint * rayHit.distance, Color.yellow);
                blackboard["Car"]=true;
            }else{
                Debug.DrawRay(transform.position, steeringPoint * 20.0f, Color.yellow);
                blackboard["Car"]=false;
            }

            // Execute your path here
            // ...

            // this is how you access information about the terrain
            int i = terrain_manager.myInfo.get_i_index(transform.position.x);
            int j = terrain_manager.myInfo.get_j_index(transform.position.z);
            float grid_center_x = terrain_manager.myInfo.get_x_pos(i);
            float grid_center_z = terrain_manager.myInfo.get_z_pos(j);

            //Debug.DrawLine(transform.position, new Vector3(grid_center_x, 0f, grid_center_z));


            Vector3 relVect = my_goal_object.transform.position - transform.position;
            bool is_in_front = Vector3.Dot(transform.forward, relVect) > 0f;
            bool is_to_right = Vector3.Dot(transform.right, relVect) > 0f;

            if(is_in_front && is_to_right)
                m_Car.Move(1f, 1f, 0f, 0f);
            if(is_in_front && !is_to_right)
                m_Car.Move(-1f, 1f, 0f, 0f);
            if(!is_in_front && is_to_right)
                m_Car.Move(-1f, -1f, -1f, 0f);
            if(!is_in_front && !is_to_right)
                m_Car.Move(1f, -1f, -1f, 0f);


            // this is how you control the car
            //m_Car.Move(1f, 1f, 1f, 0f);

        }
    }
}
