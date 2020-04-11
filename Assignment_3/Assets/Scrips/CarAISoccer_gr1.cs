using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using System;

using Action = NPBehave.Action;

//using static System.Func<bool>;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAISoccer_gr1 : MonoBehaviour
    {
        
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public string friend_tag;
        public GameObject[] enemies;
        public string enemy_tag;

        public GameObject own_goal;
        public GameObject other_goal;
        public Vector3 own_goalNormal;
        public GameObject ball;

        public Root behaviorTree;
        public Blackboard blackboard;
        public bool Shooter; 
        bool starting = true;
        GameObject activeShooter;
        GameObject Goalie;

        //Driving
        public int backingCounter=0;
        public float newSpeed;
        public float newSteer;
        public float handBrake;

        Vector3 gizTarget;
        Vector3 gizTarget2;



        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friend_tag = gameObject.tag;
            if (friend_tag == "Blue"){
                enemy_tag = "Red";
                own_goalNormal = new Vector3(1,0,0);
            }
            else{
                enemy_tag = "Blue";
                own_goalNormal = new Vector3(-1,0,0);
            }
            friends = GameObject.FindGameObjectsWithTag(friend_tag);
            enemies = GameObject.FindGameObjectsWithTag(enemy_tag);

            ball = GameObject.FindGameObjectWithTag("Ball");

            behaviorTree = new Root(
                new Selector(
                    new BlackboardCondition("OtherTeamAttack", Operator.IS_EQUAL, true, Stops.SELF,
                        new Selector(
                            new Condition(new Func<bool>(()=>CheckDefPos()),
                                new Sequence(
                                    new Action(()=>BearingBall()),
                                    new Action(()=>InterceptBall())
                                )
                            ), 
                            new Sequence(
                                new Action(()=>BearingCar()),
                                new Action(()=>InterceptEnemy())
                            )

                        )
                    ),

                    new Selector(
                        new Condition(new Func<bool>(()=>CheckShooter()),
                            new Sequence(
                                new Action(()=>BearingBallTraj()),
                                new Action(()=>AlignToBall())
                            )
                            
                        ),
                        new Action(()=>SupportRealign())
                    )
                    
                )
            );

            blackboard = behaviorTree.Blackboard;

            
#if UNITY_EDITOR
            Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
            debugger.BehaviorTree = behaviorTree;
#endif
            

            


            // Plan your path here
            // ...
        }

        //Def
        public bool CheckDefPos(){
            if(own_goal.transform.position.z < transform.position.z && transform.position.z < ball.transform.position.z){
                return true;
            }
            return false;
        }

        void BearingBall(){

        }

        void InterceptBall(){

        }

        void BearingCar(){

        }

        void InterceptEnemy(){

        }


        //Attack
        public bool CheckShooter(){
            float minLength=1000000f;
            float tempLength;
            GameObject minFriend=gameObject;
            foreach(GameObject friend in friends){
                tempLength=Vector3.Distance(ball.transform.position,friend.transform.position);
                if(minLength>tempLength){
                    minFriend=friend;
                    minLength=tempLength;
                }

            }
            activeShooter=minFriend;
            if(minFriend==gameObject){
                return true;
            }
            return false;
        }
        void BearingBallTraj(){}
        void AlignToBall(){
            Vector3 target = ball.transform.position;
            Vector3 GoaltoTarget=(target - other_goal.transform.position).normalized;
            Vector3 linePos=Vector3.Project(transform.position-target,GoaltoTarget);  
            if(30f>Vector3.Angle(transform.rotation*new Vector3(0,0,1),-GoaltoTarget) && 10>Vector3.Distance(transform.position,linePos)){
                Shoot(target);
            }else{Align(target,-1f,-1f);}

        }
        void Shoot(Vector3 target){
            print("Shoot");
            gizTarget=target;
            Drive(target,-1f);


        }
        
        void SupportRealign(){
            Vector3 target;
            if(gameObject==Goalie){
                target = own_goal.transform.position+new Vector3(30,0,0);
                float disToTarget = Vector3.Distance(transform.position,target);
                if(30>Vector3.Angle(transform.rotation*new Vector3(0,0,1),own_goalNormal) && 8>disToTarget){
                    Braking(0f);
                    
                }else{Align(target,30f,20f);}
                
            }else{
                target = own_goal.transform.position+new Vector3(70,0,0);
                Align(target,30f,20f);
            }
        }

        void Align(Vector3 target,float capSpeedFirst,float capSpeedSecond){
            Vector3 TargetToCar = transform.position-target; 
            Vector3 GoalToTarget = (target - other_goal.transform.position).normalized;  
            float disToTarget = Vector3.Distance(transform.position,target);
            Vector3 dynamicTarget;
            Debug.DrawRay(target,transform.rotation*new Vector3(0,0,1)*10f,Color.red);
            Debug.DrawRay(target,own_goalNormal*10f,Color.red);

            if(TargetToCar.x*own_goalNormal.x>0){
                Vector3 v=new Vector3(-Mathf.Sign(own_goalNormal.x),0,Mathf.Sign(TargetToCar.z)).normalized; 
                dynamicTarget = v*1.5f*disToTarget+target;
                Drive(dynamicTarget,capSpeedFirst);
                //Gizmos.DrawCube(dynamicTarget, new Vector3(1, 1, 1));
                
            }else{
                dynamicTarget=GoalToTarget*disToTarget*(0.1f*Mathf.Abs(target.z-transform.position.z))*0.35f+target;
                if(30>Vector3.Angle(transform.rotation*new Vector3(0,0,1),own_goalNormal) && 8>disToTarget){
                    Braking(0f);
                    return;
                }
                //Gizmos.DrawCube(dynamicTarget, new Vector3(1, 1, 1));
                Drive(dynamicTarget,capSpeedSecond);
                
            }
            //dynamicTarget=CapVtoField(target,dynamicTarget);
            
            
            gizTarget=dynamicTarget;
            gizTarget2=target;
            

        }

        float Braking(float newSteer){
            Vector3 vel = m_Car.GetComponent<Rigidbody>().velocity;
            handBrake=1;
            if(transform.InverseTransformDirection(vel).z>0.01){
                newSpeed=-1;
            }else if(transform.InverseTransformDirection(vel).z<-0.01){
                newSpeed=1;
            }else{
                newSpeed=0;
            }
            m_Car.Move(newSteer, newSpeed, newSpeed, handBrake); 
            return transform.InverseTransformDirection(vel).z; 
        }

        void Drive(Vector3 target,float speedCap){
            RaycastHit rayHit;
            LayerMask wallMask = LayerMask.GetMask("Wall");
            LayerMask carMask = LayerMask.GetMask("Car");

            Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
            newSteer = (carToTarget.x / carToTarget.magnitude);
            newSpeed=1;
            handBrake = 0f;
            if(backingCounter<=0 && m_Car.CurrentSpeed>10 && newSteer>0.6){
                handBrake = 1f;
            }

            /*if(5>Vector3.Distance(transform.position,target)){
                //Braking(newSteer);
                m_Car.Move(newSteer,0,0,handBrake);
                return;
            }*/




            if(backingCounter>0){
                backingCounter--;
                newSteer = -Mathf.Sign(newSteer);
                newSpeed = -1;
                print("backing");
                m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);
                return;
            }

            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            bool hit = Physics.SphereCast(transform.position,2.0f,steeringPoint,out rayHit,6.0f, wallMask);
            bool hitCar = Physics.SphereCast(transform.position,2.0f,steeringPoint,out rayHit,3.0f, carMask);
            if(hit){
                backingCounter = 7;
            }

            if(0<speedCap && m_Car.CurrentSpeed>speedCap){
                newSpeed=0;
            }
            

            m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);
            Debug.DrawLine(transform.position,target,Color.yellow);

        }

        Vector3 CapVtoField(Vector3 Start,Vector3 V){
            RaycastHit rayHit;
            LayerMask wallMask = LayerMask.GetMask("Wall");
            bool hit = Physics.Raycast(Start,V-Start,out rayHit,(V-Start).magnitude, wallMask);
            if(hit){
                V=V.normalized;
                V=V*rayHit.distance;
            }
            return V;
        }

        void OnDrawGizmos(){
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(gizTarget, 1);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(gizTarget2, 2);
        }







        private void FixedUpdate()
        {
            if(starting){
                blackboard["OtherTeamAttack"]=false;
                starting=false;
                behaviorTree.Start();
            
            }

            float tempMin=1000;
            GameObject tempGoalie=gameObject;
            foreach(GameObject friend in friends){
                if(friend!=activeShooter){
                    if(tempMin>friend.transform.position.x){
                        tempMin=friend.transform.position.x;
                        tempGoalie=friend;
                    }
                }
            }
            Goalie=tempGoalie;

            
            



        }
    }
}
